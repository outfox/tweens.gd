---
title: Endpoints and variations
description: Where a tween starts and ends, the relative By offset, and the factors, deltas, and skew/weks that derive variants.
tableOfContents: true
---

Where a tween starts and ends, and how a variant derives from those values.

## Endpoints

If a definition has no `from_value` or `to_value`, it uses the property's value
when the tween starts for that endpoint. See
[reusable definitions](/gdscript/definitions/#leave-out-from_value-or-to_value).
An array of numbers can stand in for a vector or color endpoint, delta, or offset:
`[400, 180]` for a `Vector2`, and `[1, 0.5, 0]` or `[1, 0.5, 0, 0.8]` for a
`Color`. The start converts it to the captured value's type; an array that
doesn't match rejects the start.

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
| `factor_delay` | `float` | `1.0` | Multiply `delay` |
| `delta_delay` | `float` | `0.0` | Then add these seconds, as in a per-start stagger |
| `skew` | `float` | `0.5` | In/Out split in `[0, 1]`: 0 front-loads, 0.5 balances, 1 rear-loads |
| `weks` | `float` | `0.5` | Independent return split in `[0, 1]`; set to `1 - skew` to retrace |

## Relative offsets

- Set `to_value` or `by_value`, not both. A `to_value` tween on the same
  property still sets it outright.
- With `from_value`, the tween runs from `from_value` to `from_value` plus
  `by_value`, like a `to_value` tween.
- Each repeat adds `by_value` again, so `repeats = 2` moves three times as far.
  A ping-pong cycle comes back to where it started.
- A `fill` that doesn't retain the final value takes the offset back out at the
  end, and keeps other changes.
- A quaternion offset rotates about the node's own axes, so the tween ends at
  `start * by_value`.
- Callback-only definitions add `by_value` to `initial_value`.

## Callback endpoints

Callback-only definitions, such as `Tweens.value()` and `Tweens.float_value()`,
animate no property.
With no `from_value` or `to_value`, they use `initial_value` for that endpoint:
the `from` you pass to `Tweens.value()`, or zero, transparent black, or identity
for the named value helpers.

## When tweens compete

Two tweens may animate the same property. Tweens write in the order they were
started, so each update the one started last wins. Component paths, such as
`position:x` or `modulate:a`, read the other components on every write, so an x
tween and a y tween combine.
