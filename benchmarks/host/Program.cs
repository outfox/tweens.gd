// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using tweens.gd;

internal static class Program
{
    // Matches testbed-gdscript/benchmark.gd, which measures the GDScript API and Godot's Tween.
    private const double Delta = 1.0 / 60.0;
    private const double Seconds = 1000.0;
    private static readonly string[] Backends = ["gdscript", "csharp", "godot_tween"];

    [STAThread]
    private static int Main(string[] args)
    {
        // Engine.Start changes cwd to the Godot project; resolve caller paths first.
        var index = Array.IndexOf(args, "--output");
        var output = index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : null;
        using var engine = new twodog.Engine("benchmarks.2dog", args: ["--headless", "--fixed-fps", "60"]);
        engine.Start();
        try { return args.Contains("--easing") ? EasingBenchmark.Run(engine, output) : Run(engine, output); }
        finally
        {
            // Finalize wrappers while their engine is alive.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static int Run(twodog.Engine engine, string? output)
    {
        var owner = new Node { Name = "Benchmark" };
        engine.Tree.Root.AddChild(owner);
        using var script = ResourceLoader.Load<GDScript>("res://benchmark.gd");
        var gdscript = script?.Call("run", owner).AsString();
        if (string.IsNullOrEmpty(gdscript))
        {
            Console.Error.WriteLine("The GDScript benchmark failed; see the errors above.");
            return 1;
        }
        var report = JsonNode.Parse(gdscript)!.AsObject();
        var warmup = (int)Number(report["warmup"]);
        var samples = (int)Number(report["samples"]);
        var results = report["results"]!.AsArray();
        var cases = results.Select(r => (r!["workload"]!.GetValue<string>(), (int)Number(r["count"]))).Distinct().ToList();
        // One untimed round lets tiered JIT compile and promote the C# paths before any case is measured.
        foreach (var workload in cases.Select(c => c.Item1).Distinct()) Measure(owner, workload, cases.Min(c => c.Item2), warmup, samples);
        foreach (var (workload, count) in cases) results.Add(Measure(owner, workload, count, warmup, samples));
        report["notes"] = "Manual linear updates, no rendering, one process. gdscript is the GDScript API on the tweens_gd " +
            "GDExtension, csharp the C# library's TweenScheduler, godot_tween one parallel Godot Tween. C# value tweens " +
            "target in-tree Nodes (its Float definition needs a Node); GDScript value tweens target RefCounted objects.";
        owner.Free();
        Print(results, cases);
        if (output is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Wrote {output}");
        }
        return 0;
    }

    private static JsonObject Measure(Node owner, string workload, int count, int warmup, int samples)
    {
        var targets = new List<GodotObject>(count);
        for (var i = 0; i < count; i++)
        {
            GodotObject target = workload switch
            {
                "value" => new Node(),
                "position" or "color" => new Node2D(),
                "resource" => new StandardMaterial3D(),
                _ => throw new ArgumentException($"Unknown workload {workload}."),
            };
            if (target is Node node) owner.AddChild(node);
            targets.Add(target);
        }
        // Keep earlier garbage out of the timed section; creation still pays for its own allocations.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var scheduler = new TweenScheduler();
        var watch = Stopwatch.StartNew();
        Start(scheduler, workload, targets);
        var create = watch.Elapsed.TotalMicroseconds;
        var times = new double[samples];
        for (var frame = 0; frame < warmup + samples; frame++)
        {
            watch.Restart();
            scheduler.Update(Delta);
            if (frame >= warmup) times[frame - warmup] = watch.Elapsed.TotalMicroseconds;
        }
        Array.Sort(times);
        watch.Restart();
        scheduler.Dispose();
        var dispose = watch.Elapsed.TotalMicroseconds;
        foreach (var target in targets)
        {
            if (target is Node node) node.Free();
            else target.Dispose();
        }
        return new JsonObject
        {
            ["workload"] = workload, ["count"] = count, ["backend"] = "csharp",
            ["create_us"] = (long)create, ["dispose_us"] = (long)dispose,
            ["median_update_us"] = (long)times[samples / 2], ["p95_update_us"] = (long)times[(int)(samples * 0.95) - 1],
        };
    }

    private static void Start(TweenScheduler scheduler, string workload, List<GodotObject> targets)
    {
        switch (workload)
        {
            case "value":
                var value = new Tweens.Float(1f, Seconds) { OnUpdate = static (_, _) => { } };
                foreach (var target in targets) scheduler.Add((Node)target, value);
                break;
            case "position":
                var position = new Tweens.Position2D(new Vector2(100, 200), Seconds);
                foreach (var target in targets) scheduler.Add((Node2D)target, position);
                break;
            case "color":
                var color = new Tweens.Modulate(new Color(0, 0.5f, 1, 0), Seconds);
                foreach (var target in targets) scheduler.Add((CanvasItem)target, color);
                break;
            case "resource":
                var roughness = new Tweens.MaterialRoughness(0f, Seconds);
                foreach (var target in targets) scheduler.Add((BaseMaterial3D)target, roughness);
                break;
        }
    }

    // Parsed and constructed JSON numbers convert differently; their text is the same.
    private static double Number(JsonNode? node) => double.Parse(node!.ToJsonString(), CultureInfo.InvariantCulture);

    private static void Print(JsonArray results, List<(string Workload, int Count)> cases)
    {
        long Value(string workload, int count, string backend, string field) => results
            .Where(r => r!["workload"]!.GetValue<string>() == workload && (int)Number(r["count"]) == count
                && r["backend"]!.GetValue<string>() == backend)
            .Select(r => (long)Number(r![field])).First();
        string Row(string workload, int count, string field)
            => string.Join(" ", Backends.Select(b => Value(workload, count, b, field).ToString().PadLeft(9)));
        Console.WriteLine();
        Console.WriteLine("                   median update µs (per frame)     p95 update µs                    create µs (all tweens)");
        Console.WriteLine("workload    count  " + string.Concat(Enumerable.Repeat(" GDScript        C#     Tween  ", 3)));
        foreach (var (workload, count) in cases)
            Console.WriteLine($"{workload,-9} {count,6}  {Row(workload, count, "median_update_us")}  "
                + $"{Row(workload, count, "p95_update_us")}  {Row(workload, count, "create_us")}");
    }
}
