# tweens.gd for GDScript

A beta, pure GDScript implementation of reusable tween definitions and
independent playback handles. Copy this entire `tweens_gd` directory into your
project's `addons/` directory. No plugin activation, autoload, .NET runtime or
GDExtension is required.

Both C# and GDScript support are in beta; APIs may change. Validation covers
2dog/Godot 4.7.2 in Debug and Release,
real OpenGL rendering, the installed standard Godot 4.7.2 engine, a Windows release
export and a single-threaded Web/WASM release export in Edge. Other engines,
browsers and devices have not been validated. A distributable ZIP can be built
with `./scripts/Pack-GDScript.ps1` from the repository root.

## Start and await

```gdscript
extends Node2D

const Tweens = preload("res://addons/tweens_gd/tweens.gd")

func _ready() -> void:
	var move := Tweens.property(^"position", Vector2(400, 180), 0.6)
	move.ease = Tweens.Ease.CUBIC_OUT
	var movement := Tweens.play(self, move)
	await movement.end
	print("Movement ended")
```

`TweensGd` is also registered as a global class in the editor. Explicit preloads
work before the editor has generated a global class cache.

`property(path, to, seconds)` and `value(from, to, seconds)` create mutable
`TweensGdDefinition` objects. `play(target, definition, owner = null)` snapshots
the configuration and always returns a `TweensGdHandle`. Change a definition for future
starts without changing existing playback; `definition.copy()` creates a separate
configuration. Curves are duplicated; Callables and their captured objects are
shared references. Subclass-specific fields are not copied automatically.

Node targets must be inside the tree and use their own lifetime. Resource/Object
targets require an explicit in-tree owner or SceneTree for automatic playback:

```gdscript
var fade := Tweens.property(^"modulate:a", 0.0, 0.3)
var first := Tweens.play(sprite, fade)
var second := Tweens.play(label, fade)
first.pause()
first.resume()
second.cancel()
Tweens.cancel_tweens(sprite, true) # Include descendant owners.

var roughness := Tweens.property(^"roughness", 0.2, 0.5)
var material_handle := Tweens.play(material, roughness, mesh_instance)
var tree_handle := Tweens.play(material, roughness, get_tree())
```

## Named helpers

All 331 concrete C# property/value adapters have corresponding factories, including
transforms and components, Control layout, drawing, lights, cameras, audio, particles,
paths, materials and discrete properties. See [the complete catalog](CATALOG.md).
Helpers return the same reusable definition type and validate the native target
class and captured value type before playback:

```gdscript
var move := Tweens.position_2d(Vector2(400, 180), 0.6)
move.ease = Tweens.Ease.CUBIC_OUT
var fade := Tweens.modulate_alpha(0.0, 0.3)
var together := Tweens.play_all(sprite, [move, fade])
await together.end
```

Factory arguments are `(to = null, seconds = 0.0)`. Null endpoints capture the
initial value. `float_value`, `double_value`, `vector2_value`, `vector3_value`,
`vector4_value`, `color_value`, `quaternion_value` and `rect2_value` provide named
callback-only definitions; GDScript represents both float and double as `float`.
`play_all()` / manual `scheduler.add_all()` start definitions on one target and
return a group. A rejected start cancels preceding siblings and stops further
starts. Invalid array entries are rejected before starting any definitions.

## Properties and values

Supported values: `float`, `int`, `Vector2`, `Vector3`, `Vector4`, `Color`,
`Quaternion` and `Rect2`. Property paths select a property on the target and
optional value components, such as `position:x`, `modulate:a`, or `region_rect:size:x`.
Paths cannot cross into another Object/resource or traverse nodes. Pass that
object as the target with an owner instead.

The initial property is captured at start, before `on_add`. Null `from_value`
or `to_value` endpoints use the captured value. An empty property path creates
a callback-only tween; its omitted endpoints use `initial_value` (default `0.0`).

```gdscript
var score := Tweens.value(0.0, 100.0, 1.0)
score.on_update = func(_handle, value): label.text = str(roundi(value))
var handle := Tweens.play(self, score)
```

