---
title: Groups and sequences
description: Chain, group, stagger, wait, loop, and stop multi-step animations with ordinary GDScript coroutines.
---

A sequence is an ordinary GDScript coroutine. Await each step before starting the
next, play steps together as a group, and add delays between them.

| Goal | Tool |
| --- | --- |
| Run B after A | `await a.end`, then start B |
| Run A and B together | `Tweens.play_all(node, [a, b])` or `Tweens.group(handles)`, then `await group.end` |
| Offset tweens within a step | `delay` on copies for different targets or properties |
| Wait between steps | `await Tweens.play(node, Tweens.float_value(1.0, seconds)).end` |
| Stop the sequence | [Cancel and check the result](/gdscript/cancellation/) |

Snippets run in a node script function with in-tree `sprite` (`Sprite2D`) and
`label` (`Label`) nodes, and assume
the global `Tweens` class.

## One step after another

Await each tween before starting the next:

```gdscript
await Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6)).end
await Tweens.play(sprite, Tweens.scale_2d(Vector2(1.2, 1.2), 0.2)).end
await Tweens.play(sprite, Tweens.modulate_alpha(0.0, 0.3)).end
```

Each step reads its `null` `from_value` when you start it, so it continues from
wherever the previous step left the property.

These examples ignore completion reasons. If interruption should stop the sequence,
check the result before starting the next step; [cancellation](/gdscript/cancellation/)
shows that pattern.

## Steps that run together

A group plays tweens as one step. Start several definitions on one node with
`Tweens.play_all()`:

```gdscript
var grow := Tweens.scale_2d(Vector2(1.2, 1.2), 0.2)
var dim := Tweens.modulate_alpha(0.5, 0.2)
await Tweens.play_all(sprite, [grow, dim]).end
```

Group tweens that are already playing, on any targets, with `Tweens.group()`.
`Tweens.Group.of()` is the same function:

```gdscript
var step := Tweens.group([
	Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6)),
	Tweens.play(label, Tweens.modulate_alpha(0.0, 0.6)),
])
await step.end
```

A group completes after every member settles, including its callbacks and
cleanup. If one member stops early (cancelled, freed, or failed), the group
cancels the others and reports that member's reason. `pause()`, `resume()`, and
`cancel()` act on every member. Members that already completed keep their result.

`play_all()` stops starting definitions at the first one that fails to start,
and the returned group cancels the ones it already started. `Tweens.group()`
takes handles, not definitions: start definitions with `Tweens.play()` first.
An empty array returns a group that has already failed.

:::tip[Prefer a group to awaiting each handle]
Awaiting several handles one after another also waits for all of them, but it
lets the others keep running when one stops, and it can hand the next step a
slightly wrong start time. See [timing between steps](#timing-between-steps).
:::

## Stagger with delay

`delay` offsets tweens inside one step, exact to the frame. Give each start a
copy of one definition with its own delay, from `with_delay()`, to fade in a
whole menu, one item after another:

```gdscript title="menu.gd"
extends VBoxContainer

static var fade_in := Tweens.modulate_alpha(null, 0.3) \
	.with_from(0.0).with_fill(Tweens.Fill.BOTH)

func reveal() -> void:
	var reveals := []
	for item in get_children():
		if item is Control:
			var delay := reveals.size() * 0.05
			reveals.append(Tweens.play(item, fade_in.with_delay(delay)))
	if reveals.is_empty():
		return
	await Tweens.group(reveals).end
```

`fill = Tweens.Fill.BOTH` applies `from_value` during the delay, so items that
are still waiting stay hidden instead of showing at full opacity first.

:::caution[Stagger different targets, not one property]
A tween reads its `null` `from_value` when you start it, not when its delay ends. A
delayed tween on the same property starts from the value captured at the start,
and snaps the property back to it:

```gdscript
# Wrong: the second tween captured the start position, not (400, 180).
Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6))
var rise := Tweens.position_2d(Vector2(400, 0), 0.4)
rise.delay = 0.6
Tweens.play(sprite, rise)
```

Await the first tween instead, give the delayed tween an explicit `from_value`,
or move it with [`by_value`](/gdscript/definitions/#move-by-an-offset-with-by_value),
which starts from wherever the property is when the tween plays.
:::

## Wait between steps

`Tweens.float_value()` makes a [callback-only](/gdscript/custom-tweens/#callback-values)
tween, which writes no property, so `Tweens.float_value(1.0, 0.5)` is a
half-second wait. The `1.0` is the value it would report, and unused here. The
wait follows the same pause, time scale, and lifetime rules as the animation
around it:

```gdscript
await Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6)).end
await Tweens.play(sprite, Tweens.float_value(1.0, 0.5)).end
await Tweens.play(sprite, Tweens.position_2d(Vector2(40, 180), 0.6)).end
```

:::caution[Avoid `create_timer()` for holds]
A timer from `get_tree().create_timer()` isn't tied to the node's lifetime, and by
default it keeps running while the tree is paused. The sequence can resume
against a paused or freed scene.
:::

## Loops and cancellation

For one repeating motion, use `repeats` as described in [timing](/gdscript/timing/).
To repeat a multi-step sequence or stop it when interrupted, see
[cancellation and completion reasons](/gdscript/cancellation/).

## Pause a sequence

`pause()` on a handle or group pauses only that step. If your code starts the
next step while that one is paused, the new tweens play normally. To pause every
current and future step, pause the node the tweens are bound to. With the default
[pause mode](/gdscript/lifetime/#pausing), `Tweens.Pause.BOUND`, tweens follow the node's `can_process()`:

```gdscript
sprite.process_mode = Node.PROCESS_MODE_DISABLED # Pauses every tween bound to sprite.
sprite.process_mode = Node.PROCESS_MODE_INHERIT
```

Pausing the scene tree also pauses bound tweens, unless their node processes
while paused. `set_process(false)` alone doesn't pause them.

## Errors

A tween that fails ends with `FAILED`, and `handle.error` describes the problem.
A group containing it fails too, and `group.errors` lists each member's message.
A start that can't be accepted, such as a target outside the tree, returns a
handle that has already failed, so `await handle.end` returns `FAILED` at once instead of
the sequence hanging.

:::caution[Script errors aren't caught]
tweens.gd detects invalid configuration, stale Callables, and non-finite easing
or interpolation results. An error inside your own callback or property setter
stays an ordinary Godot script error, and isn't guaranteed to become `FAILED`.
See [errors](/gdscript/playback/#errors).
:::

## Timing between steps

When a step completes, the code awaiting it resumes immediately, inside the same
scheduler update. Tweens it starts inherit the time by which the finished step
overshot its end. They appear from the next frame at exactly the point a gapless
timeline would put them, so long sequences don't drift. For a group, the time
comes from the member that finished last.

The handover applies when all of these hold:

- You await the handle's or the group's `end` before it finishes. An await
  on a handle that has already finished returns at once and hands over no time,
  which is why awaiting several handles in turn can lose it.
- The next tweens start before the sequence awaits anything else.
- They use the same scheduler, `process_mode`, and time base
  (`use_unscaled_time`) as the step they follow.

Tweens started from an `on_end` callback continue the finishing tween's timeline
in the same way. Callbacks can't check a reason as easily as a coroutine, and they
mustn't `await`, so prefer a coroutine for anything longer than a single
follow-up.
