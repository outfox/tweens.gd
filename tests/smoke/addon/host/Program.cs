// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Reflection;
using System.Runtime.Versioning;
using Godot;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var scene = args.Single() switch
        {
            "csharp" => "res://main.tscn",
            "gdscript" => "res://smoke.tscn",
            _ => throw new ArgumentException("Choose csharp or gdscript.")
        };
        // The consumer mirrors a stock Godot 4.7 project. 2dog's import raises its csproj to net10.0 after this build,
        // so make sure the run still loads the net8.0 build (rolled forward to this .NET 10 host).
        var framework = typeof(Consumer).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (framework != ".NETCoreApp,Version=v8.0")
        {
            Console.Error.WriteLine($"Consumer must be the net8.0 build, not {framework}.");
            return 1;
        }
        using var engine = new twodog.Engine("Consumer", args: ["--headless", "--fixed-fps", "60", scene])
        {
            CaptureErrors = true
        };
        engine.Start();
        var passed = false;
        var finished = false;
        // A missing script, early quit or stalled tween must never count as success.
        for (var frame = 0; frame < 600; frame++)
        {
            if (engine.Iteration()) break;
            var current = engine.Tree.CurrentScene;
            if (current is null) break;
            finished = current.GetMeta("smoke_finished", false).AsBool();
            passed = current.GetMeta("smoke_passed", false).AsBool();
            if (finished) break;
        }
        engine.Dispose();
        var errors = engine.Errors.Drain().Where(error => error.Type != twodog.GodotErrorType.Warning).ToArray();
        foreach (var error in errors) Console.Error.WriteLine(error);
        if (!finished || !passed || errors.Length != 0)
        {
            Console.Error.WriteLine($"{args[0]} addon smoke failed (finished={finished}, passed={passed}).");
            return 1;
        }
        Console.WriteLine($"{args[0]} addon smoke passed.");
        return 0;
    }
}
