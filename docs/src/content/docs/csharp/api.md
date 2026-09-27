---
title: Core API
description: The calls that start C# playback, and where the rest of the core API is listed.
---

This page lists the calls that start playback. The pages after it list every
member of the definitions you start and the handles you get back.

`using tweens.gd;` brings in the extension methods, timing options, and handles.
Definitions such as `Tweens.Position2D` live in the root `Tweens` namespace, so
they need no `using`.

## Start on a node

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `node.Tween(definition)` | `TweenInstance<TTarget, TValue>` | Start one definition on an in-tree node |
| `node.Tween(first, second, ...)` | `Group` | Start several definitions as one step |
| `node.TweenPosition(to, duration, configure)` | `TweenInstance<TTarget, TValue>` | Shorthand for one definition; every catalog definition has one, such as `TweenModulateAlpha` |
| `node.TweenPosition(to, duration, options)` | `TweenInstance<TTarget, TValue>` | Shorthand that copies a shared `TweenOptions`; the `duration` argument wins |

`TTarget` is the class the definition targets, which can be a base class of the
node: `sprite.TweenPosition(...)` returns `TweenInstance<Node2D, Vector2>`.

## Start on a resource

A material or other resource isn't in the tree, so its playback needs a tree or
an owner node that decides when it stops. See [materials](/csharp/materials/).

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `resource.Tween(definition, tree)` | `TweenInstance<TResource, TValue>` | Follow the scene tree's pause and lifetime |
| `resource.Tween(definition, owner)` | `TweenInstance<TResource, TValue>` | Stop when the owner node leaves the tree |
| `owner.Tween(resource, definition)` | `TweenInstance<TResource, TValue>` | The same, written owner first |

## Group and cancel

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Group.Of(tweens)` | `Group` | Treat tweens that are already playing as one step |
| `node.CancelTweens(includeChildren: false)` | `void` | Cancel the tweens this node owns |

## Find a member

| Page | What it lists |
| --- | --- |
| [Creating definitions](/csharp/api/definitions/) | The definition structs, their constructors, and `TweenOptions` |
| [Endpoints and variations](/csharp/api/endpoints/) | `From`, `To`, `By`, and the factors, deltas, and Skew/Weks that derive variants |
| [Timing and easing](/csharp/api/timing/) | `Duration`, `Delay`, `Repeats`, ping-pong, `Fill`, and the easing members |
| [Modes and callbacks](/csharp/api/modes/) | Process, time scale, and pause modes, and `OnAdd` through `OnFinally` |
| [Enums](/csharp/api/enums/) | Every value of `FillMode`, `TweenState`, `Reason`, and the modes |
| [Handles](/csharp/api/handles/) | What starting returns: control it, read its state, await it |
| [Groups](/csharp/api/groups/) | Several tweens controlled and awaited as one step |
| [Scheduler](/csharp/api/scheduler/) | `TweenScheduler`, to advance playback yourself |
| [Custom definitions](/csharp/api/custom/) | The members to override, `Interpolators`, and class-based definitions |
| [Catalog](/csharp/nodes/) | Every built-in definition and its shorthand method |

## Differences from GDScript

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
