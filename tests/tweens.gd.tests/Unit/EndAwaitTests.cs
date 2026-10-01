// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

public class EndAwaitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingAndLateWaitersReceiveTheSameResult(bool cancel)
    {
        Task<Reason> first, second, together;
        var expected = cancel ? Reason.Cancelled : Reason.Completed;
        using (var scheduler = new TweenScheduler())
        {
            var tween = scheduler.Add(new Box(), new PlainTween { Duration = 1 });
            var group = Group.Of(tween);
            static async Task<Reason> WaitTween(TweenInstance value) => await value.End;
            static async Task<Reason> WaitGroup(Group value) => await value.End;
            first = WaitTween(tween);
            second = WaitTween(tween);
            together = WaitGroup(group);
            Assert.False(first.IsCompleted);
            Assert.False(together.IsCompleted);
            if (cancel) tween.Cancel();
            else scheduler.Update(1);

            // Late awaits complete synchronously on the scheduler's creating thread.
            Assert.True(tween.End.IsCompleted);
            Assert.True(group.End.IsCompleted);
            Assert.Equal(expected, await tween.End);
            Assert.Equal(expected, await group.End);
        }
        // xUnit may resume pending waiters on another worker. No scheduler access follows.
        Assert.Equal(expected, await first);
        Assert.Equal(expected, await second);
        Assert.Equal(expected, await together);
    }

    [Fact]
    public async Task EndAwaitPropagatesTweenAndGroupFaults()
    {
        var error = new FormatException("callback failed");
        Task pendingTween, pendingGroup, lateTween, lateGroup;
        using (var scheduler = new TweenScheduler())
        {
            var tween = scheduler.Add(new Box(), new PlainTween { OnStart = _ => throw error });
            var group = Group.Of(tween);
            async Task WaitTween() { await tween.End; }
            async Task WaitGroup() { await group.End; }
            pendingTween = WaitTween();
            pendingGroup = WaitGroup();
            scheduler.Update(0);
            lateTween = WaitTween();
            lateGroup = WaitGroup();
        }
        Assert.Same(error, await Assert.ThrowsAsync<FormatException>(() => pendingTween));
        Assert.Same(error, await Assert.ThrowsAsync<FormatException>(() => pendingGroup));
        Assert.Same(error, await Assert.ThrowsAsync<FormatException>(() => lateTween));
        Assert.Same(error, await Assert.ThrowsAsync<FormatException>(() => lateGroup));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndAwaitStartsAnIndependentRoot(bool grouped)
    {
        using var scheduler = new TweenScheduler();
        var next = new Box();
        var first = scheduler.Add(new Box(), new PlainTween { Duration = 1 });
        var previous = SynchronizationContext.Current;
        Task sequence;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            sequence = Continue();
            scheduler.Update(1.25);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        Assert.True(sequence.IsCompletedSuccessfully);
        scheduler.Update(0.25);
        Assert.Equal(0.25f, next.Value);

        async Task Continue()
        {
            if (grouped) await Group.Of(first).End;
            else await first.End;
            scheduler.Add(next, new PlainTween { To = 1, Duration = 1 });
        }
    }
}
