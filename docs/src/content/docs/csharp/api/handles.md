---
title: Handles and groups
description: The members of TweenInstance and Group, the handles that starting playback returns.
tableOfContents: true
---

Starting one definition returns a `TweenInstance<TTarget, TValue>` handle.
Starting several, or grouping running tweens with `Group.Of`, returns a
[`Group`](/csharp/api/groups/). Await either handle's `End` task to wait for playback.

The non-generic base class `TweenInstance` has every handle member below except
`Target` and `Value`, so handles with different type arguments fit in one
collection.

## Control playback

| Member | Type | Meaning |
| --- | --- | --- |
| `Pause()`, `Resume()` | `void` | Set or clear the explicit pause |
| `IsPaused` | `bool` | True while explicitly paused; settable |
| `Cancel()` | `void` | End playback now and keep the latest value |

Pause is separate from `State`: there's no `Paused` state. See
[control and completion](/csharp/playback/).

## Read the state

| Member | Type | Meaning |
| --- | --- | --- |
| `State` | `TweenState` | `Delayed`, `Playing`, `Interval`, `Completed`, `Cancelled`, or `Faulted` |
| `IsTerminal` | `bool` | True once completed, cancelled, or faulted |
| `Progress` | `float` | Position in the current leg, from 0 to 1, before easing |
| `CompletionReason` | `Reason?` | Why playback ended; `null` until it has |
| `Error` | `Exception?` | The exception that faulted the tween, if one did |

## Await the end

| Member | Type | Meaning |
| --- | --- | --- |
| `End` | `Task<Reason>` | Shared completion that any number of callers can await, even after it ended |
| `AwaitDecommissionAsync(token)` | `Task<Reason>` | Wait with a token that cancels only the wait, not playback |

To coordinate async work, `await movement.End` instead of polling `State`. You can
also pass `End` to APIs that need a `Task<Reason>`. [Why it ended](/csharp/cancellation/#why-it-ended)
describes each `Reason`.

## Target and value

| Member | Type | Meaning |
| --- | --- | --- |
| `Target` | `TTarget` | The object the tween animates |
| `Value` | `TValue` | The value read at start, then the latest value written |

## Errors

An exception in interpolation, easing, a setter, or a callback faults the tween.
`End` throws when awaited, `Error` holds the exception, and several failures are
kept in an `AggregateException`. Cleanup and `OnFinally` still run, and other
tweens keep playing. Schedulers also report the error through
`UnhandledException`, and the automatic runner forwards it to `GD.PushError`.

:::caution[Catch errors in `async void` callbacks]
`_Ready` and other Godot callbacks are often `async void`. Wrap their sequences in
`try`/`catch`, or a faulted tween's exception is lost.
:::

## Stay on the main thread

Create and control tweens, and await `End`, on Godot's main thread. `End` finishes
there, and ordinary Godot async code keeps its synchronization context.

- Don't block with `.Wait()` or `.Result`.
- Don't use `Task.Run` or `ConfigureAwait(false)` around engine access.
- Use Godot's `ToSignal` to wait for unrelated engine signals; tweens.gd has no
  coroutine API.
