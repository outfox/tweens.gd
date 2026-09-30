// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

public class DurationTests
{
    [Fact]
    public void NumericSecondsKeepTheirPrecisionAndSupportExistingArithmetic()
    {
        Duration single = 0.1f;
        Duration precise = 1.23456789012345;
        double seconds = precise;
        Assert.Equal((double)0.1f, single.Seconds);
        Assert.Equal(1.23456789012345, seconds);
        Assert.Equal(seconds * 2 + 0.5, precise * 2 + 0.5);
        Assert.True(precise > single);
        Assert.True(precise == seconds);
        Assert.True(seconds == precise);
        precise += single;
        Assert.Equal(seconds + (double)0.1f, precise.Seconds);
    }

    [Fact]
    public void TimeSpanUsesTotalSecondsIncludingDaysAndFractionalTicks()
    {
        var time = TimeSpan.FromDays(2) + TimeSpan.FromSeconds(3) + TimeSpan.FromTicks(1);
        Duration duration = time;
        Assert.Equal(time.TotalSeconds, duration.Seconds);
        Duration tick = TimeSpan.FromTicks(1);
        Assert.Equal(0.0000001, tick.Seconds);
        Duration negative = -time;
        Assert.Equal(-time.TotalSeconds, negative.Seconds);
    }

    [Fact]
    public void DefaultAndEquivalentInputsHaveValueEquality()
    {
        Assert.Equal((Duration)TimeSpan.Zero, default(Duration));
        Assert.Equal((Duration)1.25f, (Duration)1.25);
        Assert.Equal((Duration)1.25, (Duration)TimeSpan.FromMilliseconds(1250));
        Assert.Equal(((Duration)1.25).GetHashCode(), ((Duration)TimeSpan.FromMilliseconds(1250)).GetHashCode());
    }

    [Fact]
    public void TimeSpanOptionsMatchNumericTimingThroughPlayback()
    {
        var options = new TweenOptions
        {
            Duration = TimeSpan.FromSeconds(1), FactorDuration = 2, DeltaDuration = TimeSpan.FromSeconds(-0.5),
            Delay = TimeSpan.FromSeconds(0.25), FactorDelay = 2, DeltaDelay = TimeSpan.FromSeconds(-0.25),
            PingPongInterval = TimeSpan.FromSeconds(0.25), RepeatInterval = TimeSpan.FromSeconds(0.5),
            Offset = TimeSpan.FromSeconds(0.125), UsePingPong = true, Repeats = 1,
        };
        var numeric = new TweenOptions
        {
            Duration = 1, FactorDuration = 2, DeltaDuration = -0.5, Delay = 0.25, FactorDelay = 2, DeltaDelay = -0.25,
            PingPongInterval = 0.25, RepeatInterval = 0.5, Offset = 0.125, UsePingPong = true, Repeats = 1,
        };
        Assert.Equal(numeric, options);
        var builder = new PlainTween();
        options.CopyTo(builder);
        Assert.Equal(numeric, builder.ToOptions());

        var actual = new Playback(builder.ToOptions());
        var expected = new Playback(numeric);
        for (var i = 0; i < 60; i++)
        {
            actual.Advance(0.125);
            expected.Advance(0.125);
            Assert.Equal(expected.Progress, actual.Progress);
            Assert.Equal(expected.State, actual.State);
            Assert.Equal(expected.Overshoot, actual.Overshoot);
        }
        Assert.True(actual.Completed);
    }

    [Fact]
    public void StructuredDefinitionsAndSchedulerAcceptTimeSpan()
    {
        using var scheduler = new TweenScheduler();
        var definition = new Tweens.Property<Box, float>(b => b.Value, (b, value) => b.Value = value,
            Interpolators.Float, 10, TimeSpan.FromSeconds(1), delay: TimeSpan.FromMilliseconds(250));
        var scaled = scheduler.Add(new Box(), definition);
        var unscaled = scheduler.Add(new Box(), definition with { UseUnscaledTime = true });
        scheduler.Update(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(750));
        Assert.Equal(2.5f, scaled.Value);
        Assert.Equal(5f, unscaled.Value);
        // Nullable numeric seconds still convert to the optional unscaled delta.
        double? unscaledDelta = null;
        scheduler.Update(0.5f, unscaledDelta);
        Assert.Equal(7.5f, scaled.Value);
        Assert.True(unscaled.IsTerminal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConversionPreservesInvalidValuesUntilPlaybackValidation(double seconds)
    {
        Duration duration = seconds;
        Assert.Equal(seconds, duration.Seconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Playback(new TweenOptions { Duration = duration }));
    }
}
