---
title: Definitions
description: The built-in C# definition structs, their constructors, shared TweenOptions, and what each start copies.
tableOfContents: true
---

A property definition is an immutable description of one motion: a `readonly record struct` nested in the `Tweens` class, one per property. Vary a copy with `with`; [reusable definitions](/csharp/definitions/) shows the pattern. [Keyframes](/csharp/keyframes/) define several parallel value curves in one reusable object.

```csharp
var arrive = new Tweens.Position2D((400, 180), 0.6, Out.Cubic); // Constructor
var pulse = new Tweens.Scale2D(1.2, 0.2) { PingPong = true };   // Initializer
var slow = arrive with { Duration = 1.2 };                       // Copy
```

## Constructors

| Constructor | Arguments |
| --- | --- |
| `new Tweens.Position2D(to, duration, ease, delay)` | The endpoint, then optional timing; every [catalog](/csharp/nodes/) definition takes these |
| `new Tweens.ShaderParameter<TValue>(parameter, to, duration, ease, delay)` | The uniform name first; the instance uniform definitions take the same |
| `new Tweens.Property<TTarget, TValue>(getter, setter, interpolate, to, duration, ease, delay)` | The property operations first; see [custom definitions](/csharp/api/custom/) |

The endpoint argument is never `null`. To use the property's value at start instead, leave the arguments out and set members in an initializer: `new Tweens.Position2D { Duration = 0.4 }`. The [endpoint page](/csharp/api/endpoints/#endpoint-forms) lists the tuple, array, and color forms an endpoint also accepts.

## Members

Every definition has the same members, listed by role:

- [Endpoints & variations](/csharp/api/endpoints/): `From`, `To`, `By`, and their factors and deltas.
- [Timing](/csharp/api/timing/): `Duration`, `Delay`, `Offset`, `Repeats`, `PingPong`, intervals, and `Fill`.
- [Easing](/csharp/api/easing/): `Ease`, `BlendType`, `Blend`, `Skew`, `Weks`, `EaseFunction`, and `Curve`.
- [Color interpolation](/csharp/keyframes/#color-policy): `ColorSpace`, `AlphaMode`, and `ColorEncoding`; defaults are OKLab, premultiplied alpha, and sRGB input/output.
- [Callbacks](/csharp/api/callbacks/): `OnAdd` through `OnFinally`, and `SuppressCallbacksWhenTargetInvalid`.

| Member | Type | Meaning |
| --- | --- | --- |
| `Options` | `TweenOptions` | Every timing and easing member, `SuppressCallbacksWhenTargetInvalid`, and the duration and delay factors, as one value |

## Shared options

### ColorSpace

Whole-color interpolation defaults to `ColorSpace.Oklab`; `Srgb` and `LinearRgb` select other working spaces.

### AlphaMode

`AlphaMode.Premultiplied` is the default. `Straight` interpolates color coordinates and alpha independently.

### ColorEncoding

`ColorEncoding.Srgb` accepts ordinary Godot Colors. `LinearRgb` supports APIs expecting linear values. Input and output use straight alpha. See the [color policy](/csharp/keyframes/#color-policy) for exact endpoints and relative arithmetic.

### Options value

`TweenOptions` is a readonly record struct holding the members of `Options`. A definition's flat members, such as `Duration`, read and write that same value. Assign one to several definitions, list overrides after it, or pass it to a [shorthand method](/csharp/api/start/#shorthand-methods):

```csharp
var snappy = new TweenOptions { Duration = 0.25, Ease = Out.Back };
var slowPop = new Tweens.Scale2D { Options = snappy, Duration = 0.6 };
sprite.TweenPosition((400, 180), 0.5, snappy);
```

:::caution[Order matters]
- Put `Options` first in an initializer. Assigning it replaces every timing setting listed before it, including a duration or ease passed to the constructor.
- Declare shared options above the static definitions that use them. Static fields initialize top to bottom, so a definition declared first copies empty options.
:::

## Interfaces

| Member | Meaning |
| --- | --- |
| `ITweenDefinition<TTarget, TValue>` | Connects a definition to typed playback; `TTarget` is a class and `TValue` a struct |
| `ITweenDefinition<TTarget>` | Lets [groups](/csharp/api/groups/) and [Chains](/csharp/api/chains/) mix value types; contravariant in `TTarget` |

## Copy on start

- Starting snapshots the configuration. Later `with` copies never reach running playback.
- Preparation and property capture happen on the first eligible update, before any positive delay.
- Delegates and the objects they capture stay shared; Godot `Curve` resources are duplicated for each playback.
- A `with` copy allocates nothing. Starting boxes the definition once and allocates its playback state; nothing is copied per frame.
