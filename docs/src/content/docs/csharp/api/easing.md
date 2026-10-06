---
title: Easing reference
description: Curve families, composition, blend algorithms, and migration from legacy eases.
tableOfContents: true
---

For choosing an ease and trying it visually, start with the [easing guide](/easings/).

## Compose an ease

```csharp
sprite.TweenPositionX(300, 1.2, In.Sine | Out.Cubic);
```

`InOut.Sine` is exactly `In.Sine | Out.Sine`. Each selector is an `EaseType`
constant, so it works anywhere an ease is accepted: a shorthand call, a
`TweenOptions` value, a definition, or `Easing.Evaluate`.
`InOut` includes every curve family and numbered variant, such as `InOut.Back30`,
`InOut.Elastic20`, `InOut.Bounce50`, and `InOut.Jump40`.

A single selection such as `Out.Cubic` runs that curve over the whole duration.
`In.None` and `Out.None` omit a leg. `In.Linear` and `Out.Linear` explicitly select
a straight line for that side of a blend; no selections means linear motion.
Choose at most one curve from each side. Multiple different In flags or multiple
different Out flags are rejected.

## Choose a family

| Motion | Try |
| --- | --- |
| Arriving, settling into place | `Out.Cubic`, `Out.Quart` |
| Leaving the screen | `In.Cubic`, `In.Quad` |
| Moving between resting spots | `InOut.Sine`, `InOut.SmootherStep` |
| Winding up, then bouncing into place | `In.Back \| Out.Bounce` |
| A gentle start with a spring finish | `In.Sine \| Out.Elastic` |
| Constant motion | `InOut.Linear` |

Both sides offer `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`,
`Circ`, `Back`, `Elastic`, `Bounce`, `Jump`, `SmoothStep`, and `SmootherStep`.
SmoothStep and SmootherStep are symmetric curves, so their In and Out shapes
are identical. Back, Elastic, and Jump can overshoot. Output weights aren't clamped;
native property limits still apply to constrained values such as alpha or ranges.

Back and Elastic offer **10%, 20%, 30%, 40%, and 50%** peak overshoot:
`Back10` through `Back50`, and `Elastic10` through `Elastic50`.
`Back` aliases `Back30`; `Elastic` aliases `Elastic30`.
Every variant is available in `In`, `Out`, and `InOut`.
For example, use `In.Back20 | Out.Elastic40` or `InOut.Elastic30`.

The percentage measures overshoot relative to the **full tween range**, for a
single leg or a matching pair. An Out tween from 0 to 100 with `Elastic30`
peaks at 130; its In counterpart dips to -30. `InOut.Elastic30` reaches both
extremes. A mixed pair's join can reshape overshoot inside its blend window.
Legacy `EaseType` curves keep their original shapes.

Elastic uses broader oscillations in paired and mixed curves. Single In or Out
curves relax their damping after the main swing so the follow-through stays
visible. Both profiles preserve the named peak percentage and meet their endpoints continuously.

Bounce offers `Bounce10` through `Bounce50`, with `Bounce` aliasing `Bounce30`.
The percentage is the **first rebound's depth** within the full tween range.
A tween from 0 to 100 with `Out.Bounce30` first reaches 100, rebounds to
70, then settles through two smaller bounces. Their depths are one-quarter
and one-sixteenth of the first. `In.Bounce30` mirrors this motion;
`InOut.Bounce30` retains the 30-unit rebound depth in each half.
A mixed pair's join may reshape rebounds inside its blend window.
Legacy `EaseType.BounceOut` retains its original 25% first rebound.

Jump offers `Jump10` through `Jump50`, with `Jump` aliasing `Jump30`.
It rebounds **above the target** in three parabolic arcs. From 0 to 100,
`Out.Jump30` rises directly to 130, lands at 100, then reaches 107.5 and
101.875, landing at 100 after each peak. It stays above the target once it
first reaches it. The numbered percentage sets the first peak; later peaks
are one-quarter and one-sixteenth as high above the target.
`In.Jump30` mirrors that motion. `InOut.Jump30` keeps the same 30%
first-peak height relative to the full tween range in each half. A mixed
pair's join can reshape peaks inside its blend window.

## Choose the method and width

Set `BlendType = BlendType.Makima` and `Blend = 0.2` on the tween definition or options.
The width must be finite and in `[0, 1]`. A smaller window preserves more of
each original leg; a larger window gives the join more room to reshape them.
Width zero directly splices the halves and may produce a velocity jump.

| Method | Behavior |
| --- | --- |
| Makima | Local cubic join with matched velocity; the default |
| Hermite | Local cubic join with matched velocity and midpoint acceleration |
| SmoothStep | Crossfades the two complete family InOut profiles with `u²(3 − 2u)` |
| Linear | Crossfades those profiles with `u`; velocity may jump at the window edges |

