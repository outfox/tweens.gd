# C# performance and optimization potential — 2026-10-08

The slowdown is reproducible in the original benchmark harness. The strongest
optimization candidates are the general execution plan used by every single
tween and repeated lifetime/pause checks. Creation also allocates heavily.
Interface boxing, group polling and custom easing evaluation have directly
measured allocation costs. Ordinary warmed updates do not show a per-tween
managed allocation problem in the independent checks below.

No library optimization was made for these measurements. The new C# host uses
BenchmarkDotNet 0.15.8 and was created using `dnx 2dog -- add ... --generic`.
Commands and workload contracts are in the [benchmark README](../README.md).

## Evidence and limits

- Library revision: `1340af5a60aad46a558ae5fc2cec8448fbf69ecc`; benchmark additions
  are in the working tree. Windows 11 25H2, Ryzen 9 5900X, 12 physical/24 logical
  cores, SDK 10.0.401, .NET 10.0.12, x64 RyuJIT, workstation GC, Release.
- Engine: `2dog.engine` 4.7.2.103, Godot 4.7.2, hash `c92dbe7d3`.
- All 34 BenchmarkDotNet cases completed successfully: two independent process
  launches, five warmup iterations, ten measurement iterations per launch,
  target iteration time 100 ms. Default tiering/PGO and outlier handling.
- Creation uses iteration setup/cleanup and one invocation containing 128
  starts; reported costs are **per tween**, not per batch. Its iterations are
  shorter than 100 ms. Other update costs are **per frame of Count tweens**.
- Engine startup, target creation and shutdown are outside measurements.
  No rendering, GDScript or GDExtension executes in the new host. Managed custom
  targets require no engine at all. Every update workload repeats indefinitely,
  and cleanup checks that handles remained live and fault-free.
- These are allocation totals and controlled comparisons, plus source review;
  they are **not allocation-stack or sampled CPU profiles**. Source candidates
  below are not measured percentages of total CPU or heap consumption.
- BDN emitted minimum-iteration-time warnings, and the 1,000-tween managed
  callback result was multimodal. Use normal adaptive runs before deciding small
  performance differences. Error figures below are BDN's 99.9% confidence-interval
  half-widths, not frame-tail percentiles.