Quaternion interpolation normalizes endpoints and uses shortest-path slerp.
Integers round ties away from zero and saturate at GDScript's signed 64-bit limits;
intermediate interpolation uses floating point, so values above 2^53 can lose
precision. Exact integer endpoints are preserved. C#'s integer adapters use 32-bit
limits. Colors/vectors preserve easing overshoot. Engine setters can clamp values.

Component writes read the other components at application time. Concurrent
tweens write in insertion order. Resource properties modify the supplied resource,
including when it is shared. Generic property paths and named helpers provide
access to Node2D, Node3D, Control and ordinary material properties. Shader-uniform
restoration is available through the shader helpers below.

## Timing configuration

| Field | Default and behavior |
| --- | --- |
| `duration` | `0.0`; seconds per leg; zero completes on the first eligible update |
| `delay` | `0.0`; initial wait, preserving unused delta |
| `offset` | `0.0`; seconds into the first leg, between zero and duration; delay comes first |
| `repeats` | `0`; cycles after the first; `Tweens.INFINITE` (`-1`) repeats until cancelled |
| `use_ping_pong` | `false`; forward and return legs form one cycle |
| `ping_pong_interval` | `0.0`; wait at the far endpoint before returning |
| `repeat_interval` | `0.0`; wait between cycles, never after the last |
| `ease` | `Tweens.Ease.LINEAR`; 33 functions matching the C# catalog |
| `skew` | `1.0`; positive exponent applied to normalized time before easing |
| `ease_function` / `curve` | Optional synchronous Callable or duplicated Curve; choose one |
| `fill` | `Tweens.Fill.RETAIN_FINAL_VALUE`; also `NONE`, `APPLY_FROM_DURING_DELAY`, `BOTH` |
| `process_mode` | `Tweens.Process.PROCESS`; `PHYSICS` uses physics updates |
| `pause_mode` | `Tweens.Pause.BOUND`; alternatives `SCENE_TREE` and `ALWAYS` |
| `use_unscaled_time` | `false`; automatic process uses monotonic ticks; physics uses `1 / Engine.physics_ticks_per_second` |
| `suppress_callbacks_when_target_invalid` | `false`; optionally suppress terminal callbacks for invalid targets/owners |

Timing must be finite and nonnegative. Infinite zero-time cycles are rejected.
Large deltas skip cycles arithmetically. `progress` is the current leg position,
reversing on return, rather than overall completion. A sample at an exact cycle
boundary displays the previous endpoint. Skipped cycles do not synthesize callbacks.

Without retention, natural completion restores the captured initial value;
cancellation retains the last applied value. A ping-pong tween ends at its from
endpoint. Instance pause always wins over tree/owner pause settings. A node's
`set_process(false)` alone does not disable bound tweens.

## Completion and callbacks

Use `await handle.end`. It returns the cached reason immediately if the handle
has already settled, and supports multiple waiters. `ended(reason)` is a one-shot
signal; awaiting that raw signal after emission would wait forever.

`end` is a signal-backed property: it returns `ended` while running and the stored
reason once settled. Read and await it in one expression rather than saving the
signal for later. You can ignore the result by default; inspect it when the next
action requires successful completion. The existing `wait()` coroutine remains
available, with an optional cancellation token.

Reasons: `COMPLETED`, `CANCELLED`, `TARGET_FREED`, `OWNER_EXITED`,
`RUNNER_DISPOSED`, and `FAILED`. Inspect `handle.error` for a detected failure.
Invalid starts return an already-settled `FAILED` handle instead of null, so
`await Tweens.play(target, definition).end` is safe without a null check or a
future tick. Rejected starts schedule no work, retain no target/configuration, and
run no definition callbacks. Their `value` and `target` are null and `progress` is
zero. Pause, resume and cancel remain safe; repeated waits return the same reason.
Rejection diagnostics are still reported to Godot's error log. Use `end` rather
than the raw signal: an already-rejected handle will not emit `ended` later.

Removal/reparenting ends node-owned playback immediately, including while paused.
Queued targets are never written. Direct `free()` can report `OWNER_EXITED` because
Godot emits tree exit before invalidating the node.

