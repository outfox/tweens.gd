// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

/// <summary>Factors and deltas: factor * value + delta for From, To, By and Duration.
/// Shared float cases are in the endpoint conformance fixtures.</summary>
public class AdjustmentTests
{
    private sealed class Counter
    {
        public int Value { get; set; }
    }

    private sealed class Spinner
    {
        public Quaternion Value { get; set; }
    }

    [Fact]
    public void QuaternionFactorsScaleTheAngleAndDeltasRotateLocally()
    {
        using var scheduler = new TweenScheduler();
        var tilt = new Quaternion(Vector3.Right, Mathf.Pi / 2);
        var spinner = new Spinner { Value = new Quaternion(Vector3.Up, Mathf.Pi / 2) };
        scheduler.Add(spinner, new Tweens.Property<Spinner, Quaternion>(s => s.Value, (s, v) => s.Value = v,
            Interpolators.Quaternion) { FactorTo = 0.5, DeltaTo = tilt, Duration = 1 });
        scheduler.Update(1);
        Values.AssertClose(new Quaternion(Vector3.Up, Mathf.Pi / 4) * tilt, spinner.Value);
    }

    [Fact]
    public void IntegerFactorsRoundAwayFromZero()
    {
        using var scheduler = new TweenScheduler();
        var counter = new Counter { Value = 3 };
        scheduler.Add(counter, new Tweens.Property<Counter, int>(c => c.Value, (c, v) => c.Value = v, Interpolators.Int)
        {
            FactorTo = 1.5,
            Duration = 1,
        });
        scheduler.Update(1);
        Assert.Equal(5, counter.Value);
    }

    [Fact]
    public void DurationAdjustmentsShapeTheTimeline()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        // The offset may reach the adjusted duration, past the unadjusted one.
        scheduler.Add(box, new PlainTween { To = 10, Duration = 1, FactorDuration = 2, DeltaDuration = 1, Offset = 1.5 });
        scheduler.Update(0);
        Assert.Equal(5, box.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { Duration = 1, DeltaDuration = -2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { Duration = 1, FactorDuration = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { DeltaDuration = double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { Duration = 1, FactorDuration = 0.5, Offset = 0.75 }));
    }

    [Fact]
    public void DelayAdjustmentsShapeTheWaitAndItsFill()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var tween = scheduler.Add(box, new PlainTween { To = 10, Duration = 1, Delay = 1, FactorDelay = 2, DeltaDelay = 0.5 });
        scheduler.Update(2);
        Assert.Equal(TweenState.Delayed, tween.State);
        scheduler.Update(1);
        Assert.Equal(5, box.Value);

        // A delay that comes only from the delta still applies From while waiting.
        var filled = new Box { Value = 4 };
        scheduler.Add(filled, new PlainTween { From = 0, To = 8, Duration = 1, DeltaDelay = 1, Fill = FillMode.Both });
        Assert.Equal(0, filled.Value);

        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { Delay = 1, DeltaDelay = -2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { Delay = 1, FactorDelay = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { DeltaDelay = double.PositiveInfinity }));
    }

    [Fact]
    public void AdjustmentsThatWouldBeIgnoredOrInvalidAreRejected()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 3 };
        Assert.Throws<ArgumentException>(() => scheduler.Add(box, new PlainTween { To = 1, FactorBy = 2 }));
        Assert.Throws<ArgumentException>(() => scheduler.Add(box, new PlainTween { DeltaBy = 1 }));
        Assert.Throws<ArgumentException>(() => scheduler.Add(box, new PlainTween { By = 1, FactorTo = 2 }));
        Assert.Throws<ArgumentException>(() => scheduler.Add(box, new PlainTween { By = 1, DeltaTo = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { FactorFrom = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { FactorTo = double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Add(box, new PlainTween { By = 1, FactorBy = double.NaN }));
        var unsupported = new PropertyTween<Box, Transform2D>(_ => Transform2D.Identity, (_, _) => { }, (from, _, _) => from)
        {
            FactorTo = 2,
        };
        Assert.Throws<NotSupportedException>(() => scheduler.Add(box, unsupported));
        Assert.Equal(0, scheduler.ActiveCount);
        Assert.Equal(3, box.Value);
    }

    [Fact]
    public void StructuredFactorsDefaultToOne()
    {
        Assert.Equal(1, default(Tweens.Scale2D).FactorTo);
        Assert.Equal(new Tweens.Scale2D(), new Tweens.Scale2D { FactorFrom = 1, FactorTo = 1, FactorBy = 1, FactorDuration = 1 });
        Assert.NotEqual(new Tweens.Scale2D(), new Tweens.Scale2D { FactorTo = 1.2 });
        var playback = ((ITweenDefinition<Node2D, Vector2>)new Tweens.Scale2D { FactorTo = 1.2 }).CreatePlayback();
        Assert.Equal(1.2, playback.FactorTo);
        Assert.Equal(1, playback.FactorFrom);
    }
}
