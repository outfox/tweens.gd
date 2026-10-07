// Shared by the scripts that render docs demos as animated SVGs for the Godot Asset Store page. An SVG shown as an
// image runs no scripts and loads no web fonts, so they animate with CSS only and embed fonts subset to what they draw.
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { codeToTokens } from 'shiki';
import { csharpDark, gdscriptDark } from '../src/styles/code-themes.mjs';

export const FONTS = {
	mono: 'node_modules/@fontsource-variable/jetbrains-mono/files/jetbrains-mono-latin-wght-normal.woff2',
	text: 'fonts/LexieReadable-Regular.woff2',
	display: 'fonts/LexieReadable-Bold.woff2',
};

// Dark gallery palette and accents (theme.css).
export const palette = {
	bg: '#0e1620', stage: '#111b27', surface: '#1a2635', raised: '#22334a', outline: '#2d4057', text: '#eef4fa',
	white: '#f6f9fc', soft: '#c9d6e2', muted: '#a9bccd', mint: '#79deb4', amber: '#f2bc74', blue: '#8caaff',
	blueInk: '#a9bfff',
};
export const languages = {
	csharp: { label: 'C#', theme: csharpDark, accent: '#67a2dd', accentInk: '#8fbdea', onAccent: '#08131f' },
	gdscript: { label: 'GDScript', theme: gdscriptDark, accent: '#79deb4', accentInk: '#79deb4', onAccent: '#0b1a17' },
};

const shikiTheme = ({ name, type, colors, settings }) => ({ name, type, colors, tokenColors: settings });
/** Lines of [{ content, color }] tokens, colored like the site's code blocks. */
export const highlight = async (lang, lines) =>
	(await codeToTokens(lines.join('\n'), { lang, theme: shikiTheme(languages[lang].theme) })).tokens;

export const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
export const r = (n) => Math.round(n * 100) / 100;

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
export const mix = (a, p, b) => fromOklab(oklab(a).map((v, i) => v * p + oklab(b)[i] * (1 - p)));

// Font tools run through uv, which must be on the PATH.
const fontTool = (run) => {
	const work = mkdtempSync(join(tmpdir(), 'docs-svg-'));
	try {
		return run(work);
	} finally {
		rmSync(work, { recursive: true });
	}
};

/** `file` subset to the characters of `text`, as base64 WOFF2. Dropping layout features also drops ligatures. */
export const subsetFont = (file, text) => fontTool((work) => {
	writeFileSync(join(work, 'text.txt'), text);
	const out = join(work, 'subset.woff2');
	execFileSync('uvx', ['--from', 'fonttools[woff]', 'pyftsubset', file, `--text-file=${join(work, 'text.txt')}`,
		'--layout-features=', '--flavor=woff2', `--output-file=${out}`]);
	return readFileSync(out).toString('base64');
});

/** Each text's advance width in em, without kerning. */
export const measure = (file, texts) =>
	JSON.parse(execFileSync('uv', ['run', '--no-project', '--with', 'fonttools[woff]', 'python', '-c', `
import json, sys
from fontTools.ttLib import TTFont
f = TTFont(sys.argv[1]); cmap = f.getBestCmap(); hmtx = f['hmtx']; em = f['head'].unitsPerEm
print(json.dumps([sum(hmtx[cmap[ord(c)]][0] for c in t) / em for t in json.loads(sys.argv[2])]))`, file,
	JSON.stringify(texts)], { encoding: 'utf8' }));

/**
 * A looping timeline of states: state k is fully in at `starts[k]` seconds, after values move into it over the `move`
 * seconds before. The last state moves back into the first as the `cycle` ends.
 */
export const timeline = (cycle, starts, move) => ({
	cycle, starts, move,
	pct: (seconds) => `${r((seconds / cycle) * 100)}%`,
	// Where state k ends: the next state's start, or the cycle's end.
	end: (k) => (k + 1 < starts.length ? starts[k + 1] : cycle),
});

/** Keyframes for a value that holds through each state, then eases to the next state's value just before it. */
export const steps = (tl, name, property, values, easing) => {
	const n = tl.starts.length;
	const at = (k) => `${property}: ${values[k]}`;
	const frames = [`0% { ${at(0)}; }`];
	for (let k = 1; k <= n; k++)
		frames.push(`${tl.pct(tl.end(k - 1) - tl.move)} { ${at(k - 1)}; animation-timing-function: ${easing}; }`,
			`${k === n ? '100%' : tl.pct(tl.end(k - 1))} { ${at(k % n)}; }`);
	return `@keyframes ${name} { ${frames.join(' ')} }`;
};

