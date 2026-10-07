---
title: Starting playback
description: Every C# call that starts playback on a node or resource, the shorthand methods, PlaybackOptions, and the ownership rules.
tableOfContents: true
---

Starting a definition snapshots it into independent playback and returns a handle. A node owns the tweens started on it; a resource tween needs an owner node or a scene tree. [Lifetime & pausing](/csharp/lifetime/) introduces ownership.

## On a node

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `node.Tween(definition, options)` | `TweenInstance<TTarget, TValue>` | Start one definition |
| `node.Tween(first, second, ...rest)` | `Group` | Start several definitions as one [group](/csharp/api/groups/) |
| `node.Tween(options, first, second, ...rest)` | `Group` | The same, with playback options before the `params` list |
| `node.Tween(definitions, options)` | `Group` | Start a list of definitions as one group |
| `node.Chain(definitions, options)` | `Chain` | Start a list as one [linked timeline](/csharp/api/chains/) |

`options` is an optional [`PlaybackOptions`](#playback-options). `TTarget` is the class the definition targets, which can be a base class of the node: starting a `Tweens.Position2D` on a `Sprite2D` returns a `TweenInstance<Node2D, Vector2>`.

## Shorthand methods

Every catalog definition also has a method that defines and starts it in one call, such as `TweenPosition` or `TweenModulateAlpha`. Each has three overloads:

| Method | Configure with |
| --- | --- |
| `TweenPosition(to, duration, configure, playback)` | A callback that receives the mutable definition before it starts, such as `o => o.Fill = FillMode.Both` |
| `TweenPosition(to, duration, ease, delay, playback)` | An ease and an optional delay |
| `TweenPosition(to, duration, options, playback)` | A shared [`TweenOptions`](/csharp/api/definitions/#shared-options); the `duration` argument wins over its `Duration` |

- `configure` and `playback` are optional. `to` accepts every [endpoint form](/csharp/api/endpoints/#endpoint-forms).
- `configure` receives a class-based definition such as `Position2DTween`. Write reusable configure methods against [`TweenOptionsBuilder`](/csharp/api/custom/#builders).
- Material shorthand methods take a scene tree or owner node after the duration; see the [material catalog](/csharp/nodes/materials/).

## On a resource

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `resource.Tween(definition, tree, owner, options)` | `TweenInstance<TResource, TValue>` | Follow the tree's lifetime, or the optional owner's |
| `resource.Tween(definition, owner, options)` | `TweenInstance<TResource, TValue>` | Stop when the owner leaves the tree |
| `owner.Tween(resource, definition, options)` | `TweenInstance<TResource, TValue>` | The same, written owner first |
| `resource.Chain(definitions, owner, options)` | `Chain` | Start a linked timeline on a resource |

To play several resource tweens as one step, combine their handles with `Group.Of`.

## Group, cancel, count

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Group.Of(tweens)` | `Group` | Treat running tweens, on any targets, as one step |
| `node.CancelTweens(includeChildren)` | `void` | Cancel the automatic playback this node owns, and optionally its descendants' |
| `TweenRuntime.GetActiveCount(node)` | `int` | Count the unfinished tweens of the node's scene tree |

## Playback options

`PlaybackOptions` is a readonly record struct holding one start's clock and pause policy, apart from the reusable motion. A Chain has one policy for every entry.

```csharp
var physics = new PlaybackOptions { ProcessMode = TweenProcessMode.Physics };
sprite.Tween(new Tweens.Position2D((100, 0), 1), physics);
```

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ProcessMode` | `TweenProcessMode` | `Process` | Which frames advance playback |
| `PauseMode` | `TweenPauseMode` | `Bound` | Which pause holds playback |
| `UseUnscaledTime` | `bool` | `false` | Ignore `Engine.TimeScale` |

### TweenProcessMode

| Member | Meaning |
| --- | --- |
| `TweenProcessMode.Process` | The default. Advance on process frames |
| `TweenProcessMode.Physics` | Advance on physics frames |

### TweenPauseMode

| Member | Meaning |
| --- | --- |
| `TweenPauseMode.Bound` | The default. Follow the owner's `CanProcess()`: its process mode and tree pause. Without an owner, follow tree pause |
| `TweenPauseMode.SceneTree` | Follow tree pause only |
| `TweenPauseMode.Always` | Play through any pause |

- Pausing a handle holds it in every mode. `SetProcess(false)` isn't a pause.
- The automatic runner updates at process and physics priority 1000, after nodes with default priority.
- Unscaled process updates use monotonic engine ticks. Unscaled physics updates use `1 / PhysicsTicksPerSecond` per tick, which is simulation time rather than wall-clock time during catch-up.
- Neither clock setting changes pause or ownership.

## Rules

- Start a node tween once the node is inside the tree, in `_Ready` or later; starting outside the tree throws. A node always owns its own tweens, and removing or reparenting it ends them.
- A resource tween ends when its owner leaves the tree, or with its tree. Tweens never duplicate or dispose the resources they animate.
- Starting snapshots the definition; preparation and capture happen on the first eligible update, before any positive delay. See [copy on start](/csharp/api/definitions/#copy-on-start).
- If starting one definition of a group throws, the members already started are cancelled.
- Tweens started in a callback or after an await begin on the next eligible update, with no inherited frame time.
- Start and control tweens on Godot's main thread; other threads throw.
