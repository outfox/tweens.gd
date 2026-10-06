---
title: Constants
description: Every value of Tweens.Fill, State, Reason, Pause, Process, and Ease, and Tweens.INFINITE, with its meaning.
tableOfContents: true
---

The constants that definitions and handles use, one value per entry.

## Tweens.Fill

What the property shows during the delay and after natural completion. See
[fill and restoration](/gdscript/loops/#hold-during-a-delay).

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
[process and physics](/gdscript/lifetime/#process-and-physics).

| Member | Meaning |
| --- | --- |
| `Tweens.Process.PROCESS` | The default. Advance on process frames |
| `Tweens.Process.PHYSICS` | Advance on physics frames |

## Tweens.Ease

Use integer flags from the global `In`, `Out`, and `InOut` classes. Combine
one In and one Out with `|`: `In.SINE | Out.CUBIC`.
`InOut.SINE` is `In.SINE | Out.SINE`.
Both sides provide `NONE`, `LINEAR`, `SINE`, `QUAD`, `CUBIC`, `QUART`, `QUINT`,
`EXPO`, `CIRC`, `BACK`, `ELASTIC`, `BOUNCE`, `JUMP`, `SMOOTH_STEP`, and `SMOOTHER_STEP`;
`InOut` provides every matching pair except `NONE`. A single leg runs on its own;
at the neutral split, two half-duration legs meet through a local Makima join
over 40–60% of progress. Skew moves that split; matching families use their paired profile directly.

`BACK` aliases `BACK30` and `ELASTIC` aliases `ELASTIC30`. Both families also
provide `10`, `20`, `30`, `40`, and `50` variants (for example `Out.ELASTIC30`).
The number is the peak overshoot percentage of the full tween range, for single
legs and matching pairs.

`BOUNCE` aliases `BOUNCE30`; `BOUNCE10` through `BOUNCE50` set the first
rebound depth to that percentage of the full tween range, in single legs and
matching pairs. The next two rebounds have one-quarter and one-sixteenth of
that depth. All variants are available in `In`, `Out`, and `InOut`.

`JUMP` aliases `JUMP30`; `JUMP10` through `JUMP50` set the first peak above the
target to that percentage of the full tween range. Three peaks diminish to
one-quarter and one-sixteenth of the first, touching the target between them.
Use `In`, `Out`, or `InOut` for mirrored, outgoing, or paired motion.

`Tweens.Ease.LINEAR` (zero) remains the default. Legacy `Tweens.Ease` constants
keep their numeric values and original shapes. Do not OR legacy names with new
flags. Try the composer in the [easing playground](/easings/).

## Tweens.INFINITE

| Member | Meaning |
| --- | --- |
| `Tweens.INFINITE` | `-1`, for `repeats` that run until cancelled |

## BlendType

Choose `BlendType.MAKIMA` (default), `HERMITE`, `SMOOTH_STEP`, or `LINEAR`.
The centered join width defaults to 0.2. See [easing](/gdscript/api/easing/#choose-the-method-and-width).
