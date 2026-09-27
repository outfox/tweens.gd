---
title: Definitions
description: Every member of a C# definition, grouped as endpoints, variations, timing, easing, modes, and callbacks, plus constructors, shared options, and enums.
---

A definition describes one motion. The built-in ones are `readonly record struct`
values in the root `Tweens` namespace, one per property; the
[node and value catalog](/csharp/nodes/) lists them. Store them in readonly fields
and vary a copy with `with` when you start one (see
[reusable definitions](/csharp/definitions/)):

```csharp title="Trail.cs"
public partial class Trail : PathFollow2D
{
    static readonly Tweens.PathFollow2DVOffset Offset = new() { To = 20 };

    public void Drift(double seconds, double delay) =>
        this.Tween(Offset with { Duration = seconds, Delay = delay });
}
```

## Constructors

Each definition's constructor takes the endpoint and common timing. Every
argument after the binding is optional:

| Constructor | Arguments |
| --- | --- |
| `new Tweens.Position2D(to, duration, ease, delay)` and the other catalog definitions | `To`, `Duration`, `Ease`, `Delay` |
| `new Tweens.ShaderParameter<TValue>(parameter, to, duration, ease, delay)` and the instance uniform definitions | The uniform name, then the same four |
| `new Tweens.Property<TTarget, TValue>(getter, setter, interpolate, to, duration, ease, delay)` | The property operations, then the same four |

Set anything else in an initializer after the arguments:
`new Tweens.Scale2D(Vector2.One, 0.2) { Fill = FillMode.Both }`.

## Endpoints

`TValue` is the property's value type, such as `Vector2` for a position. See
[reusable definitions](/csharp/definitions/#leave-out-from-or-to).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `From` | `TValue?` | `null` | Start value; `null` reads the property when the tween starts |
| `To` | `TValue?` | `null` | End value; `null` reads the property when the tween starts |
| `By` | `TValue?` | `null` | [Offset](/csharp/definitions/#move-by-an-offset-with-by) instead of `To`, added on top of other changes to the property while it plays |

## Variations

These derive a variant from a definition's values instead of replacing them:
when the tween starts, each value becomes factor × value + delta. See
[variations](/csharp/variations/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `FactorFrom`, `FactorTo`, `FactorBy` | `double` | `1` | Multiply `From`, `To`, or `By` when the tween starts |
| `DeltaFrom`, `DeltaTo`, `DeltaBy` | `TValue?` | `null` | Then add this; `null` adds nothing |
| `FactorDuration` | `double` | `1` | Multiply `Duration` |
| `DeltaDuration` | `double` | `0` | Then add these seconds |
| `Skew` | `double` | `1` | Raise normalized time to this power before easing: above 1 starts slower, below 1 faster |

## Timing

See [timing and loops](/csharp/timing/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Duration` | `double` | `0` | Seconds per leg |
| `Delay` | `double` | `0` | Seconds before the first leg |
| `Offset` | `double` | `0` | Seconds to skip at the start of the first leg |
| `Repeats` | `int` | `0` | Cycles after the first; `TweenOptions.Infinite` repeats until cancelled |
| `UsePingPong` | `bool` | `false` | Play each cycle forward, then back |
| `PingPongInterval` | `double` | `0` | Seconds to wait before returning |
| `RepeatInterval` | `double` | `0` | Seconds between cycles |
| `Fill` | `FillMode` | `RetainFinalValue` | What the property shows during the delay and after the end |

## Easing

See [easing](/csharp/easing/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Ease` | `EaseType` | `Linear` | One of the 33 built-in eases |
| `EaseFunction` | `Func<float, float>?` | `null` | Custom ease that overrides `Ease` |
| `Curve` | `Curve?` | `null` | Godot curve that overrides `Ease`; set it or `EaseFunction`, not both |

## Modes

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ProcessMode` | `TweenProcessMode` | `Process` | Update on [process or physics](/csharp/timing/#process-and-physics) frames |
| `UseUnscaledTime` | `bool` | `false` | Ignore `Engine.TimeScale` |
| `PauseMode` | `TweenPauseMode` | `Bound` | Which [pause](/csharp/lifetime/#pausing) the tween follows |
| `SuppressCallbacksWhenTargetInvalid` | `bool` | `false` | Skip the ending callbacks when the target or owner is gone |

## Callbacks

Each callback receives the `TweenInstance<TTarget, TValue>` handle; `OnUpdate`
also receives the value it wrote. All default to `null`. See
[callbacks](/csharp/playback/#callbacks).

| Member | Runs |
| --- | --- |
| `OnAdd` | When the tween is added |
| `OnStart` | Once, when playback begins after the delay |
| `OnUpdate` | After each write |
| `OnEnd` | On natural completion |
| `OnCancel` | When playback stops early: cancelled, target freed, owner exited, or runner disposed |
| `OnFinally` | Last, in every case, including faults |

## Shared options

`TweenOptions` is a readonly record struct that holds the timing, easing, and
mode members above, plus `Skew`, `FactorDuration`, and `DeltaDuration`. Assign it to a
definition's `Options`, or pass it to a shorthand method. A definition's flat
members, such as `Duration`, read and write that same value, so assign `Options`
first in an initializer: a later `Options` assignment replaces every timing
setting listed before it.

`ITweenDefinition<TTarget, TValue>` connects definitions to typed playback;
`ITweenDefinition<TTarget>` lets groups mix value types. `TTarget` is a class and
`TValue` is a struct. See [custom tweens](/csharp/custom-tweens/) to implement
property operations or per-playback bindings.

## Enums

| Enum | Values |
| --- | --- |
| `EaseType` | `Linear`, `SmoothStep`, `SmootherStep`, and `In`, `Out`, `InOut` forms of ten families, listed under [easing](/csharp/easing/) |
| `FillMode` | `None`, `ApplyFromDuringDelay`, `RetainFinalValue`, `Both` |
| `TweenProcessMode` | `Process`, `Physics` |
| `TweenPauseMode` | `Bound`, `SceneTree`, `Always` |
| `TweenState` | `Delayed`, `Playing`, `Interval`, `Completed`, `Cancelled`, `Faulted` |
| `Reason` | `Completed`, `Cancelled`, `TargetFreed`, `OwnerExited`, `RunnerDisposed` |

`TweenOptions.Infinite` is `-1`, for `Repeats` that run until cancelled.
