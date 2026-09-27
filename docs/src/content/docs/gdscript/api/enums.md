---
title: Constants
description: Every value of Tweens.Fill, State, Reason, Pause, Process, and Ease, and Tweens.INFINITE, with its meaning.
---

The constants that definitions and handles use, one value per entry.

## Tweens.Fill

What the property shows during the delay and after natural completion. See
[fill and restoration](/gdscript/timing/#fill-and-restoration).

| Member | Meaning |
| --- | --- |
| `Tweens.Fill.RETAIN_FINAL_VALUE` | The default. Leave the property alone during the delay, and keep the final value |
| `Tweens.Fill.BOTH` | Apply `from_value` during the delay, and keep the final value |
| `Tweens.Fill.APPLY_FROM_DURING_DELAY` | Apply `from_value` during the delay, and restore the captured value at the end |
| `Tweens.Fill.NONE` | Leave the property alone during the delay, and restore the captured value at the end |

## Tweens.State

Where a handle's timeline is, from its `state` member.

| Member | Meaning |
| --- | --- |
| `Tweens.State.DELAYED` | Waiting out the delay |
| `Tweens.State.PLAYING` | Moving through a leg |
| `Tweens.State.INTERVAL` | Holding at an endpoint, between legs or cycles |
| `Tweens.State.COMPLETED` | Reached its natural end |
| `Tweens.State.CANCELLED` | Stopped early |
| `Tweens.State.FAULTED` | Stopped by a detected problem; see the handle's `error` |

## Tweens.Reason

Why playback ended, as `end` and `wait()` report it. See
[why it ended](/gdscript/cancellation/#why-it-ended).

| Member | Meaning |
| --- | --- |
| `Tweens.Reason.COMPLETED` | Reached its natural end |
| `Tweens.Reason.CANCELLED` | `cancel()` was called |
| `Tweens.Reason.TARGET_FREED` | The target was freed or queued for deletion |
| `Tweens.Reason.OWNER_EXITED` | The owner left the scene tree |
| `Tweens.Reason.RUNNER_DISPOSED` | The runner, tree, or manual scheduler shut down |
| `Tweens.Reason.FAILED` | The start was rejected or playback detected a problem; see `error` |
| `Tweens.Reason.WAIT_CANCELLED` | Only from `wait()`: its cancellation token was cancelled, and playback continues |

## Tweens.Pause

Which pause the tween follows. Pausing the handle always stops it. See
[pausing](/gdscript/lifetime/#pausing).

| Member | Meaning |
| --- | --- |
| `Tweens.Pause.BOUND` | The default. Follow `owner.can_process()`, or tree pause without an owner |
| `Tweens.Pause.SCENE_TREE` | Follow tree pause only |
| `Tweens.Pause.ALWAYS` | Ignore owner and tree pause |

## Tweens.Process

Which frames advance the tween. See
[process and physics](/gdscript/timing/#process-and-physics).

| Member | Meaning |
| --- | --- |
| `Tweens.Process.PROCESS` | The default. Advance on process frames |
| `Tweens.Process.PHYSICS` | Advance on physics frames |

## Tweens.Ease

`LINEAR` is the default. `SMOOTH_STEP` and `SMOOTHER_STEP` are S-curves with no
variants. Ten families come in `IN`, `OUT`, and `IN_OUT` forms, such as
`Tweens.Ease.CUBIC_OUT`: `SINE`, `QUAD`, `CUBIC`, `QUART`, `QUINT`, `EXPO`,
`CIRC`, `BACK`, `ELASTIC`, and `BOUNCE`. [Easing](/gdscript/easing/) compares
their curves.

## Tweens.INFINITE

| Member | Meaning |
| --- | --- |
| `Tweens.INFINITE` | `-1`, for `repeats` that run until cancelled |
