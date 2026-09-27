---
title: Handles and groups
description: The members of TweenInstance and Group, the handles that starting playback returns.
---

Starting one definition returns a `TweenInstance<TTarget, TValue>` handle.
Starting several, or grouping running tweens with `Group.Of`, returns a `Group`.
Both can be awaited directly.

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
| `GetAwaiter()` | `TaskAwaiter<Reason>` | Lets you write `await movement`, with the same result and faults as `End` |
| `End` | `Task<Reason>` | Shared completion that any number of callers can await, even after it ended |
| `AwaitDecommissionAsync(token)` | `Task<Reason>` | Wait with a token that cancels only the wait, not playback |

To coordinate async work, `await movement` instead of polling `State`. Keep `End`
for APIs that need a `Task<Reason>`. [Why it ended](/csharp/cancellation/#why-it-ended)
describes each `Reason`.

## Target and value

| Member | Type | Meaning |
| --- | --- | --- |
| `Target` | `TTarget` | The object the tween animates |
| `Value` | `TValue` | The value read at start, then the latest value written |

## Groups

A `Group` controls several tweens as one step. It has the same control and await
members as a handle, and no `State` or `Progress`:

| Member | Type | Meaning |
| --- | --- | --- |
| `Group.Of(tweens)` | `Group` | Group tweens that are already playing |
| `Members` | `IReadOnlyList<TweenInstance>` | The grouped handles |
| `Pause()`, `Resume()`, `Cancel()`, `IsPaused` | | Act on every member |
| `IsTerminal`, `CompletionReason`, `Error` | | As on a handle, for the group as a whole |
| `GetAwaiter()`, `End` | | Await the group; it completes when every member completes |

If one member stops early, the group cancels the others and reports that member's
reason, or faults. See [sequences](/csharp/sequences/).
