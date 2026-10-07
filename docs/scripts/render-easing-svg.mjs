// Renders the easing playground (EasingPlayground.astro) as an animated SVG, public/easing.svg, for the Godot Asset
// Store page. It plays a few curves in C# and GDScript, with the playground's controls set for each. Needs uv.
// Run from docs/: node scripts/render-easing-svg.mjs
import { FAMILIES, canonicalFamily, composeEase, legEase, splitLegEase } from '../src/scripts/motion.ts';
import { constant } from '../src/scripts/lang.ts';
import {
	FONTS, MARGIN, PILL, RANGE, fades, fontFace, frames, glyphs, highlight, langPill, languages, measure, mix, palette,
	pillStyle, r, range, reducedMotion, shadowFilter, steps, subsetFont, timeline, writeSvg,
} from './svg-kit.mjs';

// The curves it plays, with the playground's defaults where unset.
const CURVES = [
	{ lang: 'csharp', entry: 'None', exit: 'Elastic', duration: 1.5 },
	{ lang: 'csharp', entry: 'Back', exit: 'Bounce', duration: 1.5 },
	{ lang: 'gdscript', entry: 'SmootherStep', exit: 'SmootherStep', linked: true, duration: 1 },
	{ lang: 'gdscript', entry: 'Quint', exit: 'Elastic20', duration: 2, skew: 0.65, blend: 0.3, blendType: 'Hermite' },
].map((curve) => ({ skew: 0.5, blend: 0.1, blendType: 'Makima', linked: false, ...curve }));
const BLEND_TYPES = {
	Makima: 'match velocity', Hermite: 'match acceleration', SmoothStep: 'crossfade', Linear: 'crossfade',
};

// Seconds: controls move into each curve, it plays a moment later, then holds.
const MOVE = 0.45;
const PLAY_AT = 0.7;
const HOLD = 1.6;
const starts = [];
let CYCLE = 0;
for (const curve of CURVES) starts.push(CYCLE), (CYCLE += PLAY_AT + curve.duration + HOLD);
const tl = timeline(CYCLE, starts, MOVE);

// Layout in px, from the live widget at 1037 px wide; positions inside it are from its top-left corner.
const WIDGET = { x: MARGIN.x, y: MARGIN.top + PILL.height + 18, w: 1037, radius: 17.6 };
const GRAPH = { x: 21, y: 72.6, w: 566.4, h: 338 };
const LANE = { x: 157, w: 430.4, cy: 472.4, pad: 8 };
const SCRUB = { x: 157, w: 430.4, cy: 496.4 };
const PLAY = { x: 21, y: 457.4, w: 120, h: 48 };
const RECIPE = { x: 21, y: 525.4, w: 995, bar: 46.4, code: 588.7 };
const CODE = { x: 43.6, size: 12.48, line: 20.592 };

const curves = CURVES.map((curve) => {
	const { entry: a, exit: b, skew: k } = curve;
	const value = (p) => composeEase(a, b, p, k, curve.blendType, curve.blend);
	const matching = canonicalFamily(a) === canonicalFamily(b);
	// As the playground scales its graph and preview: the full overshoot of the result and both legs stays in view.
	let extent = 0.5, reach = 0.5;
	for (let i = 0; i <= 1000; i++) {
		const t = i / 1000;
		reach = Math.max(reach, Math.abs(value(t) - 0.5));
		if (a !== 'None' && (b === 'None' || t <= k))
			extent = Math.max(extent, Math.abs((b === 'None' ? legEase(a, 'In', t) : splitLegEase(a, t, k)) - 0.5));
		if (b !== 'None' && (a === 'None' || t >= k))
			extent = Math.max(extent, Math.abs((a === 'None' ? legEase(b, 'Out', t) : splitLegEase(b, t, k)) - 0.5));
	}
	const graphScale = Math.min(170, 119 / Math.max(extent, reach));
	const skewOff = a === 'None' || b === 'None';
	return {
		...curve, value, matching, skewOff,
		blendOff: skewOff || matching || k === 0 || k === 1,
		graphY: (v) => 145 - graphScale * (v - 0.5),
		laneX: (v) => 0.5 + (0.5 / reach) * (v - 0.5),
	};
});

const { use, of } = glyphs();

