---
title: Core API
description: GDScript entry points, definition fields, playback handles, groups, the manual scheduler, and the adapter base class.
---

Look up the core GDScript API here: the functions that start playback and what
they return, the fields of a definition, the members of handles and groups, and the
manual scheduler. For explanations and examples, follow the links to the guides.

Preload the addon's entry script, or use its global class name `TweensGd` once the
editor has generated the global class cache:

```gdscript
const Tweens = preload("res://addons/tweens_gd/tweens.gd")
```

The examples on this site use the `Tweens` constant. The classes it returns are
also registered globally: `TweensGdDefinition`, `TweensGdHandle`, `TweensGdGroup`,
`TweensGdScheduler`, `TweensGdAdapter`, and `TweensGdCancellation`. Use the API on
Godot's main thread.

## Start playback

| Entry point | Returns | Purpose |
| --- | --- | --- |
| `Tweens.play(target, definition, owner = null)` | `TweensGdHandle` | Start one definition; a node target is its own owner |
| `Tweens.play_all(target, definitions, owner = null)` | `TweensGdGroup` | Start an array of definitions on one target |
| `Tweens.group(handles)` | `TweensGdGroup` | Treat running handles as one step with one `end` |
| `Tweens.Group.of(handles)` | `TweensGdGroup` | Same as `Tweens.group()` |
| `Tweens.cancel_tweens(owner, include_children = false)` | `void` | Cancel the automatic playback this node owns |

A resource or other non-node target needs an `owner`: an in-tree `Node`, or a
`SceneTree`, which binds playback to the tree's root. [Materials](/gdscript/materials/)
has examples.

`play()` and `play_all()` never return `null`. A rejected start returns a handle or
group that has already settled with `Tweens.Reason.FAILED`, so `await
Tweens.play(target, definition).end` needs no null check. `play_all()` stops at
the first rejected start and cancels the definitions it already started.

## Create definitions

| Factory | Returns | Purpose |
| --- | --- | --- |
| `Tweens.property(path, to, seconds = 0.0)` | `TweensGdDefinition` | Tween a property or component path such as `^"position:x"` |
| `Tweens.value(from, to, seconds = 0.0)` | `TweensGdDefinition` | Deliver values to `on_update` without writing a property |
| `Tweens.custom(getter, setter, to, seconds = 0.0, interpolator = Callable(), validator = Callable())` | `TweensGdDefinition` | Read and write custom storage through Callables |
| `Tweens.shader_parameter(parameter, to = null, seconds = 0.0)` | `TweensGdDefinition` | Tween a `ShaderMaterial` uniform |
| `Tweens.instance_shader_parameter(parameter, to = null, seconds = 0.0)` | `TweensGdDefinition` | Tween an `instance uniform` on a `CanvasItem` or `GeometryInstance3D` |
| `Tweens.position_2d(to = null, seconds = 0.0)` and the other named helpers | `TweensGdDefinition` | Tween a known property with target and value checks; see the [helper catalog](/gdscript/nodes/) |

## Definitions

A `TweensGdDefinition` is a mutable object. Each start snapshots its configuration,
so changing a definition affects only later starts. `definition.copy()` returns a
separate definition; see [definitions](/gdscript/definitions/).

| Fields | Reference |
| --- | --- |
| `from_value`, `to_value` (`null` captures the current value), `initial_value` | [Definitions](/gdscript/definitions/) |
| `duration`, `delay`, `offset`, `repeats`, `use_ping_pong`, `ping_pong_interval`, `repeat_interval`, `fill` | [Timing and loops](/gdscript/timing/) |
| `ease`, `skew`, `ease_function`, `curve` | [Easing](/gdscript/easing/) |
| `process_mode`, `use_unscaled_time` | [Timing and loops](/gdscript/timing/) |
| `pause_mode`, `suppress_callbacks_when_target_invalid` | [Lifetime and ownership](/gdscript/lifetime/) |
| `on_add`, `on_start`, `on_update`, `on_end`, `on_cancel`, `on_finally` | [Control and completion](/gdscript/playback/) |
| `property`, `adapter` | [Custom adapters](/gdscript/custom-tweens/) |

Defaults: zero `duration`, `delay`, and `offset`; `repeats = 0`; `ease =
Tweens.Ease.LINEAR`; `skew = 1.0`; `fill = Tweens.Fill.RETAIN_FINAL_VALUE`;
`process_mode = Tweens.Process.PROCESS`; `pause_mode = Tweens.Pause.BOUND`.
`initial_value` is the start value of a callback-only tween and defaults to `0.0`.

Callbacks are synchronous Callables. `on_update` receives `(handle, value)`; the
others receive `(handle)`. `definition.validate()` returns an empty string when the
configuration is valid, or a message describing the first problem.

| Constant | Values |
| --- | --- |
| `Tweens.INFINITE` | `-1`, for `repeats` that run until cancelled |
| `Tweens.Fill` | `NONE`, `APPLY_FROM_DURING_DELAY`, `RETAIN_FINAL_VALUE`, `BOTH` |
| `Tweens.Process` | `PROCESS`, `PHYSICS` |
| `Tweens.Pause` | `BOUND`, `SCENE_TREE`, `ALWAYS` |
| `Tweens.Ease` | 33 easing functions, listed under [easing](/gdscript/easing/) |

## Playback handles

`play()` returns a `TweensGdHandle`.

