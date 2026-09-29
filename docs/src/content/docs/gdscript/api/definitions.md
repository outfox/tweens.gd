---
title: Creating definitions
description: The GDScript factories and named helpers, the with_ methods that vary a copy, and the fields that choose what a definition animates.
---

A `TweensGdDefinition` describes one motion. It's a mutable object: each start
snapshots it, so changing a definition affects only later starts. To vary one
start, start a copy: `copy()` returns an unchanged one, and the
[`with_*()` methods](#with_-methods) return one with a field changed. See
[reusable definitions](/gdscript/definitions/).

## Factories

Each factory returns a new `TweensGdDefinition`. `easing` accepts integer flags,
such as `Tweens.In.SINE | Tweens.Out.CUBIC`, or a matching pair such as
`Tweens.InOut.SINE`. Legacy `Tweens.Ease` constants also work.

| Factory | Purpose |
| --- | --- |
| `Tweens.position_2d(to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` and the other named helpers | Tween a known property, with target and value checks; see the [helper catalog](/gdscript/nodes/) |
| `Tweens.property(path, to, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween any property, or a component path such as `^"position:x"` |
| `Tweens.value(from, to, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Deliver values to `on_update` without writing a property |
| `Tweens.shader_parameter(parameter, to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween a `ShaderMaterial` uniform |
| `Tweens.instance_shader_parameter(parameter, to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)` | Tween an `instance uniform` on a `CanvasItem` or `GeometryInstance3D` |
| `Tweens.custom(getter, setter, to, seconds = 0.0, interpolator = Callable(), validator = Callable())` | Read and write your own storage through Callables |

## `with_*()` methods

Every definition field has a `with_*()` method that returns a copy with that field
changed. The methods are named after the fields, with two shortenings:

- Endpoint fields drop `_value`: `with_from()`, `with_to()`, and `with_by()`.
  `with_initial_value()` keeps its name.
- `with_ping_pong()` and `with_unscaled_time()` drop `use_`. They and
  `with_suppress_callbacks_when_target_invalid()` default to `true`.

They chain, as in `pop.with_delay(0.1).with_duration(0.4)`.

## Target fields

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `property` | `NodePath` | `^""` | Property path, set by `Tweens.property()` and the named helpers |
| `adapter` | `TweensGdAdapter` | `null` | [Adapter](/gdscript/api/custom/) for other storage; use it or `property`, not both |
| `target_class` | `StringName` | `&""` | Target class that a named helper checks at start |
| `value_type` | `int` | `TYPE_NIL` | Value type that a named helper checks at start |

`definition.validate()` returns an empty string when the configuration is valid,
or a message describing the first problem.

## Fields

Every definition has the same fields, listed by role:

- [Endpoints and variations](/gdscript/api/endpoints/): `from_value`, `to_value`,
  `by_value`, and the factors, deltas, and skew/weks that derive variants.
- [Timing and easing](/gdscript/api/timing/): `duration`, `delay`, `repeats`,
  `fill`, `ease`, and the rest of the timeline.
- [Modes and callbacks](/gdscript/api/modes/): process, time scale, and pause
  modes, and `on_add` through `on_finally`.
