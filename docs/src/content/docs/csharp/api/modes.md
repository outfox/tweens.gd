---
title: Modes and callbacks
tableOfContents: true
description: Process, time scale, and pause modes, callback suppression, and the callbacks each playback runs.
---

When a tween advances, and the callbacks it runs while it plays.

## Modes

Clock policy belongs to `PlaybackOptions`, passed to the start call, rather than to a motion definition. Each Chain has one policy.

```csharp
var clock = new PlaybackOptions { ProcessMode = TweenProcessMode.Physics, UseUnscaledTime = true };
var handle = sprite.Tween(new Tweens.Position2D((100, 0), 1), clock);
```

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ProcessMode` | `TweenProcessMode` | `Process` | Update on [process or physics](/csharp/lifetime/#process-and-physics) frames |
| `UseUnscaledTime` | `bool` | `false` | Ignore `Engine.TimeScale` |
| `PauseMode` | `TweenPauseMode` | `Bound` | Which [pause](/csharp/lifetime/#pausing) the tween follows |
| `SuppressCallbacksWhenTargetInvalid` | `bool` | `false` | Skip the ending callbacks when the target or owner is gone |

`SuppressCallbacksWhenTargetInvalid` remains on the motion definition.

## Callbacks

Each callback receives the `TweenInstance<TTarget, TValue>` handle; `OnUpdate`
also receives the value it wrote. All default to `null`. See
[callbacks](/csharp/playback/#callbacks).

| Member | Runs |
| --- | --- |
| `OnAdd` | At activation, after capture |
| `OnStart` | Once, when playback begins after the delay |
| `OnUpdate` | After each write |
| `OnEnd` | On natural completion |
| `OnCancel` | When playback stops early: cancelled, target freed, owner exited, or runner disposed |
| `OnFinally` | Last, in every case, including faults |

Pending cancellation and failed preparation run no playback callbacks. Release runs only for prepared adapters.

## Callback order

Starting a tween snapshots its configuration. Preparation and capture happen at
activation, before `OnAdd`.

Callbacks run in this order:

1. `OnAdd` at activation after capture.
2. `OnUpdate` with `From`, only when the fill mode applies it during the delay.
3. `OnStart` once, when the delay ends and playback begins.
4. `OnUpdate` at each sampled timeline boundary and eligible update, plus once more when
   completion [restores the initial value](/csharp/loops/#hold-during-a-delay).
5. `OnEnd` on natural completion, or `OnCancel` when playback stops early.
6. `OnFinally` in every case, including faults.

The handle's terminal state is visible before terminal callbacks run, and each
terminal callback runs at most once. A long frame doesn't replay the callbacks
of skipped cycles. Tweens started in callbacks or after an await are independent
roots: they begin on the next eligible update with no inherited frame time. Use a
[Chain](/csharp/sequences/) for linked timing.
