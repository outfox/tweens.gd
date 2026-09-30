# Comparative benchmark

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
