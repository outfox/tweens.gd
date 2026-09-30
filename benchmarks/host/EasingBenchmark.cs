// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using tweens.gd;

internal static class EasingBenchmark
{
    private const int Warmup = 120, Samples = 240;
    private static readonly (string Name, EaseType Ease, BlendType Method)[] Profiles =
    [
        ("linear", EaseType.Linear, BlendType.Makima),
        ("legacy_sine", EaseType.SineInOut, BlendType.Makima),
        ("paired_sine", InOut.Sine, BlendType.Makima),
        ("mixed_makima", In.Quad | Out.Cubic, BlendType.Makima),
        ("mixed_hermite", In.Quad | Out.Cubic, BlendType.Hermite),
        ("mixed_smoothstep", In.Quad | Out.Cubic, BlendType.SmoothStep),
        ("mixed_linear", In.Quad | Out.Cubic, BlendType.Linear),
        ("back30_bounce20", In.Back30 | Out.Bounce20, BlendType.Makima),
        ("solo_jump30", Out.Jump30, BlendType.Makima),
    ];

    public static int Run(twodog.Engine engine, string? output)
    {
        var owner = new Node { Name = "EasingBenchmark" };
        engine.Tree.Root.AddChild(owner);
        try
        {
            using var script = ResourceLoader.Load<GDScript>("res://easing.gd");
            var json = script?.Call("run", owner).AsString();
            if (string.IsNullOrEmpty(json)) return 1;
            var report = JsonNode.Parse(json)!.AsObject();
            var results = report["results"]!.AsArray();
            // Exercise every easing path before recording C# results.
            foreach (var profile in Profiles) Measure(owner, 1000, profile);
            foreach (var count in new[] { 1000, 10000 })
            foreach (var profile in Profiles) results.Add(Measure(owner, count, profile));
            report["dotnet"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            report["tiered_compilation_env"] = System.Environment.GetEnvironmentVariable("DOTNET_TieredCompilation");
            report["notes"] = "Manual Node2D position updates, no rendering. One-second infinite loops, staggered offsets cover the entire curve each frame. Blend 0.2, skew 1. Target creation excluded. C# and native GDScript only; Godot has no equivalent mixed curves.";
            Console.WriteLine("backend   count profile              median us    p95 us   create us");
            foreach (var row in results)
                Console.WriteLine($"{row!["backend"],-9} {row["count"],5} {row["profile"],-20} {row["median_update_us"],9} {row["p95_update_us"],9} {row["create_us"],11}");
            if (output is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.WriteAllText(output, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"Wrote {output}");
            }
            return 0;
        }
        finally { owner.Free(); }
    }

    private static JsonObject Measure(Node owner, int count, (string Name, EaseType Ease, BlendType Method) profile)
    {
        var targets = new Node2D[count];
        for (var i = 0; i < count; i++) { targets[i] = new Node2D(); owner.AddChild(targets[i]); }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        using var scheduler = new TweenScheduler();
        var definition = new Tweens.Position2D(new Vector2(100, 200), 1, profile.Ease)
        { From = Vector2.Zero, Repeats = TweenOptions.Infinite, BlendType = profile.Method, Blend = 0.2 };
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < count; i++) scheduler.Add(targets[i], definition with { Offset = (double)i / count });
        var create = watch.Elapsed.TotalMicroseconds;
        var times = new double[Samples];
        for (var frame = 0; frame < Warmup + Samples; frame++)
        {
            watch.Restart();
            scheduler.Update(1.0 / 60);
            if (frame >= Warmup) times[frame - Warmup] = watch.Elapsed.TotalMicroseconds;
        }
        Array.Sort(times);
        scheduler.Dispose();
        foreach (var target in targets) target.Free();
        return new JsonObject
        {
            ["backend"] = "csharp", ["count"] = count, ["profile"] = profile.Name,
            ["median_update_us"] = times[Samples / 2], ["p95_update_us"] = times[(int)(Samples * 0.95) - 1],
            ["create_us"] = create,
        };
    }
}
