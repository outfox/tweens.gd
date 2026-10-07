---
title: Endpoints & variations
description: Where a C# tween starts and ends, the forms an endpoint accepts, relative By offsets, and the factors and deltas that derive variants.
tableOfContents: true
---

Endpoints say where a tween goes. Variations derive a stronger, slower, or later version from a definition's own values each time it starts. [Anatomy](/csharp/anatomy/#endpoints) and [variations](/csharp/variations/) introduce both.

## Endpoints

`TValue` is the property's value type, such as `Vector2` for a position.

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `From` | `TValue?` | `null` | Start value; `null` uses the property's value at start |
| `To` | `TValue?` | `null` | End value; `null` uses the property's value at start |
| `By` | `TValue?` | `null` | Offset to add instead of a `To`, on top of other changes to the property while it plays |

## Endpoint forms

Built-in constructors and shorthand methods also accept these forms. Components are `double`, so they need no `f` suffix:

| Endpoint | Also accepts | Example |
| --- | --- | --- |
| `float` | Any number | `new Tweens.ModulateAlpha(0.5, 0.2)` |
| `Vector2`, `Vector3`, `Vector4` | A tuple, or a collection of components | `(400, 180)`, `[400, 180]` |
| `Vector2`, `Vector3` scales | One number for every axis | `new Tweens.Scale2D(1.2, 0.2)` |
| `Color` | Three or four components, an HTML code, or a color name | `(1, 0.5, 0)`, `[1, 0.5, 0, 0.8]`, `"#ff8800"`, `"tomato"` |

- Three color components leave the alpha at 1.
- The compiler checks a tuple's component count, and that each component converts to `double`.
- When the tween is created, a collection with the wrong number of components throws an `ArgumentException`, and an unknown color name an `ArgumentOutOfRangeException`.

## Relative offsets

- Set `To` or `By`, not both. A `To` tween on the same property still sets it outright.
- With `From`, the tween runs from `From` to `From` plus `By`, like a `To` tween.
- Each repeat adds `By` again, so `Repeats = 2` moves three times as far. A ping-pong cycle comes back to where it started.
- A `Fill` that doesn't retain the final value takes the offset back out at the end, and keeps other changes.
- A quaternion offset rotates about the node's own axes, so the tween ends at `start * By`.

## Variations

When a tween starts, each of these values becomes factor × value + delta:

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `FactorFrom`, `FactorTo`, `FactorBy` | `double` | `1` | Multiply `From`, `To`, or `By` |
| `DeltaFrom`, `DeltaTo`, `DeltaBy` | `TValue?` | `null` | Then add this; `null` adds nothing |
| `FactorDuration` | `double` | `1` | Multiply `Duration` |
| `DeltaDuration` | `Duration` | `0` | Then add these seconds |
| `FactorDelay` | `double` | `1` | Multiply `Delay` |
| `DeltaDelay` | `Duration` | `0` | Then add these seconds, as in a per-start stagger |

With `To` left out, `FactorTo` scales the value captured at the start.

### Rules

- Factors and deltas apply once, when the tween starts. A non-retaining `Fill` restores the captured value, not an adjusted one.
- For a quaternion, the factor scales the rotation angle and the delta rotates about the node's own axes.
- `FactorBy` and `DeltaBy` need a `By`. `FactorTo` and `DeltaTo` don't apply to a `By` tween. Both combinations are rejected.
- Adjusting `From` fixes the start of a `By` tween, as an explicit `From` does.
- The adjusted duration and delay must not be negative, and `Offset` must fit within the adjusted duration.
- Factors, `DeltaDuration`, and `DeltaDelay` must be finite. Invalid values reject the start.

## Callback endpoints

Callback value definitions such as `Tweens.Float` animate no property; `OnUpdate` receives each value. With no `From` or `To`, they use zero, transparent black, or identity for that endpoint, and add `By` to their start value. The [catalog](/csharp/nodes/values/) lists them.

## When tweens compete

Two tweens may animate the same property. Each update, the one started last wins. Axis and alpha definitions, such as `Tweens.Position2DX` and `Tweens.ModulateAlpha`, read the other components on every write, so an X tween and a Y tween combine.