| Member | Type | Meaning |
| --- | --- | --- |
| `pause()`, `resume()` | `void` | Set or clear explicit pause |
| `is_paused` | `bool` | True while explicitly paused; assignable |
| `cancel()` | `void` | End playback and keep the latest sample; safe to repeat |
| `state` | `int` | A `Tweens.State` value: `DELAYED`, `PLAYING`, `INTERVAL`, `COMPLETED`, `CANCELLED`, or `FAULTED` |
| `is_terminal` | `bool` | True when completed, cancelled, or faulted |
| `is_settled` | `bool` | True once terminal callbacks and cleanup have run |
| `progress` | `float` | Normalized current-leg progress, before easing |
| `completion_reason` | `int` | A `Tweens.Reason` value, or `-1` until playback ends |
| `error` | `String` | Detected failure diagnostics; empty otherwise |
| `end` | `Variant` | Await immediately: `ended` while running, stored reason after settlement |
| `wait(cancellation = null)` | `int` | Await the completion reason; returns at once if already settled |
| `ended(reason)` | signal | Emitted once when playback ends |
| `target` | `Object` | Original target; `null` for a rejected start |
| `value` | `Variant` | Value read at start, then the latest value written |

Pause is separate from `state`: there's no paused state. Prefer `await
handle.end` to the `ended` signal, because awaiting the signal after it has
fired waits forever.

`Tweens.Reason` has `COMPLETED`, `CANCELLED`, `TARGET_FREED`, `OWNER_EXITED`,
`RUNNER_DISPOSED`, and `FAILED`. `WAIT_CANCELLED` is returned only by a `wait()`
whose `Tweens.Cancellation` token was cancelled; playback continues.
[Cancellation and completion reasons](/gdscript/cancellation/) describes each reason.

## Groups

`TweensGdGroup` has `pause()`, `resume()`, `cancel()`, `is_paused`,
`is_terminal`, `is_settled`, `completion_reason`, `error`, `end`, `wait(cancellation =
null)`, and the `ended(reason)` signal, plus:

| Member | Type | Meaning |
| --- | --- | --- |
| `members` | `Array[TweensGdHandle]` | A copy of the member handles, duplicates removed |
| `errors` | `Array[String]` | A copy of the members' detected failure messages |

A group completes after every member settles. When a member stops without
completing, the group cancels its active siblings and keeps that first reason.
`is_paused` is true only while every active member is paused. See
[groups and sequences](/gdscript/sequences/).

## Cancel a wait

`Tweens.Cancellation.new()` creates a token with `cancel()`, `is_cancelled`, and a
`cancelled` signal. Pass it to `handle.wait(token)` or `group.wait(token)` to stop
that one wait without stopping playback.

## Manual scheduler

The automatic runner is built on `TweensGdScheduler`. Create one with
`Tweens.Scheduler.new()` to drive playback yourself.

| Member | Type | Purpose |
| --- | --- | --- |
| `add(target, definition, owner = null)` | `TweensGdHandle` | Add playback; a node target becomes its own owner |
| `add_all(target, definitions, owner = null)` | `TweensGdGroup` | Add several definitions on one target |
| `update(delta, unscaled_delta = -1.0, mode = Tweens.Process.PROCESS)` | `void` | Advance the selected process lane |
| `active_count` | `int` | Number of nonterminal handles |
| `cancel_all()` | `void` | Cancel every tween in this scheduler |
| `cancel_owner(owner, include_children = false)` | `void` | Cancel this scheduler's tweens owned by a node |
| `last_error` | `String` | The latest rejection or failure message |
| `error_reported(message)` | signal | Emitted for each reported error |
| `is_disposed` | `bool` | True after `dispose()` |
| `dispose()` | `void` | Stop and settle remaining playback |

A manual scheduler can animate non-node objects without an owner; node targets must
still be inside the tree. `update()` rejects recursive calls and invalid deltas.
Always dispose a manual scheduler. `Tweens.cancel_tweens()` affects the automatic
runner only.

## Adapter base class

`Tweens.Adapter` (`TweensGdAdapter`) is the base for custom storage and per-playback
bindings. Assign an instance to `definition.adapter`.

| Method | Returns | Purpose |
| --- | --- | --- |
| `read(target)` | `Variant` | Read the current value |
| `write(target, value)` | `String` | Write a sample; empty on success |
| `prepare(target)` | `String` | Set up per-playback state before the initial read |
| `restore(target, initial)` | `String` | Restore on natural completion without retention; writes `initial` by default |
| `release()` | `String` | Release per-playback state after callbacks |
| `interpolate(from, to, weight)` | `Variant` | Blend two values |
| `validate_value(value)` | `String` | Reject unsupported or nonfinite values |
| `copy()` | `TweensGdAdapter` | Create the per-playback snapshot; shallow-copies script fields |

Constructors must take no arguments. [Custom adapters](/gdscript/custom-tweens/)
covers the lifecycle and failure handling.

## Differences from C#

The addon follows the C# library's timing, easing, grouping, and lifetime rules;
shared fixtures run against both. The remaining differences come from the language:

| Area | C# | GDScript |
| --- | --- | --- |
| Failures | Awaiting faults with an exception; `Error` holds it | Awaiting `end` returns `Tweens.Reason.FAILED`; `error` holds a diagnostic string |
| Type checks | Generic `TweenInstance<TTarget, TValue>`, checked by the compiler | Target class and value type checked when playback starts |
| Integer values | Integer adapters saturate at 32-bit limits | Integers saturate at signed 64-bit limits; shader integers stay 32-bit |
| Script errors | Exceptions from callbacks and setters fault the tween | Detected problems become `FAILED`; arbitrary errors inside callbacks and setters remain Godot script errors |
| Callbacks | Synchronous | Synchronous; don't `await` inside a callback, await `end` from a separate coroutine instead |

The addon detects invalid configuration, stale Callables, invalid easing results,
and nonfinite interpolation. Recoverable failures in custom adapters should be
returned as error strings from the adapter hooks. See [compatibility](/compatibility/)
for validated platforms.