// The playground's recipe, with options at the library's defaults left out.
const recipe = (c) => {
	const gd = c.lang === 'gdscript';
	const name = (leg, family) => `${leg}.${gd ? constant(family) : family}`;
	const ease = c.matching && c.entry !== 'None' ? name('InOut', c.entry)
		: c.entry === 'None' ? name('Out', c.exit) : c.exit === 'None' ? name('In', c.entry)
		: `${name('In', c.entry)} | ${name('Out', c.exit)}`;
	const options = [
		!c.skewOff && c.skew !== 0.5 && ['Skew', c.skew.toFixed(2)],
		!c.blendOff && c.blendType !== 'Makima' && ['BlendType', `BlendType.${gd ? constant(c.blendType) : c.blendType}`],
		!c.blendOff && c.blend !== 0.1 && ['Blend', String(c.blend)],
	].filter(Boolean);
	return gd
		? ['var move := Tweens.position_2d_x()', `move.duration = ${c.duration.toFixed(1)}`, `move.ease = ${ease}`,
			...options.map(([key, v]) => `move.${constant(key).toLowerCase()} = ${v}`), '',
			'Tweens.play(sprite, move.with_to(300.0))']
		: ['var move = new Tweens.Position2DX', '{', `    Duration = ${c.duration.toFixed(1)},`, `    Ease = ${ease},`,
			...options.map(([key, v]) => `    ${key} = ${v},`), '};', 'sprite.Tween(move with { To = 300 });'];
};
const recipes = curves.map(recipe);
const tokens = await Promise.all(curves.map((c, k) => highlight(c.lang, recipes[k])));
const codeLines = tokens.map((lines) => lines.map((line, i) => {
	const runs = line.map((tk) => `<tspan fill="${tk.color}">${use('mono', tk.content)}</tspan>`).join('');
	return `<text class="code" x="${CODE.x}" y="${r(RECIPE.code + i * CODE.line + CODE.line / 2 + CODE.size * 0.36)}">${runs}</text>`;
}));
const recipeHeight = RECIPE.code - RECIPE.y + Math.max(...recipes.map((lines) => lines.length)) * CODE.line + 17;
WIDGET.h = r(RECIPE.y + recipeHeight + 21);
const width = WIDGET.x + WIDGET.w + MARGIN.x;
const height = Math.ceil(WIDGET.y + WIDGET.h + MARGIN.bottom);

const css = [];
const accent = (c) => languages[c.lang];
// Each curve's ease as CSS easing, so the tracer and the ball follow it between keyframes.
const easings = curves.map((c, k) =>
	`--curve-${k}: linear(${Array.from({ length: 161 }, (_, i) => Math.round(c.value(i / 160) * 1000) / 1000).join(', ')});`);
// Easing leaves the holds before and after a run as they are, so the run's easing can be the whole animation's.
const playing = (k, name, from, to, easing = 'linear') => {
	const s = starts[k] + PLAY_AT;
	css.push(frames(tl, name, [[s, `transform: ${from}`], [s + curves[k].duration, `transform: ${to}`]]),
		`.${name} { animation: ${name} ${CYCLE}s infinite ${easing}; }`);
};

