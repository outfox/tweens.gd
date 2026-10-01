// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

/// <summary>By: offsets that move on top of other changes to the property, and the fixed-start fallback.
/// Shared timeline cases are in the conformance fixtures.</summary>
public class RelativeTests
{
    private sealed class Counter
    {
        public int Value { get; set; }
    }

    private sealed class Spinner
    {
        public Quaternion Value { get; set; }
    }

    /// <summary>Reports samples without storing them, like the callback value tweens.</summary>
    private sealed class Signal : TweenDefinition<Box, float>
    {
        protected override float Read(Box target) => 2;
        protected override void Write(Box target, float value) { }
        protected override float Interpolate(float from, float to, float weight) => Interpolators.Float(from, to, weight);
        protected override bool ReadsWrittenValue => false;
    }

    [Fact]
    public void RelativeTweensOnOnePropertyAddUp()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 1 };
        scheduler.Add(box, new PlainTween { By = 10, Duration = 1 });
        scheduler.Add(box, new PlainTween { By = -4, Duration = 2 });
        scheduler.Update(1);
        Assert.Equal(9, box.Value);
        scheduler.Update(1);
        Assert.Equal(7, box.Value);
    }

    [Fact]
    public void ValueAndUpdatesReportTheWrittenValue()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 3 };
        var updates = new List<float>();
        var tween = scheduler.Add(box, new PlainTween { By = 2, Duration = 1, OnUpdate = (_, value) => updates.Add(value) });
        scheduler.Update(0.5);
        Assert.Equal(4, tween.Value);
        scheduler.Update(0.5);
        Assert.Equal([3f, 4f, 5f], updates);
    }

    [Fact]
    public void ToAndByCannotBeCombined()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 3 };
        Assert.Throws<ArgumentException>(() => scheduler.Add(box, new PlainTween { To = 1, By = 1, Duration = 1 }));
        Assert.Equal(0, scheduler.ActiveCount);
        Assert.Equal(3, box.Value);
    }

    [Fact]
    public void UnsupportedValueTypesAreRejectedAtStart()
    {
        using var scheduler = new TweenScheduler();
        var definition = new PropertyTween<Box, Transform2D>(_ => Transform2D.Identity, (_, _) => { }, (from, _, _) => from)
        {
            By = Transform2D.Identity,
        };
        Assert.Throws<NotSupportedException>(() => scheduler.Add(new Box(), definition));
        definition.By = null;
        scheduler.Add(new Box(), definition).Cancel();
    }

    [Fact]
    public void IntegerOffsetsRoundEachStepAndAccumulateExactly()
    {
        using var scheduler = new TweenScheduler();
        var counter = new Counter { Value = 3 };
        scheduler.Add(counter, new Tweens.Property<Counter, int>(c => c.Value, (c, v) => c.Value = v, Interpolators.Int)
        {
            By = 5,
            Duration = 1,
            Repeats = 1,
        });
        var samples = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            scheduler.Update(0.5);
            samples.Add(counter.Value);
        }
        Assert.Equal([6, 8, 11, 13], samples);
    }

    [Fact]
    public void QuaternionOffsetsRotateAboutLocalAxes()
    {
        using var scheduler = new TweenScheduler();
        var start = new Quaternion(Vector3.Up, Mathf.Pi / 2);
        var by = new Quaternion(Vector3.Right, Mathf.Pi / 2);
        var spinner = new Spinner { Value = start };
        scheduler.Add(spinner, new Tweens.Property<Spinner, Quaternion>(s => s.Value, (s, v) => s.Value = v,
            Interpolators.Quaternion) { By = by, Duration = 1 });
        scheduler.Update(0.5);
        Values.AssertClose(start * new Quaternion(Vector3.Right, Mathf.Pi / 4), spinner.Value);
        scheduler.Update(0.5);
        Values.AssertClose(start * by, spinner.Value);
        Assert.False(Values.Close(by * start, spinner.Value));
    }

    [Fact]
    public void DefinitionsWithoutStorageAddToTheCapturedStart()
    {
        using var scheduler = new TweenScheduler();
        var updates = new List<float>();
        scheduler.Add(new Box(), new Signal { By = 10, Duration = 1, OnUpdate = (_, value) => updates.Add(value) });
        scheduler.Update(0.5);
        scheduler.Update(0.5);
        Assert.Equal([2f, 7f, 12f], updates);
    }

    [Fact]
    public void OffsetsAddAndRemoveForEveryValueType()
    {
        Check(3, 4, 7);
        Check(1.5f, 2f, 3.5f);
        Check(1.5, 2.0, 3.5);
        Check(new Vector2(1, 2), new Vector2(3, 4), new Vector2(4, 6));
        Check(new Vector3(1, 2, 3), new Vector3(3, 4, 5), new Vector3(4, 6, 8));
        Check(new Vector4(1, 2, 3, 4), new Vector4(1, 1, 1, 1), new Vector4(2, 3, 4, 5));
        Check(new Color(0.25f, 0.5f, 0.5f, 1), new Color(0.25f, 0, 0.25f, -0.5f), new Color(0.5f, 0.5f, 0.75f, 0.5f));
        Check(new Rect2(1, 2, 3, 4), new Rect2(1, 1, 2, 2), new Rect2(2, 3, 5, 6));
        var quarter = new Quaternion(Vector3.Up, Mathf.Pi / 2);
        Check(quarter, quarter, new Quaternion(Vector3.Up, Mathf.Pi));

        Assert.Equal(Quaternion.Identity, Offsets<Quaternion>.Zero);
        Assert.Equal(Vector2.Zero, Offsets<Vector2>.Zero);
        Assert.Equal(int.MaxValue, Offsets<int>.Add(int.MaxValue, 1));
        Assert.Equal(int.MinValue, Offsets<int>.Remove(int.MinValue, 1));
        Assert.Throws<ArgumentException>(() => Offsets<Quaternion>.Add(default, Quaternion.Identity));
        Assert.False(Offsets<Transform2D>.Supported);
        Assert.Throws<NotSupportedException>(() => Offsets<Transform2D>.Add(default, default));
        Assert.Throws<NotSupportedException>(() => Offsets<Transform2D>.Remove(default, default));

        static void Check<T>(T value, T offset, T sum) where T : struct
        {
            Assert.True(Offsets<T>.Supported);
            Values.AssertClose(sum, Offsets<T>.Add(value, offset));
            Values.AssertClose(value, Offsets<T>.Remove(sum, offset));
        }
    }
}