/** Keyframes from [seconds, declarations, easing onward] points, held before the first point and after the last. */
export const frames = (tl, name, points) => {
	const at = (t) => (t <= 0 ? '0%' : t >= tl.cycle ? '100%' : tl.pct(t));
	const out = points.map(([t, declarations, easing]) =>
		`${at(t)} { ${declarations};${easing ? ` animation-timing-function: ${easing};` : ''} }`);
	if (points[0][0] > 0) out.unshift(`0% { ${points[0][1]}; }`);
	if (points.at(-1)[0] < tl.cycle) out.push(`100% { ${points.at(-1)[1]}; }`);
	return `@keyframes ${name} { ${out.join(' ')} }`;
};

/**
 * Markup that differs between states, as one element per distinct markup, shown in the states that list it. In the
 * `fade` seconds before a state that swaps markup, the old fades out, then the new fades in, or both at once with
 * `cross`. Markup that every state lists stays still.
 */
export const fades = (tl, name, perState, fade = 0.3, cross = false) => {
	const css = [];
	const svg = [...new Set(perState.flat())].map((markup, i) => {
		const on = perState.map((items) => (items.includes(markup) ? 1 : 0));
		if (on.every(Boolean)) return markup;
		const points = on.flatMap((to, k) => {
			const from = on[(k + on.length - 1) % on.length];
			const t = k ? tl.starts[k] : tl.cycle;
			const [a, b] = cross ? [t - fade, t] : to ? [t - fade / 2, t] : [t - fade, t - fade / 2];
			return from === to ? [] : [[a, `opacity: ${from}`, 'ease'], [b, `opacity: ${to}`]];
		}).sort(([a], [b]) => a - b);
		css.push(frames(tl, `${name}-${i}`, points), `.${name}-${i} { animation: ${name}-${i} ${tl.cycle}s infinite; }`);
		return `<g class="${name}-${i}" style="opacity: ${on[0]}">${markup}</g>`;
	});
	return { svg: svg.join('\n'), css };
};

// Chrome's dark range input: a gray track, and a disabled control's gray fill (at the half opacity of its field).
export const RANGE = { track: '#3b3b3b', border: '#858585', off: '#757575' };

/**
 * A range input with the track filled up to the thumb. Each state sets its fraction and fill color, and RANGE.off
 * grays the track as well. `moves`, as [seconds, fraction, easing] points, replaces the thumb's steps between states.
 */
export const range = (tl, { name, x, w, cy, fractions, fills, moves }) => {
	const travel = (f) => `translateX(${r(8 + (w - 16) * f)}px)`;
	const tracks = fills.map((fill) => (fill === RANGE.off ? RANGE.off : RANGE.track));
	const varies = (values) => values.some((v) => v !== values[0]);
	const css = [moves
		? frames(tl, `${name}-move`, moves.map(([t, f, easing]) => [t, `transform: ${travel(f)}`, easing]))
		: steps(tl, `${name}-move`, 'transform', fractions.map(travel), EASE.backOut)];
	const parts = [`${name}-move ${tl.cycle}s infinite`];
	if (varies(fills)) css.push(steps(tl, `${name}-fill`, 'fill', fills, 'linear')), parts.push(`${name}-fill ${tl.cycle}s infinite`);
	css.push(`.${name}-thumb, .${name}-bar { animation: ${parts.join(', ')}; }`);
	if (varies(tracks))
		css.push(steps(tl, `${name}-track`, 'fill', tracks, 'linear'), `.${name}-track { animation: ${name}-track ${tl.cycle}s infinite; }`);
	return {
		css,
		svg: `<clipPath id="${name}-clip"><rect x="${x}" y="${cy - 4}" width="${w}" height="8" rx="4"/></clipPath>
<rect class="${name}-track" x="${x + 0.5}" y="${cy - 3.5}" width="${w - 1}" height="7" rx="3.5" fill="${tracks[0]}" stroke="${RANGE.border}"/>
<g clip-path="url(#${name}-clip)"><rect class="${name}-bar" x="${r(x - w)}" y="${cy - 4}" width="${w}" height="8" fill="${fills[0]}" style="transform: ${travel(fractions[0])}"/></g>
<circle class="${name}-thumb" cx="${x}" cy="${cy}" r="8" fill="${fills[0]}" style="transform: ${travel(fractions[0])}"/>`,
	};
};

