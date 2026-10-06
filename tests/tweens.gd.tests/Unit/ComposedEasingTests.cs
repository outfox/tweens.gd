// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using tweens.gd.Tests.Support;
using System.Text.Json;

namespace tweens.gd.Tests.Unit;

public class ComposedEasingTests
{
    [Fact]
    public void LinearSplitsMoveTheJoinAndResolveToSoloProfilesAtBothEnds()
    {
        foreach (var entry in Families)
        foreach (var exit in Families)
        {
            var a = Flag(typeof(In), entry);
            var b = Flag(typeof(Out), exit);
            foreach (var split in new[] { 0f, .01f, .25f, .5f, .75f, .99f, 1f })
            {
                Assert.Equal(0, Easing.Evaluate(a | b, 0, skew: split), 5);
                Assert.Equal(1, Easing.Evaluate(a | b, 1, skew: split), 5);
                if (split > 0 && split < 1)
                    Assert.Equal(split, Easing.Evaluate(a | b, split, skew: split), 5);
                for (var i = 0; i <= 20; i++)
                {
                    var t = i / 20f;
                    var value = Easing.Evaluate(a | b, t, skew: split);
                    Assert.True(float.IsFinite(value), $"{entry} | {exit}, split {split}, t {t}");
                    if (split == 0) Assert.Equal(Easing.Evaluate(b, t), value);
                    if (split == 1) Assert.Equal(Easing.Evaluate(a, t), value);
                    Assert.InRange(Math.Abs(1 - Easing.Evaluate(Flag(typeof(In), exit) | Flag(typeof(Out), entry), 1-t, skew: 1-split) - value), 0, 0.00001f);
                }
            }
        }
    }

    [Fact]
    public void ElasticPairsUseBroaderSwingsAndSoloFollowThroughRemainsVisible()
    {
        foreach (var paired in new[] { false, true })
        foreach (var percent in new[] { 10, 20, 30, 40, 50 })
        {
            var ease = Flag(paired ? typeof(InOut) : typeof(Out), "Elastic" + percent);
            var peaks = new List<float>();
            var values = Enumerable.Range(0, 10001).Select(i =>
                Easing.Evaluate(ease, (paired ? .5f : 0) + (paired ? .5f : 1) * i / 10000f)).ToArray();
            for (var i = 1; i < values.Length - 1; i++)
            {
                // Quantized plateaus count once; ignore rounding near the endpoint.
                if (values[i] > 1.0001f && values[i] > values[i - 1] && values[i] >= values[i + 1])
                {
                    var time = (paired ? .5f : 1) * i / 10000f;
                    if (peaks.Count == 0 || time - peaks[^1] > .05f) peaks.Add(time);
                }
            }
            Assert.Equal(2, peaks.Count);
            if (paired) Assert.InRange(peaks[1] - peaks[0], .33f, .34f);
            else
            {
                Assert.InRange(peaks[1] - peaks[0], .44f, .46f);
                Assert.True(values[(int)(peaks[1] * 10000)] > 1.003f);
            }
            // Normalization avoids snapping to the target on the last sample.
            Assert.InRange(Math.Abs(values[^2] - 1), 0, .00002f);
        }
    }

