---
title: Syntax sugar
description: Shorter ways to write endpoints, easing, and definitions in GDScript.
---

Most tweens fit on one line. Each shortcut here means exactly the same as its
longer form, so pick whichever reads best to you.

Examples use the global `Tweens` class, with an in-tree `Sprite2D` named `sprite`
and a `Label` named `label`.

## Endpoints

An array of numbers can stand in for a vector or a color. These two lines do the
same thing:

```gdscript
Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6))
Tweens.play(sprite, Tweens.position_2d([400, 180], 0.6))
```

| Longer | Shorter |
| --- | --- |
| `Tweens.scale_2d(Vector2(1.2, 1.2), 0.2)` | `Tweens.scale_2d([1.2, 1.2], 0.2)` |
| `Tweens.modulate(Color(1, 0.5, 0), 0.3)` | `Tweens.modulate([1, 0.5, 0], 0.3)` |
| `nudge.with_by(Vector2(80, 0))` | `nudge.with_by([80, 0])` |

A color with three components keeps its alpha at 1. Arrays work in every helper,
in `Tweens.property()`, and in `with_from()`, `with_to()`, `with_by()`, and the
delta methods. When the tween starts, the array becomes the property's type; one
that doesn't fit, such as three numbers for a `Vector2`, rejects the start.
`Tweens.value()` has no property to take a type from, so give it real vectors.

## Easing and delay

Every helper takes an ease and a delay right after the duration:

```gdscript
var longer := Tweens.position_2d([400, 180], 0.6)
longer.ease = Out.CUBIC
longer.delay = 0.2

var shorter := Tweens.position_2d([400, 180], 0.6, Out.CUBIC, 0.2)
```

Combine a start and a finish with `|`, as in `In.SINE | Out.CUBIC`; `InOut.SINE`
is short for `In.SINE | Out.SINE`. `In`, `Out`, and `InOut` are global, like
`Tweens`. The [easing guide](/gdscript/easing/) helps you pick a curve.

## Definitions

A helper returns a definition you can keep and play as often as you like. Chain
`with_*()` calls to vary a copy:

```gdscript
static var pop := Tweens.scale_2d([1.2, 1.2], 0.2, Out.BACK).with_ping_pong()

func celebrate() -> void:
	Tweens.play(sprite, pop)
	Tweens.play(label, pop.with_delay(0.1))
```

A `null` endpoint reads the property when the tween starts, so
`Tweens.position_2d(null, 0.4).with_from([0, 0])` ends where the node was.
