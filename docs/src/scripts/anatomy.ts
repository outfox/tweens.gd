// Every definition field for the Definitions page: its name per language, default, one line, and a schematic.
// Schematics are SVG drawn at build time; curves come from motion.ts, the docs' port of the library's easing.
import { composeEase } from './motion';
import { constant, snake, type Lang } from './lang';

export type Group = 'endpoints' | 'timing' | 'easing' | 'callbacks' | 'variations' | 'target';

/** A field's name: C# PascalCase, GDScript snake_case with `_value` on the endpoints. */
export const fieldName = (lang: Lang, name: string) =>
	lang === 'csharp' ? name : ({ From: 'from_value', To: 'to_value', By: 'by_value' } as Record<string, string>)[name] ?? snake(name);

// Drawing area and helpers. Classes: s* strokes, f* fills (k outline, m muted, a accent), t/ta labels.
const W = 240;
const H = 72;
const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const n2 = (v: number) => +v.toFixed(2);

const line = (x1: number, y1: number, x2: number, y2: number, c = 'sk') =>
	`<line class="${c}" x1="${n2(x1)}" y1="${n2(y1)}" x2="${n2(x2)}" y2="${n2(y2)}"/>`;
const text = (x: number, y: number, s: string, c = 't', anchor = 'middle') =>
	`<text class="${c}" x="${n2(x)}" y="${n2(y)}" text-anchor="${anchor}">${esc(s)}</text>`;
const dot = (x: number, y: number, c = 'fa', r = 4) => `<circle class="${c}" cx="${n2(x)}" cy="${n2(y)}" r="${r}"/>`;
const ring = (x: number, y: number, c = 'sm') => `<circle class="${c} ring" cx="${n2(x)}" cy="${n2(y)}" r="4"/>`;
const bar = (x1: number, x2: number, y: number, c = 'fa') =>
	`<rect class="${c}" x="${n2(x1)}" y="${y - 4}" width="${n2(x2 - x1)}" height="8" rx="4"/>`;
const wait = (x1: number, x2: number, y: number, c = 'sm') => line(x1, y, x2, y, `${c} dash`);
const path = (d: string, c = 'sa') => `<path class="${c}" d="${d}"/>`;
/** An arrowhead with its tip at (x, y), pointing along `angle` degrees. */
const tip = (x: number, y: number, angle: number, c = 'fa') =>
	`<path class="${c}" d="M0 0l-6 -3.5v7z" transform="translate(${n2(x)} ${n2(y)}) rotate(${n2(angle)})"/>`;
const arrow = (x1: number, x2: number, y: number, s = 'sa', f = 'fa') =>
	line(x1, y, x2 - 4 * Math.sign(x2 - x1), y, s) + tip(x2, y, x2 > x1 ? 0 : 180, f);
/** A motion arc over a value lane, from x1 to x2. */
const hop = (x1: number, x2: number, y: number, s = 'sa', f = 'fa', lift = 15) => {
	const mx = (x1 + x2) / 2;
	const top = y - 2 * lift;
	return path(`M${x1} ${y}Q${mx} ${top} ${x2 - 2} ${y - 3}`, `${s} none`) + tip(x2, y, (Math.atan2(y - top, x2 - mx) * 180) / Math.PI, f);
};
const bracket = (x1: number, x2: number, y: number, s: string, c = 'sm', tc = 't') =>
	line(x1, y, x2, y, c) + line(x1, y - 3, x1, y + 3, c) + line(x2, y - 3, x2, y + 3, c) + text((x1 + x2) / 2, y + 11, s, tc);
/** A callback firing at x on the timeline at y, named above. */
const event = (x: number, y: number, s: string, anchor = 'middle') =>
	line(x, y - 16, x, y - 6, 'sa') + dot(x, y, 'fa', 3.5) + text(x, y - 20, s, 'ta', anchor);