All methods preserve the midpoint. SmoothStep and Linear let you compare the
crossfade approach; they can still inherit steep or singular midpoint slopes
from a family such as Circ. Join settings do not alter a legacy ease, a single
leg, a matching pair, or a custom function/Curve.

Sample the same configuration with `Easing.Evaluate(ease, t, BlendType.Makima, 0.2)`.
Both settings also work in immutable `TweenOptions` and generated definitions.

`Skew` moves the In/Out split linearly from 0 to 1. The default, 0.5,
preserves the balanced pair. At 0, the Out profile fills the duration; at 1,
the In profile does. Intermediate values move the split in both time and value.
The blend window follows it and shrinks near either endpoint.

`Weks` independently controls the ping-pong return: 0 front-loads the return,
0.5 preserves it, and 1 rear-loads it. To retrace the outward curve, set
`Weks = 1 - Skew`. Duration, intervals, and raw progress stay unchanged.

These settings apply to paired In/Out flags. A single leg, legacy ease,
custom function, or Godot Curve retains its authored profile. The numbered
overshoot and rebound percentages describe the balanced pair and solo legs;
moving the split also redistributes the paired legs' value ranges.

## How the join works

At the neutral split (0.5), In owns progress and values from 0 to 0.5; Out owns 0.5 to 1. For example,
the Sine halves are `0.5 * SineIn(2 * t)` and
`0.5 + 0.5 * SineOut(2 * t - 1)`. Every pair passes through `(0.5, 0.5)`.
Each family supplies an InOut half profile. Back, Elastic, and Jump are calibrated
to their named overshoot percentage; Bounce uses its first rebound depth.
Other families keep their conventional profiles, including symmetric
SmoothStep and SmootherStep.

**Makima is the default.** It joins the two halves with two cubic segments
inside a centered window. With the default width 0.2, progress up to 0.4 follows the In
half exactly and progress from 0.6 follows the Out half exactly. Inside, the
cubics match the values and velocities at both edges and share one velocity
at the midpoint. No changing crossfade weight contributes extra motion.

Makima sets the midpoint velocity with the modified Akima weights of MATLAB's
[`makima`](https://www.mathworks.com/help/matlab/ref/makima.html): a weighted mean
of the slopes on either side, favoring the half whose slope is steadier and closer
to zero. The result stays between those slopes; for monotone families it adds no
reversals or overshoot. Hermite instead solves the midpoint velocity from equal
acceleration between the cubics, then limits it to prevent new reversals when the
selected families are monotone.

Both give a continuous velocity across the join at regular points of the
source curves. Acceleration can change at the outer edges, and with Makima also
at the midpoint. Authored bounce corners and overshoot outside the window remain
part of the selected curves.

For a window half-width `h`, edge values `y0, y1`, and edge velocities
`v0, v1`, the construction is:

```text
d0 = (0.5 - y0) / h
d1 = (y1 - 0.5) / h
Makima:  w0 = |v1 - d1| + |v1 + d1| / 2
         w1 = |d0 - v0| + |d0 + v0| / 2
         midVelocity = (w0 * d0 + w1 * d1) / (w0 + w1)
Hermite: midVelocity = clamp((3 * (d0 + d1) - v0 - v1) / 4,
                             0, 3 * max(0, min(d0, d1)))
left cubic:  (0.5 - h, y0, v0) -> (0.5, 0.5, midVelocity)
right cubic: (0.5, 0.5, midVelocity) -> (0.5 + h, y1, v1)
```

The edge velocities `v0` and `v1` stand in for the outer slopes that `makima`
takes from neighboring samples.

Matching families bypass the join: **Sine | Sine is exactly SineInOut**, and
Back, Elastic, Bounce, and Jump use their calibrated paired profiles directly. Other matching
families preserve their conventional InOut curves too.

## Migrate existing eases

Use `In.Sine` for `EaseType.SineIn`, `Out.Sine` for `EaseType.SineOut`, and
`InOut.Sine` for `EaseType.SineInOut`; the same naming applies to the other families.
Back, Elastic, and Bounce use the new percentage calibration; other shapes stay the same.
Legacy `EaseType` names and
numeric values remain available. Do not combine legacy names with flags.
New code should use `In`, `Out`, and `InOut`.

## Custom functions and curves

For punch, shake, and breathing effects, use the [FX factories](/csharp/api/effects/).

For a shape the built-in eases don't cover, pass a function that maps normalized
progress to a weight:

```csharp
var eased = sprite.TweenPosition((300, 120), 0.5,
    options => options.EaseFunction = t => t * t);
```

`EaseFunction` overrides `Ease`. A Godot `Curve` can override `Ease` instead. It's
sampled with normalized time from 0 to 1, and each playback gets its own
duplicate. Pick one of `EaseFunction` and `Curve`, since setting both is rejected.
If an easing function throws, the tween faults.
