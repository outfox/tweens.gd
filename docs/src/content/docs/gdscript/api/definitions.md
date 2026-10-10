---
title: Definitions
description: The GDScript factories and named helpers, the with_ methods that vary a copy, the binding fields, and what each start copies.
tableOfContents: true
---

A `TweensGdDefinition` is a mutable description of one motion. Each start snapshots it, so a change reaches only later starts; `with_*()` methods return a changed copy instead. [Reusable definitions](/gdscript/definitions/) shows the pattern. [Keyframes](/gdscript/keyframes/) define several parallel value curves in one reusable object.

```gdscript
var arrive := Tweens.position_2d([400, 180], 0.6, Out.CUBIC) # Factory
var slow := arrive.with_duration(1.2)                         # Copy
arrive.delay = 0.1                                            # Change
```

## Factories

Each factory returns a new `TweensGdDefinition`.

| Factory | Purpose |
| --- | --- |
| `Tweens.position_2d(to, seconds, easing, delay)` | Tween a known property, checking the target's class and the value's type; every [named helper](/gdscript/nodes/) takes these |
| `Tweens.property(path, to, seconds, easing, delay)` | Tween any property, or a component path such as `^"position:x"` |
| `Tweens.value(from, to, seconds, easing, delay)` | Deliver values to `on_update` without writing a property |
| `Tweens.shader_parameter(parameter, to, seconds, easing, delay)` | Tween a `ShaderMaterial` uniform |
| `Tweens.instance_shader_parameter(parameter, to, seconds, easing, delay)` | Tween an `instance uniform` on a `CanvasItem` or `GeometryInstance3D` |
| `Tweens.custom(getter, setter, interpolator, validator)` | Read and write your own storage through Callables; see [adapters](/gdscript/api/custom/) |

`to` defaults to `null`, which uses the property's value at start. `seconds` and `delay` default to `0.0`, and `easing` to linear; it takes [`In` and `Out` flags](/gdscript/api/easing/) or a legacy `Tweens.Ease` constant. `Tweens.custom()` needs only its getter and setter.

### Helper or property path?

Both create the same type of definition.

| | Named helper | Property path |
| --- | --- | --- |
| Call | `Tweens.position_2d(to, 0.5)` | `Tweens.property(^"position", to, 0.5)` |
| Checked at start | The target's class and the value's type | That the target has the property |
| Reaches | The properties in the [helper catalog](/gdscript/nodes/) | Any property on the target, and components such as `position:x` or `region_rect:size:x` |

Paths select a property and its value components on the target itself. They can't traverse nodes or cross into another object; pass that object as the target instead.

## Fields

Every definition has the same fields, listed by role:

- [Endpoints & variations](/gdscript/api/endpoints/): `from_value`, `to_value`, `by_value`, and their factors and deltas.
- [Timing](/gdscript/api/timing/): `duration`, `delay`, `offset`, `repeats`, `ping_pong`, intervals, and `fill`.
- [Easing](/gdscript/api/easing/): `ease`, `blend_type`, `blend`, `skew`, `weks`, `ease_function`, and `curve`.
- [Color interpolation](/gdscript/keyframes/#color-policy): `color_space`, `alpha_mode`, and `color_encoding`; defaults are OKLab, premultiplied alpha, and sRGB input/output.
- [Callbacks](/gdscript/api/callbacks/): `on_add` through `on_finally`, and `suppress_callbacks_when_target_invalid`.

<span id="color-space"></span>
### color_space

Whole-color interpolation defaults to `Tweens.ColorSpace.OKLAB`; `SRGB` and `LINEAR_RGB` select other working spaces.

<span id="alpha-mode"></span>
### alpha_mode

`Tweens.AlphaMode.PREMULTIPLIED` is the default. `STRAIGHT` interpolates color coordinates and alpha independently.

<span id="color-encoding"></span>
### color_encoding

`Tweens.ColorEncoding.SRGB` accepts ordinary Godot Colors. `LINEAR_RGB` supports APIs expecting linear values. Input and output use straight alpha. See the [color policy](/gdscript/keyframes/#color-policy) for exact endpoints and relative arithmetic.

### Binding fields

The binding fields choose what a definition animates. Factories set them, and they have no `with_*()` methods:

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `property` | `NodePath` | `^""` | Property path, set by `Tweens.property()` and the named helpers |
| `adapter` | `TweensGdAdapter` | `null` | [Adapter](/gdscript/api/custom/) for other storage; use it or `property`, not both |
| `target_class` | `StringName` | `&""` | Target class that a named helper checks at start |
| `value_type` | `int` | `TYPE_NIL` | Value type that a named helper checks at start |

## Share timing and easing

GDScript definitions have no separate options object. To give several definitions the same timing and easing, write a function that sets those fields and returns the definition:

```gdscript
static func snappy(definition: TweensGdDefinition) -> TweensGdDefinition:
	definition.duration = 0.25
	definition.ease = Out.CUBIC
	return definition
```

Change a setting after the shared function has run, as in `snappy(Tweens.scale_2d()).with_duration(0.6)`.

:::caution[Order matters]
- Call the shared function first. It assigns every field it sets, so a value you set before calling it is replaced, including a duration or easing passed to the helper.
- Static variables initialize top to bottom. A static variable that reads another one must be declared after it.
:::

## Methods

| Method | Returns | Meaning |
| --- | --- | --- |
| `with_duration(seconds)` and the other `with_*()` methods | `TweensGdDefinition` | A copy with one field changed |
| `copy()` | `TweensGdDefinition` | An unchanged copy |
| `validate()` | `String` | Empty when the configuration is valid, or a description of the first problem |

- There's one `with_*()` method for every endpoint, variation, timing, easing, and callback field. They chain, as in `pop.with_delay(0.1).with_duration(0.4)`.
- Endpoint methods drop `_value`: `with_from()`, `with_to()`, and `with_by()`. `with_initial_value()` keeps its name.
- `with_ping_pong()` and `with_suppress_callbacks_when_target_invalid()` default to `true`.

## Copy on start

- Starting snapshots the configuration. Later changes to the definition never reach running playback.
- Preparation and property capture happen on the first eligible update, before any positive delay.
- Callables and the objects they capture stay shared; `curve` resources are duplicated for each start.
- Each `copy()` and `with_*()` call creates a new definition object.
