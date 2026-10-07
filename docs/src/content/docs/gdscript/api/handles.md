---
title: Handles
description: TweensGdHandle, what starting one GDScript definition returns, with its states, completion reasons, wait cancellation, errors, and threading rules.
tableOfContents: true
---

`Tweens.play()` returns a `TweensGdHandle` that controls that playback alone. It never returns `null`: a [rejected start](/gdscript/api/start/#rejected-starts) returns a handle that has already failed. [Control & completion](/gdscript/playback/) introduces handles.

## Control

| Member | Type | Meaning |
| --- | --- | --- |
| `pause()` | `void` | Hold playback, in addition to any [pause mode](/gdscript/api/start/#tweenspause) |
| `resume()` | `void` | Release that hold |
| `is_paused` | `bool` | True while held by `pause()`; assignable |
| `cancel()` | `void` | Stop now and keep the latest value; safe to call again |

## Status

| Member | Type | Meaning |
| --- | --- | --- |
| `state` | `Tweens.State` | Where the timeline is; a paused handle keeps its state |
| `progress` | `float` | Position in the current leg, from 0 to 1, before easing. It runs backward during a ping-pong return, and isn't the share of all cycles completed |
| `is_terminal` | `bool` | True once completed, cancelled, or faulted |
| `is_settled` | `bool` | True once the ending callbacks and cleanup have run |
| `completion_reason` | `Tweens.Reason` | Why playback ended; `-1` until it has |
| `error` | `String` | What went wrong, if playback failed; empty otherwise |
| `target` | `Object` | The animated object; `null` for a rejected start |
| `value` | `Variant` | The value captured at start, then the latest value written |

## Awaiting

| Member | Type | Meaning |
| --- | --- | --- |
| `end` | `Variant` | Await it directly: the `ended` signal while running, the reason once ended |
| `wait(cancellation)` | `Tweens.Reason` | Await the reason; the optional `TweensGdCancellation` cancels only this wait |
| `ended(reason)` | signal | Emitted once when playback ends |

Prefer `await handle.end` to awaiting `ended`: awaiting the signal after it fired waits forever.

### TweensGdCancellation

| Member | Type | Meaning |
| --- | --- | --- |
| `TweensGdCancellation.new()` | `TweensGdCancellation` | A token to pass to `wait()` |
| `token.cancel()` | `void` | End the waits that use this token with `WAIT_CANCELLED`; playback continues |
| `token.is_cancelled` | `bool` | True after `cancel()` |
| `token.cancelled` | signal | Emitted by `cancel()` |

## Tweens.State

| Constant | Meaning |
| --- | --- |
| `Tweens.State.DELAYED` | Waiting out the delay |
| `Tweens.State.PLAYING` | Moving through a leg |
| `Tweens.State.INTERVAL` | Holding at an endpoint, between legs or cycles |
| `Tweens.State.COMPLETED` | Reached its natural end |
| `Tweens.State.CANCELLED` | Stopped early |
| `Tweens.State.FAULTED` | Stopped by a detected problem; see `error` |

## Tweens.Reason

| Constant | Meaning |
| --- | --- |
| `Tweens.Reason.COMPLETED` | Reached its natural end |
| `Tweens.Reason.CANCELLED` | `cancel()` or `cancel_tweens()` stopped it |
| `Tweens.Reason.TARGET_FREED` | The target was freed or queued for deletion |
| `Tweens.Reason.OWNER_EXITED` | The owner left the scene tree |
| `Tweens.Reason.RUNNER_DISPOSED` | The runner, its tree, or a manual scheduler shut down |
| `Tweens.Reason.FAILED` | The start was rejected, or playback detected a problem; see `error` |
| `Tweens.Reason.WAIT_CANCELLED` | Only from `wait()`: its token was cancelled, and playback continues |

Compare against `COMPLETED` rather than a particular early reason: freeing a node reports `TARGET_FREED` after `queue_free()`, but `OWNER_EXITED` after `free()`, because Godot exits the tree first.

## Errors

- GDScript has no exceptions to throw, so failures are data. A tween that detects a problem ends with `FAILED`, and `error` holds the message; several problems are joined with newlines.
- `on_finally` still runs, other tweens keep playing, and the automatic runner reports the message to Godot's error log.
- A rejected start's `pause()`, `resume()`, `cancel()`, and `wait()` stay safe to call.

:::caution[Script errors aren't caught]
tweens.gd detects invalid configuration, stale Callables, non-numeric or non-finite easing, and non-finite interpolation. An error inside your own callback or property setter stays an ordinary Godot script error, and isn't guaranteed to become `FAILED`.
:::

## Threading

- Create and control tweens, and await `end`, on Godot's main thread.
- A call from another thread reports an error and does nothing: starts return a handle that has already failed, and `wait()` returns `FAILED`.
- Keep callbacks synchronous; put code that awaits after `await handle.end`.