// The graph, in the playground's own 496×296 units.
const r1 = (n) => Math.round(n * 10) / 10;
const path = (c, f, start = 0, end = 1) => Array.from({ length: 241 }, (_, i) => {
	const t = start + ((end - start) * i) / 240;
	return `${i ? 'L' : 'M'}${r1(32 + 432 * t)} ${r1(c.graphY(f(t)))}`;
}).join('');
const graph = (c, k) => {
	const { entry: a, exit: b, skew: s, graphY: y } = c;
	const times = Array.from({ length: Math.floor(c.duration / 0.25) + 1 }, (_, i) => `M${r(32 + (432 * i * 0.25) / c.duration)} 16V272`);
	const half = c.blend * Math.min(s, 1 - s);
	const zone = a === 'None' || b === 'None' || c.matching ? 0 : 432 * 2 * half;
	const entry = a === 'None' ? ''
		: path(c, (t) => (b === 'None' ? legEase(a, 'In', t) : splitLegEase(a, t, s)), 0, b === 'None' ? 1 : s);
	const exit = b === 'None' ? ''
		: path(c, (t) => (a === 'None' ? legEase(b, 'Out', t) : splitLegEase(b, t, s)), a === 'None' ? 0 : s);
	playing(k, `tracer-x-${k}`, 'translateX(0px)', 'translateX(432px)');
	playing(k, `tracer-y-${k}`, 'translateY(0px)', `translateY(${r(y(1) - y(0))}px)`, `var(--curve-${k})`);
	return `<rect x="${r(32 + 432 * (s - half))}" y="16" width="${r(zone)}" height="256" rx="8" fill="${palette.mint}" opacity="0.08"/>
<path class="grid" d="${[0, 0.5, 1].map((v) => `M32 ${r(y(v))}H464`).join('')}${times.join('')}"/>
<text class="tick" x="12" y="${r(y(1) + 4)}">${use('mono', '1')}</text><text class="tick" x="4" y="${r(y(0.5) + 4)}">${use('mono', '0.5')}</text><text class="tick" x="12" y="${r(y(0) + 4)}">${use('mono', '0')}</text>
<text class="tick" x="464" y="290" text-anchor="end">${use('mono', `${c.duration.toFixed(1)} s`)}</text>
<path class="result" d="${path(c, c.value)}"/>
${entry && `<path class="entry" d="${entry}"/>`}
${exit && `<path class="exit" d="${exit}"/>`}
<g class="tracer-x-${k}"><line class="now" x1="32" x2="32" y1="16" y2="272"/><g class="tracer-y-${k}"><circle class="tracer" cx="32" cy="${r(y(0))}" r="6"/></g></g>`;
};

// The motion preview: a rail between the start and end, ghosts every tenth of the way, and the ball.
const lanePx = (f) => LANE.x + LANE.pad + (LANE.w - 2 * LANE.pad) * f;
const lane = (c, k) => {
	const [from, to] = [lanePx(c.laneX(0)), lanePx(c.laneX(1))];
	const ghosts = Array.from({ length: 11 }, (_, i) => `<circle cx="${r(lanePx(c.laneX(c.value(i / 10))))}" cy="${LANE.cy}" r="4.05" fill="none" stroke="${palette.mint}" stroke-width="1.5" opacity="0.5"/>`);
	playing(k, `ball-${k}`, 'translateX(0px)', `translateX(${r(to - from)}px)`, `var(--curve-${k})`);
	return `<rect x="${r(from)}" y="${LANE.cy - 2}" width="${r(to - from)}" height="4" fill="url(#dots)"/>
<path d="M${r(from)} ${LANE.cy - 6}v12M${r(to)} ${LANE.cy - 6}v12" stroke="${palette.muted}" stroke-opacity="0.6" stroke-width="2"/>
${ghosts.join('')}
<g class="ball-${k}"><circle cx="${r(from)}" cy="${LANE.cy}" r="10" fill="${palette.mint}"/></g>`;
};

// Sliders fill in the language's accent, or gray while disabled.
const slider = (name, x, w, cy, fractions, offs = fractions.map(() => false)) => {
	const fills = curves.map((c, k) => (offs[k] ? RANGE.off : accent(c).accent));
	const input = range(tl, { name, x, w, cy, fractions, fills });
	css.push(...input.css);
	return input.svg;
};
// Fades a disabled control to half opacity.
const dimmed = (name, offs) => {
	css.push(steps(tl, `${name}-dim`, 'opacity', offs.map((off) => (off ? 0.5 : 1)), 'linear'),
		`.${name}-dim { animation: ${name}-dim ${CYCLE}s infinite; }`);
	return `class="${name}-dim" style="opacity: ${offs[0] ? 0.5 : 1}"`;
};
const swap = (name, values, fade) => {
	const swapped = fades(tl, name, values.map((v) => [v]), fade);
	css.push(...swapped.css);
	return swapped.svg;
};
const label = (x, y, text, color = palette.muted, anchor = 'start') =>
	`<text class="label" x="${x}" y="${y}" fill="${color}" text-anchor="${anchor}">${use('text', text)}</text>`;
const output = (x, y, labelText, values, name) => swap(name, values.map((v) =>
	`<text class="label" x="${x}" y="${y}" fill="${palette.muted}">${use('text', labelText)}<tspan class="output">${use('mono', v)}</tspan></text>`));
