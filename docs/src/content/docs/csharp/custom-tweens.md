---
title: Custom tweens
description: Animate callback values and custom properties, or drive a scheduler yourself.
---

When there's no native property adapter for what you want to animate, use a
callback value tween. If you can supply a typed getter, setter, and interpolator,
use `Tweens.Property<TTarget, TValue>` instead. Neither approach needs reflection
or property paths.

## Callback values

Import `Godot` and `tweens.gd`, and run this in a Node method on Godot's main thread:

```csharp
var value = owner.Tween(new Tweens.Float
{
    From = 10,
    To = 100,
    Duration = 1,
    OnUpdate = (_, sample) => GD.Print(sample),
});
```

`owner` is an in-tree `Node` that controls the tween's lifetime, and each value
arrives through `OnUpdate`. The eight value definitions are `Tweens.Float`,
`Tweens.Double`, `Tweens.Vector2`, `Tweens.Vector3`, `Tweens.Vector4`,
`Tweens.Color`, `Tweens.Quaternion`, and `Tweens.Rect2`.

## Custom properties

`Tweens.Property<TTarget, TValue>` animates anything you can read and write with a
getter and a setter, such as a plain C# property on a node:

```csharp title="HealthBar.cs"
public partial class HealthBar : Node2D
{
    public float Fill { get; set; } = 1;

    static readonly Tweens.Property<HealthBar, float> Drain = new(
        bar => bar.Fill, (bar, value) => bar.Fill = value, Interpolators.Float,
        duration: 0.4, ease: EaseType.SmootherStep);

    public void SetHealth(float fraction) => this.Tween(Drain with { To = fraction });
}
```

It starts like any other definition, owned by the node it animates. Captured
objects stay shared between starts.

## Drive a scheduler yourself

To animate an object that isn't a node, such as a model in a test, add it to a
`TweenScheduler` and advance it yourself:

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

A `TweenScheduler` belongs to the thread that created it. Drive it with
`Update(delta, unscaledDelta, mode)`, and dispose it when you're finished to
settle any remaining work. A manual scheduler with no owner has no tree pause
policy. Native targets still require Godot's main thread, and nodes must have an
in-tree owner.

`CancelTweens` only cancels automatically scheduled tweens and doesn't reach a
separate manual scheduler. The [scheduler reference](/csharp/api/scheduler/) lists
its members.

## Custom definitions and bindings

Derive from `TweenDefinition<TTarget, TValue>` and implement the protected `Read`,
`Write`, and `Interpolate` methods. `TTarget` is a reference type and `TValue` is a
value type. The `Interpolators` helpers cover the built-in numeric and vector
types:

```csharp title="UniformZoomTween.cs"
// Tweens a camera's zoom as one number, keeping X and Y equal.
public sealed class UniformZoomTween : TweenDefinition<Camera2D, float>
{
    protected override float Read(Camera2D target) => target.Zoom.X;
    protected override void Write(Camera2D target, float value) => target.Zoom = new Vector2(value, value);
    protected override float Interpolate(float from, float to, float weight) => Interpolators.Float(from, to, weight);
}
```

```csharp
camera.Tween(new UniformZoomTween { To = 2, Duration = 0.5, Ease = EaseType.SmootherStep });
```

`By`, factors, and deltas ([variations](/csharp/variations/)) work with int,
float, double, vector, `Color`, `Quaternion`, and `Rect2` values. `By` reads the
property back on every frame, so override `ReadsWrittenValue` to return `false`
if `Read` doesn't return what `Write` stored; `By` is then added to the start
value instead.

For per-playback bindings, override `Prepare`, `Restore`, and `Release`.
`Prepare` runs on the playback's private definition snapshot, before its initial
read. `Restore` may write back a property value or remove an override instead.
`Release` also runs after a failed preparation, and it must release only resources
owned by that snapshot. The snapshot is shallow, so reference-valued configuration
on a custom definition stays shared. Don't mutate that shared configuration while
independent playbacks use it.
