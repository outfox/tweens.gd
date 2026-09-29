---
title: Handles and groups
description: The members of TweensGdHandle and TweensGdGroup, what starting playback returns, and cancellation tokens for waits.
tableOfContents: true
---

`Tweens.play()` returns a `TweensGdHandle`. `Tweens.play_all()` and
`Tweens.group()` return a [`TweensGdGroup`](/gdscript/api/groups/). Both have an
`end` you can await.

## Control playback

| Member | Type | Meaning |
| --- | --- | --- |
| `pause()`, `resume()` | `void` | Set or clear the explicit pause |
| `is_paused` | `bool` | True while explicitly paused; assignable |
| `cancel()` | `void` | End playback now and keep the latest value; safe to call again |

Pause is separate from `state`: there's no paused state. See
[control and completion](/gdscript/playback/).

## Read the state

| Member | Type | Meaning |
| --- | --- | --- |
| `state` | `Tweens.State` | `DELAYED`, `PLAYING`, `INTERVAL`, `COMPLETED`, `CANCELLED`, or `FAULTED` |
| `is_terminal` | `bool` | True once completed, cancelled, or faulted |
| `is_settled` | `bool` | True once the ending callbacks and cleanup have run |
| `progress` | `float` | Position in the current leg, from 0 to 1, before easing |
| `completion_reason` | `Tweens.Reason` | Why playback ended; `-1` until it has |
| `error` | `String` | What went wrong, if the tween failed; empty otherwise |

## Await the end

| Member | Type | Meaning |
| --- | --- | --- |
| `end` | `Variant` | Await it directly: the `ended` signal while running, the reason once it has ended |
| `wait(cancellation = null)` | `Tweens.Reason` | Await the reason, with an optional token that cancels only the wait |
| `ended(reason)` | signal | Emitted once when playback ends |

Prefer `await handle.end` to awaiting `ended`: awaiting the signal after it has
fired waits forever. [Cancellation and completion reasons](/gdscript/cancellation/)
describes each `Tweens.Reason`.

## Target and value

| Member | Type | Meaning |
| --- | --- | --- |
| `target` | `Object` | The object the tween animates; `null` for a rejected start |
| `value` | `Variant` | The value read at start, then the latest value written |


## Cancel a wait

| Member | Type | Meaning |
| --- | --- | --- |
| `TweensGdCancellation.new()` | `TweensGdCancellation` | A token to pass to `wait()` |
| `cancel()` | `void` | Stop the waits that use this token; playback continues |
| `is_cancelled` | `bool` | True after `cancel()` |
| `cancelled` | signal | Emitted by `cancel()` |

A cancelled `wait()` returns `Tweens.Reason.WAIT_CANCELLED`.
