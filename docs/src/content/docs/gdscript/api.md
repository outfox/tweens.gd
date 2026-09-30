---
title: Core API
description: The functions that start GDScript playback, and where the rest of the core API is listed.
---

This page lists the functions that start playback. The pages after it list every
field of the definitions you start and every member of the handles you get back.

Use the addon's global class `Tweens` once the editor has imported the addon;
no preload is needed. The classes the API uses come from the addon's GDExtension and
are global too: `TweensGdDefinition`, `TweensGdHandle`, `TweensGdGroup`,
`TweensGdScheduler`, and `TweensGdCancellation`. Use these names in type annotations.
`TweensGdAdapter`, the adapter base class, is a GDScript class. Use the API on
Godot's main thread.

The C# API uses the same model: reusable definitions, independent playback
handles, groups for parallel steps, and ordinary awaits for sequences.
`Tweens.play_all(node, [a, b])` corresponds to `node.Tween(a, b)`;
`TweensGdGroup.of([a, b])` corresponds to `Group.Of(a, b)`. Member names map from
snake_case to PascalCase; `from_value`, `to_value`, and `by_value` become `From`,
`To`, and `By`. C# faults throw; GDScript reports `FAILED` and an `error` string.
See the [C# core API](/csharp/api/) for its entry points.

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
| `TweensGdGroup.of(handles)` | `TweensGdGroup` | The same as `Tweens.group()` |
| `Tweens.cancel_tweens(owner, include_children = false)` | `void` | Cancel the automatic playback this node owns |

## Find a member

| Page | What it lists |
| --- | --- |
| [Creating definitions](/gdscript/api/definitions/) | The factories and named helpers, and the `with_*()` methods |
| [Endpoints and variations](/gdscript/api/endpoints/) | `from_value`, `to_value`, `by_value`, and the factors, deltas, and skew/weks that derive variants |
| [Timing and easing](/gdscript/api/timing/) | `duration`, `delay`, `repeats`, ping-pong, `fill`, and the easing fields |
| [Easing reference](/gdscript/api/easing/) | Curve families, composition, calibration, and join methods |
| [Effect factories](/gdscript/api/effects/) | Punch, shake, and breathe functions |
| [Modes and callbacks](/gdscript/api/modes/) | Process, time scale, and pause modes, and `on_add` through `on_finally` |
| [Constants](/gdscript/api/enums/) | Every value of `Tweens.Fill`, `State`, `Reason`, and the modes |
| [Handles](/gdscript/api/handles/) | What starting returns: control it, read its state, await it, and cancel a wait |
| [Groups](/gdscript/api/groups/) | Several tweens controlled and awaited as one step |
| [Scheduler](/gdscript/api/scheduler/) | `TweensGdScheduler`, to advance playback yourself |
| [Adapters](/gdscript/api/custom/) | The adapter base class, for custom storage and bindings |
| [Helper catalog](/gdscript/nodes/) | Every named helper and the property it animates |

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