    [Fact]
    public void SharedSamplesMatchNativeAndWebsiteContract()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance", "easing.json")));
        foreach (var sample in data.RootElement.GetProperty("cases").EnumerateArray())
        {
            var ease = Flag(typeof(In), sample.GetProperty("in").GetString()!) | Flag(typeof(Out), sample.GetProperty("out").GetString()!);
            var t = sample.GetProperty("progress").GetDouble();
            Assert.InRange(Math.Abs(Easing.Evaluate(ease, (float)t, sample.TryGetProperty("blendType", out var blendType) ? Enum.Parse<BlendType>(blendType.GetString()!) : BlendType.Makima, sample.TryGetProperty("blend", out var blend) ? blend.GetDouble() : 0.2, sample.GetProperty("skew").GetDouble()) - sample.GetProperty("expected").GetDouble()),
                0, data.RootElement.GetProperty("tolerance").GetDouble());
        }
    }

    private static readonly string[] Families = typeof(In).GetFields().Select(f => f.Name).Where(n => n != "None").ToArray();
    private static bool Calibrated(string family) => family.StartsWith("Back") || family.StartsWith("Elastic") || family.StartsWith("Bounce") || family.StartsWith("Jump");
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
                if (t <= 0.4f) Assert.Equal(Easing.Evaluate(Flag(typeof(InOut), entry), t), actual);
                if (t >= 0.6f) Assert.Equal(Easing.Evaluate(Flag(typeof(InOut), exit), t), actual);
            }
        }
    }

    [Fact]
    public void UncalibratedMatchingPairsExactlyReproduceTheirConventionalInOut()
    {
        foreach (var family in Families.Where(f => !Calibrated(f)))
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
    public void ConvenienceNamesMatchFlagsAndOtherSingleLegsKeepTheirShape()
    {
        foreach (var family in Families)
        {
            var entry = Flag(typeof(In), family);
            var exit = Flag(typeof(Out), family);
            var both = Flag(typeof(InOut), family);
            Assert.Equal(entry | exit, both);
            var plain = family is "Linear" or "SmoothStep" or "SmootherStep";
            var oldIn = Calibrated(family) ? EaseType.Linear : Enum.Parse<EaseType>(family + (plain ? "" : "In"));
            var oldOut = Calibrated(family) ? EaseType.Linear : Enum.Parse<EaseType>(family + (plain ? "" : "Out"));
            for (var i = 0; i <= 100; i++)
            {
                var t = i / 100f;
                if (!Calibrated(family))
                {
                    Assert.Equal(Easing.Evaluate(oldIn, t), Easing.Evaluate(entry, t));
                    Assert.Equal(Easing.Evaluate(oldOut, t), Easing.Evaluate(exit, t));
                }
                Assert.InRange(Math.Abs(1 - Easing.Evaluate(both, 1 - t) - Easing.Evaluate(both, t)), 0, 0.00001f);
            }
        }
    }

    [Theory]
    [InlineData("Back")]
    [InlineData("Elastic")]
    [InlineData("Jump")]
    public void NamedOvershootLevelsApplyToSoloAndPairedCurves(string family)
    {
        foreach (var type in new[] { typeof(In), typeof(Out), typeof(InOut) })
            Assert.Equal(Flag(type, family), Flag(type, family + "30"));
        foreach (var percent in new[] { 10, 20, 30, 40, 50 })
        {
            var entry = Flag(typeof(In), family + percent);
            var exit = Flag(typeof(Out), family + percent);
            var pair = Flag(typeof(InOut), family + percent);
            var low = 0f;
            var high = 1f;
            var pairLow = 0f;
            var pairHigh = 1f;
            for (var i = 0; i <= 10000; i++)
            {
                var t = i / 10000f;
                var a = Easing.Evaluate(entry, t);
                var b = Easing.Evaluate(exit, t);
                var both = Easing.Evaluate(pair, t);
                low = Math.Min(low, a);
                high = Math.Max(high, b);
                pairLow = Math.Min(pairLow, both);
                pairHigh = Math.Max(pairHigh, both);
                Assert.InRange(Math.Abs(a - (1 - Easing.Evaluate(exit, 1 - t))), 0, 0.000002f);
            }
            foreach (var peak in new[] { -low, high - 1, -pairLow, pairHigh - 1 })
                Assert.InRange(Math.Abs(peak - percent / 100f), 0, 0.00002f);
            foreach (var curve in new[] { entry, exit, pair })
            {
                Assert.Equal(0, Easing.Evaluate(curve, 0));
                Assert.Equal(1, Easing.Evaluate(curve, 1));
            }
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void BounceLevelsMeasureTheFirstReboundAndKeepThreeDiminishingBounces(int percent)
    {
        var entry = Flag(typeof(In), "Bounce" + percent);
        var exit = Flag(typeof(Out), "Bounce" + percent);
        var pair = Flag(typeof(InOut), "Bounce" + percent);
        foreach (var paired in new[] { false, true })
        {
            // Find the actual valleys; do not reuse the implementation's impact times.
            var samples = Enumerable.Range(0, 10001).Select(i =>
                Easing.Evaluate(paired ? pair : exit, (paired ? 0.5f : 0) + (paired ? 0.5f : 1) * i / 10000f)).ToArray();
            var depths = new List<float>();
            for (var i = 1; i < samples.Length - 1; i++)
                if (samples[i] < samples[i - 1] && samples[i] <= samples[i + 1]) depths.Add(1 - samples[i]);
            Assert.Equal(3, depths.Count);
            for (var i = 0; i < depths.Count; i++)
                Assert.InRange(Math.Abs(depths[i] - percent / 100f / MathF.Pow(4, i)), 0, 0.00002f);
        }
        foreach (var curve in new[] { entry, exit, pair })
        {
            Assert.Equal(0, Easing.Evaluate(curve, 0));
            Assert.Equal(1, Easing.Evaluate(curve, 1));
            for (var i = 0; i <= 1000; i++) Assert.InRange(Easing.Evaluate(curve, i / 1000f), -0.000001f, 1.000001f);
        }
        for (var i = 0; i <= 1000; i++)
        {
            var t = i / 1000f;
            Assert.InRange(Math.Abs(Easing.Evaluate(entry, t) - (1 - Easing.Evaluate(exit, 1-t))), 0, 0.000002f);
        }
        Assert.Equal(0.5f, Easing.Evaluate(pair, 0.5f));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void JumpHasThreePeaksAndLandsAtTheTargetBetweenThem(int percent)
    {
        var entry = Flag(typeof(In), "Jump" + percent);
        var exit = Flag(typeof(Out), "Jump" + percent);
        var pair = Flag(typeof(InOut), "Jump" + percent);
        foreach (var paired in new[] { false, true })
        {
            var samples = Enumerable.Range(0, 10001).Select(i =>
                Easing.Evaluate(paired ? pair : exit, (paired ? 0.5f : 0) + (paired ? 0.5f : 1) * i / 10000f)).ToArray();
            var peaks = new List<int>();
            var rising = false;
            var landed = false;
            for (var i = 1; i < samples.Length - 1; i++)
            {
                // Ignore equal float samples on either side of a rounded peak.
                if (samples[i] > samples[i - 1]) rising = true;
                else if (samples[i] < samples[i - 1] && rising) { peaks.Add(i - 1); rising = false; }
                if (samples[i] >= 1) landed = true;
                if (landed) Assert.InRange(samples[i], 0.999999f, 1 + percent / 100f + 0.000002f);
            }
            Assert.Equal(3, peaks.Count);
            for (var i = 0; i < peaks.Count; i++)
            {
                Assert.InRange(Math.Abs(samples[peaks[i]] - 1 - percent / 100f / MathF.Pow(4, i)), 0, 0.00002f);
                if (i != 0)
                    Assert.InRange(samples[peaks[i - 1]..(peaks[i] + 1)].Min(), 0.999999f, 1.001f);
            }
            // The launch rises straight to its first peak, with no initial dip or extra bounce.
            for (var i = 1; i <= peaks[0]; i++) Assert.True(samples[i] >= samples[i - 1] - 0.000001f);
        }
        Assert.Equal(0.5f, Easing.Evaluate(pair, 0.5f));
        Assert.Equal(entry | exit, pair);
    }

    [Fact]
    public void EveryDistinctSameLegCombinationAndLegacyMixIsRejected()
    {
        foreach (var type in new[] { typeof(In), typeof(Out) })
        {
            var curves = Families.Select(f => Flag(type, f)).Distinct().ToArray();
            for (var i = 0; i < curves.Length; i++)
            {
                for (var j = i + 1; j < curves.Length; j++)
                    Assert.Throws<NotImplementedException>(() => Easing.Evaluate(curves[i] | curves[j], 0.5f));
                foreach (var legacy in Enum.GetValues<EaseType>().Where(e => e != EaseType.Linear))
                    Assert.Throws<NotImplementedException>(() => Easing.Evaluate(curves[i] | legacy, 0.5f));
            }
        }
    }

    [Fact]
    public void BounceAliasesPreserveFlagValuesAndLegacyCurvesKeepTheirDepth()
    {
        foreach (var type in new[] { typeof(In), typeof(Out), typeof(InOut) })
            Assert.Equal(Flag(type, "Bounce"), Flag(type, "Bounce30"));
        Assert.Equal(1L << 18, (long)In.Bounce10);
        Assert.Equal(1L << 31, (long)Out.Bounce10);
        Assert.Equal(InOut.Bounce, In.Bounce30 | Out.Bounce);
        Assert.Equal(0.75f, Easing.Evaluate(EaseType.BounceOut, 6f/11), 6);
        Assert.Equal(0.875f, Easing.Evaluate(EaseType.BounceInOut, 17f/22), 6);
    }

    [Fact]
    public void OriginalFlagValuesAndLegacyElasticStrengthRemainStable()
    {
        Assert.Equal(1L << 16, (long)In.Back10);
        Assert.Equal(1L << 30, (long)Out.Elastic10);
        Assert.InRange(Easing.Evaluate(EaseType.ElasticOut, 0.13474f), 1.37f, 1.38f);
        Assert.Equal(InOut.Elastic, In.Elastic30 | Out.Elastic);
        Assert.Equal(InOut.Back, In.Back | Out.Back30);
    }

    [Theory]
    [InlineData(In.Elastic)]
    [InlineData(Out.Elastic)]
    [InlineData(In.Bounce50)]
    [InlineData(Out.Bounce50)]
    [InlineData(In.Bounce20 | Out.Bounce40)]
    [InlineData(In.Jump50)]
    [InlineData(Out.Jump10)]
    [InlineData(Out.Jump20)]
    [InlineData(Out.Jump30)]
    [InlineData(Out.Jump40)]
    [InlineData(Out.Jump50)]
    [InlineData(In.Jump20 | Out.Jump40)]
    public void CalibratedPlaybackUsesTheSameCurveAsTheViewer(EaseType ease)
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        scheduler.Add(box, new PlainTween { To = 1, Duration = 1, Ease = ease });
        scheduler.Update(0.3);
        Assert.Equal(Easing.Evaluate(ease, 0.3f), box.Value);
    }

    [Theory]
    [InlineData(In.Sine | In.Back)]
    [InlineData(Out.Sine | Out.Back)]
    [InlineData(In.Back20 | In.Elastic50)]
    [InlineData(Out.Back50 | Out.Elastic)]
    [InlineData(In.Bounce20 | In.Bounce50)]
    [InlineData(Out.Bounce40 | Out.Elastic50)]
    [InlineData(In.Sine | EaseType.BackOut)]
    [InlineData((EaseType)(1L << 62))]
    public void InvalidFlagsAreRejected(EaseType ease) =>
        Assert.Throws<NotImplementedException>(() => Easing.Evaluate(ease, 0.5f));

    [Theory]
    [InlineData(0.4f, BlendType.Makima)]
    [InlineData(0.6f, BlendType.Makima)]
    [InlineData(0.5f, BlendType.Makima)]
    [InlineData(0.4f, BlendType.Hermite)]
    [InlineData(0.6f, BlendType.Hermite)]
    [InlineData(0.5f, BlendType.Hermite)]
    public void BlendBoundariesHaveContinuousValueAndSlope(float t, BlendType method)
    {
        const float h = 0.0001f;
        var ease = In.Quad | Out.Cubic;
        var left = Easing.Evaluate(ease, t - h, method);
        var center = Easing.Evaluate(ease, t, method);
        var right = Easing.Evaluate(ease, t + h, method);
        Assert.InRange(Math.Abs((center - left) / h - (right - center) / h), 0, 0.01f);
    }

    [Theory]
    [InlineData(BlendType.Makima)]
    [InlineData(BlendType.Hermite)]
    public void MonotoneFamiliesDoNotAcquireOvershootOrReversals(BlendType method)
    {
        var monotone = Families.Where(f => !Calibrated(f)).ToArray();
        foreach (var entry in monotone)
        foreach (var exit in monotone)
        foreach (var width in new[] { 0.0, 0.01, 0.2, 0.4, 0.8, 1.0 })
        {
            var ease = Flag(typeof(In), entry) | Flag(typeof(Out), exit);
            var previous = 0f;
            for (var i = 0; i <= 1000; i++)
            {
                var actual = Easing.Evaluate(ease, i / 1000f, method, width);
                Assert.InRange(actual, previous - 0.000001f, 1.000001f);
                previous = actual;
            }
        }
    }

    [Fact]
    public void MakimaMidpointVelocityStaysBetweenTheAdjacentSecants()
    {
        // Makima weights are nonnegative, so the shared velocity is a weighted mean of both halves' secants.
        foreach (var entry in Families)
        foreach (var exit in Families)
        foreach (var width in new[] { 0.2f, 0.6f })
        {
            var ease = Flag(typeof(In), entry) | Flag(typeof(Out), exit);
            if (ease == Flag(typeof(InOut), entry)) continue; // Matching families bypass the join.
            const float e = 0.0001f;
            var h = width / 2;
            var d0 = (0.5f - Easing.Evaluate(ease, 0.5f - h, blend: width)) / h;
            var d1 = (Easing.Evaluate(ease, 0.5f + h, blend: width) - 0.5f) / h;
            var velocity = (Easing.Evaluate(ease, 0.5f + e, blend: width) - Easing.Evaluate(ease, 0.5f - e, blend: width)) / (2 * e);
            Assert.InRange(velocity, Math.Min(d0, d1) - 0.02f, Math.Max(d0, d1) + 0.02f);
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
        var options = new TweenOptions { Duration = 1, Ease = In.Quad | Out.Cubic, BlendType = BlendType.Linear, Blend = 0.4 };
        var builder = new TweenOptionsBuilder();
        Assert.Equal(0.2, builder.Blend);
        Assert.Equal(default(TweenOptions), new TweenOptions { Blend = 0.2 });
        Assert.Equal(0.4041957f, Easing.Evaluate(options.Ease, 0.45f), 5);
        Assert.Equal(0.40125f, Easing.Evaluate(options.Ease, 0.45f, BlendType.Hermite), 5);
        options.CopyTo(builder);
        Assert.Equal(options, builder.ToOptions());
        Assert.Equal(0.2, default(TweenOptions).Blend);
        Assert.Equal(BlendType.Makima, default(TweenOptions).BlendType);
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
    public void FlagsWorkThroughPlaybackAndIndependentSplits()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var ease = In.Quad | Out.Cubic;
        var handle = scheduler.Add(box, new PlainTween { To = 1, Duration = 2, Ease = ease, Skew = 0.75, Weks = 0.25, PingPong = true });
        scheduler.Update(1);
        Assert.Equal(Easing.Evaluate(ease, 0.5f, skew: 0.75), box.Value);
        scheduler.Update(2);
        Assert.Equal(Easing.Evaluate(ease, 0.5f, skew: 0.75), box.Value, 5);
        scheduler.Update(1);
        Assert.Equal(0, box.Value);
        Assert.True(handle.IsTerminal);
    }
}
