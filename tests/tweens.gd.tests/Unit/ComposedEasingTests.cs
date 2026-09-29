// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using tweens.gd.Tests.Support;
using System.Text.Json;

namespace tweens.gd.Tests.Unit;

public class ComposedEasingTests
{
    [Fact]
    public void SharedSamplesMatchNativeAndWebsiteContract()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance", "easing.json")));
        foreach (var sample in data.RootElement.GetProperty("cases").EnumerateArray())
        {
            var ease = Flag(typeof(In), sample.GetProperty("in").GetString()!) | Flag(typeof(Out), sample.GetProperty("out").GetString()!);
            var t = Math.Pow(sample.GetProperty("progress").GetDouble(), sample.GetProperty("skew").GetDouble());
            Assert.InRange(Math.Abs(Easing.Evaluate(ease, (float)t, sample.TryGetProperty("blendType", out var blendType) ? Enum.Parse<BlendType>(blendType.GetString()!) : BlendType.Hermite, sample.TryGetProperty("blend", out var blend) ? blend.GetDouble() : 0.4) - sample.GetProperty("expected").GetDouble()),
                0, data.RootElement.GetProperty("tolerance").GetDouble());
        }
    }

    private static readonly string[] Families = ["Linear", "Sine", "Quad", "Cubic", "Quart", "Quint", "Expo", "Circ", "Back", "Elastic", "Bounce", "SmoothStep", "SmootherStep"];
    private static EaseType Flag(Type type, string name) => (EaseType)type.GetField(name)!.GetValue(null)!;
    private static EaseType ConventionalPair(string family) =>
        Enum.Parse<EaseType>(family + (family is "Linear" or "SmoothStep" or "SmootherStep" ? "" : "InOut"));

    [Fact]
    public void EveryPairKeepsEndpointsMidpointAndOriginalOuterLegs()
    {
        foreach (var entry in Families)
        foreach (var exit in Families)
        {
            var a = Flag(typeof(In), entry);
            var b = Flag(typeof(Out), exit);
            var combined = a | b;
            Assert.Equal(0, Easing.Evaluate(combined, -1), 5);
            Assert.Equal(1, Easing.Evaluate(combined, 2), 5);
            Assert.Equal(0.5f, Easing.Evaluate(combined, 0.5f), 5);
            Assert.True(float.IsNaN(Easing.Evaluate(combined, float.NaN)));
            for (var i = 0; i <= 100; i++)
            {
                var t = i / 100f;
                var actual = Easing.Evaluate(combined, t);
                Assert.True(float.IsFinite(actual));
                if (t <= 0.3f) Assert.Equal(Easing.Evaluate(ConventionalPair(entry), t), actual);
                if (t >= 0.7f) Assert.Equal(Easing.Evaluate(ConventionalPair(exit), t), actual);
            }
        }
    }

    [Fact]
    public void EveryMatchingPairExactlyReproducesItsConventionalInOut()
    {
        foreach (var family in Families)
        for (var i = 0; i <= 1000; i++)
        {
            var t = i / 1000f;
            Assert.Equal(Easing.Evaluate(ConventionalPair(family), t),
                Easing.Evaluate(Flag(typeof(In), family) | Flag(typeof(Out), family), t));
        }
    }

    [Theory]
    [InlineData(0.25f, 0.14644661f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(0.75f, 0.85355339f)]
    public void SineUsesHalfDurationLegs(float t, float expected) =>
        Assert.InRange(Math.Abs(expected - Easing.Evaluate(In.Sine | Out.Sine, t)), 0, 0.000001f);

    [Fact]
    public void MixedPolynomialLegsOccupyHalfTheTimeAndValueRange()
    {
        const EaseType combined = In.Quad | Out.Cubic;
        foreach (var t in new[] { 0.1f, 0.2f, 0.3f })
            Assert.Equal(0.5f * Easing.Evaluate(In.Quad, 2 * t), Easing.Evaluate(combined, t));
        foreach (var t in new[] { 0.7f, 0.8f, 0.9f })
            Assert.InRange(Math.Abs(0.5f + 0.5f * Easing.Evaluate(Out.Cubic, 2 * t - 1) - Easing.Evaluate(combined, t)), 0, 0.000001f);
    }

    [Fact]
    public void ConvenienceNamesMatchFlagsAndSingleLegsKeepTheirShape()
    {
        foreach (var family in Families)
        {
            var entry = Flag(typeof(In), family);
            var exit = Flag(typeof(Out), family);
            var both = Flag(typeof(InOut), family);
            Assert.Equal(entry | exit, both);
            var plain = family is "Linear" or "SmoothStep" or "SmootherStep";
            var oldIn = Enum.Parse<EaseType>(family + (plain ? "" : "In"));
            var oldOut = Enum.Parse<EaseType>(family + (plain ? "" : "Out"));
            for (var i = 0; i <= 100; i++)
            {
                var t = i / 100f;
                Assert.Equal(Easing.Evaluate(oldIn, t), Easing.Evaluate(entry, t));
                Assert.Equal(Easing.Evaluate(oldOut, t), Easing.Evaluate(exit, t));
                Assert.InRange(Math.Abs(1 - Easing.Evaluate(both, 1 - t) - Easing.Evaluate(both, t)), 0, 0.00001f);
            }
        }
    }

    [Theory]
    [InlineData(In.Sine | In.Back)]
    [InlineData(Out.Sine | Out.Back)]
    [InlineData(In.Sine | EaseType.BackOut)]
    [InlineData((EaseType)(1L << 40))]
    public void InvalidFlagsAreRejected(EaseType ease) =>
        Assert.Throws<NotImplementedException>(() => Easing.Evaluate(ease, 0.5f));

    [Theory]
    [InlineData(0.3f)]
    [InlineData(0.7f)]
    [InlineData(0.5f)]
    public void BlendBoundariesHaveContinuousValueAndSlope(float t)
    {
        const float h = 0.0001f;
        var ease = In.Quad | Out.Cubic;
        var left = Easing.Evaluate(ease, t - h);
        var center = Easing.Evaluate(ease, t);
        var right = Easing.Evaluate(ease, t + h);
        Assert.InRange(Math.Abs((center - left) / h - (right - center) / h), 0, 0.01f);
    }

    [Fact]
    public void MonotoneFamiliesDoNotAcquireOvershootOrReversals()
    {
        var monotone = Families.Except(["Back", "Elastic", "Bounce"]).ToArray();
        foreach (var entry in monotone)
        foreach (var exit in monotone)
        foreach (var width in new[] { 0.0, 0.01, 0.2, 0.4, 0.8, 1.0 })
        {
            var ease = Flag(typeof(In), entry) | Flag(typeof(Out), exit);
            var previous = 0f;
            for (var i = 0; i <= 1000; i++)
            {
                var actual = Easing.Evaluate(ease, i / 1000f, blend: width);
                Assert.InRange(actual, previous - 0.000001f, 1.000001f);
                previous = actual;
            }
        }
    }

    [Fact]
    public void MixedCircularJoinHasFiniteVelocityInsteadOfAMidpointSingularity()
    {
        foreach (var h in new[] { 0.001f, 0.0001f, 0.00001f })
        {
            var velocity = (Easing.Evaluate(In.Circ | Out.Sine, 0.5f + h) - Easing.Evaluate(In.Circ | Out.Sine, 0.5f - h)) / (2 * h);
            Assert.InRange(velocity, 1, 4);
        }
    }

    [Fact]
    public void BlendSettingsSurviveOptionsCopiesAndAffectPlayback()
    {
        var options = new TweenOptions { Duration = 1, Ease = In.Quad | Out.Cubic, BlendType = BlendType.Linear, Blend = 0.2 };
        var builder = new TweenOptionsBuilder();
        options.CopyTo(builder);
        Assert.Equal(options, builder.ToOptions());
        Assert.Equal(0.4, default(TweenOptions).Blend);
        Assert.Equal(BlendType.Hermite, default(TweenOptions).BlendType);
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var definition = new PlainTween { To = 1, Duration = 1 };
        options.CopyTo(definition);
        scheduler.Add(box, definition);
        definition.Blend = 1; // Playback owns a snapshot.
        scheduler.Update(0.45);
        Assert.Equal(Easing.Evaluate(options.Ease, 0.45f, options.BlendType, options.Blend), box.Value);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidWidthsAreRejected(double width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Evaluate(InOut.Sine, 0.5f, blend: width));

    [Fact]
    public void InvalidMethodIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Easing.Evaluate(InOut.Sine, 0.5f, (BlendType)99));

    [Fact]
    public void FlagsWorkThroughPlaybackAndSkewPrecedesTheBlend()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var ease = In.Quad | Out.Cubic;
        var handle = scheduler.Add(box, new PlainTween { To = 1, Duration = 2, Ease = ease, Skew = 2, Weks = 0.5, UsePingPong = true });
        scheduler.Update(1);
        Assert.Equal(Easing.Evaluate(ease, 0.25f), box.Value);
        scheduler.Update(2);
        Assert.Equal(Easing.Evaluate(ease, MathF.Sqrt(0.5f)), box.Value, 5);
        scheduler.Update(1);
        Assert.Equal(0, box.Value);
        Assert.True(handle.IsTerminal);
    }
}