Portable [measurement evidence](2026-10-08-measurements.json) includes all cases,
retained measurement samples, uncertainty and managed bytes. Full logs, raw BDN
exports and generated reports remain under `artifacts/bdn-2026-10-08/` locally.
BenchmarkDotNet's [process isolation](https://benchmarkdotnet.org/articles/configs/toolchains.html)
and [memory diagnoser](https://benchmarkdotnet.org/articles/configs/diagnosers.html)
provide the measurement machinery; native allocations and retained memory are
outside MemoryDiagnoser's scope.

## Historical slowdown

The unchanged comparative harness was run three times sequentially. Each entry
is the median of three per-run C# update medians, in microseconds. The historical
files are the September 29 runs; current files are October 8.

| Count | Workload | Sept 29 | Oct 8 | Oct 8 range | Time multiplier |
| ---: | --- | ---: | ---: | ---: | ---: |
| 1,000 | Value callback | 769 | 2,067 | 2,003–2,103 | 2.69× |
| 1,000 | Position | 283 | 603 | 594–611 | 2.13× |
| 1,000 | Color | 270 | 507 | 502–595 | 1.88× |
| 1,000 | Resource | 133 | 264 | 264–266 | 1.98× |
| 10,000 | Value callback | 1,840 | 4,458 | 4,423–4,507 | 2.42× |
| 10,000 | Position | 2,378 | 5,447 | 5,410–5,951 | 2.29× |
| 10,000 | Color | 2,272 | 5,176 | 5,156–5,303 | 2.28× |
| 10,000 | Resource | 1,439 | 2,659 | 2,649–2,718 | 1.85× |

This establishes an observational slowdown on this machine, not a controlled
commit bisect. The engine hash changed, the original warmup is short, and no old
checkout was rebuilt. It is particularly unsafe to treat the 1,000-value result
as steady-state throughput. Current native binaries were reused for this legacy
harness; only its C# results are analyzed here. See the
[comparison evidence](2026-10-08-historical-comparison.json).

The execution-plan implementation arrived in `a98e720` on October 1, after the
saved baseline. Its additional work makes it a strong investigation candidate,
but this timing correlation alone does not attribute the regression to it.

## Current steady-state CPU cost

BenchmarkDotNet means, in microseconds per frame:

| Workload | 1,000 tweens | 10,000 tweens |
| --- | ---: | ---: |
| Managed absolute float | 73.79 ± 0.68 | 773.06 ± 5.38 |
| Managed relative float | 82.97 ± 0.42 | 852.49 ± 26.06 |
| Managed float callback | 93.00 ± 15.61 | 762.73 ± 7.22 |
| Node value callback | 333.45 ± 1.86 | 3,524.84 ± 92.44 |
| Node2D position | 397.48 ± 1.81 | 4,854.62 ± 399.27 |
| CanvasItem color | 400.30 ± 41.06 | 4,471.22 ± 245.12 |
| Material roughness | 123.01 ± 2.82 | 1,458.51 ± 90.49 |

Node position is about 5.4× managed absolute float at 1,000 targets. Different
value types/adapters prevent interpreting that ratio as a pure interop penalty,
but it motivates reducing repeated native lifetime/pause queries before tuning
arithmetic. A simple managed callback has no consistent large premium: the
1,000-target result is noisy, and the 100/10,000 cases are close to absolute.

BDN reports 0–60 managed bytes per update invocation across these workloads,
far below one object per tween. The independent `--verify` check warms each
exact delegate path for 1,024 frames, then measures another 1,024 frames of
1,000 tweens with `GC.GetAllocatedBytesForCurrentThread`. Managed absolute,
relative, callback and Node2D position each allocate **zero bytes** in that
measured loop. Tiny BDN readings should therefore not be projected into a
per-tween allocation rate. Other adapters/runtimes still need separate checks.

## Measured allocation sites

| Operation | Mean | Managed bytes/op | Interpretation |
| --- | ---: | ---: | --- |
| Position creation, struct argument | 3.000 µs | 2,063 | Includes interface conversion per start |
| Position creation, preboxed argument | 2.908 µs | 1,687 | Same definition, boxed once outside measurement |
| Position creation, mutable definition | 2.679 µs | 1,687 | Reused builder, cloned for each playback |
| Managed start + cancel + dispose | 263.0 ns | 1,472 | Includes a fresh scheduler and its storage |
| Same, requesting `End` | 284.0 ns | 1,568 | Lazy completion adds 96 B |
| Managed start + complete + dispose | 487.4 ns | 1,720 | Includes activation, writes and completion |
| Four-entry chain start + cancel | 1,969.5 ns | 8,272 | Includes root, leaves, plans, coordination |
| Four-entry group start + cancel | 1,318.0 ns | 6,744 | Includes members and group subscriptions |
| `Group.IsPaused`, four members | 30.61 ns | 104 | Allocates even when only reading state |
| `Group.IsPaused`, 100 members | 574.41 ns | 872 | Allocation grows with active-member count |
| Linear `Easing.Evaluate` | 5.35 ns | 0 | Includes progress generation and lookup |
| Default composed `Easing.Evaluate` | 18.49 ns | 0 | Cached composition |
| Custom-skew `Easing.Evaluate` | 123.56 ns | 360 | Rebuilds composition for each call |

Creation figures contain amortized scheduler-list growth and small harness
overhead. They exclude node construction, activation and cancellation. All 128
starts use one existing owner; creation on 128 separate owners can differ.

### Boxing: real versus optimized away

`TweenScheduler.Add` takes `ITweenDefinition<TTarget,TValue>`, while generated
`Tweens.Position2D` is a readonly record struct. Passing the struct repeatedly
causes **376 B/start** more allocation than passing a preboxed interface value:
about 18% of measured creation bytes, or 3.76 MB per 10,000 starts. Preboxing does
not remove the playback snapshot. The 3% timing difference is small enough that
the allocation reduction is the stronger conclusion.

Candidate: a constrained generic/internal creation path and generated overloads
that retain the concrete definition type until `CreatePlayback`, while keeping
the heterogeneous interface APIs for chains and groups. Check overload inference,
code-size growth and net8.0 compatibility. Reusing an interface-typed immutable
definition is an immediate caller-side option where convenient.

The `(T)(object)` casts in [Offsets.cs](../../addons/tweens_gd/csharp/Interpolation/Offsets.cs)
are not evidence of surviving heap boxes by themselves. The relative-float
workload exercises offset addition repeatedly and its independent warmed check
allocates zero. Do not replace those casts with unsafe code on appearance alone.
Vector, quaternion, Rect2, shader Variant and net8/WASM paths remain unmeasured.
Likewise, generic `Enum.IsDefined` and `HasFlag` calls need generated-code or
allocation evidence before being labeled boxing hot spots.

## Prioritized optimization work

| Priority | Site and proposed change | Evidence and acceptance criteria |
| --- | --- | --- |
| P1 | Specialize single-tween execution; avoid constructing unused leaf plans inside chains | General plan allocated for every C# leaf; high creation totals and duplicated chain planning. Measure start/cancel, start/complete, chain creation and updates. |
| P1 | Reduce redundant target/owner/pause checks between user-code boundaries | Repeated checks across scheduler, plan and sample paths; engine targets substantially costlier. Preserve every reentrant callback/lifetime guarantee. |
| P1 | Remove `Group.IsPaused` query allocations with one indexed scan | Directly measured 104/872 B per poll; target 0 B with identical empty/terminal/paused semantics. |
| P1 | Preserve concrete struct types on common single-definition starts | Direct 376 B creation delta; verify allocation reduction without changing snapshot semantics. |
| P2 | Reuse prepared custom easing; resolve return easing only when needed | `Evaluate` custom skew allocates 360 B/call; constructor resolves both directions. Test all blend/skew/Weks and ping-pong behavior. |
| P2 | Avoid scheduler compaction writes when nothing became terminal | Every update scans and rewrites survivors. Compare large live sets and completion-heavy frames; preserve additions during callbacks. |
| P3 | Consolidate per-owner lifetime subscriptions and inspect shader binding caches | Source-level candidates; no isolated allocation/CPU attribution in this run. Validate owner exit, shader changes and cleanup before caching. |

### Execution-plan construction and traversal

[TweenInstance.cs](../../addons/tweens_gd/csharp/Core/TweenInstance.cs) constructs
`new ExecutionPlan(this, [this])` in every typed instance. The
[plan constructor](../../addons/tweens_gd/csharp/Core/ExecutionPlan.cs) allocates
an entry array, an Entry object per leaf, a temporary time array, and a boundary
pipeline using `Where/Append/Distinct/Order/ToArray`. Even one leaf takes that
general route. The handle also owns a playback snapshot and a `Playback` object.

`AddChainCore` first constructs typed leaves with their own single-entry plans,
then creates a separate plan on the chain root; coordinated leaves are sampled
by the root rather than advanced through their private plans. Native
[scheduler.cpp](../../gdextension/src/scheduler.cpp) already avoids individual
plans when adding chain leaves with enrollment disabled. Bringing that property
to C# is a narrower first experiment than redesigning the scheduler.

A single-tween fast path could avoid boundary sorting, plan phases, Entry
indirection and repeated scans. Preserve delay fill, negative offsets, completion
ordering and pause/resume inside callbacks. Do not equate removing one plan with
removing all 1,472 B of lifecycle allocation; no per-object heap trace was taken.

### Repeated lifetime checks

`TweenScheduler.Update` calls `CheckTarget` then `CanAdvance`, which calls
`CheckTarget` again. `ExecutionPlan.StopRequested` calls `CanAdvance` at multiple
phase boundaries. `SampleAt`, `Apply` and `NotifyUpdate` repeatedly test target
validity and `PlaybackInterrupted`, which may again call `CanAdvance`. A node
usually serves as both target and owner, adding overlapping validity, queued
deletion and in-tree queries, plus `CanProcess`.

These guards protect real behavior: getters, setters, easing and callbacks may
free targets, pause, cancel, throw or dispose the scheduler. Optimize redundant
checks only across regions that cannot run user code; retain checks after those
boundaries. The conformance, lifetime, callback-reentrancy and warning-sensitive
tests must gate any implementation change.

### Other allocation and CPU candidates

- [Group.IsPaused](../../addons/tweens_gd/csharp/Core/Group.cs) materializes
  `members.Where(...).ToArray()` before `All`. An indexed scan needs no temporary
  collection and can preserve the current requirement that some member is active.
- [Easing.GetFunction](../../addons/tweens_gd/csharp/Easing/Easing.cs) uses the
  composition cache only for default Makima/blend/skew. Custom `Evaluate` calls
  construct captured delegates repeatedly. Playback resolves delegates at start,
  so the 360 B/sample result must **not** be charged to ordinary tween updates.
  Prefer explicit prepared evaluators or bounded caching over an unbounded cache
  keyed by arbitrary floating-point values.
- `End` is already lazy. Preserve that: awaiting adds 96 B in this test, but users
  who never request it avoid the completion source and task.
- Per-node `BindLifetime` creates a bound delegate and a Godot signal connection
  per root. Groups also subscribe bound handlers per member. Sharing subscriptions
  may help bulk creation, but must release owners and members deterministically.
- [ShaderParameterTween](../../addons/tweens_gd/csharp/Tweens/ShaderParameterTween.cs)
  constructs a StringName and watch, enumerates uniform metadata at activation,
  and validates shader identity on reads/writes. Possible versioned metadata
  caching needs dedicated shader benchmarks; material roughness does not cover it.
- The native [execution plan](../../gdextension/src/execution_plan.cpp) resolves
  leaf ObjectIDs repeatedly in phase and completion scans. A native single-leaf
  path may help too, but raw-pointer caching must survive callbacks that release
  objects. C# BDN allocation totals do not describe those native allocations.

## Next experiments and validation

Start with unused chain-leaf plans and allocation-free group polling, then compare
a single-leaf fast path and the repeated-check changes separately. Keep each
experiment small so the new benchmarks can attribute improvement. For regression
attribution, bisect the October 1 change using a compatible common workload and
the same engine/runtime package versions on both revisions.

Use normal adaptive BDN jobs and multiple launches for final decisions; retain
uncertainty, not just a ratio. Add allocation-stack and CPU sampling profiles
before assigning percentages to individual constructors or guards. Extend
coverage to vectors/quaternions, long overlapping chains, mass completion,
cancellation callbacks, shaders, automatic runner frames, net8.0 and WASM before
generalizing these desktop/manual-scheduler results.

Validation performed for this integration: Release build; all 34 BDN measurement
cases; independent warmed allocation checks; three sequential legacy comparison
runs. Library behavior was not changed, so the existing conformance suite was not
rerun for this benchmark/report-only change.
