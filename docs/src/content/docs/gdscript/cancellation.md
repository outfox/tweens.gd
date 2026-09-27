---
title: Cancellation and completion reasons
description: Cancel playback, stop a sequence, and inspect completion reasons when the next action depends on success.
---

Awaiting a tween waits until it ends. Usually you can ignore the returned reason.
Check it when the next action requires the tween to have reached its destination,
or when cancelling an animation must stop the rest of a sequence.

Examples run in a node script function with an in-tree `Sprite2D` named `sprite`
and `const Tweens = preload("res://addons/tweens_gd/tweens.gd")`.

## Cancel playback

Call `movement.cancel()` to stop one tween, or `group.cancel()` to stop a group.
Cancellation keeps the latest value and resumes code waiting for the tween.
It does not stop the surrounding coroutine automatically.

`Tweens.cancel_tweens(sprite, true)` cancels every tween owned by the node
and its descendants. Leaving the tree also ends owned tweens, even while paused.

## Continue only on success

Use an early return before a follow-up animation or a gameplay action that requires
arrival. This also prevents the next step from using a target that has been freed.

```gdscript
var movement := Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6))
if await movement.end != Tweens.Reason.COMPLETED:
	return

print("Arrived")
await Tweens.play(sprite, Tweens.modulate_alpha(0.0, 0.3)).end
```

A bare `await movement.end` deliberately ignores this distinction: it resumes after cancellation
too. Use the guarded form above for interruptible sequences.

## Why it ended


`await movement.end` returns a `Tweens.Reason` value. Any number of callers can await it, and
an await after playback has ended returns the same reason at once.

| Reason | Meaning |
| --- | --- |
| `COMPLETED` | Reached its natural end |
| `CANCELLED` | `cancel()` was called |
| `TARGET_FREED` | The target was freed or queued for deletion |
| `OWNER_EXITED` | The owner left the scene tree |
| `RUNNER_DISPOSED` | The runner, tree, or manual scheduler shut down |
| `FAILED` | The start was rejected or playback detected an error; see `error` |
| `WAIT_CANCELLED` | Only from `wait()`: its cancellation token was cancelled, and playback continues |

Checking the reason lets a sequence stop when playback is interrupted. Removing or reparenting a
node ends the tweens it owns immediately, even while they're paused. Godot emits
tree exit before it invalidates a node freed with `free()`, so that case may
report `OWNER_EXITED`.

## Cancel a wait, not the tween

`wait()` accepts a `Tweens.Cancellation`. Cancelling it ends only that wait,
which returns `WAIT_CANCELLED`; other waiters and playback continue unless you
call `cancel()`.

```gdscript
var movement := Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6))
var cancellation := Tweens.Cancellation.new()
get_tree().create_timer(0.25).timeout.connect(cancellation.cancel)
if await movement.wait(cancellation) == Tweens.Reason.WAIT_CANCELLED:
	# This wait was cancelled. Stop playback too if that is your policy.
	movement.cancel()
```

`WAIT_CANCELLED` belongs to the wait alone; it never becomes a handle's
`completion_reason`. If playback had already ended, the wait returns that reason
even when the token is cancelled. Groups accept the same token.

## Loops

Repeat a whole sequence with an ordinary loop that ends when a step doesn't
complete. To repeat a single tween, set `repeats` instead, as described in
[timing](/gdscript/timing/).

```gdscript
var up := Tweens.position_2d_y(120.0, 0.4)
var down := Tweens.position_2d_y(180.0, 0.4)
while true:
	if await Tweens.play(sprite, up).end != Tweens.Reason.COMPLETED:
		break
	if await Tweens.play(sprite, down).end != Tweens.Reason.COMPLETED:
		break
```


For callback failures and script errors, see [errors](/gdscript/playback/#errors).
