---
title: Groups
description: The members of TweensGdGroup, which controls and awaits several tweens as one step.
---

A `TweensGdGroup` controls several tweens as one step. `Tweens.play_all()` returns
one, and `Tweens.group()` groups handles that are already playing. It has the
control and await members of a [handle](/gdscript/api/handles/), but no `state`,
`progress`, `target`, or `value`.

## Create a group

| Member | Type | Meaning |
| --- | --- | --- |
| `Tweens.group(handles)` | `TweensGdGroup` | Group handles that are already playing |
| `Tweens.Group.of(handles)` | `TweensGdGroup` | The same as `Tweens.group()` |
| `members` | `Array[TweensGdHandle]` | A copy of the grouped handles, without duplicates |

## Control the group

| Member | Type | Meaning |
| --- | --- | --- |
| `pause()`, `resume()` | `void` | Pause or resume every member |
| `is_paused` | `bool` | True only while every active member is paused |
| `cancel()` | `void` | Cancel every member that is still playing |

## Await the group

| Member | Type | Meaning |
| --- | --- | --- |
| `end` | `Variant` | Await it directly: the `ended` signal while running, the reason once the group has ended |
| `wait(cancellation = null)` | `Tweens.Reason` | Await the reason, with an optional token that cancels only the wait |
| `ended(reason)` | signal | Emitted once when the group ends |
| `is_terminal`, `is_settled` | `bool` | True once the group has ended, and once its members have settled |
| `completion_reason` | `Tweens.Reason` | `COMPLETED`, or the reason of the first member that stopped early |
| `error`, `errors` | `String`, `Array[String]` | The members' failure messages, joined and as a list |

A group ends after every member ends. If one member stops early, the group
cancels the others and keeps that member's reason. See
[groups and sequences](/gdscript/sequences/).
