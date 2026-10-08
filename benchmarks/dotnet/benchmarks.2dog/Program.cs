// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--verify"]) return TweensBenchmarks.Verification.Run();
        if (args is ["--verify-catalog"])
        {
            new TweensBenchmarks.AdapterCatalogBenchmarks().VerifyCatalog();
            return 0;
        }
        // Engine startup belongs to GlobalSetup in each BDN child process.
        if (args.Any(a => a is "--inProcess" or "-i"))
            throw new ArgumentException("Use the default out-of-process toolchain for engine isolation.");
        var config = DefaultConfig.Instance
            .WithArtifactsPath(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/benchmarkdotnet")))
            .AddExporter(JsonExporter.Full);
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        return summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;
    }
}
