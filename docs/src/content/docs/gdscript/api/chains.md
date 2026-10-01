---
title: Chains
description: One target, linked definitions, signed delays, and one playback controller.
---

A `TweensGdChain` schedules a flat list on one target.
See [sequences](/gdscript/sequences/) for overlap, pre-roll, and capture rules.

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
