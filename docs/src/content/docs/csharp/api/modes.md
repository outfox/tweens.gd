---
title: Modes and callbacks
description: Process, time scale, and pause modes, callback suppression, and the callbacks each playback runs.
---

When a tween advances, and the callbacks it runs while it plays.

## Modes

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ProcessMode` | `TweenProcessMode` | `Process` | Update on [process or physics](/csharp/timing/#process-and-physics) frames |
| `UseUnscaledTime` | `bool` | `false` | Ignore `Engine.TimeScale` |
| `PauseMode` | `TweenPauseMode` | `Bound` | Which [pause](/csharp/lifetime/#pausing) the tween follows |
| `SuppressCallbacksWhenTargetInvalid` | `bool` | `false` | Skip the ending callbacks when the target or owner is gone |

## Callbacks

Each callback receives the `TweenInstance<TTarget, TValue>` handle; `OnUpdate`
also receives the value it wrote. All default to `null`. See
[callbacks](/csharp/playback/#callbacks).

| Member | Runs |
| --- | --- |
| `OnAdd` | When the tween is added |
| `OnStart` | Once, when playback begins after the delay |
| `OnUpdate` | After each write |
| `OnEnd` | On natural completion |
| `OnCancel` | When playback stops early: cancelled, target freed, owner exited, or runner disposed |
| `OnFinally` | Last, in every case, including faults |
