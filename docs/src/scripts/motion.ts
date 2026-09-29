// Easing math from addons/tweens_gd/csharp/Easing/Easing.cs, which follows unity-tweens (MIT; see THIRD-PARTY-NOTICES.md).
// The docs animate with the same curves the library ships.

const A = 1.70158;
const B = A * 1.525;
const C = A + 1;
const D = (2 * Math.PI) / 3;
const E = (2 * Math.PI) / 4.5;
const F = 7.5625;
const G = 2.75;

const bounceOut = (t: number) => {
	if (t < 1 / G) return F * t * t;
	if (t < 2 / G) return F * (t -= 1.5 / G) * t + 0.75;
	if (t < 2.5 / G) return F * (t -= 2.25 / G) * t + 0.9375;
	return F * (t -= 2.625 / G) * t + 0.984375;
};

export const EASES = {
	Linear: (t: number) => t,
	// Hermite smoothstep and Perlin's smootherstep: symmetric, no overshoot, zero speed at both ends.
	SmoothStep: (t: number) => t * t * (3 - 2 * t),
	SmootherStep: (t: number) => t * t * t * (t * (6 * t - 15) + 10),
	SineIn: (t: number) => 1 - Math.cos((t * Math.PI) / 2),
	SineOut: (t: number) => Math.sin((t * Math.PI) / 2),
	SineInOut: (t: number) => -(Math.cos(Math.PI * t) - 1) / 2,
	QuadIn: (t: number) => t * t,
	QuadOut: (t: number) => 1 - (1 - t) * (1 - t),
	QuadInOut: (t: number) => (t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2),
	CubicIn: (t: number) => t * t * t,
	CubicOut: (t: number) => 1 - Math.pow(1 - t, 3),
	CubicInOut: (t: number) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2),
	QuartIn: (t: number) => t ** 4,
	QuartOut: (t: number) => 1 - Math.pow(1 - t, 4),
	QuartInOut: (t: number) => (t < 0.5 ? 8 * t ** 4 : 1 - Math.pow(-2 * t + 2, 4) / 2),
	QuintIn: (t: number) => t ** 5,
	QuintOut: (t: number) => 1 - Math.pow(1 - t, 5),
	QuintInOut: (t: number) => (t < 0.5 ? 16 * t ** 5 : 1 - Math.pow(-2 * t + 2, 5) / 2),
	ExpoIn: (t: number) => (t === 0 ? 0 : Math.pow(2, 10 * t - 10)),
	ExpoOut: (t: number) => (t === 1 ? 1 : 1 - Math.pow(2, -10 * t)),
	ExpoInOut: (t: number) =>
		t === 0 ? 0 : t === 1 ? 1 : t < 0.5 ? Math.pow(2, 20 * t - 10) / 2 : (2 - Math.pow(2, -20 * t + 10)) / 2,
	CircIn: (t: number) => 1 - Math.sqrt(1 - t * t),
	CircOut: (t: number) => Math.sqrt(1 - (t - 1) ** 2),
	CircInOut: (t: number) =>
		t < 0.5 ? (1 - Math.sqrt(1 - (2 * t) ** 2)) / 2 : (Math.sqrt(1 - (-2 * t + 2) ** 2) + 1) / 2,
	BackIn: (t: number) => C * t ** 3 - A * t * t,
	BackOut: (t: number) => 1 + C * (t - 1) ** 3 + A * (t - 1) ** 2,
	BackInOut: (t: number) =>
		t < 0.5
			? ((2 * t) ** 2 * ((B + 1) * 2 * t - B)) / 2
			: ((2 * t - 2) ** 2 * ((B + 1) * (t * 2 - 2) + B) + 2) / 2,
	ElasticIn: (t: number) =>
		t === 0 ? 0 : t === 1 ? 1 : -Math.pow(2, 10 * t - 10) * Math.sin((t * 10 - 10.75) * D),
	ElasticOut: (t: number) =>
		t === 0 ? 0 : t === 1 ? 1 : Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * D) + 1,
	ElasticInOut: (t: number) =>
		t === 0
			? 0
			: t === 1
				? 1
				: t < 0.5
					? -(Math.pow(2, 20 * t - 10) * Math.sin((20 * t - 11.125) * E)) / 2
					: (Math.pow(2, -20 * t + 10) * Math.sin((20 * t - 11.125) * E)) / 2 + 1,
	BounceIn: (t: number) => 1 - bounceOut(1 - t),
	BounceOut: bounceOut,
	BounceInOut: (t: number) => (t < 0.5 ? (1 - bounceOut(1 - 2 * t)) / 2 : (1 + bounceOut(2 * t - 1)) / 2),
} as const;

export type EaseName = keyof typeof EASES;
export const ease = (name: EaseName, t: number) => EASES[name](Math.min(1, Math.max(0, t)));

