---
title: Enums
description: Every value of FillMode, TweenState, Reason, TweenPauseMode, TweenProcessMode, and EaseType, with its meaning.
tableOfContents: true
---

The enums that definitions and handles use, one value per entry.

## FillMode

What the property shows during the delay and after natural completion. See
[fill and restoration](/csharp/timing/#fill-and-restoration).

| Member | Meaning |
| --- | --- |
| `FillMode.RetainFinalValue` | The default. Leave the property alone during the delay, and keep the final value |
| `FillMode.Both` | Apply `From` during the delay, and keep the final value |
| `FillMode.ApplyFromDuringDelay` | Apply `From` during the delay, and restore the captured value at the end |
| `FillMode.None` | Leave the property alone during the delay, and restore the captured value at the end |

## TweenState

Where a handle's timeline is, from its `State` member.

| Member | Meaning |
| --- | --- |
| `TweenState.Delayed` | Waiting out the delay |
| `TweenState.Playing` | Moving through a leg |
| `TweenState.Interval` | Holding at an endpoint, between legs or cycles |
| `TweenState.Completed` | Reached its natural end |
| `TweenState.Cancelled` | Stopped early |
| `TweenState.Faulted` | Stopped by an exception; see the handle's `Error` |

## Reason

Why playback ended, as `End` reports it. See
[why it ended](/csharp/cancellation/#why-it-ended).

| Member | Meaning |
| --- | --- |
| `Reason.Completed` | Reached its natural end |
| `Reason.Cancelled` | `Cancel()` was called |
| `Reason.TargetFreed` | The target was disposed, freed, or queued for deletion |
| `Reason.OwnerExited` | The owner left the scene tree |
| `Reason.RunnerDisposed` | The runner, tree, or manual scheduler shut down |

## TweenPauseMode

Which pause the tween follows. Pausing the handle always stops it. See
[pausing](/csharp/lifetime/#pausing).

| Member | Meaning |
| --- | --- |
| `TweenPauseMode.Bound` | The default. Follow `owner.CanProcess()`, or tree pause without an owner |
| `TweenPauseMode.SceneTree` | Follow tree pause only |
| `TweenPauseMode.Always` | Ignore owner and tree pause |

## TweenProcessMode

Which frames advance the tween. See
[process and physics](/csharp/timing/#process-and-physics).

| Member | Meaning |
| --- | --- |
| `TweenProcessMode.Process` | The default. Advance on process frames |
| `TweenProcessMode.Physics` | Advance on physics frames |

## EaseType

`EaseType` accepts `In`, `Out`, and `InOut` constants. Combine one In and one Out
with `|`, for example `In.Sine | Out.Cubic`. `InOut.Sine` is `In.Sine | Out.Sine`.
Both sides provide `None`, `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`,
`Expo`, `Circ`, `Back`, `Elastic`, `Bounce`, `Jump`, `SmoothStep`, and `SmootherStep`;
`InOut` provides every matching pair except `None`. A single leg runs on its own;
at the neutral split, two half-duration legs meet through a local Makima join
over 40–60% of progress. Skew moves that split; matching families use their paired profile directly.

`Back` aliases `Back30` and `Elastic` aliases `Elastic30`. Both families also
provide `10`, `20`, `30`, `40`, and `50` variants (for example `Out.Elastic30`).
The number is the peak overshoot percentage of the full tween range, for single
legs and matching pairs.

`Bounce` aliases `Bounce30`; `Bounce10` through `Bounce50` set the first
rebound depth to that percentage of the full tween range, in single legs and
matching pairs. The next two rebounds have one-quarter and one-sixteenth of
that depth. All variants are available in `In`, `Out`, and `InOut`.

`Jump` aliases `Jump30`; `Jump10` through `Jump50` set the first peak above the
target to that percentage of the full tween range. Three peaks diminish to
one-quarter and one-sixteenth of the first, touching the target between them.
Use `In`, `Out`, or `InOut` for mirrored, outgoing, or paired motion.

`EaseType.Linear` (zero) remains the default. Legacy `EaseType` constants keep
their numeric values and original shapes. Do not OR legacy names with the new
flags. Try the composer in the [easing playground](/easings/).

## Constants

| Member | Meaning |
| --- | --- |
| `TweenOptions.Infinite` | `-1`, for `Repeats` that run until cancelled |

## BlendType

Choose `Makima` (default), `Hermite`, `SmoothStep`, or `Linear`.
The centered join width defaults to 0.2. See [easing](/csharp/api/easing/#choose-the-method-and-width).
