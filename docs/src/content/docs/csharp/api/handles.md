---
title: Handles
description: TweenInstance, what starting one C# definition returns, with its states, completion reasons, errors, and threading rules.
tableOfContents: true
---

Starting one definition returns a `TweenInstance<TTarget, TValue>` that controls that playback alone. The non-generic base class `TweenInstance` has every member but `Target` and `Value`, so handles of different types fit in one collection. [Await Completion](/csharp/playback/) introduces handles.

## Control

| Member | Type | Meaning |
| --- | --- | --- |
| `Pause()` | `void` | Hold playback, in addition to any [pause mode](/csharp/api/start/#tweenpausemode) |
| `Resume()` | `void` | Release that hold |
| `IsPaused` | `bool` | True while held by `Pause()`; settable |
| `Cancel()` | `void` | Stop now and keep the latest value; does nothing once playback has ended |

## Status

| Member | Type | Meaning |
| --- | --- | --- |
| `State` | `TweenState` | Where the timeline is; a paused handle keeps its state |
| `Progress` | `float` | Position in the current leg, from 0 to 1, before easing. It runs backward during a ping-pong return, and isn't the share of all cycles completed |
| `IsTerminal` | `bool` | True once completed, cancelled, or faulted |
| `IsSettled` | `bool` | True once the ending callbacks and cleanup have run |
| `CompletionReason` | `Reason?` | Why playback ended; `null` until it has |
| `Error` | `Exception?` | The exception that faulted playback, if one did |
| `Target` | `TTarget` | The animated object |
| `Value` | `TValue` | The value captured at start, then the latest value written |

## Awaiting

| Member | Type | Meaning |
| --- | --- | --- |
| `End` | `Task<Reason>` | Completes after the ending callbacks and cleanup. Any number of callers can await it, even after the end |
| `AwaitDecommissionAsync(token)` | `Task<Reason>` | The same wait, but `token` cancels only this wait and throws `OperationCanceledException`; playback continues |

## TweenState

| Member | Meaning |
| --- | --- |
| `TweenState.Delayed` | Waiting out the delay |
| `TweenState.Playing` | Moving through a leg |
| `TweenState.Interval` | Holding at an endpoint, between legs or cycles |
| `TweenState.Completed` | Reached its natural end |
| `TweenState.Cancelled` | Stopped early |
| `TweenState.Faulted` | Stopped by an exception; see `Error` |

## Reason

| Member | Meaning |
| --- | --- |
| `Reason.Completed` | Reached its natural end |
| `Reason.Cancelled` | `Cancel()` or `CancelTweens()` stopped it |
| `Reason.TargetFreed` | The target was disposed, freed, or queued for deletion |
| `Reason.OwnerExited` | The owner left the scene tree |
| `Reason.RunnerDisposed` | The runner, its tree, or a manual scheduler shut down |

Compare against `Completed` rather than a particular early reason: freeing a node reports `TargetFreed` after `QueueFree()`, but `OwnerExited` after `Free()`, because Godot exits the tree first.

## Errors

- An exception in interpolation, easing, a setter, or a callback faults the tween. `State` becomes `Faulted`, `Error` holds the exception, and awaiting `End` throws it; several failures are kept in an `AggregateException`.
- Cleanup and `OnFinally` still run, and other tweens keep playing.
- The scheduler reports the exception through `UnhandledException`, and the automatic runner forwards it to `GD.PushError`.
- Invalid start arguments throw at the start call instead.

:::caution[Catch errors in `async void` callbacks]
`_Ready` and other Godot callbacks are often `async void`. Wrap awaited sequences in `try`/`catch`, or a faulted tween's exception is lost.
:::

## Threading

- Create and control tweens, and await `End`, on Godot's main thread. `End` completes there, so ordinary Godot async code keeps its synchronization context.
- Don't block with `.Wait()` or `.Result`, and keep engine access out of `Task.Run` and `ConfigureAwait(false)`.
- tweens.gd has no coroutine API; use Godot's `ToSignal` for unrelated engine signals.
