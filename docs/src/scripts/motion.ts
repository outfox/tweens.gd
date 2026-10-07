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
export const canonicalFamily = (family: EaseLeg): EaseLeg => ['Back', 'Elastic', 'Bounce', 'Jump'].includes(family) ? (family + '30') as EaseLeg : family;
const BACK_SOLO = [1.701540198866824, 2.5923889015162995, 3.3940516581445603, 4.155744652639195, 4.894859521133737];
const BACK_PAIRED = [2.5923889015162995, 4.155744652639195, 5.619622918334311, 7.042439379340937, 8.44353560159325];
// Reproduce these peak calibrations with scripts/calibrate-elastic.mjs at the repository root.
const ELASTIC_SOLO_DECAY = 17.553423501870573, ELASTIC_SOLO_TAIL = 8, ELASTIC_PAIR_DECAY = 7.537490798899971;
const ELASTIC_SOLO_PERIOD = 0.43031056027706766, ELASTIC_PAIR_PERIOD = 0.6732985463200285;
const ELASTIC_SOLO_KICK = [-0.2974298881021775, 0.5992618094300022, 1.036207742895828, 1.3991518140146244, 1.730459189003298];
const ELASTIC_PAIR_KICK = [0.054242444203084675, 0.9078807808396336, 1.4611442537053763, 1.9533326533438204, 2.419656309841953];
const BOUNCE_SOLO_ROOT = [.1,.2,.3,.4,.5].map(Math.sqrt);
const BOUNCE_PAIR_ROOT = [.2,.4,.6,.8,1].map(Math.sqrt);
const JUMP_SOLO_LAUNCH = [1.1,1.2,1.3,1.4,1.5].map(Math.sqrt);
const JUMP_PAIR_LAUNCH = [1.2,1.4,1.6,1.8,2].map(Math.sqrt);
const strengthLevel = (family: EaseLeg) => +(family.match(/\d+$/)?.[0] ?? 30)/10-1;
const isOvershoot = (family: EaseLeg) => family.startsWith('Back') || family.startsWith('Elastic');

function elasticScale(kick: number, paired: boolean): number {
	const omega = 2*Math.PI/(paired ? ELASTIC_PAIR_PERIOD : ELASTIC_SOLO_PERIOD);
	const residual = 2**(-(paired ? ELASTIC_PAIR_DECAY : ELASTIC_SOLO_DECAY-ELASTIC_SOLO_TAIL))*(Math.cos(omega)-kick*Math.sin(omega));
	return 1/(1-residual);
}

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
	const exponent = -(paired ? ELASTIC_PAIR_DECAY : ELASTIC_SOLO_DECAY)*u+(paired ? 0 : ELASTIC_SOLO_TAIL*u*u);
	const value = elasticScale(kick, paired)*(1-2**exponent*(Math.cos(angle)-kick*Math.sin(angle)));
	return direction === 'In' ? 1-value : value;
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
export type BlendType = 'Makima' | 'Hermite' | 'SmoothStep' | 'Linear';
export function composeEase(entry: EaseLeg, exit: EaseLeg, progress: number, skew = 0.5, method: BlendType = 'Makima', width = 0.1): number {
	if (!['Makima', 'Hermite', 'SmoothStep', 'Linear'].includes(method) || !Number.isFinite(width) || width < 0 || width > 1) throw new RangeError('Invalid easing blend');
	if (!Number.isFinite(skew) || skew < 0 || skew > 1) throw new RangeError('Invalid easing split');
	entry = canonicalFamily(entry); exit = canonicalFamily(exit);
	const t = Math.min(1, Math.max(0, progress));
	if (entry === 'None') return legEase(exit, 'Out', t);
	if (exit === 'None') return legEase(entry, 'In', t);
	if (entry === exit) return splitLegEase(entry, t, skew);
	if (skew === 0) return legEase(exit,'Out',t);
	if (skew === 1) return legEase(entry,'In',t);
	const h = width * Math.min(skew,1-skew), left = skew - h, right = skew + h;
	if (t <= left) return splitLegEase(entry, t, skew);
	if (t >= right) return splitLegEase(exit, t, skew);
	if (method === 'SmoothStep' || method === 'Linear') {
		const u = (t - left) / (2*h), w = method === 'SmoothStep' ? u * u * (3 - 2 * u) : u;
		return splitLegEase(entry, t, skew) * (1 - w) + splitLegEase(exit, t, skew) * w;
	}
	const y0 = splitLegEase(entry, left, skew), y1 = splitLegEase(exit, right, skew);
	const v0 = splitSlope(entry, left, skew), v1 = splitSlope(exit, right, skew);
	const d0 = (skew - y0) / h, d1 = (y1 - skew) / h;
	const middle = method === 'Makima' ? makimaSlope(v0, d0, d1, v1)
		: Math.min(3 * Math.max(0, Math.min(d0, d1)), Math.max(0, (3 * (d0 + d1) - v0 - v1) / 4));
	return t <= skew ? hermite((t - left) / h, y0, skew, h * v0, h * middle)
		: hermite((t - skew) / h, skew, y1, h * middle, h * v1);
}