const select = (x, y, w, color, values, name, align = 'start') => `<rect x="${x + 0.5}" y="${y + 0.5}" width="${w - 1}" height="41.8" rx="8" fill="${palette.stage}" stroke="${palette.outline}"/>
<path d="M${r(x + w - 18)} ${r(y + 19)}l4 4 4-4" fill="none" stroke="${color}" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
${swap(name, values.map((v) => `<text class="select" x="${align === 'end' ? r(x + w - 24) : r(x + 12)}" y="${r(y + 21.4 + 13.6 * 0.36)}" fill="${color}" text-anchor="${align}">${use('mono', v)}</text>`))}`;

// Play turns to Stop while a curve plays.
const [playWidth, stopWidth] = [measure(FONTS.display, ['Play'])[0] * 16, measure(FONTS.text, ['Stop'])[0] * 16];
const playStarts = curves.flatMap((c, k) => [starts[k], starts[k] + PLAY_AT, starts[k] + PLAY_AT + c.duration]);
const button = (c, stop) => {
	const { accent: fill, accentInk: ink, onAccent } = accent(c);
	const left = PLAY.x + (PLAY.w - 22.4 - (stop ? stopWidth : playWidth)) / 2;
	const icon = stop ? `<rect x="${r(left + 3)}" y="${PLAY.y + 19}" width="10" height="10" rx="1.5" fill="${ink}"/>`
		: `<path d="M${r(left + 4)} ${PLAY.y + 18.5}v11l9.5-5.5z" fill="${onAccent}"/>`;
	return `${stop ? '' : `<rect x="${PLAY.x + 6}" y="${PLAY.y + 12}" width="${PLAY.w - 12}" height="${PLAY.h - 6}" rx="21" fill="${fill}" opacity="0.4" filter="url(#glow)"/>`}
<rect x="${PLAY.x + 0.5}" y="${PLAY.y + 0.5}" width="${PLAY.w - 1}" height="${PLAY.h - 1}" rx="23.5" fill="${stop ? 'none' : fill}" stroke="${stop ? ink : 'none'}"/>
${icon}<text class="${stop ? 'stop' : 'play'}" x="${r(left + 22.4)}" y="${r(PLAY.y + 24 + 16 * 0.36)}" fill="${stop ? ink : onAccent}">${use(stop ? 'text' : 'bold', stop ? 'Stop' : 'Play')}</text>`;
};
const looks = curves.flatMap((c) => [[button(c, false)], [button(c, true)], [button(c, false)]]);
const play = fades(timeline(CYCLE, playStarts, 0.12), 'play', looks, 0.12);
css.push(...play.css);

// The scrub follows time: back to the start as the next curve comes in, then across while it plays.
const scrubbing = range(tl, {
	name: 'scrub', ...SCRUB, fractions: [0], fills: curves.map((c) => accent(c).accent),
	moves: curves.flatMap((c, k) => {
		const s = starts[k] + PLAY_AT;
		return [...(k ? [[starts[k] - MOVE, 1, 'ease-in-out']] : []), [starts[k], 0], [s, 0, 'linear'], [s + c.duration, 1]];
	}).concat([[CYCLE - MOVE, 1, 'ease-in-out'], [CYCLE, 0]]),
});
css.push(...scrubbing.css);

// The Link toggle: pressed in the accent, and the bar between the curves turns into a chain.
css.push(steps(tl, 'link-fill', 'fill', curves.map((c) => (c.linked ? accent(c).accent : palette.raised)), 'linear'),
	steps(tl, 'link-text', 'fill', curves.map((c) => (c.linked ? accent(c).onAccent : palette.white)), 'linear'),
	`.link-button { animation: link-fill ${CYCLE}s infinite; }`, `.link-label { animation: link-text ${CYCLE}s infinite; }`);
const link = `<rect class="link-button" x="792.5" y="81.1" width="42.4" height="24" rx="12" fill="${curves[0].linked ? accent(curves[0]).accent : palette.raised}"/>
<text class="link-label" x="813.7" y="${r(93.1 + 13 * 0.36)}" fill="${curves[0].linked ? accent(curves[0]).onAccent : palette.white}">${use('bold', 'Link')}</text>
${swap('glyph', curves.map((c) => (c.linked
		? `<g fill="none" stroke="${accent(c).accentInk}" stroke-width="2"><rect x="809.7" y="112.4" width="8" height="12" rx="4"/><rect x="809.7" y="121.4" width="8" height="12" rx="4"/></g>`
		: `<text class="bar" x="813.7" y="${r(122.9 + 20 * 0.36)}">${use('mono', '|')}</text>`)), 0.2)}`;

