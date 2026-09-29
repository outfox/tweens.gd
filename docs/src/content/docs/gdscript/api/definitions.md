---
title: Creating definitions
description: The GDScript factories and named helpers, the with_ methods that vary a copy, and the fields that choose what a definition animates.
tableOfContents: true
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

## Share timing and easing

GDScript definitions have no separate options object. To give several
definitions the same timing and easing, write a function that sets those fields
and returns the definition:

```gdscript
static func snappy(definition: TweensGdDefinition) -> TweensGdDefinition:
	definition.duration = 0.25
	definition.ease = Tweens.Out.CUBIC
	return definition
```

To override one setting, change it after the shared function has run:

```gdscript
var slow_pop := snappy(Tweens.scale_2d()).with_duration(0.6)
```

A definition that shares its timing with nothing else can take it from the
helper instead: `Tweens.modulate_alpha(0.0, 0.25, Tweens.Ease.BACK_OUT)`.

:::caution[Order matters]
- Call the shared function first. It assigns every field it sets, so a value
  you set before calling it is replaced, including a duration or easing passed
  to the helper.
- Static variables initialize top to bottom. A static variable that reads
  another one must be declared after it.
:::

## Helper or property path?

Every definition is the same type, whether a named helper or `Tweens.property()`
created it.

| | Named helper | Property path |
| --- | --- | --- |
| Call | `Tweens.position_2d(to, 0.5)` | `Tweens.property(^"position", to, 0.5)` |
| Checked at start | The target's class and the value's type | That the target has the property |
| Reaches | The properties in the [helper catalog](/gdscript/nodes/) | Any property on the target, and components such as `position:x` or `region_rect:size:x` |

Paths select a property and its value components on the target itself. They
can't traverse nodes or cross into another object; pass that object as the
target instead.

## What each start copies

Starting a definition snapshots its configuration and captures the property's
current value. Later changes to the definition never reach running playback.
Callables and the objects they capture stay shared, but `curve` resources are
duplicated for each start. Each `copy()` and `with_*()` call creates a new
definition object.
