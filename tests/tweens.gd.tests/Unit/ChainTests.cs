// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Text.Json;
using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

public class ChainTests
{
    [Fact]
    public async Task SharedTracesMatch()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance", "chains.json")));
        foreach (var test in data.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var scheduler = new TweenScheduler();
            var box = new Box();
            var trace = new List<string>();
            var definitions = test.GetProperty("entries").EnumerateArray().Select((entry, i) =>
            {
                double Number(string name) => entry.TryGetProperty(name, out var value) ? value.GetDouble() : 0;
                return (ITweenDefinition<Box>)new PlainTween
                {
                    From = entry.TryGetProperty("from", out var from) ? from.GetSingle() : null,
                    To = (float)Number("to"), Duration = Number("duration"), Delay = Number("delay"), Offset = Number("offset"),
                    Repeats = (int)Number("repeats"), RepeatInterval = Number("repeat_interval"), PingPongInterval = Number("ping_pong_interval"),
                    PingPong = entry.TryGetProperty("ping_pong", out var ping) && ping.GetBoolean(),
                    OnAdd = _ => trace.Add($"add:{i}"), OnStart = _ => trace.Add($"start:{i}"),
                    OnEnd = _ => { trace.Add($"end:{i}"); if (entry.TryGetProperty("end_set", out var value)) box.Value = value.GetSingle(); },
                };
            }).ToArray();
            var chain = scheduler.AddChain(box, definitions);
            Assert.Empty(trace);
            Assert.Equal(definitions.Length, chain.PendingCount);
            Assert.Equal(1, scheduler.ActiveCount);
            Assert.Equal(test.GetProperty("duration").GetDouble(), chain.Duration, 9);
            foreach (var sample in test.GetProperty("samples").EnumerateArray())
            {
                if (sample.TryGetProperty("set", out var value)) box.Value = value.GetSingle();
                else scheduler.Update(sample.GetProperty("delta").GetDouble());
                Assert.True(Math.Abs(box.Value - sample.GetProperty("value").GetDouble()) < 0.000001,
                    $"{test.GetProperty("name")}: got {box.Value}, expected {sample.GetProperty("value")}");
                if (sample.TryGetProperty("trace", out var expected)) Assert.Equal(expected.EnumerateArray().Select(e => e.GetString()), trace);
            }
            Assert.True(chain.IsSettled, test.GetProperty("name").GetString());
            Assert.Equal(Reason.Completed, await chain.End);
            Assert.Equal(0, scheduler.ActiveCount);
        }
    }

    [Theory]
    [InlineData(1.0 / 120)]
    [InlineData(1.0 / 40)]
    [InlineData(0.3)]
    [InlineData(10)]
    public void LinksDoNotStretchAcrossUpdates(double delta)
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { To = 10, Duration = 0.15 },
            new PlainTween { To = 20, Duration = 0.1 },
            new PlainTween { To = 30, Duration = 0.1 },
        });
        var time = 0.0;
        while (time < chain.Duration) { scheduler.Update(delta); time += delta; }
        Assert.True(chain.IsSettled);
        Assert.Equal(30, box.Value);
    }

    [Fact]
    public async Task LeafPausePreservesBoundaryPhaseAndDiscardsUnusedFrame()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        TweenInstance? leaf = null;
        var adds = 0;
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { To = 10, Duration = 1, OnStart = h => { leaf = h; h.Pause(); }, OnAdd = _ => adds++ },
            new PlainTween { To = 20, Duration = 1 },
        });
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.Equal(0, chain.Elapsed);
        Assert.Equal(0, box.Value);
        leaf!.Resume();
        scheduler.Update(0.5);
        Assert.Equal(5, box.Value);
        Assert.Equal(1, adds);
        Assert.False(chain.IsTerminal);
        leaf.Cancel();
        Assert.Equal(Reason.Cancelled, await chain.End);
        Assert.Equal(0, chain.PendingCount);
    }

    [Fact]
    public void PausedFactoryDefersCaptureAndSnapshotsConfiguration()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var definition = new PlainTween { To = 10, Duration = 1 };
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] { definition });
        chain.Pause();
        definition.To = 99;
        box.Value = 4;
        scheduler.Update(1);
        Assert.Equal(4, box.Value);
        chain.Resume();
        scheduler.Update(0.5);
        Assert.Equal(7, box.Value);
    }

    [Fact]
    public async Task FailedPreparationStopsPendingEntriesAndSettlesAfterCleanup()
    {
        using var scheduler = new TweenScheduler();
        var releases = 0;
        var adds = 0;
        var chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new ProbeTween { Duration = 1, Preparing = _ => throw new FormatException("prepare"), Releasing = () => releases++ },
            new ProbeTween { Duration = 1, OnAdd = _ => adds++, Releasing = () => releases++ },
        });
        scheduler.Update(10);
        await Assert.ThrowsAsync<FormatException>(() => chain.End);
        Assert.Equal(1, releases);
        Assert.Equal(0, adds);
    }

    [Fact]
    public void SignedSoloDelayUsesPreRollAndLazyCapture()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var handle = scheduler.Add(box, new PlainTween { To = 10, Duration = 1, Delay = -0.25 });
        box.Value = 2;
        scheduler.Update(0);
        Assert.Equal(4, box.Value);
        scheduler.Update(0.75);
        Assert.Equal(10, box.Value);
        Assert.True(handle.IsSettled);
    }

    [Fact]
    public void EmptyInvalidAndInfiniteSchedulesRejectWithoutHooks()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        Assert.Throws<ArgumentException>(() => scheduler.AddChain(box, Array.Empty<ITweenDefinition<Box>>()));
        Assert.Throws<ArgumentException>(() => scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { Duration = 1, Repeats = TweenOptions.Infinite }, new PlainTween() }));
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] { new PlainTween { Duration = 1, Repeats = TweenOptions.Infinite } });
        Assert.True(double.IsPositiveInfinity(chain.Duration));
        scheduler.Update(100);
        Assert.False(chain.IsTerminal);
        chain.Cancel();
        Assert.True(chain.IsSettled);
    }

    [Fact]
    public void CallbackAndSingleGroupChainCompletionStartIndependentRootsWithoutCredit()
    {
        using var scheduler = new TweenScheduler();
        var boxes = Enumerable.Range(0, 4).Select(_ => new Box()).ToArray();
        void Start(int i) => scheduler.Add(boxes[i], new PlainTween { To = 10, Duration = 1 });
        var solo = scheduler.Add(new Box(), new PlainTween { Duration = 1, OnEnd = _ => Start(0) });
        var group = Group.Of(solo, scheduler.Add(new Box(), new PlainTween { Duration = 0.5 }));
        var chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] { new PlainTween { Duration = 1 } });
        solo.End.ContinueWith(_ => Start(1), TaskContinuationOptions.ExecuteSynchronously);
        group.End.ContinueWith(_ => Start(2), TaskContinuationOptions.ExecuteSynchronously);
        chain.End.ContinueWith(_ => Start(3), TaskContinuationOptions.ExecuteSynchronously);
        scheduler.Update(10);
        Assert.All(boxes, b => Assert.Equal(0, b.Value));
        scheduler.Update(0.25);
        Assert.All(boxes, b => Assert.Equal(2.5f, b.Value));
    }
    [Theory]
    [InlineData("prepare")]
    [InlineData("read")]
    [InlineData("adjust")]
    [InlineData("add")]
    [InlineData("start")]
    [InlineData("update")]
    [InlineData("restore")]
    [InlineData("write")]
    [InlineData("end")]
    public void EveryHookCanPauseWithoutReplayingOrAdvancingSuccessors(string hook)
    {
        using var scheduler = new TweenScheduler();
        Chain? chain = null;
        var events = new List<string>();
        var paused = false;
        void Hook(string name)
        {
            events.Add(name);
            if (hook == name && !paused) { paused = true; chain!.Pause(); }
        }
        var definition = new ProbeTween {
            From = 0, To = 10, Duration = 1, FactorFrom = 2, Fill = FillMode.None,
            Preparing = _ => Hook("prepare"),
            Reader = b => { Hook("read"); return b.Value; },
            Interpolator = (a, b, w) => { Hook(w == 2 ? "adjust" : "interpolate"); return a + (b - a) * w; },
            Writer = (b, v) => { b.Value = v; Hook("write"); },
            OnAdd = _ => Hook("add"), OnStart = _ => Hook("start"),
            OnUpdate = (_, _) => Hook("update"), OnEnd = _ => Hook("end"),
            Restoring = (b, v) => { b.Value = v; Hook("restore"); },
        };
        chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            definition, new PlainTween { To = 20, Duration = 1, OnAdd = _ => events.Add("next") } });
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.DoesNotContain("next", events);
        var count = events.Count(e => e == hook);
        chain.Resume();
        scheduler.Update(10);
        Assert.True(chain.IsSettled);
        if (hook is not ("update" or "write")) Assert.Equal(count, events.Count(e => e == hook));
        Assert.Equal(1, events.Count(e => e == "next"));
    }

    [Fact]
    public async Task ReentrantCancellationCollectsSiblingAndReleaseFaultsBeforePublishingCompletion()
    {
        using var scheduler = new TweenScheduler();
        Chain? chain = null;
        Task<Reason>? end = null;
        var releases = 0;
        var errors = new List<Exception>();
        scheduler.UnhandledException += errors.Add;
        var second = new ProbeTween { Duration = 1, Delay = -1,
            OnCancel = _ => throw new FormatException("cancel"), Releasing = () => { releases++; throw new IOException("release B"); } };
        var first = new ProbeTween { Duration = 2, Releasing = () => { releases++; throw new IOException("release A"); },
            OnUpdate = (_, v) => {
                if (v == 1.5f) { chain!.Cancel(); Assert.False(end!.IsCompleted); Assert.False(chain.IsSettled); }
            }, To = 2 };
        chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] { first, second });
        end = chain.End;
        scheduler.Update(1); // Activates overlapping second entry.
        scheduler.Update(0.5);
        var error = await Assert.ThrowsAsync<AggregateException>(() => end);
        Assert.Equal(3, error.Flatten().InnerExceptions.Count);
        Assert.Equal(2, releases);
        Assert.Equal(Reason.Cancelled, chain.CompletionReason);
        Assert.Equal(2, chain.EntryCount);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Single(errors);
    }

    [Fact]
    public async Task WaitCancellationDoesNotStopPlaybackAndZeroDeltaStillSamples()
    {
        using var scheduler = new TweenScheduler();
        using var token = new CancellationTokenSource();
        var updates = 0;
        var chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new PlainTween { Duration = 1, OnUpdate = (_, _) => updates++ } });
        var wait = chain.AwaitDecommissionAsync(token.Token);
        token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        scheduler.Update(0);
        scheduler.Update(0);
        Assert.Equal(2, updates);
        Assert.False(chain.IsTerminal);
        scheduler.Update(1);
        Assert.Equal(Reason.Completed, await chain.End);
    }

    [Fact]
    public void MixedRootsPreserveEnrollmentOrderAndPreRollCancellationDiscardsLaterHistory()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var trace = new List<string>();
        scheduler.Add(box, new PlainTween { To = 1, Duration = 1, OnUpdate = (_, _) => trace.Add("solo A") });
        scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { To = 2, Duration = 1, OnUpdate = (_, _) => trace.Add("chain") } });
        scheduler.Add(box, new PlainTween { To = 3, Duration = 1, OnUpdate = (_, _) => trace.Add("solo B") });
        scheduler.Update(0);
        Assert.Equal(new[] { "solo A", "chain", "solo B" }, trace);
        trace.Clear();
        var preRoll = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new PlainTween { Duration = 1, Delay = -2, OnStart = h => { trace.Add("past"); h.Cancel(); } },
            new PlainTween { Duration = 0, OnAdd = _ => trace.Add("future") } });
        scheduler.Update(0);
        Assert.Equal(Reason.Cancelled, preRoll.CompletionReason);
        Assert.Contains("past", trace);
        Assert.DoesNotContain("future", trace);
    }

    [Fact]
    public void PausingDuringPriorityReassertionResumesWithoutRepeatingTheWrite()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var olderActive = false;
        var paused = false;
        var laterWrites = 0;
        var chain = scheduler.AddChain(box, new ITweenDefinition<Box>[] {
            new PlainTween { From = 0, To = 10, Duration = 1, OnAdd = _ => olderActive = true },
            new PlainTween { From = 0, To = 20, Duration = 2, Delay = -1.5, OnUpdate = (h, value) => {
                laterWrites++;
                if (olderActive && value == 5 && !paused) { paused = true; h.Pause(); }
            } },
        });
        scheduler.Update(10);
        Assert.True(chain.IsPaused);
        Assert.Equal(5, box.Value);
        Assert.Equal(3, laterWrites);
        chain.Resume();
        scheduler.Update(0.5);
        Assert.Equal(10, box.Value);
        Assert.Equal(4, laterWrites);
        Assert.Equal(0.5, chain.Elapsed);
        scheduler.Update(1);
        Assert.True(chain.IsSettled);
    }

    [Fact]
    public void WriterPauseDefersItsUpdateNotificationWithoutRepeatingTheWrite()
    {
        using var scheduler = new TweenScheduler();
        Chain? chain = null;
        var writes = 0;
        var updates = new List<float>();
        chain = scheduler.AddChain(new Box(), new ITweenDefinition<Box>[] {
            new ProbeTween { From = 0, To = 10, Duration = 1,
                Writer = (box, value) => { box.Value = value; if (++writes == 1) chain!.Pause(); },
                OnUpdate = (_, value) => updates.Add(value),
            },
        });
        scheduler.Update(10);
        Assert.Equal(1, writes);
        Assert.Empty(updates);
        chain.Resume();
        scheduler.Update(0.5);
        Assert.Equal(2, writes);
        Assert.Equal(new[] { 0f, 5f }, updates);
        scheduler.Update(0.5);
        Assert.True(chain.IsSettled);
        Assert.Equal(new[] { 0f, 5f, 10f }, updates);
    }

}
