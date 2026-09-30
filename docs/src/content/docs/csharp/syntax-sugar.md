---
title: Syntax sugar
description: Shorter ways to write endpoints, easing, and definitions in C#.
---

Most tweens fit on one line. Each shortcut here means exactly the same as its
longer form, so pick whichever reads best to you.

Examples use `using Godot;` and `using tweens.gd;`, with an in-tree `Sprite2D`
named `sprite` and a `Label` named `label`.

## Endpoints

Vectors take a tuple or a collection of their components. These three lines do
the same thing:

```csharp
sprite.TweenPosition(new Vector2(400, 180), 0.6);
sprite.TweenPosition((400, 180), 0.6);
sprite.TweenPosition([400, 180], 0.6);
```

Components are `double`, so they need no `f` suffix. Neither do `float`
endpoints: `label.TweenModulateAlpha(0.5, 0.3)`.

| Longer | Shorter |
| --- | --- |
| `sprite.TweenScale(new Vector2(1.2f, 1.2f), 0.2)` | `sprite.TweenScale(1.2, 0.2)` |
| `label.TweenModulate(new Color(1, 0.5f, 0), 0.3)` | `label.TweenModulate((1, 0.5, 0), 0.3)` |
| `label.TweenModulate(new Color("#ff8800"), 0.3)` | `label.TweenModulate("#ff8800", 0.3)` |
| `label.TweenModulate(Colors.Tomato, 0.3)` | `label.TweenModulate("tomato", 0.3)` |

One number on a scale sets every axis. A color with three components keeps its
alpha at 1. The compiler checks a tuple's size; a collection with the wrong
number of components throws an `ArgumentException` when the tween is created.

## Easing and delay

Shorthand methods take an ease and a delay after the duration:

```csharp
sprite.TweenPosition((400, 180), 0.6, options => options.Ease = Out.Cubic);
sprite.TweenPosition((400, 180), 0.6, Out.Cubic);

sprite.TweenPosition((400, 180), 0.6, options => { options.Ease = Out.Cubic; options.Delay = 0.2; });
sprite.TweenPosition((400, 180), 0.6, Out.Cubic, 0.2);
```

Combine a start and a finish with `|`, as in `In.Sine | Out.Cubic`; `InOut.Sine`
is short for `In.Sine | Out.Sine`. For anything else, such as repeats or
callbacks, pass the configure callback or a `TweenOptions` value. The
[easing guide](/csharp/easing/) helps you pick a curve.

## Definitions

`Tweens.*` definitions take the same endpoint forms, in the same order: endpoint,
duration, ease, delay.

```csharp
var longer = new Tweens.Position2D { To = new Vector2(400, 180), Duration = 0.6, Ease = Out.Cubic };
var shorter = new Tweens.Position2D((400, 180), 0.6, Out.Cubic);
```

Set anything else in an initializer after the arguments:
`new Tweens.Scale2D(1.2, 0.2) { UsePingPong = true }`. The constructor always
needs an endpoint. To end where the property was when the tween started, leave
`To` unset instead: `new Tweens.Position2D { From = Vector2.Zero, Duration = 0.4 }`.