/** Linear In/Out split; neutral 0.5 preserves the authored paired profile. */
export function splitLegEase(family: EaseLeg, t: number, split = .5): number {
	if (split === 0) return legEase(family,'Out',t);
	if (split === 1) return legEase(family,'In',t);
	if (split === .5) return pairedLegEase(family,t);
	if (t === 0 || t === 1) return t;
	if (t <= split) {
		const x=t/split, mix=Math.max(0,2*split-1);
		return split*((1-mix)*2*pairedLegEase(family,x/2)+mix*legEase(family,'In',x));
	}
	const span=1-split,x=(t-split)/span,mix=Math.max(0,2*span-1);
	return split+span*((1-mix)*(2*pairedLegEase(family,.5+x/2)-1)+mix*legEase(family,'Out',x));
}

function soloSlope(family: EaseLeg, t: number, out: boolean): number {
	if (out) t=1-t;
	if (family === 'SmoothStep') return 6*t*(1-t);
	if (family === 'SmootherStep') return 30*t*t*(1-t)**2;
	if (family === 'Sine') return Math.PI/2*Math.sin(Math.PI/2*t);
	if (family === 'Expo') return 10*Math.LN2*2**(10*t-10);
	if (family === 'Circ') return t/Math.sqrt(1-t*t);
	const level=strengthLevel(family);
	if (family.startsWith('Back')) { const s=BACK_SOLO[level]; return 3*(s+1)*t*t-2*s*t; }
	if (family.startsWith('Elastic')) {
		const u=1-t,omega=2*Math.PI/ELASTIC_SOLO_PERIOD,decay=(ELASTIC_SOLO_DECAY-2*ELASTIC_SOLO_TAIL*u)*Math.LN2,kick=ELASTIC_SOLO_KICK[level];
		return elasticScale(kick,false)*2**(-ELASTIC_SOLO_DECAY*u+ELASTIC_SOLO_TAIL*u*u)*((decay+kick*omega)*Math.cos(omega*u)+(omega-kick*decay)*Math.sin(omega*u));
	}
	if (family.startsWith('Bounce') || family.startsWith('Jump')) {
		const jump=family.startsWith('Jump'),r=BOUNCE_SOLO_ROOT[level],a=jump?JUMP_SOLO_LAUNCH[level]:1,scale=jump?a+2.5*r:1+3.5*r;
		let u=(1-t)*scale;
		if (jump) return 2*scale*((u<a+r?a:u<a+2*r?a+1.5*r:a+2.25*r)-u);
		if (u>=1+3*r) u-=1+3.25*r;
		else if (u>=1+2*r) u-=1+2.5*r;
		else if (u>=1) u-=1+r;
		return 2*scale*u;
	}
	const powers: Partial<Record<EaseLeg, number>> = { Quad: 2, Cubic: 3, Quart: 4, Quint: 5 };
	const power = powers[family];
	return power ? power*t**(power-1) : 1;
}

function splitSlope(family: EaseLeg, t: number, split: number): number {
	if (split === .5) return pairSlope(family,t);
	const out=t>split,span=out?1-split:split,x=out?(t-split)/span:t/span,mix=Math.max(0,2*span-1);
	return (1-mix)*pairSlope(family,out?.5+x/2:x/2)+mix*soloSlope(family,x,out);
}

// Modified Akima (makima) slope at the midpoint from the four surrounding slopes. The legs' edge
// velocities stand in for the outer secants. Each half stays on its side of 0.5, so the weights never both vanish.
function makimaSlope(s0: number, s1: number, s2: number, s3: number) {
	const w1 = Math.abs(s3 - s2) + Math.abs(s3 + s2) / 2, w2 = Math.abs(s1 - s0) + Math.abs(s1 + s0) / 2;
	return (w1 * s1 + w2 * s2) / (w1 + w2);
}

