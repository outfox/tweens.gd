---
title: Groups
description: TweensGdGroup, which pauses, cancels, and awaits several GDScript tweens as one step.
tableOfContents: true
---

A `TweensGdGroup` treats several tweens as one step: it pauses, cancels, and completes them together. [Sequences](/gdscript/sequences/#play-together) introduces groups.

```gdscript
var step := Tweens.group([
	Tweens.play(sprite, Tweens.position_2d_x(300.0, 0.6)),
	Tweens.play(label, Tweens.modulate_alpha(0.0, 0.6)),
])
await step.end
```

## Create

| Member | Type | Meaning |
| --- | --- | --- |
| `Tweens.group(handles)` | `TweensGdGroup` | Group running handles, on any targets |
| `TweensGdGroup.of(handles)` | `TweensGdGroup` | The same as `Tweens.group()` |
| `Tweens.play_all(target, definitions)` | `TweensGdGroup` | Start several definitions on one target as a group; see [starting playback](/gdscript/api/start/#start) |

## Control

| Member | Type | Meaning |
| --- | --- | --- |
| `pause()` | `void` | Pause every member |
| `resume()` | `void` | Resume every member |
| `is_paused` | `bool` | True while every active member is paused; assignable |
| `cancel()` | `void` | Cancel every member still playing |

## Status

| Member | Type | Meaning |
| --- | --- | --- |
| `members` | `Array[TweensGdHandle]` | A copy of the grouped handles, without duplicates |
| `is_terminal` | `bool` | True once the group has ended |
| `is_settled` | `bool` | True once its members have settled as well |
| `completion_reason` | `Tweens.Reason` | `COMPLETED`, or the reason of the first member that stopped early; `-1` until the group ends |
| `error` | `String` | The members' failure messages, joined with newlines |
| `errors` | `Array[String]` | The same messages as a list |

## Awaiting

| Member | Type | Meaning |
| --- | --- | --- |
| `end` | `Variant` | Await it directly: the `ended` signal while running, the reason once the group has ended |
| `wait(cancellation)` | `Tweens.Reason` | Await the reason; the optional [token](/gdscript/api/handles/#tweensgdcancellation) cancels only this wait |
| `ended(reason)` | signal | Emitted once when the group ends |

## Rules

- If one member stops early or fails, the group cancels the others and keeps that member's reason.
- A group has no `state`, `progress`, `target`, or `value`; read its members for those.
- A group doesn't link timing: each member keeps its own start and delay. A [Chain](/gdscript/api/chains/) plays definitions in order.
