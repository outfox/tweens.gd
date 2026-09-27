---
title: Custom adapters
description: Animate callback values, script properties, and custom storage, or drive a scheduler yourself.
---

When no named helper covers what you want to animate, use a callback value tween,
a property path, or a custom adapter. `Tweens.custom()` takes Callables for reading
and writing a value; subclasses of `Tweens.Adapter` add preparation and cleanup
hooks for bindings that need them.

The examples assume `const Tweens = preload("res://addons/tweens_gd/tweens.gd")` and
run on Godot's main thread in a method of an in-tree node.

## Callback values

```gdscript
var score := Tweens.value(10.0, 100.0, 1.0)
score.on_update = func(_handle, sample): label.text = str(roundi(sample))
Tweens.play(self, score)
```

The node passed to `Tweens.play()` controls the tween's lifetime, and each value
arrives through `on_update(handle, value)`. `Tweens.value(from, to, seconds)` works
with any supported value type: `float`, `int`, `Vector2`, `Vector3`, `Vector4`,
`Color`, `Quaternion`, and `Rect2`. The named helpers `float_value`,
`double_value`, `vector2_value`, `vector3_value`, `vector4_value`, `color_value`,
`quaternion_value`, and `rect2_value` take the same arguments as the other
helpers and check the value type; their omitted endpoints fall back to zero,
transparent black, or identity. GDScript represents both float and double as
`float`.

## Script properties

`Tweens.property(path, to, seconds)` reads and writes any property the target
exposes, including script variables, so a plain script needs no adapter:

```gdscript title="health_bar.gd"
extends Node2D

const Tweens = preload("res://addons/tweens_gd/tweens.gd")

var fill := 1.0

static var drain := Tweens.property(^"fill", null, 0.4, Tweens.Ease.SMOOTHER_STEP)

func set_health(fraction: float) -> void:
	Tweens.play(self, drain.with_to(fraction))
```

## Drive a scheduler yourself

To animate an object that isn't a node, such as a model in a test, add it to a
`Tweens.Scheduler` and advance it yourself:

```gdscript title="meter_example.gd"
extends RefCounted

const Tweens = preload("res://addons/tweens_gd/tweens.gd")

class Meter:
	var value := 0.0

static func sample_midpoint() -> float:
	var meter := Meter.new()
	var scheduler := Tweens.Scheduler.new()
	var fill := Tweens.property(^"value", 100.0, 1.0)
	fill.from_value = 0.0
	scheduler.add(meter, fill)
	scheduler.update(0.5)
	scheduler.dispose()
	return meter.value # 50.0 with the default linear easing.
```

A manual scheduler can animate objects that aren't nodes without an owner. Drive it
with `update(delta, unscaled_delta = -1.0, mode = Tweens.Process.PROCESS)`; an
omitted `unscaled_delta` uses `delta`. Call `dispose()` when you're finished to
settle any remaining work with `Tweens.Reason.RUNNER_DISPOSED`. A manual scheduler
with no owner has no tree pause policy. Node targets still need to be inside the
tree, and all calls belong on Godot's main thread.

`add()` always returns a handle, already settled with `FAILED` on invalid input.
The scheduler keeps the last message in `last_error` and emits
`error_reported(message)`. `cancel_all()`, `cancel_owner(owner, include_children)`
and `active_count` inspect and control its work. `Tweens.cancel_tweens()` only
cancels automatically scheduled tweens and doesn't reach separate manual
schedulers.

The [scheduler reference](/gdscript/api/scheduler/) lists its members.

## Custom storage with Callables

When the value isn't a property, supply a getter and a setter:

```gdscript
var intensity := Tweens.custom(
	func(target): return target.get_meta(&"intensity", 0.0),
	func(target, value): target.set_meta(&"intensity", value),
	1.0, 0.5)
Tweens.play(self, intensity)
```

`Tweens.custom(getter, setter, to, seconds = 0.0, interpolator, validator)`
returns an ordinary definition, so every timing, easing, and callback field
applies. The getter receives the target; the setter receives the target and the
value. A setter can return an error string to fail playback; returning nothing
means success.

The optional `interpolator(from, to, weight)` replaces the built-in interpolation,
and the optional `validator(value)` returns an empty string on success or a
message on failure. Supply both to animate Variant types beyond the built-in ones.

`by_value`, factors, and deltas ([variations](/gdscript/variations/)) work with
int, float, vector, `Color`, `Quaternion`, and `Rect2` values, whether the value
comes from a property path, `Tweens.custom()`, or an adapter.

## Adapters with bindings

For per-playback bindings, extend `Tweens.Adapter` and assign an instance to the
definition's `adapter` field. Override `read(target)` and `write(target, value)`,
and optionally `prepare(target)`, `restore(target, initial)`, `release()`,
`interpolate(from, to, weight)`, and `validate_value(value)`. A script's
`extends` line needs a path or a global class, so name the adapter script by its
path, or by `TweensGdAdapter` once the editor has built its class cache:

```gdscript
class_name MetaAdapter
extends "res://addons/tweens_gd/adapter.gd"

var key: StringName = &"intensity"

func read(target: Object) -> Variant:
	return target.get_meta(key, 0.0)

func write(target: Object, value: Variant) -> String:
	target.set_meta(key, value)
	return ""
```

```gdscript
var glow := Tweens.Definition.new()
glow.adapter = MetaAdapter.new()
glow.to_value = 1.0
glow.duration = 0.5
Tweens.play(self, glow)
```

Every hook except `read` and `interpolate` returns an error string, empty on
success. A returned message fails playback with `Tweens.Reason.FAILED`, and
several detected failures are kept together in `handle.error`.

Each start owns its own copy of the adapter. The constructor must accept no
arguments. The default `copy()` creates a new adapter and copies its script
variables shallowly, so Arrays, resources, and captured objects stay shared unless
you duplicate them; override `copy()` when configuration needs a different policy.
Initialize private playback state in `prepare()`.

`prepare()` runs on the start's copy before the initial read. `restore()` runs when
a tween ends with a fill mode that doesn't keep the final value, and may write back a
value or remove an override instead; by default it calls `write()`. `release()`
runs after the terminal callbacks and before waiters resume, including after a
failed preparation, and must release only what that copy owns. If a setter or
interpolator cancels its own tween, release waits until that call returns.

:::caution[Script errors aren't failures]
The addon can only report problems it detects: error strings from hooks, invalid
Callables, and non-finite values. A GDScript runtime error inside a hook or
Callable remains an ordinary Godot script error, with no conversion to `FAILED`.
Return an error string for recoverable problems.
:::
