---
title: Easing reference
description: Curve families, composition, blend algorithms, and migration from legacy eases.
tableOfContents: true
---

For choosing an ease and trying it visually, start with the [easing guide](/gdscript/easing/).

## Compose an ease

```gdscript
var move := Tweens.position_2d_x(300.0, 1.2, Tweens.In.SINE | Tweens.Out.CUBIC)
Tweens.play(sprite, move)
```

`Tweens.InOut.SINE` is exactly `Tweens.In.SINE | Tweens.Out.SINE`.
Pass the flags after the duration in any helper, assign them to a definition's
`ease`, use `with_ease()`, or sample them with `Tweens.Easing.evaluate(ease, t)`.
Easing arguments accept integers so flags from either side compose naturally.
`Tweens.InOut` includes every curve family and numbered variant, such as
`Tweens.InOut.BACK30`, `Tweens.InOut.ELASTIC20`, `Tweens.InOut.BOUNCE50`, and `Tweens.InOut.JUMP40`.

A single selection such as `Tweens.Out.CUBIC` runs that curve over the whole duration.
`Tweens.In.NONE` and `Tweens.Out.NONE` omit a leg. `Tweens.In.LINEAR` and
`Tweens.Out.LINEAR` explicitly select a straight line for that side of a blend;
no selections means linear motion. Choose at most one curve from each side.
Multiple different In flags or multiple different Out flags are rejected.

## Choose a family

| Motion | Try |
| --- | --- |
| Arriving, settling into place | `Out.CUBIC`, `Out.QUART` |
| Leaving the screen | `In.CUBIC`, `In.QUAD` |
| Moving between resting spots | `InOut.SINE`, `InOut.SMOOTHER_STEP` |
| Winding up, then bouncing into place | `In.BACK \| Out.BOUNCE` |
| A gentle start with a spring finish | `In.SINE \| Out.ELASTIC` |
| Constant motion | `InOut.LINEAR` |

All selectors above live under `Tweens`. Both sides offer `LINEAR`, `SINE`, `QUAD`,
`CUBIC`, `QUART`, `QUINT`, `EXPO`, `CIRC`, `BACK`, `ELASTIC`, `BOUNCE`, `JUMP`,
`SMOOTH_STEP`, and `SMOOTHER_STEP`. SmoothStep and SmootherStep are symmetric,
so their In and Out shapes are identical. Back, Elastic, and Jump can overshoot.
Output weights aren't clamped; native property setters still apply their own limits.

Back and Elastic offer **10%, 20%, 30%, 40%, and 50%** peak overshoot:
`BACK10` through `BACK50`, and `ELASTIC10` through `ELASTIC50`.
`BACK` aliases `BACK10`; `ELASTIC` aliases `ELASTIC10`.
Every variant is available in `Tweens.In`, `Tweens.Out`, and `Tweens.InOut`.
For example, use `Tweens.In.BACK20 | Tweens.Out.ELASTIC40` or `Tweens.InOut.ELASTIC30`.

The percentage measures overshoot relative to the **full tween range**, for a
single leg or a matching pair. An Out tween from 0 to 100 with `ELASTIC30`
peaks at 130; its In counterpart dips to -30. `InOut.ELASTIC30` reaches both
extremes. A mixed pair's join can reshape overshoot inside its blend window.
Legacy `Tweens.Ease` curves keep their original shapes.

Bounce offers `BOUNCE10` through `BOUNCE50`, with `BOUNCE` aliasing `BOUNCE10`.
The percentage is the **first rebound's depth** within the full tween range.
A tween from 0 to 100 with `Tweens.Out.BOUNCE30` first reaches 100, rebounds to
70, then settles through two smaller bounces. Their depths are one-quarter
and one-sixteenth of the first. `Tweens.In.BOUNCE30` mirrors this motion;
`Tweens.InOut.BOUNCE30` retains the 30-unit rebound depth in each half.
A mixed pair's join may reshape rebounds inside its blend window.
Legacy `Tweens.Ease.BOUNCE_OUT` retains its original 25% first rebound.

Jump offers `JUMP10` through `JUMP50`, with `JUMP` aliasing `JUMP10`.
It rebounds **above the target** in three parabolic arcs. From 0 to 100,
`Tweens.Out.JUMP30` rises directly to 130, lands at 100, then reaches 107.5 and
101.875, landing at 100 after each peak. It stays above the target once it
first reaches it. The numbered percentage sets the first peak; later peaks
are one-quarter and one-sixteenth as high above the target.
`Tweens.In.JUMP30` mirrors that motion. `Tweens.InOut.JUMP30` keeps the same 30%
first-peak height relative to the full tween range in each half. A mixed
pair's join can reshape peaks inside its blend window.

## Choose the method and width

