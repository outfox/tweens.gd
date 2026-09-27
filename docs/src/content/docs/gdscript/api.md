---
title: Core API
description: The functions that start GDScript playback, and where the rest of the core API is listed.
---

This page lists the functions that start playback. The pages after it list every
field of the definitions you start and every member of the handles you get back.

Preload the addon's entry script, or use its global class name `TweensGd` once the
editor has generated the global class cache:

```gdscript
const Tweens = preload("res://addons/tweens_gd/tweens.gd")
```

The examples on this site use the `Tweens` constant. The classes it returns are
also registered globally: `TweensGdDefinition`, `TweensGdHandle`, `TweensGdGroup`,
`TweensGdScheduler`, `TweensGdAdapter`, and `TweensGdCancellation`. Use the API
on Godot's main thread.

## Start playback

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Tweens.play(target, definition, owner = null)` | `TweensGdHandle` | Start one definition; a node target is its own owner |
| `Tweens.play_all(target, definitions, owner = null)` | `TweensGdGroup` | Start an array of definitions on one target as one step |

A resource or other non-node target needs an `owner`: an in-tree `Node`, or a
`SceneTree`, which binds playback to the tree's root. See
[materials](/gdscript/materials/).

`play()` and `play_all()` never return `null`. A rejected start returns a handle
or group that has already ended with `Tweens.Reason.FAILED`, so
`await Tweens.play(target, definition).end` needs no null check. `play_all()`
stops at the first rejected start and cancels the definitions it already started.

## Group and cancel

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Tweens.group(handles)` | `TweensGdGroup` | Treat handles that are already playing as one step |
| `Tweens.Group.of(handles)` | `TweensGdGroup` | The same as `Tweens.group()` |
| `Tweens.cancel_tweens(owner, include_children = false)` | `void` | Cancel the automatic playback this node owns |

## Find a member

- [Definitions](/gdscript/api/definitions/): factories and `with_*()` methods,
  then every field by role: endpoints, variations, timing, easing, modes, and
  callbacks. Also the constants.
- [Handles and groups](/gdscript/api/handles/): what starting returns, and how
  to control it, read its state, and await it.
- [Scheduler](/gdscript/api/scheduler/): `TweensGdScheduler`, to advance
  playback yourself.
- [Adapters](/gdscript/api/custom/): the adapter base class, for custom storage
  and bindings.
- [Helper catalog](/gdscript/nodes/): every named helper and the property it
  animates.

## Differences from C#

Both implementations share their timing, easing, grouping, and lifetime rules,
and shared fixtures test both. The differences come from the languages:

| Area | C# | GDScript |
| --- | --- | --- |
| Definitions | Immutable values, varied with `with` | Mutable objects, varied with `with_*()` copies |
| Failures | Awaiting faults with an exception; `Error` holds it | Awaiting `end` returns `Tweens.Reason.FAILED`; `error` holds a message |
| Type checks | Generic `TweenInstance<TTarget, TValue>`, checked by the compiler | Target class and value type checked when playback starts |
| Integer values | Saturate at 32-bit limits | Saturate at signed 64-bit limits; shader integers stay 32-bit |
| Script errors | Exceptions from callbacks and setters fault the tween | Detected problems become `FAILED`; other errors in callbacks and setters stay Godot script errors |
| Callbacks | Synchronous | Synchronous; don't `await` inside one, await `end` from a separate coroutine instead |

The addon detects invalid configuration, stale Callables, invalid easing results,
and non-finite interpolation. Custom adapters report recoverable failures by
returning an error string from their hooks. See [compatibility](/compatibility/)
for validated platforms.