function hermite(u: number, y0: number, y1: number, m0: number, m1: number) {
	return (2*u**3 - 3*u*u + 1)*y0 + (u**3 - 2*u*u + u)*m0 + (-2*u**3 + 3*u*u)*y1 + (u**3 - u*u)*m1;
}

function pairSlope(family: EaseLeg, time: number): number {
	const t = Math.min(time, 1-time), x = 2*t, level = strengthLevel(family);
	if (family.startsWith('Back')) { const s=BACK_PAIRED[level]; return 3*(s+1)*x*x-2*s*x; }
	if (family.startsWith('Elastic')) {
		const u=1-x, omega=2*Math.PI/ELASTIC_PAIR_PERIOD, decay=ELASTIC_PAIR_DECAY*Math.LN2, kick=ELASTIC_PAIR_KICK[level];
		return elasticScale(kick,true)*2**(-ELASTIC_PAIR_DECAY*u)*((decay+kick*omega)*Math.cos(omega*u)+(omega-kick*decay)*Math.sin(omega*u));
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

// Effect math from addons/tweens_gd/csharp/Easing/FX.cs, with the same defaults as Tweens.FX.

// Settings and progress are validated as in FX.cs, which throws ArgumentOutOfRangeException for the same inputs.
const unit = (t: number) => {
	if (!Number.isFinite(t)) throw new RangeError('Invalid effect progress');
	return Math.min(1, Math.max(0, t));
};
const validate = (frequency: number, amplitude: number, offset: number) => {
	if (!Number.isFinite(frequency) || frequency < 0 || !Number.isFinite(amplitude) || !Number.isFinite(offset))
		throw new RangeError('Invalid effect settings');
};
const smoother = (t: number) => t * t * t * (t * (6 * t - 15) + 10);

/** Ramps in over `attack` of the duration, then fades out; zero attack starts at full strength. */
export const attackRelease = (attack = 0.1, decay = 2) => {
	if (!Number.isFinite(attack) || attack < 0 || attack >= 1 || !Number.isFinite(decay) || decay <= 0)
		throw new RangeError('Invalid effect envelope');
	return (progress: number) => {
		const t = unit(progress);
		if (t === 1) return 0;
		if (attack === 0) return (1 - t) ** decay;
		if (t <= attack) return smoother(t / attack);
		return Math.max(0, 1 - smoother((t - attack) / (1 - attack))) ** decay;
	};
};

export function punch(frequency = 6, amplitude = 1, decay = 2, phase = 0, attack = 0) {
	validate(frequency, amplitude, phase);
	const envelope = attackRelease(attack, decay);
	return (progress: number) => {
		const t = unit(progress);
		return t === 1 ? 0 : amplitude * Math.sin(2 * Math.PI * (frequency * t + phase)) * envelope(t);
	};
}

export function shake(frequency = 12, amplitude = 1, seed = 0, offset = 0, decay = 2, attack = 0.1) {
	validate(frequency, amplitude, offset);
	// FX.cs takes the seed as an int.
	if (!Number.isInteger(seed)) throw new RangeError('Invalid effect seed');
	const envelope = attackRelease(attack, decay);
	return (progress: number) => {
		const t = unit(progress);
		return t === 1 ? 0 : amplitude * noise(offset + frequency * t, seed) * envelope(t);
	};
}

export function breathe(frequency = 1, amplitude = 1, phase = 0) {
	validate(frequency, amplitude, phase);
	return (progress: number) => amplitude * (0.5 - 0.5 * Math.cos(2 * Math.PI * (frequency * unit(progress) + phase)));
}

// Value noise on a periodic 20-bit lattice; the hash wraps in unsigned 32-bit arithmetic, as in FX.cs.
function noise(position: number, seed: number) {
	const cell = Math.floor(position);
	const index = cell % 1048576;
	const a = lattice(index, seed);
	return a + (lattice(index + 1, seed) - a) * smoother(position - cell);
}

function lattice(index: number, seed: number) {
	let h = (Math.imul(index & 0xfffff, 374761393) + Math.imul(seed, 668265263)) >>> 0;
	h = Math.imul(h ^ (h >>> 13), 1274126177) >>> 0;
	h = (h ^ (h >>> 16)) >>> 0;
	return (h / 4294967295) * 2 - 1;
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
