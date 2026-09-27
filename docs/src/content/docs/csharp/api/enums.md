---
title: Enums
description: Every value of FillMode, TweenState, Reason, TweenPauseMode, TweenProcessMode, and EaseType, with its meaning.
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

`EaseType.Linear` is the default. `SmoothStep` and `SmootherStep` are S-curves
with no variants. Ten families come in `In`, `Out`, and `InOut` forms, such as
`EaseType.CubicOut`: `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`,
`Back`, `Elastic`, and `Bounce`. [Easing](/csharp/easing/) compares their curves.

## Constants

| Member | Meaning |
| --- | --- |
| `TweenOptions.Infinite` | `-1`, for `Repeats` that run until cancelled |
