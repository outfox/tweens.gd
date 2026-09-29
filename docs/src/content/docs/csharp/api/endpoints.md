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
| `From` | `TValue?` | `null` | Start value; `null` reads the property when the tween starts |
| `To` | `TValue?` | `null` | End value; `null` reads the property when the tween starts |
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
| `DeltaDuration` | `double` | `0` | Then add these seconds |
| `FactorDelay` | `double` | `1` | Multiply `Delay` |
| `DeltaDelay` | `double` | `0` | Then add these seconds, as in a per-start stagger |
| `Skew` | `double` | `1` | Forward progress exponent before easing: above 1 starts slower, below 1 faster |
| `Weks` | `double` | `1` | Independent exponent for descending ping-pong return progress before easing; set equal to `Skew` to retrace |

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

Callback value tweens such
as `Tweens.Float` have no property to read, so their omitted endpoints fall back
to zero, transparent black, or identity.

## When tweens compete

Two tweens may animate the same property. Each frame, the one started last wins.
Axis and alpha adapters, such as `Tweens.Position2DX` and `Tweens.ModulateAlpha`,
read the other components on every write, so an X tween and a Y tween combine.
