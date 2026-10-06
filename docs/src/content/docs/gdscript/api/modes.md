---
title: Modes and callbacks
tableOfContents: true
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

## Callback order

Starting a tween snapshots its configuration. Preparation and capture happen at
activation, before `on_add`.

Callbacks run synchronously, in this order:

1. `on_add(handle)` at activation after capture.
2. `on_update(handle, value)` with `from_value`, only when the fill mode applies
   it during the delay.
3. `on_start(handle)` once, when the delay ends and playback begins.
4. `on_update(handle, value)` at each sampled timeline boundary and eligible update,
   plus once more when completion [restores the initial value](/gdscript/timing/#fill-and-restoration).
5. `on_end(handle)` on natural completion, or `on_cancel(handle)` when playback
   stops early.
6. `on_finally(handle)` in every case, including failures.

The handle's terminal state is visible inside terminal callbacks, and awaiting `end`
resumes after them. A long frame doesn't replay the callbacks of skipped cycles.
Tweens started in callbacks or after an await are independent roots: they begin
on the next eligible update with no inherited frame time. Use a
[Chain](/gdscript/sequences/) for linked timing.

:::caution[Don't `await` inside callbacks]
Callbacks must return before playback continues. Put anything that awaits in a
separate coroutine that awaits `end`.
:::
