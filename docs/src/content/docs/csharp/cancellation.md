---
title: Cancellation and completion reasons
description: Cancel playback, stop a sequence, and inspect completion reasons when the next action depends on success.
---

Awaiting a tween waits until it ends. Usually you can ignore the returned reason.
Check it when the next action requires the tween to have reached its destination,
or when cancelling an animation must stop the rest of a sequence.

Examples run in an async Node method with an in-tree `Sprite2D` named `sprite`,
`using Godot;`, `using tweens.gd;`, and the usual System implicit usings.

## Cancel playback

Call `movement.Cancel()` to stop one tween, or `group.Cancel()` to stop a group.
Cancellation keeps the latest value and resumes code waiting for the tween.
It does not stop the surrounding async method automatically.

`sprite.CancelTweens(includeChildren: true)` cancels every tween owned by the node
and its descendants. Leaving the tree also ends owned tweens, even while paused.

## Continue only on success

Use an early return before a follow-up animation or a gameplay action that requires
arrival. This also prevents the next step from using a target that has been freed.

```csharp
var movement = sprite.TweenPosition(new Vector2(400, 180), 0.6);
if (await movement != Reason.Completed)
    return;

GD.Print("Arrived");
await sprite.TweenModulateAlpha(0, 0.3);
```

A bare `await movement;` deliberately ignores this distinction: it resumes after cancellation
too. Use the guarded form above for interruptible sequences.

## Why it ended


`End` is a shared `Task<Reason>`. Any number of callers can await it, even after
playback has ended.

| Reason | Meaning |
| --- | --- |
| `Completed` | Reached its natural end |
| `Cancelled` | `Cancel()` was called |
| `TargetFreed` | The target was disposed, freed, or queued for deletion |
| `OwnerExited` | The owner left the scene tree |
| `RunnerDisposed` | The runner, tree, or manual scheduler shut down |

Checking the reason lets a sequence stop when playback is interrupted. Godot emits tree exit before it invalidates a node freed with
`Free()`, so that case may report `OwnerExited`. `QueueFree()` reports
`TargetFreed` for a node that owns its own tween.

## Cancel a wait, not the tween

`AwaitDecommissionAsync` waits until the tween has ended and been cleaned up,
like `End`, but also takes a `CancellationToken` from your own code, such as one
that fires when a menu closes. The token cancels only that wait: other waiters
and playback continue unless you call `Cancel()`.

```csharp
var movement = sprite.TweenPosition(new Vector2(400, 180), 0.6);
try
{
    await movement.AwaitDecommissionAsync(cancellationToken);
}
catch (OperationCanceledException)
{
    // This wait was cancelled. Stop playback too if that is your policy.
    movement.Cancel();
}
```

Cancelling playback itself never throws `OperationCanceledException`. `End`
reports `Cancelled` instead.

## Loops

Repeat a whole sequence with an ordinary loop that ends when a step doesn't
complete. To repeat a single tween, set `Repeats` instead, as described in
[timing](/csharp/timing/).

```csharp
while (await sprite.TweenPositionY(120, 0.4) == Reason.Completed
       && await sprite.TweenPositionY(180, 0.4) == Reason.Completed)
{
}
```


For callback failures and exceptions, see [errors](/csharp/playback/#errors).
