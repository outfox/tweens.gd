// Renders the home page's comparison of Godot's Tween and tweens.gd as an animated SVG, public/compare.svg, for the
// Godot Asset Store page. It cycles C# and GDScript with one and three sprites using CSS animation only, since an SVG
// shown as an image runs no scripts and loads no web fonts; fonts are subset and embedded. Needs uv on the PATH.
// Run from docs/: node scripts/render-compare-svg.mjs
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { codeToTokens } from 'shiki';
import { csharpDark, gdscriptDark } from '../src/styles/code-themes.mjs';

const MONO = 'node_modules/@fontsource-variable/jetbrains-mono/files/jetbrains-mono-latin-wght-normal.woff2';
const DISPLAY = 'fonts/LexieReadable-Bold.woff2';

// Dark gallery palette and accents (theme.css).
const palette = { stage: '#111b27', surface: '#1a2635', outline: '#2d4057', text: '#eef4fa', muted: '#a9bccd' };
const languages = {
	csharp: { label: 'C#', theme: csharpDark, accent: '#67a2dd', onAccent: '#08131f' },
	gdscript: { label: 'GDScript', theme: gdscriptDark, accent: '#79deb4', onAccent: '#0b1a17' },
};
const variants = ['One sprite', 'Three sprites'];
const order = [['csharp', 0], ['csharp', 1], ['gdscript', 0], ['gdscript', 1]];

// Seconds each example shows, and how long switches and code take to change.
const SLOT = 4.5;
const MOVE = 0.45;
const FADE_IN = 0.6;
const FADE_OUT = 0.35;
const STAGGER = 0.12;
const CYCLE = SLOT * order.length;

// Layout in px, matching the site's code blocks: JetBrains Mono advances 0.6em.
const CODE = { size: 14, line: 22.4, advance: 14 * 0.6 };
const PILL = { size: 13, height: 34, advance: 13 * 0.6, pad: 15 };
const FRAME = { padX: 20, padY: 16, bar: 38, radius: 14.4, tabRadius: 8, title: 15, gap: 24 };
const MARGIN = { x: 24, top: 8, bottom: 32 };

// The code blocks of index.mdx, in page order: one sprite, then three, each with a C# pair and a GDScript pair.
const mdx = readFileSync('src/content/docs/index.mdx', 'utf8');
const blocks = { csharp: [], gdscript: [] };
for (const [, lang, title, code] of mdx.matchAll(/```(csharp|gdscript) title="([^"]+)"\r?\n([\s\S]*?)\r?\n```/g))
	blocks[lang].push({ title, lines: code.replace(/\t/g, '    ').split(/\r?\n/) });
if (blocks.csharp.length !== 4 || blocks.gdscript.length !== 4) throw new Error('Expected 4 C# and 4 GDScript blocks.');

const shikiTheme = ({ name, type, colors, settings }) => ({ name, type, colors, tokenColors: settings });
const highlight = async (lang, lines) =>
	(await codeToTokens(lines.join('\n'), { lang, theme: shikiTheme(languages[lang].theme) })).tokens;

const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const r = (n) => Math.round(n * 100) / 100;
const pct = (seconds) => `${r((seconds / CYCLE) * 100)}%`;

// Mixes like CSS color-mix(in oklab, a p, b).
const oklab = (hex) => {
	const [R, G, B] = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255).map((c) =>
		c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
	const [l, m, s] = [
		0.4122214708 * R + 0.5363325363 * G + 0.0514459929 * B,
		0.2119034982 * R + 0.6806995451 * G + 0.1073969566 * B,
		0.0883024619 * R + 0.2817188376 * G + 0.6299787005 * B,
	].map(Math.cbrt);
	return [
		0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
		1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
		0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s,
	];
};
const fromOklab = ([L, a, b]) => {
	const [l, m, s] = [L + 0.3963377774 * a + 0.2158037573 * b, L - 0.1055613458 * a - 0.0638541728 * b,
		L - 0.0894841775 * a - 1.291485548 * b].map((c) => c ** 3);
	return '#' + [
		4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
		-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
		-0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
	].map((c) => (c <= 0.0031308 ? 12.92 * c : 1.055 * c ** (1 / 2.4) - 0.055))
		.map((c) => Math.round(Math.min(1, Math.max(0, c)) * 255).toString(16).padStart(2, '0')).join('');
};
const mix = (a, p, b) => fromOklab(oklab(a).map((v, i) => v * p + oklab(b)[i] * (1 - p)));

