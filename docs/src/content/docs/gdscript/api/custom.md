---
title: Adapters
description: The adapter base class for custom storage and per-playback bindings, and the Callable-based custom factory.
---

To animate storage that a property path can't reach, extend `Tweens.Adapter`
(`TweensGdAdapter`) and assign an instance to `definition.adapter`, or pass
Callables to `Tweens.custom()`. [Custom adapters](/gdscript/custom-tweens/) walks
through examples.

## Methods to override

Each start works on its own copy of the adapter. Hooks that return a `String`
return an empty one on success and an error message otherwise.

| Method | Returns | Purpose |
| --- | --- | --- |
| `read(target)` | `Variant` | Read the current value |
| `write(target, value)` | `String` | Write a value |
| `prepare(target)` | `String` | Set up per-playback state before the first read |
| `restore(target, initial)` | `String` | Restore at a non-retaining end; writes `initial` by default |
| `release()` | `String` | Free per-playback state, after the ending callbacks |
| `interpolate(from, to, weight)` | `Variant` | Blend two values; `weight` can leave 0 to 1 when an ease overshoots |
| `validate_value(value)` | `String` | Reject unsupported or non-finite values |
| `copy()` | `TweensGdAdapter` | Create the per-playback copy; copies script fields shallowly |

Constructors must take no arguments. By default, `interpolate()` and
`validate_value()` handle every built-in value type.

## Callables instead of a subclass

| Factory | Purpose |
| --- | --- |
| `Tweens.custom(getter, setter, to, seconds = 0.0, interpolator = Callable(), validator = Callable())` | Read with `getter(target)`, write with `setter(target, value)`; the optional `interpolator(from, to, weight)` and `validator(value)` replace the defaults |

The Callables and the objects they capture stay shared between starts.

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
var glow := TweensGdDefinition.new()
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
