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

Use integer flags from `Tweens.In`, `Tweens.Out`, and `Tweens.InOut`. Combine
one In and one Out with `|`: `Tweens.In.SINE | Tweens.Out.CUBIC`.
`Tweens.InOut.SINE` is `Tweens.In.SINE | Tweens.Out.SINE`.
Both sides provide `NONE`, `LINEAR`, `SINE`, `QUAD`, `CUBIC`, `QUART`, `QUINT`,
`EXPO`, `CIRC`, `BACK`, `ELASTIC`, `BOUNCE`, `SMOOTH_STEP`, and `SMOOTHER_STEP`;
`InOut` provides every matching pair except `NONE`. A single leg runs on its own;
two half-duration legs meet through a local Hermite join over 30–70% of progress
after skew. Matching families use their paired profile directly.

`BACK` aliases `BACK10` and `ELASTIC` aliases `ELASTIC10`. Both families also
provide `20`, `30`, `40`, and `50` variants (for example `Tweens.Out.ELASTIC30`).
The number is the peak overshoot percentage of the full tween range, for single
legs and matching pairs.

`Tweens.Ease.LINEAR` (zero) remains the default. Legacy `Tweens.Ease` constants
keep their numeric values and original shapes. Do not OR legacy names with new
flags. [Easing](/gdscript/easing/) includes the composer and migration details.

## Tweens.INFINITE

| Member | Meaning |
| --- | --- |
| `Tweens.INFINITE` | `-1`, for `repeats` that run until cancelled |

## BlendType

Choose `Tweens.BlendType.HERMITE` (default), `SMOOTH_STEP`, or `LINEAR`.
The centered join width defaults to 0.4. See [easing](/gdscript/easing/#choose-the-method-and-width).