// Subsets each font to the characters it draws, and measures the display font's titles.
const work = mkdtempSync(join(tmpdir(), 'compare-svg-'));
const titles = [...new Set(Object.values(blocks).flat().map((b) => b.title))];
const fontData = (file, text) => {
	writeFileSync(join(work, 'text.txt'), text);
	const out = join(work, 'subset.woff2');
	execFileSync('uvx', ['--from', 'fonttools[woff]', 'pyftsubset', file, `--text-file=${join(work, 'text.txt')}`,
		'--layout-features=', '--flavor=woff2', `--output-file=${out}`]);
	return readFileSync(out).toString('base64');
};
const measure = (file, texts) =>
	JSON.parse(execFileSync('uv', ['run', '--no-project', '--with', 'fonttools[woff]', 'python', '-c', `
import json, sys
from fontTools.ttLib import TTFont
f = TTFont(sys.argv[1]); cmap = f.getBestCmap(); hmtx = f['hmtx']; em = f['head'].unitsPerEm
print(json.dumps([sum(hmtx[cmap[ord(c)]][0] for c in t) / em for t in json.loads(sys.argv[2])]))`, file,
	JSON.stringify(texts)], { encoding: 'utf8' }));

const allCode = Object.values(blocks).flat().flatMap((b) => b.lines).join('');
const pillText = [...Object.values(languages).map((l) => l.label), ...variants].join('');
const monoFont = fontData(MONO, allCode + pillText);
const displayFont = fontData(DISPLAY, titles.join(''));
const titleWidths = Object.fromEntries(measure(DISPLAY, titles).map((w, i) => [titles[i], w * FRAME.title]));
rmSync(work, { recursive: true });

// Two equal columns, wide enough for the longest line of any example.
const longest = Math.max(...Object.values(blocks).flat().flatMap((b) => b.lines.map((l) => l.length)));
const column = Math.ceil(longest * CODE.advance + 2 * FRAME.padX);
const width = 2 * MARGIN.x + 2 * column + FRAME.gap;
const frameTop = MARGIN.top + PILL.height + 18;
const frameHeight = (lines) => FRAME.bar + 2 * FRAME.padY + lines * CODE.line;
const tallest = Math.max(...Object.values(blocks).flat().map((b) => frameHeight(b.lines.length)));
const height = Math.ceil(frameTop + tallest + MARGIN.bottom);

// One frame: a tab bar with the title's tab, then the highlighted code.
const frame = (x, block, tokens, lang) => {
	const { radius: R, tabRadius: t, bar } = FRAME;
	const y = frameTop;
	const w = column;
	const h = frameHeight(block.lines.length);
	const bg = mix(languages[lang].accent, 0.1, palette.stage);
	const tab = r(titleWidths[block.title] + 32);
	const lines = tokens.map((line, i) => {
		const baseline = r(y + bar + FRAME.padY + i * CODE.line + CODE.line / 2 + CODE.size * 0.36);
		const runs = line.map((tk) => `<tspan fill="${tk.color}">${esc(tk.content)}</tspan>`).join('');
		return `<text class="code" x="${x + FRAME.padX}" y="${baseline}">${runs}</text>`;
	});
	return `<g>
<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${R}" fill="${bg}" filter="url(#shadow)"/>
<path d="M${x} ${y + bar}V${y + R}a${R} ${R} 0 0 1 ${R}-${R}H${x + w - R}a${R} ${R} 0 0 1 ${R} ${R}V${y + bar}Z" fill="${palette.surface}"/>
<path d="M${x} ${y + bar}V${y + R}a${R} ${R} 0 0 1 ${R}-${R}H${x + tab - t}a${t} ${t} 0 0 1 ${t} ${t}V${y + bar}Z" fill="${bg}"/>
<rect x="${x + 0.5}" y="${y + 0.5}" width="${w - 1}" height="${h - 1}" rx="${R - 0.5}" fill="none" stroke="${palette.outline}"/>
<text class="title" x="${x + 16}" y="${r(y + bar / 2 + FRAME.title * 0.36)}">${esc(block.title)}</text>
${lines.join('\n')}
</g>`;
};

// Keyframes for a value that changes once per example: it holds, then eases to the next one just before it shows.
const steps = (name, property, values, easing) => {
	const at = (k) => `${property}: ${values[k]}`;
	const frames = [`0% { ${at(0)}; }`];
	for (let k = 1; k <= order.length; k++)
		frames.push(`${pct(k * SLOT - MOVE)} { ${at(k - 1)}; animation-timing-function: ${easing}; }`,
			`${k === order.length ? '100%' : pct(k * SLOT)} { ${at(k % order.length)}; }`);
	return `@keyframes ${name} { ${frames.join(' ')} }`;
};

