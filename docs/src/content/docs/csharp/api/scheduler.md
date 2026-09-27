---
title: Scheduler
description: TweenScheduler, the manual scheduler the automatic runner is built on, for tests and managed targets.
---

The automatic Godot runner is built on `TweenScheduler`. Create your own to
decide when playback advances, for example in deterministic tests or to animate
plain C# objects:

```csharp
using var scheduler = new TweenScheduler();
var move = scheduler.Add(sprite, new Tweens.Position2D(new Vector2(100, 0), 1));
scheduler.Update(0.5); // move.Progress is now 0.5.
```

## Members

| Member | Type | Purpose |
| --- | --- | --- |
| `Add(target, definition)` | `TweenInstance<TTarget, TValue>` | Add playback; a node target becomes its own owner |
| `Add(target, definition, owner)` | `TweenInstance<TTarget, TValue>` | Bind a separate target to an in-tree owner |
| `Update(delta, unscaledDelta = null, mode = TweenProcessMode.Process)` | `void` | Advance every tween in the given process mode |
| `ActiveCount` | `int` | Number of tweens that haven't ended |
| `CancelAll()` | `void` | Cancel every tween in this scheduler |
| `UnhandledException` | `event Action<Exception>` | Receives errors once failing tweens are cleaned up |
| `Dispose()` | `void` | Stop and settle the remaining playback |

## Rules

- Create, update, and dispose a scheduler on the same thread. Native targets
  still need Godot's main thread.
- `Update` rejects recursive calls from inside a callback.
- Ordinary node and resource tweens use the automatic runner; you don't need a
  scheduler for them.
- Dispose the scheduler when you're done, so remaining tweens end and their
  callbacks run.