const blendPosition = (blend) => Math.log(1 + 100 * blend) / Math.log(101);
const controls = `${label(611.4, 88.3, 'In curve', palette.amber)}${label(1016, 88.3, 'Out curve', palette.blueInk, 'end')}
${select(611.4, 99.5, 173.1, palette.amber, curves.map((c) => (c.entry === 'None' ? 'None (Out only)' : c.entry)), 'entry')}
${select(842.9, 99.5, 173.1, palette.blueInk, curves.map((c) => (c.exit === 'None' ? 'None (In only)' : c.exit)), 'exit', 'end')}
${link}
${output(611.4, 179, 'Duration ', curves.map((c) => `${c.duration.toFixed(1)} s`), 'duration-out')}
${slider('duration', 611.4, 404.6, 198.1, curves.map((c) => (c.duration - 0.2) / 3.8))}
<g ${dimmed('skew', curves.map((c) => c.skewOff))}>
${output(611.4, 242.8, 'Skew ', curves.map((c) => c.skew.toFixed(2)), 'skew-out')}
${slider('skew', 611.4, 194.3, 262, curves.map((c) => c.skew), curves.map((c) => c.skewOff))}
</g>
<g ${dimmed('blend', curves.map((c) => c.blendOff))}>
${output(821.7, 242.8, 'Blend\u00a0 ', curves.map((c) => `${Math.round(c.blend * 100)}%`), 'blend-out')}
${slider('blend', 821.7, 194.3, 262, curves.map((c) => blendPosition(c.blend)), curves.map((c) => c.blendOff))}
${label(611.4, 305.8, 'Blend type')}
${select(611.4, 316.9, 404.6, palette.text, curves.map((c) => `${c.blendType} · ${BLEND_TYPES[c.blendType]}`), 'blend-type')}
</g>`;

// Curves crossfade; text swaps fade out, then in.
const visual = fades(tl, 'curve', curves.map((c, k) => [`<g>${graph(c, k)}</g>`]), 0.4, true);
const preview = fades(tl, 'lane', curves.map((c, k) => [`<g>${lane(c, k)}</g>`]), 0.4, true);
const code = fades(tl, 'code', codeLines);
css.push(...visual.css, ...preview.css, ...code.css);

// The legend, placed as the live widget lays it out.
const legend = [
	[174.2, 'In curve', palette.amber], [236.9, 'Out curve', palette.blueInk], [308, 'Result', palette.mint],
	[359.3, 'Blend window', palette.muted],
].map(([x, text, color]) => `<text class="small" x="${x}" y="431.5" fill="${color}">${use('text', text)}</text>`);
const curveCount = new Set(FAMILIES.map(canonicalFamily)).size;
const codeBg = (c) => mix(accent(c).accent, 0.1, palette.stage);
css.push(steps(tl, 'eyebrow', 'fill', curves.map((c) => accent(c).accentInk), 'linear'),
	steps(tl, 'recipe-bg', 'fill', curves.map(codeBg), 'linear'),
	`.eyebrow { animation: eyebrow ${CYCLE}s infinite; }`, `.recipe-bg { animation: recipe-bg ${CYCLE}s infinite; }`);
const lang = langPill(tl, MARGIN.x, MARGIN.top, curves.map((c) => c.lang));
css.unshift(...lang.css);

