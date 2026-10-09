// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

public class MonotonicClockTests
{
    [Fact]
    public void SamplesElapsedSecondsAndIgnoresBackwardJumps()
    {
        var readings = new Queue<ulong>([1_000_000, 1_500_000, 1_200_000, 3_200_000]);
        var clock = new MonotonicClock(readings.Dequeue);
        Assert.Equal(0.5, clock.Sample());
        Assert.Equal(0, clock.Sample());
        Assert.Equal(2, clock.Sample());
    }

    [Fact]
    public void PausedUnscaledTweensDoNotAccumulateSampledTime()
    {
        ulong microseconds = 100;
        var clock = new MonotonicClock(() => microseconds);
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var tween = scheduler.Add(box, new PlainTween { To = 10, Duration = 1 },
            new PlaybackOptions { UseUnscaledTime = true });
        microseconds += 250_000;
        scheduler.Update(0, clock.Sample());
        Assert.Equal(2.5f, box.Value);
        tween.Pause();
        microseconds += 10_000_000;
        scheduler.Update(0, clock.Sample());
        tween.Resume();
        microseconds += 250_000;
        scheduler.Update(0, clock.Sample());
        Assert.Equal(5, box.Value);
    }
}
