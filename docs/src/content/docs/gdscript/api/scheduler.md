---
title: Scheduler
description: TweensGdScheduler, the manual GDScript scheduler the automatic runner is built on, for deterministic tests and objects that aren't nodes.
tableOfContents: true
---

A `TweensGdScheduler` advances playback only when you call `update()`, for deterministic tests or objects outside the scene tree. The automatic runner is built on one.

```gdscript
var scheduler := TweensGdScheduler.new()
var move := scheduler.add(sprite, Tweens.position_2d([100, 0], 1.0))
scheduler.update(0.5) # move.progress is now 0.5.
scheduler.dispose()
```

## Members

| Member | Type | Purpose |
| --- | --- | --- |
| `add(target, definition, owner, options)` | `TweensGdHandle` | Start one definition; a node target owns itself, other targets need no owner |
| `add_all(target, definitions, owner, options)` | `TweensGdGroup` | Start an array of definitions as one [group](/gdscript/api/groups/) |
| `add_chain(target, definitions, owner, options)` | `TweensGdChain` | Start an array as one [Chain](/gdscript/api/chains/) |
| `update(delta, unscaled_delta, mode)` | `void` | Advance every tween of `mode`, which defaults to `PROCESS`. Tweens with unscaled time advance by `unscaled_delta`, which defaults to `delta` |
| `active_count` | `int` | Unfinished tweens; a Chain counts once |
| `cancel_all()` | `void` | Cancel every tween in this scheduler |
| `cancel_owner(owner, include_children)` | `void` | Cancel this scheduler's tweens owned by a node, and optionally its descendants' |
| `last_error` | `String` | The latest rejection or failure message |
| `error_reported(message)` | signal | Emitted for each reported error |
| `is_disposed` | `bool` | True after `dispose()` |
| `dispose()` | `void` | Stop and settle the remaining playback |

`owner`, `options`, and `include_children` are optional.

## Plain objects

This example needs no scene or automatic runner. The scheduler advances a model halfway through a one-second motion:

```gdscript title="meter_example.gd"
extends RefCounted

class Meter:
	var value := 0.0

static func sample_midpoint() -> float:
	var meter := Meter.new()
	var scheduler := TweensGdScheduler.new()
	var fill := Tweens.property(^"value", 100.0, 1.0)
	fill.from_value = 0.0
	scheduler.add(meter, fill)
	scheduler.update(0.5)
	scheduler.dispose()
	return meter.value # 50.0 with the default linear easing.
```

## Rules

- A manual scheduler can animate objects without an owner. Node targets must still be inside the tree.
- `update()` rejects invalid deltas, and recursive calls from inside a callback.
- Tweens started during an update first advance on the next one, with no inherited time.
- `Tweens.cancel_tweens()` reaches only the automatic runner; use `cancel_owner()` here.
- Always dispose a manual scheduler, so remaining tweens end and run their callbacks.
