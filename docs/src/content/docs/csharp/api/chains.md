---
title: Chains
tableOfContents: true
description: One target, linked definitions, signed delays, and one playback controller.
---

A `Chain` schedules a flat list on one target.
Learn the basics in [sequences](/csharp/sequences/) before consulting the timeline rules below.

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

## Overlap and gaps

A positive delay captures at the preceding entry's end, then waits before moving.
A negative delay starts before that end. The next entry follows the overlapped
entry's own end, even while an earlier tail is still playing:

```csharp
var animation = sprite.Chain([
    new Tweens.Position2DX(100, 1.0),
    new Tweens.ModulateAlpha(0, 0.2) { Delay = -0.6 },
    new Tweens.Scale2D(Vector2.One * 1.2f, 0.1),
]);
```

| Entry | Starts | Ends |
| --- | --- | --- |
| Position | 0.0 | 1.0 |
| Fade | 0.4 | 0.6 |
| Scale | 0.6 | 0.7 |

The Chain completes at 1.0, after every tail ends. When entries write the same
property, later definitions write last while both are active. An earlier tail
can become visible again after a later entry completes. Each entry keeps its
usual fill and relative-value behavior. If signed delays reorder starts, an older
entry can activate after a later entry. Higher-priority active entries are sampled
again at that timestamp after activation, preserving capture order and write
priority; their setters and update callbacks can therefore run again.

## Pre-roll

If a signed delay places work before visible time zero, the Chain simulates
that history on its first eligible update. A one-second first entry with delay
-0.25 is already 25% through its motion at visible time zero. Crossed lifecycle
callbacks run chronologically; declaration order resolves simultaneous events.

The target's current value at the earliest activation supplies the synthetic
initial state. External systems are not rewound. Callbacks can have real
side effects. An entirely past Chain completes on the first eligible update,
after its hooks and cleanup; its visible duration is zero. Pausing before that
update defers all preparation, capture, and replay.

Signed delays also work for a standalone tween. Offset selects progress inside
a leaf; pre-roll executes crossed history in the composed timeline.

## Control and completion

`Pause()`, `Resume()`, and `Cancel()` control the whole Chain. An active leaf handle passed to a callback
forwards these controls to its Chain; controls on a terminal leaf do nothing.
Completion waits for every entry, callback, and release hook. Cancellation,
owner exit, or failure stops active leaves and discards pending entries without
preparing them or running their playback or release hooks.

Pausing inside a callback stops at the current timestamp. Resume finishes that
boundary without replaying completed hooks. The unused part of the interrupted
frame is discarded. A long update can visit multiple boundaries and invoke
setters and update callbacks several times.

Await `End` to learn when the Chain settles. Check the completion reason
when later logic depends on success. C# completion faults on detected exceptions;
GDScript keeps diagnostics in `error` and `errors`. Cancelling a wait leaves
playback running, using the same wait helpers as individual handles.

## Completion-driven logic

Ordinary awaits remain useful for conditional work, user input, and transitions
between different targets. They do not transfer leftover frame time to playback
started in a callback, signal, task continuation, or late wait. Independent
roots first sample on their next eligible update.

A Chain currently accepts one target and a flat list, including different
properties and value types on that target. Multiple targets, nested Chains,
explicit parallel steps, and repeating a whole Chain are future composition work.
A final infinitely repeating entry is allowed; a successor after an infinite
entry is rejected. Empty lists, invalid definitions, and overflowed schedules
reject before playback.

See [Chain members](/csharp/api/chains/), [timing](/csharp/api/timing/),
and [cancellation](/csharp/cancellation/) for the detailed contract.
