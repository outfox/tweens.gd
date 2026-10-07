---
title: Scheduler
description: TweenScheduler, the manual C# scheduler the automatic runner is built on, for deterministic tests and plain objects.
tableOfContents: true
---

A `TweenScheduler` advances playback only when you call `Update`, for deterministic tests or objects outside the scene tree. The automatic runner is built on one.

```csharp
using var scheduler = new TweenScheduler();
var move = scheduler.Add(sprite, new Tweens.Position2D((100, 0), 1));
scheduler.Update(0.5); // move.Progress is now 0.5.
```

## Members

| Member | Type | Purpose |
| --- | --- | --- |
| `Add(target, definition, options)` | `TweenInstance<TTarget, TValue>` | Start one definition; a node target owns itself, other targets need no owner |
| `Add(target, definition, owner, options)` | `TweenInstance<TTarget, TValue>` | Start one definition on a separate target, owned by an in-tree node |
| `AddAll(target, definitions, owner, options)` | `Group` | Start a list of definitions as one [group](/csharp/api/groups/) |
| `AddChain(target, definitions, owner, options)` | `Chain` | Start a list as one [Chain](/csharp/api/chains/) |
| `Update(delta, unscaledDelta, mode)` | `void` | Advance every tween of `mode`, which defaults to `Process`. Tweens with unscaled time advance by `unscaledDelta`, which defaults to `delta` |
| `ActiveCount` | `int` | Unfinished tweens; a Chain counts once |
| `CancelAll()` | `void` | Cancel every tween in this scheduler |
| `UnhandledException` | `event Action<Exception>` | Receives each failure once its tween is cleaned up |
| `Dispose()` | `void` | Stop and settle the remaining playback |

`owner` and `options` are optional, except in the owner overload of `Add`.

## Plain objects

This example needs no scene or automatic runner. The scheduler advances a model halfway through a one-second motion:

```csharp title="MeterExample.cs"
using tweens.gd;

public sealed class Meter
{
    public float Value { get; set; }
}

public static class MeterExample
{
    public static float SampleMidpoint()
    {
        var meter = new Meter();
        using var scheduler = new TweenScheduler();
        var definition = new Tweens.Property<Meter, float>(
            target => target.Value,
            (target, value) => target.Value = value,
            Interpolators.Float)
        {
            From = 0,
            To = 100,
            Duration = 1,
        };

        scheduler.Add(meter, definition);
        scheduler.Update(0.5);
        return meter.Value; // 50 with the default linear easing.
    }
}
```

## Rules

- Create, update, and dispose a scheduler on one thread. Native targets still need Godot's main thread.
- `Update` rejects negative deltas, and recursive calls from inside a callback.
- Tweens started during an update first advance on the next one, with no inherited time.
- Node and resource tweens normally use the automatic runner. `CancelTweens()` reaches only the automatic runner, not your schedulers.
- Dispose the scheduler when done, so remaining tweens end and run their callbacks.
