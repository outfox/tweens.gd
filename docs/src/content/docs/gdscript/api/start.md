---
title: Starting playback
description: Every GDScript function that starts playback, what a rejected start returns, owners, playback options, and the start rules.
tableOfContents: true
---

Starting a definition snapshots it into independent playback and returns a handle. A node owns the tweens started on it; any other target needs an owner node or a scene tree. [Lifetime & pausing](/gdscript/lifetime/) introduces ownership.

## Start

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Tweens.play(target, definition, owner, options)` | `TweensGdHandle` | Start one definition |
| `Tweens.play_all(target, definitions, owner, options)` | `TweensGdGroup` | Start an array of definitions on one target as one [group](/gdscript/api/groups/) |
| `Tweens.chain(target, definitions, owner, options)` | `TweensGdChain` | Start an array as one [linked timeline](/gdscript/api/chains/) |

`owner` and `options` are optional. Pass `null` as the owner to give options to a node target, which owns itself.

## Rejected starts

`play()`, `play_all()`, and `chain()` never return `null`. A rejected start, such as a freed target, a node outside the tree, or an invalid definition, returns a handle that has already ended with `Tweens.Reason.FAILED`, so `await Tweens.play(target, definition).end` needs no null check.

- A rejected start schedules no work and runs no callbacks. Its `target` and `value` are `null`, its `error` holds the reason, and the automatic runner logs it.
- `play_all()` checks every array entry before starting any. It stops at the first rejected start and cancels the definitions it already started.
- [Named helpers](/gdscript/nodes/) check the target's class and the captured value's type when playback starts, since GDScript has no generic types.

## Owners

| Target | Owner | Playback ends when |
| --- | --- | --- |
| A node | Itself; `owner` stays `null` | The node leaves the tree |
| A resource or other object | An in-tree `Node` | The owner leaves the tree |
| A resource or other object | The `SceneTree` | The tree shuts down |

Tweens never duplicate or dispose the resources they animate. See [materials](/gdscript/materials/) for owner examples.

## Group and cancel

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Tweens.group(handles)` | `TweensGdGroup` | Treat running handles, on any targets, as one step |
| `TweensGdGroup.of(handles)` | `TweensGdGroup` | The same as `Tweens.group()` |
| `Tweens.cancel_tweens(owner, include_children)` | `void` | Cancel the automatic playback this node owns, and optionally its descendants' |

## Playback options

A `TweensGdPlaybackOptions` holds one start's clock and pause policy, apart from the reusable motion. A Chain has one policy for every entry. `Tweens.playback_options(process_mode, pause_mode, use_unscaled_time)` creates one, with each argument optional:

```gdscript
var physics := Tweens.playback_options(Tweens.Process.PHYSICS)
Tweens.play(sprite, Tweens.position_2d([100, 0], 1.0), null, physics)
```

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `process_mode` | `Tweens.Process` | `PROCESS` | Which frames advance playback |
| `pause_mode` | `Tweens.Pause` | `BOUND` | Which pause holds playback |
| `use_unscaled_time` | `bool` | `false` | Ignore `Engine.time_scale` |

### Tweens.Process

| Constant | Meaning |
| --- | --- |
| `Tweens.Process.PROCESS` | The default. Advance on process frames |
| `Tweens.Process.PHYSICS` | Advance on physics frames |

### Tweens.Pause

| Constant | Meaning |
| --- | --- |
| `Tweens.Pause.BOUND` | The default. Follow the owner's `can_process()`: its process mode and tree pause. Without an owner, follow tree pause |
| `Tweens.Pause.SCENE_TREE` | Follow tree pause only |
| `Tweens.Pause.ALWAYS` | Play through any pause |

- Pausing a handle holds it in every mode. `set_process(false)` isn't a pause.
- The automatic runner updates at process and physics priority 1000, after nodes with default priority.
- Unscaled process updates use monotonic engine ticks. Unscaled physics updates use `1 / Engine.physics_ticks_per_second` per tick, which is simulation time rather than wall-clock time during catch-up.
- Neither clock setting changes pause or ownership.

## Rules

- Start a node tween once the node is inside the tree, in `_ready()` or later. A node always owns its own tweens, and removing or reparenting it ends them.
- Starting snapshots the definition; preparation and capture happen on the first eligible update, before any positive delay. See [what each start copies](/gdscript/api/definitions/#what-each-start-copies).
- Tweens started in a callback or after an await begin on the next eligible update, with no inherited frame time.
- Use the API on Godot's main thread. A call from another thread reports an error and does nothing: starts return a handle that has already failed.
