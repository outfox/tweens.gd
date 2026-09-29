---
title: Custom definitions
description: The members to override in TweenDefinition, PropertyTween for delegates, the Interpolators helpers, and the class-based definitions.
tableOfContents: true
---

To animate a value the catalog doesn't cover, derive from
`TweenDefinition<TTarget, TValue>` or pass delegates to `Tweens.Property`. Both
start like any other definition. [Custom tweens](/csharp/custom-tweens/) walks
through examples.

## Members to override

`TweenDefinition<TTarget, TValue>` is abstract. `TTarget` is a class and `TValue`
a struct. Each start works on a private copy of the definition, so per-playback
state belongs in its fields.

| Method | Returns | Purpose |
| --- | --- | --- |
| `Read(target)` | `TValue` | Read the current value; required |
| `Write(target, value)` | `void` | Write a value; required |
| `Interpolate(from, to, weight)` | `TValue` | Blend two values; required. `weight` can leave 0 to 1 when an ease overshoots |
| `Prepare(target)` | `void` | Set up per-playback bindings before the first read |
| `Restore(target, initial)` | `void` | Restore at a non-retaining end; writes `initial` by default, or can remove an override instead |
| `Release()` | `void` | Free this playback's resources, also after a failed `Prepare` |
| `ReadsWrittenValue` | `bool` | Whether `Read` returns what `Write` stored; defaults to `true`. If not, `By` adds to the start value |

All six methods are `protected`, as is `ReadsWrittenValue`. The definition's
public members, from `From` to `OnFinally`, are the same as on the
[built-in definitions](/csharp/api/definitions/).

## Delegates instead of a subclass

| Constructor | Arguments |
| --- | --- |
| `new Tweens.Property<TTarget, TValue>(getter, setter, interpolate, to, duration, ease, delay)` | The property operations, then the optional endpoint and timing |
| `new PropertyTween<TTarget, TValue>(getter, setter, interpolate)` | The same operations, as a mutable class definition |

Captured objects stay shared between playbacks.

## Interpolators

`Interpolators` has one static method per built-in value type, each taking
`(from, to, weight)`. Pass one as the `interpolate` argument, or call it from
your own `Interpolate`.

| Method | Returns | Purpose |
| --- | --- | --- |
| `Float`, `Double` | `float`, `double` | Linear blend; overshoot passes through |
| `Int` | `int` | Rounds half away from zero; overshoot saturates at the `int` limits |
| `Vector2`, `Vector3`, `Vector4`, `Color`, `Rect2` | the same type | Component-wise linear blend |
| `Quaternion` | `Quaternion` | Shortest-path spherical blend; endpoints must be nonzero |

## Class-based definitions

The older `*Tween` classes, such as `Position2DTween`, and your own subclasses of
`TweenDefinition<TTarget, TValue>` are mutable. They inherit
`TweenOptionsBuilder`, the mutable form of `TweenOptions`, and each start
snapshots them. The built-in classes are hidden from IntelliSense, which offers
the `Tweens.*` definitions instead.

A shorthand method's configure callback receives one of these builders:
`TweenPosition` on a `Node2D` passes a `Position2DTween`. Write reusable
configure methods against `TweenOptionsBuilder`, not `TweenOptions`.