// A switch like the site's: equal cells and an indicator that slides to the chosen one.
const pillCell = (labels) => Math.ceil(Math.max(...labels.map((l) => l.length)) * PILL.advance + 2 * PILL.pad);
const css = [];
const pill = (x, labels, name, pick) => {
	const cell = pillCell(labels);
	const w = labels.length * cell + 8;
	const y = MARGIN.top;
	const picks = order.map(pick);
	const accents = order.map(([lang]) => languages[lang]);
	css.push(steps(`${name}-move`, 'transform', picks.map((i) => `translateX(${i * cell}px)`), 'var(--back-out)'),
		steps(`${name}-fill`, 'fill', accents.map((l) => l.accent), 'linear'),
		...labels.map((_, j) => steps(`${name}-label-${j}`, 'fill',
			picks.map((i, k) => (i === j ? accents[k].onAccent : palette.muted)), 'linear')),
		`.${name}-indicator { animation: ${name}-move ${CYCLE}s infinite, ${name}-fill ${CYCLE}s infinite; }`,
		...labels.map((_, j) => `.${name}-label-${j} { animation: ${name}-label-${j} ${CYCLE}s infinite; }`));
	const first = picks[0];
	return {
		width: w,
		svg: `<g>
<rect x="${x + 0.5}" y="${y + 0.5}" width="${w - 1}" height="${PILL.height - 1}" rx="${PILL.height / 2}" fill="${palette.stage}" stroke="${palette.outline}"/>
<rect class="${name}-indicator" x="${x + 4}" y="${y + 4}" width="${cell}" height="${PILL.height - 8}" rx="${(PILL.height - 8) / 2}" fill="${accents[0].accent}" style="transform: translateX(${first * cell}px)"/>
${labels.map((l, j) => `<text class="pill ${name}-label-${j}" x="${r(x + 4 + j * cell + cell / 2)}" y="${r(y + PILL.height / 2 + PILL.size * 0.36)}" fill="${j === first ? accents[0].onAccent : palette.muted}">${esc(l)}</text>`).join('\n')}
</g>`,
	};
};

const langPill = pill(MARGIN.x, Object.values(languages).map((l) => l.label), 'lang',
	([lang]) => Object.keys(languages).indexOf(lang));
const variantPill = pill(MARGIN.x + langPill.width + 12, variants, 'variant', ([, v]) => v);

// Each example fades and rises in, holds, then fades out upward while the switches move, before the next one arrives.
css.push(`@keyframes show {
	0% { opacity: 0; transform: translateY(14px); animation-timing-function: var(--expo-out); }
	${pct(FADE_IN)} { opacity: 1; transform: none; }
	${pct(SLOT - MOVE)} { opacity: 1; transform: none; animation-timing-function: ease-in; }
	${pct(SLOT - MOVE + FADE_OUT)} { opacity: 0; transform: translateY(-10px); }
	100% { opacity: 0; transform: translateY(-10px); }
}`);

const examples = [];
for (const [k, [lang, variant]] of order.entries()) {
	const [left, right] = blocks[lang].slice(variant * 2, variant * 2 + 2);
	const [leftTokens, rightTokens] = [await highlight(lang, left.lines), await highlight(lang, right.lines)];
	for (const [i, block, tokens] of [[0, left, leftTokens], [1, right, rightTokens]]) {
		const delay = r(k * SLOT + i * STAGGER);
		examples.push(`<g class="example" style="opacity: ${k === 0 ? 1 : 0}; animation-delay: ${delay}s">
${frame(MARGIN.x + i * (column + FRAME.gap), block, tokens, lang)}
</g>`);
	}
}

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<title>Godot's Tween next to tweens.gd, in C# and GDScript</title>
<style>
@font-face { font-family: 'JetBrains Mono'; font-weight: 100 800; src: url(data:font/woff2;base64,${monoFont}) format('woff2'); }
@font-face { font-family: 'Lexie Readable'; font-weight: 700; src: url(data:font/woff2;base64,${displayFont}) format('woff2'); }
svg {
	--back-out: linear(0, 0.185, 0.349, 0.493, 0.618, 0.726, 0.817, 0.894, 0.956, 1.005, 1.043, 1.07, 1.088, 1.097, 1.1, 1.097, 1.089, 1.078, 1.064, 1.049, 1.035, 1.021, 1.01, 1.003, 1);
	--expo-out: cubic-bezier(0.16, 1, 0.3, 1);
}
text { white-space: pre; }
.code { font: 400 ${CODE.size}px 'JetBrains Mono', ui-monospace, monospace; }
.title { font: 700 ${FRAME.title}px 'Lexie Readable', system-ui, sans-serif; fill: ${palette.text}; }
.pill { font: 650 ${PILL.size}px 'JetBrains Mono', ui-monospace, monospace; text-anchor: middle; }
.example { animation: show ${CYCLE}s infinite both; }
${css.join('\n')}
@media (prefers-reduced-motion: reduce) { * { animation: none !important; } }
</style>
<defs>
<filter id="shadow" x="-10%" y="-10%" width="120%" height="140%">
<feDropShadow dx="0" dy="8" stdDeviation="10" flood-color="#03080e" flood-opacity="0.5"/>
</filter>
</defs>
${langPill.svg}
${variantPill.svg}
${examples.join('\n')}
</svg>
`;

writeFileSync('public/compare.svg', svg);
console.log(`public/compare.svg: ${width}x${height}, ${(svg.length / 1024).toFixed(1)} KiB`);
