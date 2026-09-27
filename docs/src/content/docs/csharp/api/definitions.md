---
title: Creating definitions
description: The built-in C# definition structs, their constructors, shared TweenOptions, and the interfaces that start them.
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

## Shared options

`TweenOptions` is a readonly record struct that holds the
[timing, easing](/csharp/api/timing/), and [mode](/csharp/api/modes/) members,
plus `Skew`, `FactorDuration`, and `DeltaDuration`. Assign it to a
definition's `Options`, or pass it to a shorthand method. A definition's flat
members, such as `Duration`, read and write that same value, so assign `Options`
first in an initializer: a later `Options` assignment replaces every timing
setting listed before it.

## Interfaces

`ITweenDefinition<TTarget, TValue>` connects definitions to typed playback;
`ITweenDefinition<TTarget>` lets groups mix value types. `TTarget` is a class and
`TValue` is a struct. See [custom tweens](/csharp/custom-tweens/) to implement
property operations or per-playback bindings.

## Members

Every definition has the same members, listed by role:

- [Endpoints and variations](/csharp/api/endpoints/): `From`, `To`, `By`, and the
  factors, deltas, and skew that derive variants.
- [Timing and easing](/csharp/api/timing/): `Duration`, `Delay`, `Repeats`, `Fill`,
  `Ease`, and the rest of the timeline.
- [Modes and callbacks](/csharp/api/modes/): process, time scale, and pause modes,
  and `OnAdd` through `OnFinally`.
