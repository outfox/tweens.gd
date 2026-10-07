// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using BenchmarkDotNet.Attributes;
using tweens.gd;

namespace TweensBenchmarks;

public sealed class ValueTarget { public float Value; }

internal static class Definitions
{
    internal static PropertyTween<ValueTarget, float> Float(bool relative = false, bool callback = false)
        => new(static t => t.Value, static (t, v) => t.Value = v, static (a, b, t) => a + (b - a) * t)
        {
            From = relative ? null : 0, To = relative ? null : 1, By = relative ? 1 : null,
            Duration = 1, Repeats = -1,
            OnUpdate = callback ? static (t, v) => t.Target.Value = v : null,
        };
}

// No engine startup: public custom-storage API on ordinary managed objects.
[MemoryDiagnoser]
public class ManagedUpdateBenchmarks
{
    [Params(100, 1000, 10000)] public int Count { get; set; }
    private TweenScheduler absolute = null!, relative = null!, callback = null!;
    private ValueTarget[] targets = null!;
    private TweenInstance[] handles = null!;

    [GlobalSetup]
    public void Setup()
    {
        absolute = new(); relative = new(); callback = new();
        targets = new ValueTarget[Count];
        handles = new TweenInstance[Count * 3];
        var a = Definitions.Float(); var r = Definitions.Float(relative: true); var c = Definitions.Float(callback: true);
        for (var i = 0; i < Count; i++)
        {
            targets[i] = new();
            handles[i * 3] = absolute.Add(targets[i], a);
            handles[i * 3 + 1] = relative.Add(new ValueTarget(), r);
            handles[i * 3 + 2] = callback.Add(new ValueTarget(), c);
        }
        absolute.Update(1.0 / 60); relative.Update(1.0 / 60); callback.Update(1.0 / 60);
        Verify();
    }

    [Benchmark(Baseline = true)] public void Absolute() => absolute.Update(1.0 / 60);
    [Benchmark] public void Relative() => relative.Update(1.0 / 60);
    [Benchmark] public void Callback() => callback.Update(1.0 / 60);

    private void Verify()
    {
        if (handles.Any(h => h.IsTerminal || h.Error is not null) || targets.Any(t => !float.IsFinite(t.Value)))
            throw new InvalidOperationException("The workload must remain live and finite.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try { Verify(); }
        finally { absolute.Dispose(); relative.Dispose(); callback.Dispose(); }
    }
}

[MemoryDiagnoser]
public class ManagedLifecycleBenchmarks
{
    private readonly ValueTarget target = new();
    private readonly PropertyTween<ValueTarget, float> definition = Definitions.Float();
    private readonly PropertyTween<ValueTarget, float> finite = new(static t => t.Value,
        static (t, v) => t.Value = v, static (a, b, t) => a + (b - a) * t) { From = 0, To = 1, Duration = 1 };
    private ITweenDefinition<ValueTarget>[] chain = null!;

    [GlobalSetup] public void Setup() => chain = [finite, finite, finite, finite];

    [Benchmark(Baseline = true)]
    public void StartCancel()
    {
        using var scheduler = new TweenScheduler();
        scheduler.Add(target, definition).Cancel();
    }

    [Benchmark]
    public Task<Reason> StartCancelWithEndTask()
    {
        using var scheduler = new TweenScheduler();
        var handle = scheduler.Add(target, definition);
        var end = handle.End;
        handle.Cancel();
        return end;
    }

    [Benchmark]
    public void StartComplete()
    {
        using var scheduler = new TweenScheduler();
        var handle = scheduler.Add(target, finite);
        scheduler.Update(1);
        if (handle.CompletionReason != Reason.Completed || handle.Error is not null)
            throw new InvalidOperationException("Completion workload failed.");
    }

    [Benchmark]
    public void ChainFourStartCancel()
    {
        using var scheduler = new TweenScheduler();
        scheduler.AddChain(target, chain).Cancel();
    }

    [Benchmark]
    public void GroupFourStartCancel()
    {
        using var scheduler = new TweenScheduler();
        scheduler.AddAll(target, chain).Cancel();
    }
}

[MemoryDiagnoser]
public class GroupPollingBenchmarks
{
    [Params(4, 100)] public int Count { get; set; }
    private TweenScheduler scheduler = null!;
    private Group group = null!;

    [GlobalSetup]
    public void Setup()
    {
        scheduler = new();
        var definition = Definitions.Float();
        group = Group.Of(Enumerable.Range(0, Count).Select(_ => (TweenInstance)scheduler.Add(new ValueTarget(), definition)).ToArray());
        group.Pause();
    }
    [Benchmark] public bool IsPaused() => group.IsPaused;
    [GlobalCleanup] public void Cleanup() => scheduler.Dispose();
}

[MemoryDiagnoser]
public class EasingBenchmarks
{
    private float progress;
    private float Next() { progress += 0.001f; if (progress >= 1) progress = 0; return progress; }
    [Benchmark(Baseline = true)] public float Linear() => Easing.Evaluate(EaseType.Linear, Next());
    [Benchmark] public float CachedComposition() => Easing.Evaluate(In.Quad | Out.Cubic, Next());
    [Benchmark] public float CustomComposition() => Easing.Evaluate(In.Quad | Out.Cubic, Next(), skew: 0.3);
}
