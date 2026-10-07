// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
namespace TweensBenchmarks;

internal static class Verification
{
    // An independent allocation sanity check, not a timing benchmark. Warm the
    // exact call path and exclude engine/target setup, delegates and output.
    internal static int Run()
    {
        var managed = new ManagedUpdateBenchmarks { Count = 1000 };
        managed.Setup();
        try
        {
            Measure("managed absolute", managed.Absolute);
            Measure("managed relative", managed.Relative);
            Measure("managed callback", managed.Callback);
        }
        finally { managed.Cleanup(); }

        var engine = new EngineUpdateBenchmarks { Count = 1000, Workload = EngineWorkload.Position };
        engine.Setup();
        try { Measure("Node2D position", engine.Update); }
        finally { engine.Cleanup(); }
        return 0;
    }

    private static void Measure(string name, Action update)
    {
        const int frames = 1024;
        for (var i = 0; i < frames; i++) update();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < frames; i++) update();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"{name}: {allocated} managed bytes / {frames} frames / 1000 tweens");
    }
}
