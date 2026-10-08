# Benchmarks

## Node3D engine-iteration comparison

The `PositionIterationBenchmarks` collection in `dotnet/benchmarks.2dog` compares
GodotSharp's built-in tweens with the automatic C# tweens.gd runtime. It contains only Node3D position tweens
at 100 and 10,000 nodes. Both variants use the same seeded random starting
positions and destinations, a 100-second duration, and Elastic-out easing.
Each node owns one tween. There are no loops, callbacks, or manual scheduler updates.

```powershell
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*PositionIterationBenchmarks*'
```

Each BenchmarkDotNet batch creates fresh nodes and tweens outside timing, calls
`twodog.Engine.Iteration()` exactly twice to initialize playback, then measures
1,024 calls to `Engine.Iteration()`. Each reported operation is one complete
engine iteration. The headless engine uses a fixed 1/60-second simulation delta
without a real-time frame cap, so each batch advances about 17.1 seconds and
cannot exhaust the 100-second tweens. Setup and cleanup validate that every node
has moved and every tween remains active; increasing the invocation count enough
to finish the tweens fails validation. Destruction is also outside timing.

The default job runs three warmup batches and ten measurement batches, with a
fresh engine process per variant/count. Output is in
`artifacts/benchmarkdotnet/`. Standard BenchmarkDotNet options such as
`--list flat`, `--filter '*GodotSharp*'`, and `--artifacts <path>` are supported
after `--` (keep the collection filter when passing run options). Memory diagnostics
cover managed allocations only. The bounded batches can trigger BenchmarkDotNet's
minimum-iteration-time warning at 100 nodes; inspect the reported uncertainty.

## C# BenchmarkDotNet suite

The same host also contains the existing microbenchmark collections.
Their scheduler-update measurements do not measure full engine iterations.

The independent generic 2dog host in `dotnet/benchmarks.2dog` measures the C#
library with BenchmarkDotNet 0.15.8. It was scaffolded with
`dnx 2dog -- add benchmarks/dotnet --generic`, then reduced to a console host
and an empty Godot scene. It references the canonical library project and shares
the comparative host's engine versions. No GDScript, GDExtension build, addon
staging, installed Godot editor, or import is required.

See the [optimization follow-up](reports/2026-10-08-followup.md) and
[complete definition rankings](reports/2026-10-08-catalog-ranking.md) for measured
results. The rankings are also available as CSV and JSON beside the report.

Run from the repository root with .NET 10:

```powershell
# Discover benchmarks without starting Godot.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --list flat

# Workload validity and an independent warmed allocation sanity check (not timing).
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --verify

# Verify every baseline pair and the cached report-grouping implementation.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --verify-orderer

# Smoke only: verifies execution, not useful timing results.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*' --job Dry

# All cases (catalog uses short jobs; position iteration uses bounded batches).
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*'

# Focused investigation; multiple filters are supported.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*DefinitionCreation*' '*ManagedLifecycle*'

# Reproduce the initial report's bounded measurement budget.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*DefinitionCreation*' '*ManagedLifecycle*' '*ManagedUpdate*' '*EngineUpdate*' '*GroupPolling*' '*EasingBenchmarks*' --launchCount 2 --warmupCount 5 --iterationCount 10 --iterationTime 100 --artifacts artifacts/bdn-followup
```

Default output is `artifacts/benchmarkdotnet/`: Markdown, CSV, HTML, full JSON,
and logs containing runtime, CPU, GC, confidence intervals and per-iteration
measurements. `--artifacts` overrides it. Failed benchmark reports return a
nonzero exit code. Use fresh output folders for comparisons; run sequentially
on an otherwise idle machine with the same runtime, engine and power settings.
The host leaves tiering/PGO at runtime defaults. Record any environment overrides.
Report grouping caches BenchmarkDotNet's own logical-group keys; this avoids
repeated catalog scans during baseline-table export without changing the timed
workloads or baseline pairing.

| Suite | What one reported operation measures |
| --- | --- |
| `PositionIterationBenchmarks` | One complete engine iteration with 100/10,000 active Node3D position tweens, comparing GodotSharp and the automatic C# tweens.gd runtime. |
| `ManagedUpdateBenchmarks` | One frame of 100/1,000/10,000 custom float tweens on plain C# objects: absolute, relative, or callback. No engine is started. |
| `EngineUpdateBenchmarks` | One frame at the same counts: value callback, Node2D position, color, or material roughness, through the C# API. |
| `DefinitionCreationBenchmarks` | One tween creation, averaged over 128 starts on an existing node. Compares a struct passed per call, a previously boxed definition, and a mutable definition. |
| `ManagedLifecycleBenchmarks` | Scheduler construction, start, and cancellation/completion/disposal; also four-entry chains and groups, and requesting an `End` task. |
| `GroupPollingBenchmarks` | Reading `IsPaused` for 4 or 100 paused members. |
| `EasingBenchmarks` | A public `Easing.Evaluate` call, for linear, cached composition, or custom skew. Includes lookup/construction, unlike playback's cached delegate. |
| `AdapterCatalogBenchmarks` | Every generated definition on 100/1,000 targets, paired with its direct-write baseline. Includes every supported shader value type and nine custom-property value types. |

