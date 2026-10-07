// The Tweens.FX previews at their default settings, shared by EffectPreview's static plot and its animation.
import { breathe, punch, shake } from './motion';

export type Effect = 'punch' | 'shake' | 'breathe';

/** Plot size in SVG units, with vertical padding. */
export const PLOT = { width: 160, height: 64, pad: 7 };

interface Preview {
	/** Seconds of motion, then of rest. */
	play: number;
	rest: number;
	/** The lowest plotted value: breathe rises from zero, punch and shake swing both ways around it. */
	lo: number;
	sample: (t: number) => number;
	/** The sprite's offset in pixels and its scale at progress t. */
	pose: (t: number) => [x: number, y: number, scale: number];
}

const punchX = punch();
const shakeX = shake();
// Shake2D's default offset decorrelates the second axis.
const shakeY = shake(12, 1, 0, 101.37);
const breath = breathe();

export const PREVIEWS: Record<Effect, Preview> = {
	punch: { play: 0.8, rest: 1, lo: -1, sample: punchX, pose: (t) => [punchX(t) * 14, 0, 1] },
	shake: { play: 1, rest: 0.9, lo: -1, sample: shakeX, pose: (t) => [shakeX(t) * 12, shakeY(t) * 12, 1] },
	breathe: { play: 2.4, rest: 0, lo: 0, sample: breath, pose: (t) => [0, 0, 1 + breath(t) * 0.25] },
};

/** The plot's y for a value of the effect. */
export const plotY = (effect: Effect, value: number) => {
	const { lo } = PREVIEWS[effect];
	return PLOT.pad + (1 - (value - lo) / (1 - lo)) * (PLOT.height - 2 * PLOT.pad);
};

/** The plotted point at progress t. */
export const plotPoint = (effect: Effect, t: number): [x: number, y: number] => [
	t * PLOT.width,
	plotY(effect, PREVIEWS[effect].sample(t)),
];
