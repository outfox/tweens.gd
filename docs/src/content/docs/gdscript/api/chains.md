---
title: Chains
tableOfContents: true
description: One target, linked definitions, signed delays, and one playback controller.
---

A `TweensGdChain` schedules a flat list on one target.
Learn the basics in [sequences](/gdscript/sequences/) before consulting the timeline rules below.

```gdscript
var animation := Tweens.chain(sprite, [
    Tweens.position_2d([400, 180], 0.6),
    Tweens.scale_2d([1.2, 1.2], 0.2),
    Tweens.modulate_alpha(0.0, 0.3),
])
await animation.end
```

`Tweens.chain(target, definitions, owner = null, options = null)` and manual `scheduler.add_chain(target, definitions, owner = null, options = null)` return `TweensGdChain`, including an already failed controller on rejection.

Pass a nonempty Array of `TweensGdDefinition` objects. Configuration, adapters, and curves are copied at creation; value binding is deferred until activation.
One playback policy and one lifetime subscription apply to the root.
Scheduler active counts count the Chain once; leaves are private to that root.
Dropping the controller reference does not stop playback.
Settlement releases copied leaf definitions, adapters, and callbacks.

| Member | Type | Meaning |
| --- | --- | --- |
| `end` | `Variant` | ended signal while running, cached reason after settlement |
| `wait(cancellation = null)` | `Variant` | Cancel only this wait |
| `completion_reason` | `int` | NONE while running; stored reason when terminal |
| `error, errors` | `String, Array[String]` | Joined diagnostics and a copy of individual leaf errors |
| `is_terminal, is_settled` | `bool` | Stopped, and fully cleaned up |
| `is_paused` | `bool` | Explicit root pause |
| `elapsed, duration` | `float` | Visible consumed time and last scheduled end, clamped at zero |
| `entry_count, active_count, pending_count` | `int` | Declared entries and unfinished leaf diagnostics |
| `pause(), resume(), cancel()` | `void` | Control the whole root |

## Overlap and gaps

A positive delay captures at the preceding entry's end, then waits before moving.
A negative delay starts before that end. The next entry follows the overlapped
entry's own end, even while an earlier tail is still playing:

```gdscript
var animation := Tweens.chain(sprite, [
    Tweens.position_2d_x(100.0, 1.0),
    Tweens.modulate_alpha(0.0, 0.2).with_delay(-0.6),
    Tweens.scale_2d([1.2, 1.2], 0.1),
])
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

`pause()`, `resume()`, and `cancel()` control the whole Chain. An active leaf handle passed to a callback
forwards these controls to its Chain; controls on a terminal leaf do nothing.
Completion waits for every entry, callback, and release hook. Cancellation,
owner exit, or failure stops active leaves and discards pending entries without
preparing them or running their playback or release hooks.

Pausing inside a callback stops at the current timestamp. Resume finishes that
boundary without replaying completed hooks. The unused part of the interrupted
frame is discarded. A long update can visit multiple boundaries and invoke
setters and update callbacks several times.

Await `end` to learn when the Chain settles. Check the completion reason
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

See [Chain members](/gdscript/api/chains/), [timing](/gdscript/api/timing/),
and [cancellation](/gdscript/cancellation/) for the detailed contract.
