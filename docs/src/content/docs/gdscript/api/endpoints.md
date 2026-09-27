---
title: Endpoints and variations
description: Where a tween starts and ends, the relative By offset, and the factors, deltas, and skew that derive variants.
---

Where a tween starts and ends, and how a variant derives from those values.

## Endpoints

A `null` endpoint reads the property when the tween starts. See
[reusable definitions](/gdscript/definitions/#leave-out-from_value-or-to_value).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `from_value` | `Variant` | `null` | Start value |
| `to_value` | `Variant` | `null` | End value |
| `by_value` | `Variant` | `null` | [Offset](/gdscript/definitions/#move-by-an-offset-with-by_value) instead of `to_value`, added on top of other changes to the property while it plays |
| `initial_value` | `Variant` | `0.0` | Start value of a callback-only tween, which has no property to read |

## Variations

These derive a variant from a definition's values instead of replacing them:
when the tween starts, each value becomes factor × value + delta. See
[variations](/gdscript/variations/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `factor_from`, `factor_to`, `factor_by` | `float` | `1.0` | Multiply `from_value`, `to_value`, or `by_value` |
| `delta_from`, `delta_to`, `delta_by` | `Variant` | `null` | Then add this, in the property's value type; `null` adds nothing |
| `factor_duration` | `float` | `1.0` | Multiply `duration` |
| `delta_duration` | `float` | `0.0` | Then add these seconds |
| `skew` | `float` | `1.0` | Raise normalized time to this power before easing: above 1 starts slower, below 1 faster |
