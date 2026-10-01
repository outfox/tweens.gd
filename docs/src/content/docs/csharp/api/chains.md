---
title: Chains
description: One target, linked definitions, signed delays, and one playback controller.
---

A `Chain` schedules a flat list on one target.
See [sequences](/csharp/sequences/) for overlap, pre-roll, and capture rules.

```csharp
var animation = sprite.Chain([
    new Tweens.Position2D((400, 180), 0.6),
    new Tweens.Scale2D(Vector2.One * 1.2f, 0.2),
    new Tweens.ModulateAlpha(0, 0.3),
]);
await animation.End;
```

Node `Chain(definitions, options = default)`, Resource `Chain(definitions, owner, options = default)`, and manual `scheduler.AddChain(target, definitions, owner = null, options = default)` return `Chain`.

Use `IReadOnlyList<ITweenDefinition<TTarget>>`; value types can differ and base-target definitions are accepted through contravariance. Callbacks keep their typed leaf handles.
One playback policy and one lifetime subscription apply to the root.
Scheduler active counts count the Chain once; leaves are private to that root.
Dropping the controller reference does not stop playback.
Settlement releases copied leaf definitions, adapters, and callbacks.

| Member | Type | Meaning |
| --- | --- | --- |
| `End` | `Task<Reason>` | Shared completion |
| `AwaitDecommissionAsync(token)` | `Task<Reason>` | Cancel only this wait |
| `CompletionReason` | `Reason?` | First terminal reason; null while running |
| `Error` | `Exception?` | Detected failure, including aggregated cleanup errors |
| `IsTerminal, IsSettled` | `bool` | Stopped, and fully cleaned up |
| `IsPaused` | `bool` | Explicit root pause |
| `Elapsed, Duration` | `double` | Visible consumed time and last scheduled end, clamped at zero |
| `EntryCount, ActiveCount, PendingCount` | `int` | Declared entries and unfinished leaf diagnostics |
| `Pause(), Resume(), Cancel()` | `void` | Control the whole root |
