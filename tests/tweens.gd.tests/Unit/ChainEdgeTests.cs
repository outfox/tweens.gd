// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

public class ChainEdgeTests
{
    [Theory]
    [InlineData("target")]
    [InlineData("definitions")]
    [InlineData("first")]
    [InlineData("later")]
    [InlineData("endpoints")]
    [InlineData("delay")]
    [InlineData("process")]
    [InlineData("pause")]
    public void RejectedFactoriesDoNotActivateOrReleaseSnapshots(string invalid)
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 7 };
        var hooks = new List<string>();
        ProbeTween Definition() => new() {
            To = 10, Duration = 1,
            Preparing = _ => hooks.Add("prepare"), Reader = b => { hooks.Add("read"); return b.Value; },
            Writer = (b, v) => { hooks.Add("write"); b.Value = v; },
            OnAdd = _ => hooks.Add("add"), OnStart = _ => hooks.Add("start"),
            OnEnd = _ => hooks.Add("end"), OnCancel = _ => hooks.Add("cancel"),
            OnFinally = _ => hooks.Add("finally"), Releasing = () => hooks.Add("release"),
        };
        var first = Definition();
        var second = Definition();
        ITweenDefinition<Box>[] definitions = [first, second];
        var options = default(PlaybackOptions);
        switch (invalid)
        {
            case "first": definitions[0] = null!; break;
            case "later": definitions[1] = null!; break;
            case "endpoints": second.By = 1; break;
            case "delay": second.Delay = double.MaxValue; second.FactorDelay = 2; break;
            case "process": options = new() { ProcessMode = (TweenProcessMode)99 }; break;
            case "pause": options = new() { PauseMode = (TweenPauseMode)99 }; break;
        }
        var survivor = scheduler.Add(new Box(), new PlainTween { Duration = 1 });

        var error = Record.Exception(() => scheduler.AddChain(
            invalid == "target" ? null! : box,
            invalid == "definitions" ? null! : definitions, options: options));

        Assert.IsAssignableFrom<ArgumentException>(error);
        if (invalid is "target" or "definitions" or "first" or "later")
            Assert.IsType<ArgumentNullException>(error);
        if (invalid == "delay") Assert.IsType<ArgumentOutOfRangeException>(error);
        Assert.Empty(hooks);
        Assert.Equal(7, box.Value);
        Assert.Equal(1, scheduler.ActiveCount);
        scheduler.Update(1);
        Assert.True(survivor.IsSettled);
        Assert.Empty(hooks);
        var valid = scheduler.AddChain(box, new ITweenDefinition<Box>[] { new PlainTween { To = 9, Duration = 1 } });
        scheduler.Update(1);
        Assert.True(valid.IsSettled);
        Assert.Equal(9, box.Value);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("end")]
    [InlineData("negative-start")]
    public void IndividuallyFiniteEntriesRejectAnOverflowingCombinedSchedule(string overflow)
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = 3 };
        var hooks = 0;
        ProbeTween Definition() => new() {
            Preparing = _ => hooks++, Reader = b => { hooks++; return b.Value; },
            Writer = (_, _) => hooks++, OnAdd = _ => hooks++, OnCancel = _ => hooks++,
            OnFinally = _ => hooks++, Releasing = () => hooks++,
        };
        var first = Definition();
        var second = Definition();
        var large = double.MaxValue * 0.75;
        if (overflow == "negative-start") first.Delay = second.Delay = -large;
        else
        {
            first.Duration = large;
            if (overflow == "start") second.Delay = large;
            else second.Duration = large;
        }

        // Each definition can be enrolled on its own without overflowing its local clock.
        scheduler.Add(box, first).Cancel();
        scheduler.Add(box, second).Cancel();
        var error = Assert.Throws<ArgumentException>(() => scheduler.AddChain(box,
            new ITweenDefinition<Box>[] { first, second }));

        Assert.Contains("schedule overflows", error.Message);
        scheduler.Update(0);
        Assert.Equal(0, hooks);
        Assert.Equal(3, box.Value);
        Assert.Equal(0, scheduler.ActiveCount);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("end")]
    [InlineData("cancel")]
    public async Task FaultsCollectEveryActiveLeafFailureOnceAndDiscardPendingWork(string failure)
    {
        using var scheduler = new TweenScheduler();
        var errors = new Exception[] {
            new FormatException("first callback"), new InvalidOperationException("first finally"),
            new IOException("first release"), new FormatException("second cancel"),
            new InvalidOperationException("second finally"), new IOException("second release"),
        };
        var releases = 0;
        var pendingHooks = 0;
        var reports = new List<Exception>();
        scheduler.UnhandledException += reports.Add;
        TweenInstance? firstLeaf = null, secondLeaf = null;
        var first = new ProbeTween {
            From = 0, To = 10, Duration = 1, OnAdd = h => firstLeaf = h,
            OnUpdate = (_, value) => { if (failure == "update" && value == 5) throw errors[0]; },
            OnEnd = _ => { if (failure == "end") throw errors[0]; },
            OnCancel = _ => { if (failure == "cancel") throw errors[0]; },
            OnFinally = _ => throw errors[1], Releasing = () => { releases++; throw errors[2]; },
        };
        var second = new ProbeTween {
            From = 0, To = 20, Duration = 3, Delay = -1, OnAdd = h => secondLeaf = h,
            OnCancel = _ => throw errors[3], OnFinally = _ => throw errors[4],
            Releasing = () => { releases++; throw errors[5]; },
        };
        var chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            first, second, new ProbeTween {
                Duration = 1, Preparing = _ => pendingHooks++, OnAdd = _ => pendingHooks++,
                OnCancel = _ => pendingHooks++, OnFinally = _ => pendingHooks++, Releasing = () => pendingHooks++,
            },
        });
        var end = chain.End;
        Assert.Null(chain.Error);
        scheduler.Update(0);
        Assert.Equal(2, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        var firstEnd = firstLeaf!.End;
        var secondEnd = secondLeaf!.End;
        var independent = new Box();
        var survivor = scheduler.Add(independent, new PlainTween { To = 4, Duration = 1 });

        if (failure == "cancel") chain.Cancel();
        else scheduler.Update(failure == "end" ? 1 : 0.5);

        var error = await Assert.ThrowsAsync<AggregateException>(() => end);
        Assert.Same(error, chain.Error);
        Assert.Equal(errors.Length, error.Flatten().InnerExceptions.Count);
        Assert.All(errors, expected => Assert.Contains(expected, error.Flatten().InnerExceptions));
        Assert.Same(error, Assert.Single(reports));
        Assert.Equal(2, releases);
        Assert.Equal(0, pendingHooks);
        Assert.True(chain.IsTerminal);
        Assert.True(chain.IsSettled);
        Assert.Equal(failure == "end" ? Reason.Completed : Reason.Cancelled, chain.CompletionReason);
        Assert.Equal(3, chain.EntryCount);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(0, chain.PendingCount);
        await Assert.ThrowsAsync<AggregateException>(() => firstEnd);
        await Assert.ThrowsAsync<AggregateException>(() => secondEnd);
        scheduler.Update(1);
        Assert.True(survivor.IsSettled);
        Assert.Equal(4, independent.Value);
        Assert.Single(reports);
        Assert.Equal(0, scheduler.ActiveCount);
    }

    [Fact]
    public async Task CompletedLeafControlsCannotPauseResumeOrCancelItsRemainingChain()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        TweenInstance? completed = null;
        var ends = 0;
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { From = 0, To = 10, Duration = 1, OnAdd = h => completed = h, OnEnd = _ => ends++ },
            new PlainTween { From = 0, To = 20, Duration = 1, Delay = -0.5 },
            new PlainTween { To = 30, Duration = 1, Delay = 0.5 },
        });
        Assert.Equal(3, chain.EntryCount);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(3, chain.PendingCount);
        Assert.Equal(1, scheduler.ActiveCount);
        scheduler.Update(0.75);
        Assert.Equal(2, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        scheduler.Update(0.25);
        Assert.Equal(Reason.Completed, await completed!.End);
        Assert.Equal(1, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        var leafEnd = completed.End;

        completed.Pause();
        completed.Cancel();
        Assert.False(chain.IsPaused);
        Assert.False(chain.IsTerminal);
        chain.Pause();
        Assert.True(completed.IsPaused);
        completed.Resume();
        Assert.True(chain.IsPaused);
        scheduler.Update(10);
        Assert.Equal(1, chain.Elapsed);
        chain.Resume();
        scheduler.Update(0.5);
        Assert.Equal(1, chain.ActiveCount); // The third entry is captured, but still in its delay.
        Assert.Equal(0, chain.PendingCount);
        Assert.Equal(20, box.Value);
        scheduler.Update(1.5);

        Assert.Equal(Reason.Completed, await chain.AwaitDecommissionAsync());
        chain.Pause();
        chain.Resume();
        chain.Cancel();
        Assert.False(chain.IsPaused);
        Assert.Null(chain.Error);
        Assert.Same(leafEnd, completed.End);
        Assert.Equal(1, ends);
        Assert.Equal(30, box.Value);
        Assert.Equal(3, chain.EntryCount);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(0, chain.PendingCount);
        Assert.Equal(0, scheduler.ActiveCount);
    }

    [Theory]
    [InlineData("ease")]
    [InlineData("interpolate")]
    [InlineData("relative-interpolate")]
    [InlineData("follow-read")]
    public async Task MidSamplePauseDefersTheWriteAndDiscardsTheUnusedDelta(string hook)
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        Chain? chain = null;
        TweenInstance? leaf = null;
        var paused = false;
        var writes = new List<float>();
        var successorAdds = 0;
        void PauseOnce()
        {
            if (!paused) { paused = true; chain!.Pause(); }
        }
        var relative = hook is "relative-interpolate" or "follow-read";
        chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new ProbeTween {
                To = relative ? null : 10, By = relative ? 10 : null, Duration = 1,
                OnAdd = h => leaf = h,
                EaseFunction = t => { if (hook == "ease" && t == 0.5f) PauseOnce(); return t; },
                Interpolator = (a, b, w) => {
                    if ((hook is "interpolate" or "relative-interpolate") && w == 0.5f) PauseOnce();
                    return Interpolators.Float(a, b, w);
                },
                Reader = b => { if (hook == "follow-read" && leaf?.Progress == 0.5f) PauseOnce(); return b.Value; },
                Writer = (b, v) => { writes.Add(v); b.Value = v; },
            },
            // Its activation supplies a midpoint boundary; its samples do not overwrite the first property.
            new ProbeTween { Duration = 1, Delay = -0.5, Writer = (_, _) => { }, OnAdd = _ => successorAdds++ },
        });
        scheduler.Update(0);
        scheduler.Update(10);
        Assert.True(paused);
        Assert.True(chain.IsPaused);
        Assert.Equal(0.5, chain.Elapsed);
        Assert.Equal(0, box.Value);
        Assert.Equal(new[] { 0f }, writes);
        Assert.Equal(0, successorAdds);
        Assert.Equal(1, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        chain.Resume();
        scheduler.Update(0.25);
        Assert.Equal(0.75, chain.Elapsed);
        Assert.Equal(7.5f, box.Value);
        Assert.Equal(new[] { 0f, 5f, 7.5f }, writes);
        Assert.Equal(1, successorAdds);
        Assert.False(chain.IsTerminal);
        scheduler.Update(0.75);
        Assert.Equal(Reason.Completed, await chain.End);
        Assert.Equal(new[] { 0f, 5f, 7.5f, 10f }, writes);
        Assert.Equal(1, successorAdds);
    }

    [Fact]
    public async Task PauseDuringRestorationReadPreservesExternalChangesAndDoesNotReadTwice()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        Chain? chain = null;
        var restoreReads = 0;
        var restores = new List<float>();
        var updates = new List<float>();
        var successorAdds = 0;
        chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new ProbeTween {
                By = 10, Duration = 1, Fill = FillMode.None,
                Reader = b => {
                    if (b.Value == 130) { restoreReads++; chain!.Pause(); }
                    return b.Value;
                },
                OnUpdate = (_, v) => { updates.Add(v); if (v == 110) box.Value += 20; },
                Restoring = (b, v) => { restores.Add(v); b.Value = v; },
            },
            new PlainTween { To = 140, Duration = 1, OnAdd = _ => successorAdds++ },
        });
        scheduler.Update(0.5);
        box.Value += 100;
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.Equal(1, chain.Elapsed);
        Assert.Equal(130, box.Value);
        Assert.Equal(new[] { 0f, 5f, 110f }, updates);
        Assert.Empty(restores);
        Assert.Equal(0, successorAdds);
        chain.Resume();
        scheduler.Update(0);
        Assert.Equal(new[] { 120f }, restores);
        Assert.Equal(1, restoreReads);
        Assert.Equal(1, successorAdds);
        Assert.Equal(120, box.Value);
        scheduler.Update(0.5);
        Assert.Equal(130, box.Value);
        scheduler.Update(0.5);
        Assert.Equal(Reason.Completed, await chain.End);
        Assert.Equal(140, box.Value);
        Assert.Equal(new[] { 0f, 5f, 110f, 120f }, updates);
    }

    [Fact]
    public async Task DeferredUpdateCanPauseAgainWithoutRepeatingItsWriteOrNotification()
    {
        using var scheduler = new TweenScheduler();
        Chain? chain = null;
        var box = new Box();
        var writes = new List<float>();
        var updates = new List<float>();
        chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new ProbeTween {
                To = 10, Duration = 1,
                Writer = (b, v) => { writes.Add(v); b.Value = v; if (writes.Count == 1) chain!.Pause(); },
                OnUpdate = (_, v) => { updates.Add(v); if (updates.Count == 1) chain!.Pause(); },
            },
        });
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.Equal(new[] { 0f }, writes);
        Assert.Empty(updates);
        chain.Resume();
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.Equal(0, chain.Elapsed);
        Assert.Equal(new[] { 0f }, writes);
        Assert.Equal(new[] { 0f }, updates);
        chain.Resume();
        scheduler.Update(0.5);
        Assert.Equal(new[] { 0f, 5f }, writes);
        Assert.Equal(writes, updates);
        scheduler.Update(0.5);
        Assert.Equal(Reason.Completed, await chain.End);
        Assert.Equal(new[] { 0f, 5f, 10f }, updates);
    }

    [Fact]
    public async Task RecursiveUpdateFaultsOnlyTheCallingChainAndDiscardsItsPendingEntries()
    {
        using var scheduler = new TweenScheduler();
        var reports = new List<Exception>();
        scheduler.UnhandledException += reports.Add;
        var releases = 0;
        var pendingHooks = 0;
        var chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new ProbeTween {
                Duration = 1, OnUpdate = (_, _) => scheduler.Update(0), Releasing = () => releases++,
            },
            new ProbeTween { Preparing = _ => pendingHooks++, OnFinally = _ => pendingHooks++, Releasing = () => pendingHooks++ },
        });
        var box = new Box();
        var survivor = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { To = 1, Duration = 0.5 }, new PlainTween { To = 2, Duration = 0.5 },
        });
        scheduler.Update(0.5);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => chain.End);
        Assert.Contains("Recursive", error.Message);
        Assert.Same(error, chain.Error);
        Assert.Same(error, Assert.Single(reports));
        Assert.Equal(Reason.Cancelled, chain.CompletionReason);
        Assert.True(chain.IsSettled);
        Assert.Equal(1, releases);
        Assert.Equal(0, pendingHooks);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(0, chain.PendingCount);
        Assert.Equal(0.5, survivor.Elapsed);
        Assert.Equal(1, box.Value);
        Assert.Equal(1, scheduler.ActiveCount);
        scheduler.Update(0.5);
        Assert.Equal(Reason.Completed, await survivor.End);
        Assert.Equal(2, box.Value);
        Assert.Single(reports);
    }

    [Fact]
    public async Task DisposalInsideAChainHookSettlesAfterCleanupAndRetainsTerminalFailures()
    {
        using var scheduler = new TweenScheduler();
        Chain? chain = null;
        Task<Reason>? end = null;
        var releaseError = new IOException("release");
        var releases = 0;
        var pendingHooks = 0;
        var independent = new Box();
        chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new ProbeTween {
                Duration = 1,
                OnUpdate = (_, _) => {
                    scheduler.Dispose();
                    Assert.True(chain!.IsTerminal);
                    Assert.False(chain.IsSettled);
                    Assert.False(end!.IsCompleted);
                },
                OnCancel = _ => scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] { new PlainTween() }),
                Releasing = () => { releases++; Assert.False(end!.IsCompleted); throw releaseError; },
            },
            new ProbeTween { Preparing = _ => pendingHooks++, OnFinally = _ => pendingHooks++, Releasing = () => pendingHooks++ },
        });
        end = chain.End;
        var other = scheduler.Add(independent, new PlainTween { To = 10, Duration = 1 });
        scheduler.Update(10);

        var error = await Assert.ThrowsAsync<AggregateException>(() => end);
        Assert.Collection(error.Flatten().InnerExceptions,
            e => Assert.IsType<ObjectDisposedException>(e), e => Assert.Same(releaseError, e));
        Assert.Same(error, chain.Error);
        Assert.Equal(Reason.RunnerDisposed, chain.CompletionReason);
        Assert.True(chain.IsSettled);
        Assert.Equal(1, releases);
        Assert.Equal(0, pendingHooks);
        Assert.Equal(0, independent.Value);
        Assert.Equal(Reason.RunnerDisposed, await other.End);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(0, chain.PendingCount);
        Assert.Equal(0, scheduler.ActiveCount);
        scheduler.Dispose();
        Assert.Throws<ObjectDisposedException>(() => scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] { new PlainTween() }));
    }
}