export const FAMILIES = ['Linear', 'Sine', 'Quad', 'Cubic', 'Quart', 'Quint', 'Expo', 'Circ', 'Back', 'Elastic', 'Bounce', 'SmoothStep', 'SmootherStep', 'Back10', 'Back20', 'Back30', 'Back40', 'Back50', 'Elastic10', 'Elastic20', 'Elastic30', 'Elastic40', 'Elastic50', 'Bounce10', 'Bounce20', 'Bounce30', 'Bounce40', 'Bounce50', 'Jump', 'Jump10', 'Jump20', 'Jump30', 'Jump40', 'Jump50'] as const;
export type EaseFamily = (typeof FAMILIES)[number];
export type EaseLeg = EaseFamily | 'None';
export const canonicalFamily = (family: EaseLeg): EaseLeg => family === 'Back10' ? 'Back' : family === 'Elastic10' ? 'Elastic' : family === 'Bounce10' ? 'Bounce' : family === 'Jump10' ? 'Jump' : family;
const BACK_SOLO = [1.701540198866824, 2.5923889015162995, 3.3940516581445603, 4.155744652639195, 4.894859521133737];
const BACK_PAIRED = [2.5923889015162995, 4.155744652639195, 5.619622918334311, 7.042439379340937, 8.44353560159325];
const ELASTIC_SOLO_PERIOD = 0.7553423501870573, ELASTIC_PAIR_PERIOD = 0.5074981597799941;
const ELASTIC_SOLO_KICK = [0, 0.6853132138892408, 1.1091787748281363, 1.4696240828544362, 1.8012905799033314];
const ELASTIC_PAIR_KICK = [0, 0.8829462755133655, 1.4362972653938577, 1.9263692424370968, 2.3898041023212153];
const BOUNCE_SOLO_ROOT = [.1,.2,.3,.4,.5].map(Math.sqrt);
const BOUNCE_PAIR_ROOT = [.2,.4,.6,.8,1].map(Math.sqrt);
const JUMP_SOLO_LAUNCH = [1.1,1.2,1.3,1.4,1.5].map(Math.sqrt);
const JUMP_PAIR_LAUNCH = [1.2,1.4,1.6,1.8,2].map(Math.sqrt);
const strengthLevel = (family: EaseLeg) => +(family.match(/\d+$/)?.[0] ?? 10)/10-1;
const isOvershoot = (family: EaseLeg) => family.startsWith('Back') || family.startsWith('Elastic');

function overshootLeg(family: EaseLeg, direction: 'In' | 'Out', t: number, paired: boolean): number {
	if (t === 0 || t === 1) return t;
	const level = strengthLevel(family);
	if (family.startsWith('Back')) {
		const s = (paired ? BACK_PAIRED : BACK_SOLO)[level], u = direction === 'In' ? t : 1-t;
		const value = (s+1)*u*u*u-s*u*u;
		return direction === 'In' ? value : 1-value;
	}
	const u = direction === 'In' ? 1-t : t;
	const angle = 2*Math.PI*u/(paired ? ELASTIC_PAIR_PERIOD : ELASTIC_SOLO_PERIOD);
	const kick = (paired ? ELASTIC_PAIR_KICK : ELASTIC_SOLO_KICK)[level];
	const value = 2**(-10*u)*(Math.cos(angle)-kick*Math.sin(angle));
	return direction === 'In' ? value : 1-value;
}

// Three rebounds at h, h/4, h/16; one acceleration fixes their relative flight times.
function bounceLegOut(t: number, level: number, paired: boolean): number {
	if (t === 0 || t === 1) return t;
	const h = (level+1)*(paired ? .2 : .1), r = (paired ? BOUNCE_PAIR_ROOT : BOUNCE_SOLO_ROOT)[level];
	let u = t*(1+3.5*r);
	if (u < 1) return u*u;
	if (u < 1+2*r) { u -= 1+r; return 1-h+u*u; }
	if (u < 1+3*r) { u -= 1+2.5*r; return 1-h/4+u*u; }
	u -= 1+3.25*r;
	return 1-h/16+u*u;
}

// Launch to the first overshoot, then rebound twice above the target.
function jumpLegOut(t: number, level: number, paired: boolean): number {
	if (t === 0 || t === 1) return t;
	const h = (level+1)*(paired ? .2 : .1), r = (paired ? BOUNCE_PAIR_ROOT : BOUNCE_SOLO_ROOT)[level];
	const a = (paired ? JUMP_PAIR_LAUNCH : JUMP_SOLO_LAUNCH)[level];
	let u = t*(a+2.5*r);
	if (u < a+r) { u -= a; return 1+h-u*u; }
	if (u < a+2*r) { u -= a+1.5*r; return 1+h/4-u*u; }
	u -= a+2.25*r;
	return 1+h/16-u*u;
}

