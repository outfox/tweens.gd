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