// The site's switches: monospace labels in equal cells.
export const PILL = { size: 13, height: 34, advance: 13 * 0.6, pad: 15 };
export const pillStyle = `.pill { font: 650 ${PILL.size}px 'JetBrains Mono', ui-monospace, monospace; text-anchor: middle; }`;

/** A switch whose indicator slides to each state's pick, in that state's accent. Returns its CSS and labels apart. */
export const pill = (tl, { x, y, name, labels, picks, accents }) => {
	const cell = Math.ceil(Math.max(...labels.map((l) => l.length)) * PILL.advance + 2 * PILL.pad);
	const w = labels.length * cell + 8;
	const css = [
		steps(tl, `${name}-move`, 'transform', picks.map((i) => `translateX(${i * cell}px)`), EASE.backOut),
		steps(tl, `${name}-fill`, 'fill', accents.map((l) => l.accent), 'linear'),
		...labels.map((_, j) => steps(tl, `${name}-label-${j}`, 'fill',
			picks.map((i, k) => (i === j ? accents[k].onAccent : palette.muted)), 'linear')),
		`.${name}-indicator { animation: ${name}-move ${tl.cycle}s infinite, ${name}-fill ${tl.cycle}s infinite; }`,
		...labels.map((_, j) => `.${name}-label-${j} { animation: ${name}-label-${j} ${tl.cycle}s infinite; }`),
	];
	const first = picks[0];
	return {
		width: w,
		css,
		text: labels.join(''),
		svg: `<g>
<rect x="${x + 0.5}" y="${y + 0.5}" width="${w - 1}" height="${PILL.height - 1}" rx="${PILL.height / 2}" fill="${palette.stage}" stroke="${palette.outline}"/>
<rect class="${name}-indicator" x="${x + 4}" y="${y + 4}" width="${cell}" height="${PILL.height - 8}" rx="${(PILL.height - 8) / 2}" fill="${accents[0].accent}" style="transform: translateX(${first * cell}px)"/>
${labels.map((l, j) => `<text class="pill ${name}-label-${j}" x="${r(x + 4 + j * cell + cell / 2)}" y="${r(y + PILL.height / 2 + PILL.size * 0.36)}" fill="${j === first ? accents[0].onAccent : palette.muted}">${esc(l)}</text>`).join('\n')}
</g>`,
	};
};

/** The language switch: its labels, and each state's pick and accent from its language. */
export const langPill = (tl, x, y, langs) => pill(tl, {
	x, y, name: 'lang', labels: Object.values(languages).map((l) => l.label),
	picks: langs.map((lang) => Object.keys(languages).indexOf(lang)), accents: langs.map((lang) => languages[lang]),
});

/** Collects the text each font draws, to subset it to. `use` returns the text escaped for markup. */
export const glyphs = () => {
	const used = {};
	return { use: (font, text) => ((used[font] ??= []).push(text), esc(text)), of: (font) => (used[font] ?? []).join('') };
};

export const fontFace = (family, weight, data) =>
	`@font-face { font-family: '${family}'; font-weight: ${weight}; src: url(data:font/woff2;base64,${data}) format('woff2'); }`;

// Easings from theme.css, written out: Chrome and Firefox ignore var() in a keyframe's timing function.
const theme = readFileSync('src/styles/theme.css', 'utf8');
const themeEase = (name) => theme.match(new RegExp(`--ease-${name}: ([^;]+);`))[1];
export const EASE = { backOut: themeEase('back-out'), expoOut: themeEase('expo-out') };

export const reducedMotion = '@media (prefers-reduced-motion: reduce) { * { animation: none !important; } }';

// Space around the content, with the switches along the top margin; the shadow of the frames below spills downward.
export const MARGIN = { x: 24, top: 8, bottom: 32 };
export const shadowFilter = `<filter id="shadow" x="-10%" y="-10%" width="120%" height="140%">
<feDropShadow dx="0" dy="8" stdDeviation="10" flood-color="#03080e" flood-opacity="0.5"/>
</filter>`;

export const writeSvg = (path, svg, width, height) => {
	writeFileSync(path, svg);
	console.log(`${path}: ${width}x${height}, ${(svg.length / 1024).toFixed(1)} KiB`);
};
