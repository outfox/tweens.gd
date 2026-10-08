// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Godot;
using tweens.gd;
using Environment = System.Environment;
using OS = Godot.OS;

namespace TweensBenchmarks;

public abstract class EngineBenchmark
{
    private twodog.Engine engine = null!;
    private string previousDirectory = null!;
    protected Node Owner = null!;

    protected void StartEngine()
    {
        previousDirectory = Environment.CurrentDirectory;
        // BDN's generated executable has a different entry assembly and output directory.
        var project = typeof(EngineBenchmark).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "GodotProjectDir").Value!;
        engine = new twodog.Engine("benchmarks", project, ["--headless", "--audio-driver", "Dummy"]) { CaptureErrors = true };
        engine.Start();
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Benchmarks must run on Godot's main thread.");
        Owner = new Node();
        engine.Tree.Root.AddChild(Owner);
    }

    protected void StopEngine()
    {
        Owner.Free();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        engine.Dispose();
        Environment.CurrentDirectory = previousDirectory;
        CheckEngineErrors();
    }

    protected void CheckEngineErrors()
    {
        var errors = engine.Errors.Drain();
        if (errors.Any()) throw new InvalidOperationException(string.Join("\n", errors));
    }
}

public enum EngineWorkload { ValueCallback, Position, Color, Resource }

[MemoryDiagnoser]
[ThreadingDiagnoser]
public class EngineUpdateBenchmarks : EngineBenchmark
{
    [Params(100, 1000, 10000)] public int Count { get; set; }
    [ParamsAllValues] public EngineWorkload Workload { get; set; }
    private TweenScheduler scheduler = null!;
    private TweenInstance[] handles = null!;
    private StandardMaterial3D[] materials = null!;
    private float sink;

    [GlobalSetup]
    public void Setup()
    {
        StartEngine();
        scheduler = new();
        handles = new TweenInstance[Count];
        materials = Workload == EngineWorkload.Resource ? new StandardMaterial3D[Count] : [];
        // Infinite repeats keep pilot, warmup, measurement, and diagnostic passes live.
        var value = new Tweens.Float(1, 1) { From = 0, Repeats = -1, OnUpdate = (_, v) => sink = v };
        var position = new Tweens.Position2D(new Vector2(100, 200), 1) { From = Vector2.Zero, Repeats = -1 };
        var color = new Tweens.Modulate(new Color(0, 0.5f, 1, 0), 1) { From = Colors.White, Repeats = -1 };
        var resource = new Tweens.MaterialRoughness(0, 1) { From = 1, Repeats = -1 };
        for (var i = 0; i < Count; i++)
        {
            if (Workload == EngineWorkload.Resource)
            {
                materials[i] = new();
                handles[i] = scheduler.Add((BaseMaterial3D)materials[i], resource);
            }
            else
            {
                var node = new Node2D();
                Owner.AddChild(node);
                handles[i] = Workload switch
                {
                    EngineWorkload.ValueCallback => scheduler.Add((Node)node, value),
                    EngineWorkload.Position => scheduler.Add(node, position),
                    _ => scheduler.Add((CanvasItem)node, color),
                };
            }
        }
        scheduler.Update(1.0 / 60);
        Verify();
    }

    [Benchmark] public void Update() => scheduler.Update(1.0 / 60);

    private void Verify()
    {
        if (handles.Any(h => h.IsTerminal || h.Error is not null) || !float.IsFinite(sink))
            throw new InvalidOperationException("The workload must remain live and finite.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try { Verify(); }
        finally
        {
            scheduler.Dispose();
            foreach (var material in materials) material.Dispose();
            StopEngine();
        }
    }
}

// Setup/teardown are untimed. Report time and managed bytes per created tween.
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class DefinitionCreationBenchmarks : EngineBenchmark
{
    private const int Batch = 128;
    private Node2D target = null!;
    private TweenScheduler scheduler = null!;
    private readonly Tweens.Position2D value = new(new Vector2(100, 200), 1);
    private ITweenDefinition<Node2D, Vector2> boxed = null!;
    private readonly Position2DTween mutable = new() { To = new Vector2(100, 200), Duration = 1 };

    [GlobalSetup]
    public void Setup()
    {
        StartEngine();
        target = new Node2D(); Owner.AddChild(target);
        boxed = value;
    }
    [IterationSetup] public void Prepare() => scheduler = new();
    [IterationCleanup]
    public void Release()
    {
        try
        {
            if (scheduler.ActiveCount != Batch) throw new InvalidOperationException("Creation workload failed.");
        }
        finally { scheduler.Dispose(); }
    }
    [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
    public void StructDefinition() { for (var i = 0; i < Batch; i++) scheduler.Add(target, value); }
    [Benchmark(OperationsPerInvoke = Batch)]
    public void PreboxedDefinition() { for (var i = 0; i < Batch; i++) scheduler.Add(target, boxed); }
    [Benchmark(OperationsPerInvoke = Batch)]
    public void MutableDefinition() { for (var i = 0; i < Batch; i++) scheduler.Add(target, mutable); }
    [GlobalCleanup] public void Cleanup() => StopEngine();
}
