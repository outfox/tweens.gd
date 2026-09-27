---
title: Endpoints and variations
description: Where a tween starts and ends, the relative By offset, and the factors, deltas, and skew that derive variants.
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
| `Skew` | `double` | `1` | Raise normalized time to this power before easing: above 1 starts slower, below 1 faster |
