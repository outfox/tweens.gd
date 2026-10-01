---
title: Modes and callbacks
description: Process, time scale, and pause modes, callback suppression, and the callbacks each playback runs.
---

When a tween advances, and the callbacks it runs while it plays.

## Modes

Clock policy belongs to `TweensGdPlaybackOptions`, passed to the start call, rather than to a motion definition. Each Chain has one policy.

```gdscript
var clock := Tweens.playback_options(Tweens.Process.PHYSICS, Tweens.Pause.BOUND, true)
var handle := Tweens.play(sprite, Tweens.position_2d([100, 0], 1.0), null, clock)
```

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `process_mode` | `Tweens.Process` | `PROCESS` | Update on [process or physics](/gdscript/timing/#process-and-physics) frames |
| `use_unscaled_time` | `bool` | `false` | Ignore `Engine.time_scale` |
| `pause_mode` | `Tweens.Pause` | `BOUND` | Which [pause](/gdscript/lifetime/#pausing) the tween follows |
| `suppress_callbacks_when_target_invalid` | `bool` | `false` | Skip the ending callbacks when the target or owner is gone |

`suppress_callbacks_when_target_invalid` remains on the motion definition.

## Callbacks

Callbacks are synchronous Callables that receive the handle; `on_update`
receives `(handle, value)`. See
[control and completion](/gdscript/playback/#callbacks).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `on_add` | `Callable` | `Callable()` | Runs at activation, after capture |
| `on_start` | `Callable` | `Callable()` | Runs once, when the delay ends and playback begins |
| `on_update` | `Callable` | `Callable()` | Runs after each write |
| `on_end` | `Callable` | `Callable()` | Runs on natural completion |
| `on_cancel` | `Callable` | `Callable()` | Runs when playback stops early: cancelled, target freed, owner exited, or runner disposed |
| `on_finally` | `Callable` | `Callable()` | Runs last, in every case, including failures |

Pending cancellation and failed preparation run no playback callbacks. Release runs only for prepared adapters.