Callbacks are synchronous Callables: `on_add(handle)`, `on_start(handle)`,
`on_update(handle, value)`, `on_end(handle)`, `on_cancel(handle)`, and
`on_finally(handle)`. Order is add, optional delay fill, start once, updates,
end/cancel, finally, then the completion signal. Detected failures run finally.
Terminal state is visible inside terminal callbacks. Calling cancel repeatedly is
safe. New tweens created by callbacks first sample on the next eligible update.

Tweens started synchronously inside `on_end` or a resuming await of `end` inherit the
finished tween's overshoot on the same scheduler, process lane and time scale.
Unrelated starts, late waits on already-finished handles and continuations after
another awaited signal do not inherit it. Check the reason before starting a next
step when cancellation should stop a sequence.

Use the API on Godot's main thread. Do not `await` inside callbacks; put sequences
in a separate coroutine awaiting `end`. GDScript cannot catch arbitrary script
errors as C# exceptions. Invalid configuration, stale Callables, nonnumeric/nonfinite
easing and nonfinite interpolation are detected; arbitrary errors inside callbacks
or custom property setters remain Godot script errors, with no promised conversion
to `FAILED`. C# exception-task behavior cannot be reproduced for arbitrary script errors.

Individual waits can be cancelled without stopping playback:

```gdscript
var cancellation := Tweens.Cancellation.new()
get_tree().create_timer(0.25).timeout.connect(cancellation.cancel)
var reason := await handle.wait(cancellation)
if reason == Tweens.Reason.WAIT_CANCELLED:
	print("Stopped waiting; playback continues")
```

Groups accept the same optional token. Other waiters are unaffected. Already-settled
playback returns its actual reason even if the token is cancelled. `WAIT_CANCELLED`
is a wait result only; it never becomes a handle/group completion reason.

## Custom adapters

Use Callables for custom storage, with optional interpolation and value validation:

```gdscript
var intensity := Tweens.custom(
	func(target): return target.get_meta(&"intensity", 0.0),
	func(target, value): target.set_meta(&"intensity", value),
	1.0, 0.5)
var handle := Tweens.play(self, intensity)
```

The optional fifth argument is `interpolator(from, to, weight)`. The sixth is
`validator(value)`, returning an empty string on success or a diagnostic on failure.
Override both to support additional Variant value types. A setter can return a
diagnostic string to fail playback; a void return means success.
Default interpolation follows the captured storage type: float storage keeps
fractional samples with integer endpoints, and integer storage rounds samples.

For resource bindings, extend `Tweens.Adapter` and assign an instance to
`definition.adapter`. Override `read(target)`, `write(target, value)`, and optionally
`prepare(target)`, `restore(target, initial)`, `release()`, `interpolate(from, to, weight)`
and `validate_value(value)`. All hooks except read/interpolate return an error string,
empty on success. Constructors must accept no arguments. `copy()` creates a new
adapter and shallow-copies script fields; override it when configuration needs a
different copy policy. Arrays, resources and captured objects remain shared unless
explicitly duplicated. Initialize private playback state in `prepare()`.

Each start owns its adapter snapshot. Preparation precedes the initial read;
restoration runs on natural non-retaining completion; release runs after callbacks
and before waiters resume, including after failed preparation. Reentrant cancellation
waits for active setters/interpolators to return before release. Multiple detected
failures are retained in `handle.error`. Arbitrary GDScript runtime errors still
cannot be caught; use the hook error returns for recoverable failures.

## Shader uniforms

```gdscript
var dissolve := Tweens.shader_parameter(&"dissolve", 1.0, 0.5)
dissolve.fill = Tweens.Fill.NONE
var material_tween := Tweens.play(shader_material, dissolve, self)
var pulse := Tweens.play(mesh, Tweens.instance_shader_parameter(&"pulse", 1.0, 0.5))
```

Material uniforms target a `ShaderMaterial`. Instance uniforms target `CanvasItem`
or `GeometryInstance3D` and must be declared with `instance uniform`. Supported
uniform types are float, int, Vector2/3/4 and Color. Endpoints must match uniform
metadata exactly; Color and Vector4 are distinct. Shader integers use signed 32-bit
limits and saturating interpolation. Names and bindings are captured per start.