The default out-of-process toolchain builds real BenchmarkDotNet child executables.
Engine cases start Godot in `GlobalSetup` on the measurement thread, assert that
it is Godot's main thread, and dispose it in `GlobalCleanup`. Engine startup,
target creation and shutdown are untimed. `--inProcess` is rejected: the engine
must not be reused/restarted across cases in a shared benchmark process.

Scheduler-update cases initialize once and repeat forever; pilot/warmup cannot exhaust
them. Cleanup validates that every handle stayed live and fault-free, avoiding
misleading measurements of empty schedulers. Managed fixtures create only the
selected workload, so unused schedulers do not influence its heap layout.
Updates include repeat timeline
math. Creation uses iteration setup/cleanup and one invocation per iteration;
disposal is outside its timed region. Its short iterations can produce BDN's
minimum-iteration-time warning, so inspect the reported uncertainty and increase
the batch if investigating small timing differences. Lifecycle cases deliberately
include teardown and scheduler allocation. Baseline ratios are meaningful within
each suite, not between suites.

`MemoryDiagnoser` reports managed allocations and GC collections; it does not
measure Godot/native memory, retained heap size, or total process memory. Zero
allocated bytes is not zero CPU cost. The existing microbenchmarks also use `ThreadingDiagnoser`,
which reports managed thread-pool completions and monitor contention, not Godot's
native worker-thread count. See the
[initial optimization report](reports/2026-10-08-optimization.md) for measured
findings, source-level candidates, and limits.

## Full definition ranking

```powershell
# Fast fixture/coverage check: every definition, no benchmark timing.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --verify-catalog

# Full ranking: 361 concrete definitions × 2 counts × 2 methods = 1,444 cases.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*AdapterCatalogBenchmarks*' --artifacts artifacts/bdn-catalog

# Export sorted cost, overhead, allocation and threading rankings.
node benchmarks/summarize-catalog.mjs artifacts/bdn-catalog/results/TweensBenchmarks.AdapterCatalogBenchmarks-report-full.json artifacts/catalog-ranking

# Follow up a family; BDN filters include parameter values.
dotnet run --project benchmarks/dotnet/benchmarks.2dog -c Release -- --filter '*AdapterCatalogBenchmarks*ShaderParameter*'
```

The catalog is discovered from `Tweens` at runtime: currently 331 nongeneric
definitions, 21 shader instantiations (three families × seven supported value
types), and nine custom-property instantiations. New nongeneric definitions are
included automatically; unknown generic families fail discovery until fixtures
are supplied. The suite uses one launch, three warmup and three measurement
iterations, targeting 100 ms per iteration. Short jobs locate broad outliers;
repeat selected cases with longer measurements before deciding small differences.

Each benchmark child starts a headless engine, creates distinct valid targets,
and initializes infinite linear tweens. The `DirectWrite` baseline interpolates
the same endpoints and calls the adapter's actual typed setter (including
read/modify/write for component properties). These delegates are extracted once
during setup; reflection and definition boxing stay outside timing. Shader
baselines interpolate and set the typed uniform directly. Callback-value baselines
write a sink; custom-property baselines use a plain C# field. Resources use their
actual resource type rather than an invented node proxy.

Values change with time in both methods. Setup checks a direct sample against
the tween sample with tolerance for Godot normalization. Targets satisfy adapter
preconditions (paths, sprite frames, scroll ranges, particle settings and physical
light units); shaders use explicit initial overrides because headless rendering
does not supply defaults. Faulted/terminal handles and Godot warnings/errors fail
validation. No engine frames are rendered: GPU costs, drawing and automatic-runner
scheduling are outside this ranking.

Rank by `tween_ns_per_target` for total expense and by `overhead_ns_per_target`
or `ratio_of_means` for scheduler/adapter premium over direct writes. The latter
ratio is computed from means and is labeled separately from BDN's ratio statistic.
The exporter marks values above Q3 + 3×IQR within each count as screening outliers,
retains confidence intervals, and rejects missing/failed baseline pairs. Threading
zeros are expected for main-thread workloads; they do not prove that native
Godot internals used no worker threads.

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
