---
title: Creating definitions
description: The built-in C# definition structs, their constructors, shared TweenOptions, and the interfaces that start them.
tableOfContents: true
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
plus `Skew`, `Weks`, `FactorDuration`, and `DeltaDuration`. Assign it to a
definition's `Options`, or pass it to a shorthand method. A definition's flat
members, such as `Duration`, read and write that same value.

To override one setting, list it after `Options`. Shorthand methods accept
options too:

```csharp
var snappy = new TweenOptions { Duration = 0.25, Ease = EaseType.BackOut };
var slowPop = new Tweens.Scale2D { Options = snappy, Duration = 0.6 };
sprite.TweenPosition(new Vector2(400, 180), 0.5, snappy);
```

:::caution[Order matters]
- Put `Options` first in an initializer. Assigning it replaces every timing
  setting listed before it, including a duration or ease passed to the
  constructor.
- Declare shared options above the static definitions that use them. Static
  fields initialize top to bottom, so a definition declared first copies empty
  options.
:::

## Interfaces

`ITweenDefinition<TTarget, TValue>` connects definitions to typed playback;
`ITweenDefinition<TTarget>` lets groups mix value types. `TTarget` is a class and
`TValue` is a struct. See [custom tweens](/csharp/custom-tweens/) to implement
property operations or per-playback bindings.

## Members

Every definition has the same members, listed by role:

- [Endpoints and variations](/csharp/api/endpoints/): `From`, `To`, `By`, and the
  factors, deltas, and Skew/Weks that derive variants.
- [Timing and easing](/csharp/api/timing/): `Duration`, `Delay`, `Repeats`, `Fill`,
  `Ease`, and the rest of the timeline.
- [Modes and callbacks](/csharp/api/modes/): process, time scale, and pause modes,
  and `OnAdd` through `OnFinally`.

## Definition or shorthand?

Every built-in definition also has a typed shorthand method. Both return the same
kind of handle.

| | Definition | Shorthand |
| --- | --- | --- |
| Call | `sprite.Tween(new Tweens.Position2D { ... })` | `sprite.TweenPosition(to, 0.5)` |
| Best for | Motion you name, reuse, or tune | One-off motion next to game logic |
| Configure with | Constructor arguments, an initializer, and `with` | A callback such as `o => o.Ease = ...`, or a `TweenOptions` value |

A shorthand call's duration argument wins over the duration in a `TweenOptions`
value. Reusable configure callbacks take a `TweenOptionsBuilder`.

## What each start copies

Starting a definition snapshots it and captures the property's current value.
Later `with` copies never reach running playback. Delegates and the objects they
capture stay shared, but Godot `Curve` resources are duplicated for each playback.

A `with` copy allocates nothing. Starting boxes the definition once and allocates
its playback state. After that, nothing is copied per frame.
