---
title: Scheduler
description: TweensGdScheduler, the manual scheduler the automatic runner is built on, for tests and non-node objects.
---

The automatic runner is built on `TweensGdScheduler`. Create your own to decide
when playback advances, for example in deterministic tests or to animate objects
that aren't nodes:

```gdscript
var scheduler := TweensGdScheduler.new()
var move := scheduler.add(sprite, Tweens.position_2d(Vector2(100, 0), 1.0))
scheduler.update(0.5) # move.progress is now 0.5.
scheduler.dispose()
```

## Members

| Member | Type | Purpose |
| --- | --- | --- |
| `add(target, definition, owner = null)` | `TweensGdHandle` | Add playback; a node target becomes its own owner |
| `add_all(target, definitions, owner = null)` | `TweensGdGroup` | Add several definitions on one target as one step |
| `update(delta, unscaled_delta = -1.0, mode = Tweens.Process.PROCESS)` | `void` | Advance every tween in the given process mode |
| `active_count` | `int` | Number of tweens that haven't ended |
| `cancel_all()` | `void` | Cancel every tween in this scheduler |
| `cancel_owner(owner, include_children = false)` | `void` | Cancel this scheduler's tweens owned by a node |
| `last_error` | `String` | The latest rejection or failure message |
| `error_reported(message)` | signal | Emitted for each reported error |
| `is_disposed` | `bool` | True after `dispose()` |
| `dispose()` | `void` | Stop and settle the remaining playback |

## Rules

- A manual scheduler can animate objects without an owner. Node targets must
  still be inside the tree.
- `update()` rejects recursive calls and invalid deltas.
- Always dispose a manual scheduler, so remaining tweens end and their callbacks
  run.
- `Tweens.cancel_tweens()` affects the automatic runner only; use
  `cancel_owner()` here.
