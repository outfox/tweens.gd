---
title: Endpoints and variations
description: Where a tween starts and ends, the relative By offset, and the factors, deltas, and Skew/Weks that derive variants.
tableOfContents: true
---

Where a tween starts and ends, and how a variant derives from those values.

## Endpoints

`TValue` is the property's value type, such as `Vector2` for a position. See
[reusable definitions](/csharp/definitions/#leave-out-from-or-to).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `From` | `TValue?` | `null` | Start value; with no `From`, the tween uses the property's value at start |
| `To` | `TValue?` | `null` | End value; with no `To`, the tween uses the property's value at start |
| `By` | `TValue?` | `null` | [Offset](/csharp/definitions/#move-by-an-offset-with-by) instead of `To`, added on top of other changes to the property while it plays |

## Variations

These derive a variant from a definition's values instead of replacing them:
when the tween starts, each value becomes factor × value + delta. See
[variations](/csharp/variations/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `FactorFrom`, `FactorTo`, `FactorBy` | `double` | `1` | Multiply `From`, `To`, or `By` when the tween starts |
| `DeltaFrom`, `DeltaTo`, `DeltaBy` | `TValue?` | `null` | Then add this; `null` adds nothing |
| `FactorDuration` | `double` | `1` | Multiply `Duration` |
| `DeltaDuration` | `Duration` | `0` | Then add these seconds |
| `FactorDelay` | `double` | `1` | Multiply `Delay` |
| `DeltaDelay` | `Duration` | `0` | Then add these seconds, as in a per-start stagger |
| `Skew` | `double` | `0.5` | In/Out split in `[0, 1]`: 0 front-loads, 0.5 balances, 1 rear-loads |
| `Weks` | `double` | `0.5` | Independent return split in `[0, 1]`; set to `1 - Skew` to retrace |

## Relative offsets

- Set `To` or `By`, not both. A `To` tween on the same property still sets it
  outright.
- With `From`, the tween runs from `From` to `From` plus `By`, like a `To` tween.
- Each repeat adds `By` again, so `Repeats = 2` moves three times as far. A
  ping-pong cycle comes back to where it started.
- A `Fill` that doesn't retain the final value takes the offset back out at the
  end, and keeps other changes.
- A quaternion offset rotates about the node's own axes, so the tween ends at
  `start * By`.
- Callback value tweens such as `Tweens.Float` add `By` to their start value.

## Callback endpoints

Callback value tweens such as `Tweens.Float` animate no property. With no `From`
or `To`, they use zero, transparent black, or identity for that endpoint.

## When tweens compete

Two tweens may animate the same property. Each frame, the one started last wins.
Axis and alpha adapters, such as `Tweens.Position2DX` and `Tweens.ModulateAlpha`,
read the other components on every write, so an X tween and a Y tween combine.

## Change the pacing with `Skew`

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

## Rules

- Factors and deltas apply once, when the tween starts. A non-retaining `Fill`
  restores the captured value, not an adjusted one.
- For a quaternion, the factor scales the rotation angle and the delta rotates
  about the node's own axes.
- `FactorBy` and `DeltaBy` need a `By`. `FactorTo` and `DeltaTo` don't apply to
  a `By` tween. Both combinations are rejected.
- Adjusting `From` fixes the start of a `By` tween, as an explicit `From` does.
- The adjusted duration and delay must not be negative, and `Offset` must fit
  within the adjusted duration.
- Factors, `DeltaDuration`, `DeltaDelay`, `Skew`, and `Weks` must be finite.
  Both splits must be in `[0, 1]`, even without ping-pong. Invalid values reject the start.