const widget = `<g transform="translate(${WIDGET.x} ${WIDGET.y})">
<rect x="0.5" y="0.5" width="${WIDGET.w - 1}" height="${WIDGET.h - 1}" rx="${WIDGET.radius}" fill="${palette.stage}" stroke="${palette.outline}" filter="url(#shadow)"/>
<text class="eyebrow" x="21" y="41.1" fill="${accent(curves[0]).accentInk}">${use('mono', 'EASING PLAYGROUND')}</text>
<text class="badge" x="1016" y="41.1" text-anchor="end">${use('mono', `${curveCount} × ${curveCount} curves`)}</text>
<svg x="${GRAPH.x}" y="${GRAPH.y}" width="${GRAPH.w}" height="${GRAPH.h}" viewBox="0 0 496 296">
<text class="tick" x="32" y="290">${use('mono', '0 s')}</text>
${visual.svg}
</svg>
${legend.join('')}
${play.svg}
${preview.svg}
${scrubbing.svg}
${controls}
<rect class="recipe-bg" x="${RECIPE.x + 0.5}" y="${RECIPE.y + 0.5}" width="${RECIPE.w - 1}" height="${r(recipeHeight - 1)}" rx="14" fill="${codeBg(curves[0])}" stroke="${palette.outline}"/>
<path d="M${RECIPE.x + 1} ${RECIPE.y + RECIPE.bar}V${RECIPE.y + 14.4}a13.4 13.4 0 0 1 13.4-13.4H${RECIPE.x + RECIPE.w - 14.4}a13.4 13.4 0 0 1 13.4 13.4V${RECIPE.y + RECIPE.bar}Z" fill="${palette.surface}"/>
<path d="M${RECIPE.x + 1} ${RECIPE.y + RECIPE.bar - 0.5}H${RECIPE.x + RECIPE.w - 1}" stroke="${palette.outline}"/>
<text class="hint" x="38" y="553.2">${use('text', 'Source code recipe for this curve')}</text>
<rect x="951.4" y="532.8" width="55.6" height="31.6" rx="15.8" fill="${palette.raised}"/>
<text class="copy" x="979.2" y="${r(548.6 + 12 * 0.36)}">${use('bold', 'Copy')}</text>
${code.svg}
</g>`;

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<title>The tweens.gd easing playground playing curves, with their code in C# and GDScript</title>
<style>
${fontFace('JetBrains Mono', '100 800', subsetFont(FONTS.mono, of('mono') + lang.text))}
${fontFace('Lexie Readable', 400, subsetFont(FONTS.text, of('text')))}
${fontFace('Lexie Readable', 700, subsetFont(FONTS.display, of('bold')))}
svg { ${easings.join(' ')} }
text { white-space: pre; }
.code { font: 400 ${CODE.size}px 'JetBrains Mono', ui-monospace, monospace; }
.eyebrow { font: 600 12px 'JetBrains Mono', ui-monospace, monospace; letter-spacing: 1.56px; }
.badge { font: 400 12px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.muted}; }
.tick { font: 400 11px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.muted}; }
.select { font: 400 13.6px 'JetBrains Mono', ui-monospace, monospace; }
.output { font: 400 13px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.white}; }
.bar { font: 400 20px 'JetBrains Mono', ui-monospace, monospace; fill: ${palette.muted}; text-anchor: middle; }
.label, .small, .hint, .stop { font: 400 13px 'Lexie Readable', system-ui, sans-serif; }
.small { font-size: 12px; }
.hint { font-size: 12.8px; fill: ${palette.muted}; }
.stop { font-size: 16px; }
.play, .copy, .link-label { font: 700 16px 'Lexie Readable', system-ui, sans-serif; }
.copy, .link-label { text-anchor: middle; }
.copy { font-size: 12px; fill: ${palette.white}; }
.link-label { font-size: 13px; }
.grid { fill: none; stroke: ${mix(palette.outline, 0.55, palette.stage)}; stroke-dasharray: 3 5; }
.result, .entry, .exit { fill: none; stroke-linecap: round; stroke-linejoin: round; }
.result { stroke: ${mix(palette.mint, 0.6, '#000000')}; stroke-width: 12; }
.entry, .exit { stroke-width: 3; opacity: 0.8; }
.entry { stroke: ${palette.amber}; stroke-dasharray: 7 5; }
.exit { stroke: ${palette.blue}; stroke-dasharray: 2 4; }
.now { stroke: ${palette.muted}; stroke-dasharray: 2 4; opacity: 0.5; }
.tracer { fill: ${palette.stage}; stroke: ${palette.mint}; stroke-width: 3; }
${pillStyle}
${css.join('\n')}
${reducedMotion}
</style>
<defs>
${shadowFilter}
<filter id="glow" x="-50%" y="-100%" width="200%" height="300%"><feGaussianBlur stdDeviation="8"/></filter>
<pattern id="dots" y="${LANE.cy - 2}" width="7" height="4" patternUnits="userSpaceOnUse"><circle cx="3.5" cy="2" r="1.2" fill="${palette.outline}"/></pattern>
</defs>
${lang.svg}
${widget}
</svg>
`;

writeSvg('public/easing.svg', svg, width, height);