Missing uniforms, incompatible types and nonfinite values fail before registration.
An absent override captures the declared default, which requires a working renderer.
Natural completion without `RETAIN_FINAL_VALUE` restores the original explicit
override or removes the new override if none existed. Cancellation keeps the last
sample. Shared material uniforms affect every user of that material.

Changes to a tracked shader, mesh, effective material, overlay or next pass fail the
next write/restore with `FAILED`. Inherited CanvasItem materials are tracked too.
Metadata is checked at start; binding identity and shader change signals are checked
during playback. Shader subscriptions are released on every termination path.

## Groups

Group existing handles to control and await parallel playback as one step:

```gdscript
var motion := Tweens.group([
	Tweens.play(sprite, Tweens.property(^"position", Vector2(400, 180), 0.6)),
	Tweens.play(sprite, Tweens.property(^"modulate:a", 0.0, 0.3)),
])
motion.pause()
motion.resume()
await motion.end
print("Motion ended")
```

`Tweens.Group.of(handles)` is equivalent. A group completes after every member
settles, including its callbacks and cleanup. When a member stops without completing,
the group cancels its active siblings and keeps that first stop reason. This includes
already-rejected `FAILED` handles. `group.cancel()` cancels the remaining playback;
completed members keep their result. The group supports multiple and late waits,
and emits `ended(reason)` once. Use `end` for possibly already-settled groups.

`is_terminal`, `is_settled`, `completion_reason`, `error` and `errors` expose the
result. `error` joins detected member diagnostics with newlines; `errors` returns
a copy of the individual messages. `is_paused` is true only when there is active
playback and every active member is paused; assigning it pauses/resumes those members.
`members` returns a copy of the handles, with duplicates removed in first-occurrence
order. Input arrays can be changed after construction without changing the group.
Groups retain their handles for inspection; release groups you no longer need.
Dropping a group does not cancel playback or remove its sibling-cancellation rule.

An empty array or a non-handle member logs an error and returns an already-settled
`FAILED` group, without changing any supplied handles. The factory never returns
null. Groups contain playback handles; definitions must be started with `play()`
or `scheduler.add()` first.

Successful inline group continuations inherit the overshoot of the member that
finished last: the latest update, then the smallest overshoot within that update.
Every member and the continuation must share a scheduler, process lane and time
scale. Mixed-clock groups and interrupted groups carry no overshoot. New playback
still first samples on the next eligible update.

## Manual scheduling and diagnostics

```gdscript
var scheduler := Tweens.Scheduler.new()
var handle := scheduler.add(target, definition) # Optional third argument: owner.
scheduler.update(0.25) # Explicit delta; optional unscaled delta and process lane.
if handle.completion_reason == Tweens.Reason.FAILED:
	print(handle.error)
scheduler.dispose()
```

A manual scheduler can animate non-node Objects without an owner. Node targets
still require an in-tree node. `add()` also always returns a handle, already settled
with `FAILED` on invalid input. `error_reported(message)`, `last_error` and
`handle.error` provide diagnostics. `update()` rejects
recursive calls and invalid deltas. `cancel_all()`, `cancel_owner()` and
`active_count` inspect/control its work. Always dispose manual schedulers to settle
their active handles. Automatic `cancel_tweens()` affects the per-tree runner only.

The automatic runner attaches deferred under the root and uses process/physics
priority 1000. It releases playback on teardown. Handles retain their target for
inspection; release handles you no longer need.

## Validation and remaining work

The repository's `testbed-gdscript/` contains a standalone Godot test project,
a minimal 2dog launcher, shared C#/GDScript timing/easing/group fixtures and a reproducible
desktop microbenchmark. See its README for commands. Performance at high tween
counts needs further work; this implementation is not advertised as equivalent
to the built-in native Tween's throughput.

Remaining language differences: no automatic conversion of arbitrary GDScript errors
to C# exception tasks, runtime target/value checks in place of C# generics, and
GDScript's numeric representations. Wider browser/device coverage and performance
profiling remain future work; the accepted 1,000-tween baseline has not been retuned.
The addon remains pure GDScript.