const cross = (x: number, y: number, c = 'sa') => line(x - 4, y - 4, x + 4, y + 4, c) + line(x - 4, y + 4, x + 4, y - 4, c);
const box = (x: number, y: number, w: number, h: number, c = 'sm') =>
	`<rect class="${c} none" x="${x}" y="${y}" width="${w}" height="${h}" rx="5"/>`;

// Value plots: progress runs left to right, weight bottom (0) to top (1).
const PY0 = 58;
const PY1 = 14;
const py = (v: number) => PY0 + v * (PY1 - PY0);
const curve = (f: (t: number) => number, c = 'sa none', x0 = 20, x1 = 220, samples = 120) => {
	let d = '';
	for (let i = 0; i <= samples; i++) d += `${i ? 'L' : 'M'}${n2(x0 + (i / samples) * (x1 - x0))} ${n2(py(f(i / samples)))}`;
	return path(d, c);
};
const guides = (x0 = 20, x1 = 220) => line(x0, py(0), x1, py(0), 'sk dash') + line(x0, py(1), x1, py(1), 'sk dash');
const lane = (y = 44, x1 = 16, x2 = 224) => line(x1, y, x2, y, 'sk');

type Draw = (n: (name: string) => string, lang: Lang) => string;
interface Field {
	name: string;
	group: Group;
	/** Reference page under api/, and the C# name of its row when several fields share one. */
	page: string;
	row?: string;
	only?: Lang;
	defaults?: [csharp: string, gdscript: string];
	/** `code` renders as code; {Name} as that field's name in the reader's language. */
	text: string | Record<Lang, string>;
	draw: Draw;
}

const ease = (lang: Lang, entry: string, exit: string) =>
	lang === 'csharp' ? `In.${entry} | Out.${exit}` : `In.${constant(entry)} | Out.${constant(exit)}`;

// Factor and delta schematics on a value lane whose origin is marked, so scaling reads from zero.
const scaled = (n: (s: string) => string, of: string, base: number, label: string, next: number) =>
	lane() + line(20, 40, 20, 48, 'sk') + text(20, 62, '0') + ring(base, 44) + text(base, 62, n(of)) +
	arrow(base + 6, next - 6, 30, 'sa', 'fa') + text((base + next) / 2, 22, label, 'ta') + dot(next, 44);
const offset = (n: (s: string) => string, label: string, extra: number) =>
	lane() + dot(40, 44, 'fm') + text(40, 62, 'current') + arrow(40, 110, 30, 'sm', 'fm') + text(75, 22, n('By')) +
	(extra ? arrow(110, 110 + extra, 30) + text(110 + extra / 2, 22, label, 'ta') : '') + dot(110 + extra, 44);
const lengths = (label: string, n: (s: string) => string, grow: boolean) =>
	bar(20, 110, 24, 'fm') + text(116, 27, n('Duration'), 't', 'start') +
	(grow ? bar(20, 150, 50) : bar(20, 110, 50, 'fm') + bar(106, 150, 50)) + text(156, 53, label, 'ta', 'start');
const callbacks = (inner: string) => wait(20, 60, 44) + bar(60, 210, 44, 'fm') + inner;

