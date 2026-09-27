---
title: Definitions
description: Every field of a GDScript definition, grouped as endpoints, variations, timing, easing, modes, and callbacks, plus the factories, with_ methods, and constants.
---

A `TweensGdDefinition` describes one motion. It's a mutable object: each start
snapshots it, so changing a definition affects only later starts. To vary one
start, start a copy: `copy()` returns an unchanged one, and the
[`with_*()` methods](#with_-methods) return one with a field changed. See
[reusable definitions](/gdscript/definitions/).

## Factories

Each factory returns a new `TweensGdDefinition`. `easing` is a `Tweens.Ease`
constant, such as `Tweens.Ease.CUBIC_OUT`.

| Factory | Purpose |
| --- | --- |
| `Tweens.position_2d(to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` and the other named helpers | Tween a known property, with target and value checks; see the [helper catalog](/gdscript/nodes/) |
| `Tweens.property(path, to, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween any property, or a component path such as `^"position:x"` |
| `Tweens.value(from, to, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Deliver values to `on_update` without writing a property |
| `Tweens.shader_parameter(parameter, to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween a `ShaderMaterial` uniform |
| `Tweens.instance_shader_parameter(parameter, to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween an `instance uniform` on a `CanvasItem` or `GeometryInstance3D` |
| `Tweens.custom(getter, setter, to, seconds = 0.0, interpolator = Callable(), validator = Callable())` | Read and write your own storage through Callables |

## `with_*()` methods

Every field below has a `with_*()` method that returns a copy with that field
changed. The methods are named after the fields, with two shortenings:

- Endpoint fields drop `_value`: `with_from()`, `with_to()`, and `with_by()`.
  `with_initial_value()` keeps its name.
- `with_ping_pong()` and `with_unscaled_time()` drop `use_`. They and
  `with_suppress_callbacks_when_target_invalid()` default to `true`.

They chain, as in `pop.with_delay(0.1).with_duration(0.4)`.

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

## Timing

See [timing and loops](/gdscript/timing/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `duration` | `float` | `0.0` | Seconds per leg |
| `delay` | `float` | `0.0` | Seconds before the first leg |
| `offset` | `float` | `0.0` | Seconds to skip at the start of the first leg |
| `repeats` | `int` | `0` | Cycles after the first; `Tweens.INFINITE` repeats until cancelled |
| `use_ping_pong` | `bool` | `false` | Play each cycle forward, then back |
| `ping_pong_interval` | `float` | `0.0` | Seconds to wait before returning |
| `repeat_interval` | `float` | `0.0` | Seconds between cycles |
| `fill` | `Tweens.Fill` | `RETAIN_FINAL_VALUE` | What the property shows during the delay and after the end |

## Easing

See [easing](/gdscript/easing/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ease` | `Tweens.Ease` | `LINEAR` | One of the 33 built-in eases |
| `ease_function` | `Callable` | `Callable()` | Maps normalized time to a weight; overrides `ease` |
| `curve` | `Curve` | `null` | Godot curve that overrides `ease`; set it or `ease_function`, not both |

## Modes

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `process_mode` | `Tweens.Process` | `PROCESS` | Update on [process or physics](/gdscript/timing/#process-and-physics) frames |
| `use_unscaled_time` | `bool` | `false` | Ignore `Engine.time_scale` |
| `pause_mode` | `Tweens.Pause` | `BOUND` | Which [pause](/gdscript/lifetime/#pausing) the tween follows |
| `suppress_callbacks_when_target_invalid` | `bool` | `false` | Skip the ending callbacks when the target or owner is gone |

## Callbacks

Callbacks are synchronous Callables that receive the handle; `on_update`
receives `(handle, value)`. See
[control and completion](/gdscript/playback/#callbacks).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `on_add` | `Callable` | `Callable()` | Runs when the tween is added |
| `on_start` | `Callable` | `Callable()` | Runs once, when the delay ends and playback begins |
| `on_update` | `Callable` | `Callable()` | Runs after each write |
| `on_end` | `Callable` | `Callable()` | Runs on natural completion |
| `on_cancel` | `Callable` | `Callable()` | Runs when playback stops early: cancelled, target freed, owner exited, or runner disposed |
| `on_finally` | `Callable` | `Callable()` | Runs last, in every case, including failures |

## Target fields

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `property` | `NodePath` | `^""` | Property path, set by `Tweens.property()` and the named helpers |
| `adapter` | `TweensGdAdapter` | `null` | [Adapter](/gdscript/api/custom/) for other storage; use it or `property`, not both |
| `target_class` | `StringName` | `&""` | Target class that a named helper checks at start |
| `value_type` | `int` | `TYPE_NIL` | Value type that a named helper checks at start |

`definition.validate()` returns an empty string when the configuration is valid,
or a message describing the first problem.

## Constants

| Constant | Values |
| --- | --- |
| `Tweens.INFINITE` | `-1`, for `repeats` that run until cancelled |
| `Tweens.Ease` | `LINEAR`, `SMOOTH_STEP`, `SMOOTHER_STEP`, and `IN`, `OUT`, `IN_OUT` forms of ten families, listed under [easing](/gdscript/easing/) |
| `Tweens.Fill` | `NONE`, `APPLY_FROM_DURING_DELAY`, `RETAIN_FINAL_VALUE`, `BOTH` |
| `Tweens.Process` | `PROCESS`, `PHYSICS` |
| `Tweens.Pause` | `BOUND`, `SCENE_TREE`, `ALWAYS` |
| `Tweens.State` | `DELAYED`, `PLAYING`, `INTERVAL`, `COMPLETED`, `CANCELLED`, `FAULTED` |
| `Tweens.Reason` | `COMPLETED`, `CANCELLED`, `TARGET_FREED`, `OWNER_EXITED`, `RUNNER_DISPOSED`, `FAILED`, `WAIT_CANCELLED` |
