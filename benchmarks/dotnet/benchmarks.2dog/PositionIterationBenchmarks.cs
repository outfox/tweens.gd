// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Godot;
using tweens.gd;
using Environment = System.Environment;
using OS = Godot.OS;

namespace TweensBenchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10, invocationCount: 1024)]
public class PositionIterationBenchmarks
{
    private const double DurationSeconds = 100;
    [Params(100, 10000)] public int Count { get; set; }

    private twodog.Engine engine = null!;
    private string previousDirectory = null!;
    private Node owner = null!;
    private Node3D[] nodes = [];
    private Vector3[] starts = [];
    private Tween[] godotTweens = [];
    private TweenInstance[] libraryTweens = [];

    [GlobalSetup]
    public void StartEngine()
    {
        previousDirectory = Environment.CurrentDirectory;
        var project = typeof(PositionIterationBenchmarks).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "GodotProjectDir").Value!;
        // Fixed simulation delta without a real-time frame cap. A batch advances
        // only (1024 + 2) / 60 seconds, safely below the finite tween duration.
        engine = new twodog.Engine("benchmarks", project,
            ["--headless", "--audio-driver", "Dummy", "--fixed-fps", "60"]) { CaptureErrors = true };
        engine.Start();
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Benchmarks must run on Godot's main thread.");
        CheckEngineErrors();
    }

    [IterationSetup(Target = nameof(GodotSharp))]
    public void SetupGodotSharp() => Setup(useGodotSharp: true);

    [IterationSetup(Target = nameof(TweensGd))]
    public void SetupTweensGd() => Setup(useGodotSharp: false);

    private void Setup(bool useGodotSharp)
    {
        var random = new Random(12345);
        owner = new Node();
        engine.Tree.Root.AddChild(owner);
        nodes = new Node3D[Count];
        starts = new Vector3[Count];
        godotTweens = useGodotSharp ? new Tween[Count] : [];
        libraryTweens = useGodotSharp ? [] : new TweenInstance[Count];
        for (var i = 0; i < Count; i++)
        {
            starts[i] = RandomPosition(random);
            var destination = RandomPosition(random);
            var node = nodes[i] = new Node3D { Position = starts[i] };
            owner.AddChild(node);
            if (useGodotSharp)
            {
                var tween = godotTweens[i] = node.CreateTween();
                tween.TweenProperty(node, "position", destination, DurationSeconds)
                    .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            }
            else
            {
                libraryTweens[i] = node.Tween(new Tweens.Position3D(destination, DurationSeconds, Out.Elastic));
            }
        }

        // Attach the automatic runner and initialize playback before timing.
        engine.Iteration();
        engine.Iteration();
        Verify();
    }

    private static Vector3 RandomPosition(Random random) => new(
        random.NextSingle() * 200 - 100,
        random.NextSingle() * 200 - 100,
        random.NextSingle() * 200 - 100);

    [Benchmark(Baseline = true)] public bool GodotSharp() => engine.Iteration();
    [Benchmark] public bool TweensGd() => engine.Iteration();

    private void Verify()
    {
        CheckEngineErrors();
        if (godotTweens.Any(t => !t.IsValid() || !t.IsRunning()) ||
            libraryTweens.Any(t => t.IsTerminal || t.Error is not null) ||
            (libraryTweens.Length > 0 && TweenRuntime.GetActiveCount(owner) != Count))
            throw new InvalidOperationException("Every tween must remain active throughout the batch.");
        for (var i = 0; i < nodes.Length; i++)
            if (!nodes[i].Position.IsFinite() || nodes[i].Position == starts[i])
                throw new InvalidOperationException("Every Node3D must have moved to a finite position.");
    }

    [IterationCleanup]
    public void CleanupBatch()
    {
        try { Verify(); }
        finally
        {
            foreach (var tween in godotTweens) { tween.Kill(); tween.Dispose(); }
            foreach (var tween in libraryTweens) tween.Cancel();
            owner.Free();
            nodes = [];
            starts = [];
            godotTweens = [];
            libraryTweens = [];
        }
    }

    [GlobalCleanup]
    public void StopEngine()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            engine.Dispose();
            CheckEngineErrors();
        }
        finally { Environment.CurrentDirectory = previousDirectory; }
    }

    private void CheckEngineErrors()
    {
        var errors = engine.Errors.Drain();
        if (errors.Any()) throw new InvalidOperationException(string.Join("\n", errors));
    }
}
