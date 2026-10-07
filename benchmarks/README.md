# Benchmarks

## C# BenchmarkDotNet suite

The independent generic 2dog host in `dotnet/benchmarks.2dog` measures the C#
library with BenchmarkDotNet 0.15.8. It was scaffolded with
`dnx 2dog -- add benchmarks/dotnet --generic`, then reduced to a console host
and an empty Godot scene. It references the canonical library project and shares
the comparative host's engine versions. No GDScript, GDExtension build, addon
staging, installed Godot editor, or import is required.

Run from the repository root with .NET 10:

```powershell
# Discover benchmarks without starting Godot.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --list flat

# Workload validity and an independent warmed allocation sanity check (not timing).
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --verify

# Smoke only: verifies execution, not useful timing results.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*' --job Dry

# Normal BenchmarkDotNet adaptive measurements, all cases.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*'

# Focused investigation; multiple filters are supported.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*DefinitionCreation*' '*ManagedLifecycle*'

# Reproduce the initial report's bounded measurement budget.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*' --launchCount 2 --warmupCount 5 --iterationCount 10 --iterationTime 100 --artifacts artifacts/bdn-2026-10-08
```

Default output is `artifacts/benchmarkdotnet/`: Markdown, CSV, HTML, full JSON,
and logs containing runtime, CPU, GC, confidence intervals and per-iteration
measurements. `--artifacts` overrides it. Failed benchmark reports return a
nonzero exit code. Use fresh output folders for comparisons; run sequentially
on an otherwise idle machine with the same runtime, engine and power settings.
The host leaves tiering/PGO at runtime defaults. Record any environment overrides.

| Suite | What one reported operation measures |
| --- | --- |
| `ManagedUpdateBenchmarks` | One frame of 100/1,000/10,000 custom float tweens on plain C# objects: absolute, relative, or callback. No engine is started. |
| `EngineUpdateBenchmarks` | One frame at the same counts: value callback, Node2D position, color, or material roughness, through the C# API. |
| `DefinitionCreationBenchmarks` | One tween creation, averaged over 128 starts on an existing node. Compares a struct passed per call, a previously boxed definition, and a mutable definition. |
| `ManagedLifecycleBenchmarks` | Scheduler construction, start, and cancellation/completion/disposal; also four-entry chains and groups, and requesting an `End` task. |
| `GroupPollingBenchmarks` | Reading `IsPaused` for 4 or 100 paused members. |
| `EasingBenchmarks` | A public `Easing.Evaluate` call, for linear, cached composition, or custom skew. Includes lookup/construction, unlike playback's cached delegate. |

The default out-of-process toolchain builds real BenchmarkDotNet child executables.
Engine cases start Godot in `GlobalSetup` on the measurement thread, assert that
it is Godot's main thread, and dispose it in `GlobalCleanup`. Engine startup,
target creation and shutdown are untimed. `--inProcess` is rejected: the engine
must not be reused/restarted across cases in a shared benchmark process.

Update cases initialize once and repeat forever; pilot/warmup cannot exhaust
them. Cleanup validates that every handle stayed live and fault-free, avoiding
misleading measurements of empty schedulers. Updates include repeat timeline
math. Creation uses iteration setup/cleanup and one invocation per iteration;
disposal is outside its timed region. Its short iterations can produce BDN's
minimum-iteration-time warning, so inspect the reported uncertainty and increase
the batch if investigating small timing differences. Lifecycle cases deliberately
include teardown and scheduler allocation. Baseline ratios are meaningful within
each suite, not between suites.

`MemoryDiagnoser` reports managed allocations and GC collections; it does not
measure Godot/native memory, retained heap size, or total process memory. Zero
allocated bytes is not zero CPU cost. See the
[initial optimization report](reports/2026-10-08-optimization.md) for measured
findings, source-level candidates, and limits.

## Comparative benchmark

Runs the same workloads through three backends in one 2dog engine process:
the GDScript API on the `tweens_gd` GDExtension, the C# library's `TweenScheduler`,
and one parallel Godot `Tween`. It is a rough comparison for one machine, not a
supported tween count or frame budget.

```powershell
scons -C gdextension target=template_release
dotnet run --project benchmarks/host -c Release -- --output artifacts/benchmark-compare.json
```

Use Release: Debug builds unoptimized C# and loads the debug engine. The host stages
the addon and `testbed-gdscript/benchmark.gd`, which measures GDScript and Godot's
Tween; `host/Program.cs` then measures C# with the same counts, warmup, samples and
1/60 s delta, after one untimed round for the JIT. It prints median and p95 update
times and creation times, and `--output` writes all results as JSON.

Workloads are 100, 1,000 and 10,000 tweens of a value with a per-frame callback,
Node2D position, Node2D modulate and material roughness, updated manually without
rendering. C# value tweens target in-tree Nodes, since its `Float` definition needs a
Node; GDScript value tweens target RefCounted objects. Callbacks run in each
backend's own language. Results at 10,000 tweens vary noticeably between runs.

The project is not in the solution or CI. Godot 4.7.2 can crash on exit the first
time an import registers the extension; if the first build fails in the 2dog import,
run it again.

For the composed easing paths, use `--easing`:

```powershell
$env:DOTNET_TieredCompilation = '0'
dotnet run --project benchmarks/host -c Release -- --easing --output artifacts/benchmark-easing.json
```

This measures 1,000 and 10,000 Node2D position tweens in C# and native GDScript:
linear, legacy and paired Sine, Quad/Cubic with all four blend methods,
Back30/Bounce20, and solo Jump30. One-second infinite loops use staggered offsets
so every update includes the whole curve, including the 0.2 blend window.
Each case has 120 warmup updates and 240 measured updates; C# also gets an untimed
pass through every profile. Disabling tiered compilation avoids compilation-tier
changes during these short measurements. Remove or restore that environment
variable afterward. The original comparison remains unchanged and uses its
shorter warmup; use the same environment when comparing runs.

Repeat runs sequentially and retain their ranges: allocation patterns, CPU load,
and runtime warmup can materially change results, particularly at 10,000 tweens.