/** Join half-duration profiles locally; crossfade modes are available for comparison. */
export type BlendType = 'Hermite' | 'SmoothStep' | 'Linear';
export function composeEase(entry: EaseLeg, exit: EaseLeg, progress: number, skew = 1, method: BlendType = 'Hermite', width = 0.2): number {
	if (!['Hermite', 'SmoothStep', 'Linear'].includes(method) || !Number.isFinite(width) || width < 0 || width > 1) throw new RangeError('Invalid easing blend');
	entry = canonicalFamily(entry); exit = canonicalFamily(exit);
	const t = Math.min(1, Math.max(0, progress)) ** skew;
	if (entry === 'None') return legEase(exit, 'Out', t);
	if (exit === 'None') return legEase(entry, 'In', t);
	if (entry === exit) return pairedLegEase(entry, t);
	const h = width / 2, left = 0.5 - h, right = 0.5 + h;
	if (t <= left) return pairedLegEase(entry, t);
	if (t >= right) return pairedLegEase(exit, t);
	if (method !== 'Hermite') {
		const u = (t - left) / width, w = method === 'SmoothStep' ? u * u * (3 - 2 * u) : u;
		return pairedLegEase(entry, t) * (1 - w) + pairedLegEase(exit, t) * w;
	}
	const y0 = pairedLegEase(entry, left), y1 = pairedLegEase(exit, right);
	const v0 = pairSlope(entry, left), v1 = pairSlope(exit, right);
	const d0 = (0.5 - y0) / h, d1 = (y1 - 0.5) / h;
	const middle = Math.min(3 * Math.max(0, Math.min(d0, d1)), Math.max(0, (3 * (d0 + d1) - v0 - v1) / 4));
	return t <= 0.5 ? hermite((t - left) / h, y0, 0.5, h * v0, h * middle)
		: hermite((t - 0.5) / h, 0.5, y1, h * middle, h * v1);
}

function hermite(u: number, y0: number, y1: number, m0: number, m1: number) {
	return (2*u**3 - 3*u*u + 1)*y0 + (u**3 - 2*u*u + u)*m0 + (-2*u**3 + 3*u*u)*y1 + (u**3 - u*u)*m1;
}

function pairSlope(family: EaseLeg, time: number): number {
	const t = Math.min(time, 1-time), x = 2*t, level = strengthLevel(family);
	if (family.startsWith('Back')) { const s=BACK_PAIRED[level]; return 3*(s+1)*x*x-2*s*x; }
	if (family.startsWith('Elastic')) {
		const u=1-x, omega=2*Math.PI/ELASTIC_PAIR_PERIOD, decay=10*Math.LN2, kick=ELASTIC_PAIR_KICK[level];
		return 2**(-10*u)*((decay+kick*omega)*Math.cos(omega*u)+(omega-kick*decay)*Math.sin(omega*u));
	}
	if (family.startsWith('Bounce')) {
		const r = BOUNCE_PAIR_ROOT[level], scale = 1+3.5*r;
		let u = (1-x)*scale;
		if (u >= 1+3*r) u -= 1+3.25*r;
		else if (u >= 1+2*r) u -= 1+2.5*r;
		else if (u >= 1) u -= 1+r;
		return 2*scale*u;
	}
	if (family.startsWith('Jump')) {
		const r = BOUNCE_PAIR_ROOT[level], a = JUMP_PAIR_LAUNCH[level], scale = a+2.5*r, u = (1-x)*scale;
		const center = u < a+r ? a : u < a+2*r ? a+1.5*r : a+2.25*r;
		return 2*scale*(center-u);
	}
	switch (family) {
		case 'Sine': return Math.PI*Math.sin(Math.PI*t)/2;
		case 'Quad': return 4*t;
		case 'Cubic': return 12*t*t;
		case 'Quart': return 32*t**3;
		case 'Quint': return 80*t**4;
		case 'Expo': return 10*Math.LN2*2**(20*t-10);
		case 'Circ': return x/Math.sqrt(1-x*x);
		case 'SmoothStep': return 6*t*(1-t);
		case 'SmootherStep': return 30*t*t*(1-t)**2;
		default: return 1;
	}
}

export function pairedLegEase(family: EaseLeg, t: number): number {
	t = Math.min(1, Math.max(0, t));
	if (family.startsWith('Jump')) return t < .5 ? (1-jumpLegOut(1-2*t, strengthLevel(family), true))/2 : .5+jumpLegOut(2*t-1, strengthLevel(family), true)/2;
	if (family.startsWith('Bounce')) return t < .5 ? (1-bounceLegOut(1-2*t, strengthLevel(family), true))/2 : .5+bounceLegOut(2*t-1, strengthLevel(family), true)/2;
	if (isOvershoot(family)) return t < 0.5 ? overshootLeg(family, 'In', 2*t, true)/2 : 0.5+overshootLeg(family, 'Out', 2*t-1, true)/2;
	if (family === 'None') return t;
	const name = ['Linear', 'SmoothStep', 'SmootherStep'].includes(family) ? family : family + 'InOut';
	return ease(name as EaseName, t);
}