Set `blend_type = Tweens.BlendType.HERMITE` and `blend = 0.2` on the tween definition.
The width must be finite and in `[0, 1]`. A smaller window preserves more of
each original leg; a larger window gives the join more room to reshape them.
Width zero directly splices the halves and may produce a velocity jump.

| Method | Behavior |
| --- | --- |
| Hermite | Local cubic join with matched velocity; the default |
| SmoothStep | Crossfades the two complete family InOut profiles with `u²(3 − 2u)` |
| Linear | Crossfades those profiles with `u`; velocity may jump at the window edges |

All methods preserve the midpoint. SmoothStep and Linear let you compare the
crossfade approach; they can still inherit steep or singular midpoint slopes
from a family such as Circ. Join settings do not alter a legacy ease, a single
leg, a matching pair, or a custom function/Curve.

Sample the same configuration with `Tweens.Easing.evaluate(ease, t, Tweens.BlendType.HERMITE, 0.2)`.
The copy helpers are `with_blend_type()` and `with_blend()`.

Skew warps normalized time **before** the join. A value of 1 leaves time
unchanged; values above 1 delay motion, and values between 0 and 1 bring it
forward. Weks independently warps the ping-pong return. In and Out shape each
traversal; they do not mean forward and ping-pong return.
See [variations](/gdscript/variations/#change-the-pacing-with-skew).

## How the join works

In owns progress and values from 0 to 0.5; Out owns 0.5 to 1. For example,
the Sine halves are `0.5 * SineIn(2 * t)` and
`0.5 + 0.5 * SineOut(2 * t - 1)`. Every pair passes through `(0.5, 0.5)`.
Each family supplies an InOut half profile. Back, Elastic, and Jump are calibrated
to their named overshoot percentage; Bounce uses its first rebound depth.
Other families keep their conventional profiles, including symmetric
SmoothStep and SmootherStep.

**Hermite is the default.** It joins the two halves with two cubic segments
inside a centered window. With the default width 0.2, progress up to 0.4 follows the In
half exactly and progress from 0.6 follows the Out half exactly. Inside, the
cubics match the values and velocities at both edges and share one velocity
at the midpoint. No changing crossfade weight contributes extra motion.

The midpoint velocity is solved from equal acceleration between the cubics,
then limited to prevent new reversals when the selected families are monotone.
This gives a continuous velocity across the join at regular points of the
source curves. Acceleration can change at the outer edges. Authored bounce
corners and overshoot outside the window remain part of the selected curves.

For a window half-width `h`, edge values `y0, y1`, and edge velocities
`v0, v1`, the construction is:

```text
d0 = (0.5 - y0) / h
d1 = (y1 - 0.5) / h
midVelocity = clamp((3 * (d0 + d1) - v0 - v1) / 4,
                    0, 3 * max(0, min(d0, d1)))
left cubic:  (0.5 - h, y0, v0) -> (0.5, 0.5, midVelocity)
right cubic: (0.5, 0.5, midVelocity) -> (0.5 + h, y1, v1)
```

Matching families bypass the join: **Sine | Sine is exactly SineInOut**, and
Back, Elastic, Bounce, and Jump use their calibrated paired profiles directly. Other matching
families preserve their conventional InOut curves too.

## Migrate existing eases

Use `Tweens.In.SINE` for `Tweens.Ease.SINE_IN`, `Tweens.Out.SINE` for
`Tweens.Ease.SINE_OUT`, and `Tweens.InOut.SINE` for `Tweens.Ease.SINE_IN_OUT`;
the same naming applies to the other families. Back, Elastic, and Bounce use the new
percentage calibration; other shapes stay the same. Legacy `Tweens.Ease` names and numeric values remain
available. Do not combine legacy names with flags. New code should use
`Tweens.In`, `Tweens.Out`, and `Tweens.InOut`.

## Custom functions and curves

For punch, shake, and breathing effects, use the [FX factories](/gdscript/api/effects/).

For a shape the built-in eases don't cover, assign a Callable that maps normalized
progress to a weight:

```gdscript
var eased := Tweens.position_2d(Vector2(300, 120), 0.5)
eased.ease_function = func(t: float) -> float: return t * t
Tweens.play(sprite, eased)
```

`ease_function` overrides `ease`. A Godot `Curve` assigned to `curve` can override
`ease` instead. It's sampled with normalized time from 0 to 1, and each start gets
its own copy of the curve. Set only one of `ease_function` and `curve`; a definition
with both is rejected.

The Callable runs synchronously on every update. If it becomes invalid or returns
anything other than a finite number, the tween ends with `Tweens.Reason.FAILED` and
the message in `handle.error`. A script error raised inside the Callable remains an
ordinary Godot script error; the addon can't convert it into a failure reason.