const FIELDS: Field[] = [
	// Endpoints
	{
		name: 'From', group: 'endpoints', page: 'endpoints', defaults: ['null', 'null'],
		text: 'Start value. Leave it out to read the property’s current value as the tween starts.',
		draw: (n) => lane() + ring(36, 44) + text(36, 62, 'current') + dot(96, 44) + text(96, 62, n('From'), 'ta') +
			hop(96, 196, 40, 'sm', 'fm') + ring(204, 44) + text(204, 62, n('To')),
	},
	{
		name: 'To', group: 'endpoints', page: 'endpoints', defaults: ['null', 'null'],
		text: 'End value. Leave it out to end at the value the property had at start.',
		draw: (n) => lane() + dot(36, 44, 'fm') + text(36, 62, 'current') + hop(36, 196, 40) + dot(204, 44) + text(204, 62, n('To'), 'ta'),
	},
	{
		name: 'By', group: 'endpoints', page: 'endpoints', defaults: ['null', 'null'],
		text: 'An offset instead of {To}. It adds on top of other changes to the property.',
		draw: (n) => lane() + dot(36, 44, 'fm') + text(36, 62, 'current') + arrow(36, 150, 28) + text(93, 20, `+ ${n('By')}`, 'ta') +
			dot(150, 44) + line(36, 44, 150, 44, 'sa'),
	},
	{
		name: 'initial_value', group: 'endpoints', page: 'endpoints', only: 'gdscript', defaults: ['', '0.0'],
		text: 'Start value of a callback-only definition, which has no property to read.',
		draw: (n) => lane() + box(12, 6, 76, 17, 'sm dash') + text(50, 18, 'no property') + dot(50, 44) +
			text(50, 62, n('initial_value'), 'ta') + hop(50, 196, 40, 'sm', 'fm') + ring(204, 44) + text(204, 62, n('To')),
	},

	// Timing
	{
		name: 'Duration', group: 'timing', page: 'timing', defaults: ['0', '0.0'],
		text: 'Seconds per leg. Zero completes on the first update.',
		draw: (n) => bar(20, 220, 32) + bracket(20, 220, 48, n('Duration'), 'sa', 'ta'),
	},
	{
		name: 'Delay', group: 'timing', page: 'timing', defaults: ['0', '0.0'],
		text: 'Wait before the first leg. A negative delay starts partway in.',
		draw: (n) => wait(20, 84, 32, 'sa') + bar(84, 220, 32, 'fm') + bracket(20, 84, 48, n('Delay'), 'sa', 'ta'),
	},
	{
		name: 'Offset', group: 'timing', page: 'timing', defaults: ['0', '0.0'],
		text: 'Start this far into the first forward leg. The delay still comes first.',
		draw: (n) => bar(20, 220, 32, 'fm faint') + bar(84, 220, 32, 'fm') + dot(84, 32, 'fa', 5) +
			bracket(20, 84, 48, n('Offset'), 'sa', 'ta'),
	},
	{
		name: 'Repeats', group: 'timing', page: 'timing', defaults: ['0', '0'],
		text: { csharp: 'Cycles after the first. `TweenOptions.Infinite` repeats until cancelled.', gdscript: 'Cycles after the first. `Tweens.INFINITE` repeats until cancelled.' },
		draw: (n) => text(120, 16, `${n('Repeats')} = 2`, 'ta') + bar(20, 82, 34, 'fm') + bar(89, 151, 34) + bar(158, 220, 34) +
			text(51, 54, 'first') + text(120, 54, '+1', 'ta') + text(189, 54, '+2', 'ta'),
	},
	{
		name: 'PingPong', group: 'timing', page: 'timing', defaults: ['false', 'false'],
		text: 'Each cycle runs forward, then back to its start.',
		draw: () => bar(20, 116, 40, 'fm') + arrow(40, 96, 24, 'sm', 'fm') + bar(124, 220, 40) + arrow(200, 144, 24) +
			text(68, 60, 'forward') + text(172, 60, 'back', 'ta'),
	},
	{
		name: 'PingPongInterval', group: 'timing', page: 'timing', defaults: ['0', '0.0'],
		text: 'Wait at the far end before returning.',
		draw: (n) => bar(20, 92, 32, 'fm') + wait(92, 148, 32, 'sa') + bar(148, 220, 32, 'fm') + text(56, 18, 'forward') +
			text(184, 18, 'back') + bracket(92, 148, 48, n('PingPongInterval'), 'sa', 'ta'),
	},
	{
		name: 'RepeatInterval', group: 'timing', page: 'timing', defaults: ['0', '0.0'],
		text: 'Wait between cycles, never after the last.',
		draw: (n) => bar(20, 92, 32, 'fm') + wait(92, 148, 32, 'sa') + bar(148, 220, 32, 'fm') + text(56, 18, 'cycle 1') +
			text(184, 18, 'cycle 2') + bracket(92, 148, 48, n('RepeatInterval'), 'sa', 'ta'),
	},
	{
		name: 'Fill', group: 'timing', page: 'timing', defaults: ['RetainFinalValue', 'RETAIN_FINAL_VALUE'],
		text: 'What the property shows during the delay and after the end. Dashed: the alternatives.',
		draw: (n) => `<rect class="band" x="20" y="8" width="52" height="54" rx="4"/><rect class="band" x="168" y="8" width="52" height="54" rx="4"/>` +
			line(24, 54, 68, 54, 'sm') + line(24, py(0.4), 68, py(0.4), 'sa dash') + text(46, py(0.4) - 6, n('From'), 'ta') +
			curve((t) => 0.4 + 0.6 * t * t * (3 - 2 * t), 'sm none', 72, 168) +
			line(168, py(1) + 3, 216, py(1) + 3, 'sm') + path(`M168 ${py(1) + 3}C190 ${py(1) + 3} 190 54 216 54`, 'sa dash none') +
			text(46, 70, 'delay') + text(194, 70, 'after'),
	},

	// Easing
	{
		name: 'Ease', group: 'easing', page: 'easing', defaults: ['Linear', 'LINEAR'],
		text: 'The curve: an In, an Out, or one of each joined with `|`.',
		draw: (_, lang) => guides() + curve((t) => composeEase('Sine', 'Cubic', t)) + text(24, 26, ease(lang, 'Sine', 'Cubic'), 'ta', 'start'),
	},
	{
		name: 'BlendType', group: 'easing', page: 'easing', defaults: ['Makima', 'MAKIMA'],
		text: 'How a mixed pair joins in the middle.',
		draw: (_, lang) => `<rect class="band" x="80" y="8" width="80" height="54" rx="4"/>` + guides() +
			curve((t) => composeEase('Quint', 'Circ', t, 0.5, 'Linear', 0.4), 'sm dash none') +
			curve((t) => composeEase('Quint', 'Circ', t, 0.5, 'Makima', 0.4)) +
			text(24, 26, lang === 'csharp' ? 'Makima' : 'MAKIMA', 'ta', 'start') + text(216, 52, lang === 'csharp' ? 'Linear' : 'LINEAR', 't', 'end'),
	},
	{
		name: 'Blend', group: 'easing', page: 'easing', defaults: ['0.1', '0.1'],
		text: 'Width of the join window, from 0 to 1.',
		draw: (n) => `<rect class="band" x="80" y="8" width="80" height="44" rx="4"/>` + guides() +
			curve((t) => composeEase('Quint', 'Circ', t, 0.5, 'Makima', 0.4)) + bracket(80, 160, 58, n('Blend'), 'sa', 'ta'),
	},
	{
		name: 'Skew', group: 'easing', page: 'easing', defaults: ['0.5', '0.5'],
		text: 'Where the forward leg hands over from In to Out.',
		draw: (n) => guides() + curve((t) => composeEase('Cubic', 'Cubic', t), 'sm dash none') + curve((t) => composeEase('Cubic', 'Cubic', t, 0.3)) +
			line(80, py(0.3), 80, py(0), 'sa dash') + dot(80, py(0.3), 'fa', 3.5) + text(84, 66, `${n('Skew')} 0.3`, 'ta', 'start'),
	},
	{
		name: 'Weks', group: 'easing', page: 'easing', defaults: ['0.5', '0.5'],
		text: 'Where the ping-pong return hands over: skew, backwards.',
		draw: (n) => guides() + curve((t) => composeEase('Cubic', 'Cubic', t), 'sm none', 20, 120) +
			curve((t) => 1 - composeEase('Cubic', 'Cubic', t, 0.3), 'sa none', 120, 220) +
			line(150, py(0.7), 150, py(0), 'sa dash') + dot(150, py(0.7), 'fa', 3.5) + text(154, 66, `${n('Weks')} 0.3`, 'ta', 'start') +
			text(70, 66, 'forward'),
	},
	{
		name: 'EaseFunction', group: 'easing', page: 'easing', defaults: ['null', 'Callable()'],
		text: 'Your own function of progress. It replaces {Ease}.',
		draw: () => guides() + curve((t) => t * t) + text(24, 26, 'f(t) = t²', 'ta', 'start'),
	},
	{
		name: 'Curve', group: 'easing', page: 'easing', defaults: ['null', 'null'],
		text: 'A Godot `Curve` sampled over progress. It replaces {Ease}.',
		draw: () => guides() + path(`M20 ${py(0)}C60 ${py(0)} 80 ${py(0.9)} 120 ${py(0.8)}S190 ${py(1)} 220 ${py(1)}`, 'sa none') +
			line(120, py(0.8), 92, py(0.75), 'sm') + line(120, py(0.8), 148, py(0.85), 'sm') + dot(92, py(0.75), 'fm', 2.5) +
			dot(148, py(0.85), 'fm', 2.5) + line(20, py(0), 60, py(0), 'sm') + dot(60, py(0), 'fm', 2.5) +
			`<rect class="fa" x="16.5" y="${py(0) - 3.5}" width="7" height="7"/><rect class="fa" x="116.5" y="${n2(py(0.8) - 3.5)}" width="7" height="7"/><rect class="fa" x="216.5" y="${py(1) - 3.5}" width="7" height="7"/>`,
	},

	// Color interpolation
	{
		name: 'ColorSpace', group: 'endpoints', page: 'definitions', defaults: ['ColorSpace.Oklab', 'Tweens.ColorSpace.OKLAB'],
		text: 'Working coordinates for whole-color interpolation.',
		draw: () => text(35, 26, 'Color') + arrow(62, 105, 36) + text(130, 39, 'OKLab', 'ta') + arrow(164, 208, 36),
	},
	{
		name: 'AlphaMode', group: 'endpoints', page: 'definitions', defaults: ['AlphaMode.Premultiplied', 'Tweens.AlphaMode.PREMULTIPLIED'],
		text: 'Premultiply working coordinates by alpha before interpolation.',
		draw: () => text(55, 30, 'color × alpha') + arrow(100, 145, 36) + text(186, 39, 'interpolate', 'ta'),
	},
	{
		name: 'ColorEncoding', group: 'endpoints', page: 'definitions', defaults: ['ColorEncoding.Srgb', 'Tweens.ColorEncoding.SRGB'],
		text: 'RGB encoding accepted and returned at the Godot API boundary.',
		draw: () => text(42, 39, 'sRGB') + arrow(70, 112, 36) + text(150, 39, 'working', 'ta') + arrow(184, 222, 36),
	},

	// Callbacks
	{
		name: 'OnAdd', group: 'callbacks', page: 'callbacks',
		text: 'Runs at activation, after the start value is captured.',
		draw: (n) => callbacks(event(20, 44, n('OnAdd'), 'start')) + text(40, 62, 'delay'),
	},
	{
		name: 'OnStart', group: 'callbacks', page: 'callbacks',
		text: 'Runs once, when the delay ends and the motion begins.',
		draw: (n) => callbacks(event(60, 44, n('OnStart'))) + text(40, 62, 'delay'),
	},
	{
		name: 'OnUpdate', group: 'callbacks', page: 'callbacks',
		text: 'Runs after each write.',
		draw: (n) => callbacks(Array.from({ length: 14 }, (_, i) => line(70 + i * 10, 36, 70 + i * 10, 52, 'sa')).join('')) +
			text(135, 24, n('OnUpdate'), 'ta') + text(40, 62, 'delay'),
	},
	{
		name: 'OnEnd', group: 'callbacks', page: 'callbacks',
		text: 'Runs on natural completion.',
		draw: (n) => callbacks(event(210, 44, n('OnEnd'), 'end')) + text(40, 62, 'delay'),
	},
	{
		name: 'OnCancel', group: 'callbacks', page: 'callbacks',
		text: 'Runs when playback stops early: cancelled, target freed, owner exited, or runner disposed.',
		draw: (n) => wait(20, 60, 44) + bar(60, 140, 44, 'fm') + bar(140, 210, 44, 'fm faint') + cross(144, 44, 'sa thick') +
			text(144, 24, n('OnCancel'), 'ta') + text(40, 62, 'delay'),
	},
	{
		name: 'OnFinally', group: 'callbacks', page: 'callbacks',
		text: 'Runs last, regardless of how tween playback actually ended.',
		draw: (n) => bar(20, 140, 22, 'fm') + ring(146, 22) + dot(164, 22, 'fa', 3.5) + text(172, 25, n('OnFinally'), 'ta', 'start') +
			bar(20, 90, 50, 'fm') + cross(96, 50, 'sm thick') + dot(114, 50, 'fa', 3.5) + text(122, 53, n('OnFinally'), 'ta', 'start') +
			text(146, 38, 'ended') + text(96, 66, 'stopped'),
	},
	{
		name: 'SuppressCallbacksWhenTargetInvalid', group: 'callbacks', page: 'callbacks', defaults: ['false', 'false'],
		text: 'Skips {OnEnd}, {OnCancel}, and {OnFinally} once the target or owner is gone.',
		draw: (n) => bar(20, 100, 44, 'fm') + cross(106, 44, 'sm thick') + text(106, 64, 'target freed') +
			ring(142, 44) + line(136, 50, 148, 38, 'sa') + text(142, 26, n('OnCancel')) +
			ring(204, 44) + line(198, 50, 210, 38, 'sa') + text(204, 26, n('OnFinally')),
	},

	// Variations: factor × value + delta, once per start.
	{ name: 'FactorFrom', group: 'variations', page: 'endpoints', defaults: ['1', '1.0'], text: 'Multiplies {From}.', draw: (n) => scaled(n, 'From', 84, '× 1.5', 116) },
	{ name: 'DeltaFrom', group: 'variations', page: 'endpoints', defaults: ['null', 'null'], text: 'Then adds to {From}.', draw: (n) => scaled(n, 'From', 84, '+ delta', 134) },
	{ name: 'FactorTo', group: 'variations', page: 'endpoints', row: 'FactorFrom', defaults: ['1', '1.0'], text: 'Multiplies {To}.', draw: (n) => scaled(n, 'To', 140, '× 1.5', 200) },
	{ name: 'DeltaTo', group: 'variations', page: 'endpoints', row: 'DeltaFrom', defaults: ['null', 'null'], text: 'Then adds to {To}.', draw: (n) => scaled(n, 'To', 140, '+ delta', 190) },
	{ name: 'FactorBy', group: 'variations', page: 'endpoints', row: 'FactorFrom', defaults: ['1', '1.0'], text: 'Multiplies {By}.', draw: (n) => offset(n, '× 1.5', 35) },
	{ name: 'DeltaBy', group: 'variations', page: 'endpoints', row: 'DeltaFrom', defaults: ['null', 'null'], text: 'Then adds to {By}.', draw: (n) => offset(n, '+ delta', 45) },
	{ name: 'FactorDuration', group: 'variations', page: 'endpoints', defaults: ['1', '1.0'], text: 'Multiplies {Duration}.', draw: (n) => lengths('× 1.5', n, true) },
	{ name: 'DeltaDuration', group: 'variations', page: 'endpoints', defaults: ['0', '0.0'], text: 'Then adds seconds to {Duration}.', draw: (n) => lengths('+ seconds', n, false) },
	{
		name: 'FactorDelay', group: 'variations', page: 'endpoints', defaults: ['1', '1.0'], text: 'Multiplies {Delay}.',
		draw: (n) => wait(20, 60, 24) + bar(60, 150, 24, 'fm') + text(40, 14, n('Delay')) + wait(20, 80, 50, 'sa') + bar(80, 170, 50, 'fm') +
			text(50, 66, '× 1.5', 'ta'),
	},
	{
		name: 'DeltaDelay', group: 'variations', page: 'endpoints', defaults: ['0', '0.0'], text: 'Then adds seconds to {Delay}, as in a per-start stagger.',
		draw: () => [0, 1, 2].map((i) => (i ? wait(20, 20 + i * 30, 16 + i * 20, 'sa') : '') + bar(20 + i * 30, 100 + i * 30, 16 + i * 20, 'fm') +
			text(106 + i * 30, 19 + i * 20, ['start 1', '+ delta', '+ 2 × delta'][i], i ? 'ta' : 't', 'start')).join(''),
	},

	// Target: GDScript's helpers set these; C# definitions are typed instead.
	{
		name: 'property', group: 'target', page: 'definitions', only: 'gdscript', defaults: ['', '^""'],
		text: 'Property path, set by the named helpers and `Tweens.property()`.',
		draw: () => box(20, 8, 92, 56) + text(30, 22, 'Sprite2D', 't', 'start') + `<rect class="band" x="24" y="28" width="84" height="14" rx="3"/>` +
			text(30, 38, 'position', 'ta', 'start') + text(30, 56, 'rotation', 't', 'start') + arrow(116, 150, 35) + lane(35, 156) +
			dot(164, 35, 'fm') + hop(164, 212, 31, 'sa', 'fa', 8) + dot(216, 35),
	},
	{
		name: 'adapter', group: 'target', page: 'definitions', only: 'gdscript', defaults: ['', 'null'],
		text: 'An adapter for other storage. Use it or {property}, not both.',
		draw: () => box(16, 18, 64, 36) + text(48, 39, 'storage') + box(122, 18, 64, 36, 'sa') + text(154, 39, 'adapter', 'ta') +
			arrow(116, 86, 29) + text(101, 22, 'write') + arrow(86, 116, 43) + text(101, 58, 'read') + bar(196, 224, 36, 'fm') +
			text(210, 56, 'tween'),
	},
	{
		name: 'target_class', group: 'target', page: 'definitions', only: 'gdscript', defaults: ['', '&""'],
		text: 'The class a named helper checks at start.',
		draw: () => box(20, 20, 84, 32) + text(62, 40, 'position_2d') + arrow(108, 140, 36) + box(144, 20, 76, 32, 'sa') + text(172, 40, 'Node2D', 'ta') +
			path('M204 36l4 4l8 -9', 'sa none thick'),
	},
	{
		name: 'value_type', group: 'target', page: 'definitions', only: 'gdscript', defaults: ['', 'TYPE_NIL'],
		text: 'The value type a named helper checks at start.',
		draw: () => box(20, 20, 84, 32) + text(62, 40, 'Vector2') + arrow(108, 140, 36) + box(144, 20, 76, 32, 'sa') + text(172, 40, 'position', 'ta') +
			path('M204 36l4 4l8 -9', 'sa none thick'),
	},
];

const anchor = (s: string) => s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

const inline = (s: string, lang: Lang) =>
	esc(s)
		.replace(/\{(\w+)\}/g, (_, name) => `<code>${fieldName(lang, name)}</code>`)
		.replace(/`([^`]+)`/g, '<code>$1</code>');

export interface Card {
	id: string;
	name: string;
	href: string;
	default?: string;
	text: string;
	svg: string;
}

/** The fields of one group for one language, ready to render. */
export const fieldsOf = (group: Group, lang: Lang): Card[] =>
	FIELDS.filter((f) => f.group === group && (!f.only || f.only === lang)).map((f) => {
		const name = fieldName(lang, f.name);
		const n = (other: string) => fieldName(lang, other);
		const row = anchor(fieldName(lang, f.row ?? f.name));
		const body = f.draw(n, lang);
		return {
			id: anchor(name),
			name,
			href: `/${lang}/api/${f.page}/#${row}`,
			default: f.defaults?.[lang === 'csharp' ? 0 : 1] || undefined,
			text: inline(typeof f.text === 'string' ? f.text : f.text[lang], lang),
			svg: `<svg viewBox="0 0 ${W} ${H}" aria-hidden="true">${body}</svg>`,
		};
	});