export function legEase(family: EaseLeg, direction: 'In' | 'Out', t: number): number {
	t = Math.min(1, Math.max(0, t));
	if (family.startsWith('Jump')) return direction === 'In' ? 1-jumpLegOut(1-t, strengthLevel(family), false) : jumpLegOut(t, strengthLevel(family), false);
	if (family.startsWith('Bounce')) return direction === 'In' ? 1-bounceLegOut(1-t, strengthLevel(family), false) : bounceLegOut(t, strengthLevel(family), false);
	if (isOvershoot(family)) return overshootLeg(family, direction, Math.min(1, Math.max(0, t)), false);
	if (family === 'None') return t;
	const name = ['Linear', 'SmoothStep', 'SmootherStep'].includes(family) ? family : family + direction;
	return ease(name as EaseName, t);
}

export const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

/** `Element.animate` for small feedback pops, skipped under reduced motion (the CSS rule cannot reach script animations). */
export const animate = (el: Element, keyframes: Keyframe[], options: KeyframeAnimationOptions) =>
	reducedMotion() ? undefined : el.animate(keyframes, options);

export const lerp = (a: number, b: number, w: number) => a + (b - a) * w;

export interface TweenHandle {
	cancel(): void;
	completion: Promise<'completed' | 'cancelled'>;
}

/** A single leg, sampled every frame. Reduced motion jumps straight to the end. */
export function tween(
	duration: number,
	onUpdate: (weight: number, progress: number) => void,
	easeName: EaseName = 'CubicOut',
	delay = 0,
): TweenHandle {
	let raf = 0;
	let settle!: (r: 'completed' | 'cancelled') => void;
	const completion = new Promise<'completed' | 'cancelled'>((r) => (settle = r));
	if (reducedMotion()) {
		onUpdate(1, 1);
		settle('completed');
		return { cancel() {}, completion };
	}
	const start = performance.now() + delay * 1000;
	const step = (now: number) => {
		const p = Math.min(1, Math.max(0, (now - start) / (duration * 1000)));
		if (now >= start) onUpdate(ease(easeName, p), p);
		if (p < 1) raf = requestAnimationFrame(step);
		else settle('completed');
	};
	raf = requestAnimationFrame(step);
	return {
		cancel() {
			cancelAnimationFrame(raf);
			settle('cancelled');
		},
		completion,
	};
}

/**
 * Runs `frame(elapsedSeconds)` while the element is on screen and the tab is visible.
 * Returns a stop function. Under reduced motion `frame` runs once at `stillTime`.
 */
export function loop(el: Element, frame: (t: number, dt: number) => void, stillTime = 0) {
	if (reducedMotion()) {
		frame(stillTime, 0);
		return () => {};
	}
	let raf = 0;
	let visible = false;
	let last = 0;
	let elapsed = stillTime;
	const tick = (now: number) => {
		const dt = last ? Math.min(0.1, (now - last) / 1000) : 0;
		last = now;
		elapsed += dt;
		frame(elapsed, dt);
		raf = requestAnimationFrame(tick);
	};
	const sync = () => {
		const run = visible && !document.hidden;
		if (run && !raf) {
			last = 0;
			raf = requestAnimationFrame(tick);
		} else if (!run && raf) {
			cancelAnimationFrame(raf);
			raf = 0;
		}
	};
	frame(elapsed, 0);
	const io = new IntersectionObserver(([entry]) => {
		visible = entry.isIntersecting;
		sync();
	});
	io.observe(el);
	document.addEventListener('visibilitychange', sync);
	return () => {
		io.disconnect();
		document.removeEventListener('visibilitychange', sync);
		cancelAnimationFrame(raf);
		raf = 0;
	};
}

/** Calls `cb` once, the first time the element scrolls into view. */
export function onceVisible(el: Element, cb: () => void, threshold = 0.25) {
	const io = new IntersectionObserver(
		(entries) => {
			if (entries.some((e) => e.isIntersecting)) {
				io.disconnect();
				cb();
			}
		},
		{ threshold },
	);
	io.observe(el);
}

/** SVG path for an ease curve inside a w×h box, y flipped, with headroom for overshoot. */
export function curvePath(name: EaseName, w: number, h: number, pad = 0, samples = 96) {
	let d = '';
	for (let i = 0; i <= samples; i++) {
		const t = i / samples;
		const x = t * w;
		const y = h - pad - EASES[name](t) * (h - 2 * pad);
		d += `${i ? 'L' : 'M'}${x.toFixed(2)} ${y.toFixed(2)}`;
	}
	return d;
}
