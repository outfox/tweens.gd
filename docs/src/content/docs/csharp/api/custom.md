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

## Custom definitions and bindings

Derive from `TweenDefinition<TTarget, TValue>` and implement the protected `Read`,
`Write`, and `Interpolate` methods. `TTarget` is a reference type and `TValue` is a
value type. The `Interpolators` helpers cover the built-in numeric and vector
types:

```csharp title="UniformZoomTween.cs"
// Tweens a camera's zoom as one number, keeping X and Y equal.
public sealed class UniformZoomTween : TweenDefinition<Camera2D, float>
{
    protected override float Read(Camera2D target) => target.Zoom.X;
    protected override void Write(Camera2D target, float value) => target.Zoom = new Vector2(value, value);
    protected override float Interpolate(float from, float to, float weight) => Interpolators.Float(from, to, weight);
}
```

```csharp
camera.Tween(new UniformZoomTween { To = 2, Duration = 0.5, Ease = InOut.SmootherStep });
```

`By`, factors, and deltas ([variations](/csharp/variations/)) work with int,
float, double, vector, `Color`, `Quaternion`, and `Rect2` values. `By` reads the
property back on every frame, so override `ReadsWrittenValue` to return `false`
if `Read` doesn't return what `Write` stored; `By` is then added to the start
value instead.

For per-playback bindings, override `Prepare`, `Restore`, and `Release`.
`Prepare` runs on the playback's private definition snapshot, before its initial
read. `Restore` may write back a property value or remove an override instead.
`Release` also runs after a failed preparation, and it must release only resources
owned by that snapshot. The snapshot is shallow, so reference-valued configuration
on a custom definition stays shared. Don't mutate that shared configuration while
independent playbacks use it.
