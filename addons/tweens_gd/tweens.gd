# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name Tweens
extends "catalog.gd"
## Creates reusable definitions and starts independent playback handles.
##
## A definition describes one motion; a handle controls one playback; a group controls
## several handles as one parallel step. Build sequences with ordinary [code]await[/code]:
## [codeblock]
## var move := Tweens.position_2d(Vector2(400, 180), 0.6, Out.CUBIC)
## var fade := Tweens.modulate_alpha(0.0, 0.6)
## await Tweens.play_all(sprite, [move, fade]).end
## await Tweens.play(sprite, Tweens.scale_2d(Vector2.ONE, 0.2)).end
## [/codeblock]
## C# equivalents: [code]new Tweens.Position2D(...)[/code], [code]sprite.Tween(move, fade)[/code],
## and [code]await handle.End[/code]. Member names use snake_case here and PascalCase in C#.
## Timing uses seconds; [code]In.SINE | Out.CUBIC[/code] combines easing legs in both languages.
## Playback and controls use Godot's main thread. No autoload or plugin activation is needed.
##
## @tutorial(GDScript API): https://tweens.gd/gdscript/api/

## Timing, mode, state, and completion-reason constants.
const Types = preload("types.gd")
## Samples easing curves. C# equivalent: [code]tweens.gd.Easing[/code].
const Easing = preload("easing.gd")
## Creates deterministic progress-to-offset functions. C# equivalent: [code]Tweens.FX[/code].
const FX = preload("fx.gd")
## Base adapter for custom storage. C# equivalent: [code]TweenDefinition[/code] property operations.
const Adapter = preload("adapter.gd")
## Adapter configured with getter, setter, interpolator, and validator Callables.
const CallableAdapter = preload("callable_adapter.gd")
## Adapter for material or instance shader uniforms.
const ShaderAdapter = preload("shader_adapter.gd")
## Repeat indefinitely until cancelled. C# equivalent: [code]TweenOptions.Infinite[/code].
const INFINITE := Types.INFINITE
## Delay and completion value behavior. C# equivalent: [code]FillMode[/code].
const Fill = Types.Fill
## Process or physics updates. C# equivalent: [code]TweenProcessMode[/code].
const Process = Types.Process
## Scene and owner pause behavior. C# equivalent: [code]TweenPauseMode[/code].
const Pause = Types.Pause
## Timeline states. C# equivalent: [code]TweenState[/code].
const State = Types.State
## Completion reasons. GDScript also has [code]FAILED[/code] and [code]WAIT_CANCELLED[/code].
const Reason = Types.Reason
## Legacy easing constants. Prefer [In], [Out], and [InOut] for new definitions.
const Ease = Types.Ease

# Definitions: configure first, then play as often as needed.

## Creates a reusable definition for a property or component path, such as [code]^"position:x"[/code].
## [param to] is the endpoint; [code]null[/code] captures the current value at start, before the delay.
## Duration and delay are in seconds. Paths stay on the target; pass a Resource as its own target.
## Vector and Color endpoints also accept numeric component Arrays, resolved to the captured type at start.
static func property(path: NodePath, to: Variant, seconds: float = 0.0, easing: int = Types.Ease.LINEAR,
		delay: float = 0.0) -> TweensGdDefinition:
	return TweensGdDefinition.named(path, &"", TYPE_NIL, to, seconds, easing, delay)

## Creates a reusable callback-value definition without writing a property.
## [param from] supplies the initial value; [param to] supplies the endpoint. Set [code]on_update(handle, value)[/code]
## to consume samples. C# equivalent: a value definition such as [code]Tweens.Float[/code].
static func value(from: Variant, to: Variant, seconds: float = 0.0, easing: int = Types.Ease.LINEAR,
		delay: float = 0.0) -> TweensGdDefinition:
	var definition := TweensGdDefinition.named(^"", &"", TYPE_NIL, to, seconds, easing, delay)
	definition.initial_value = from
	definition.from_value = from
	return definition

## Creates a reusable definition for custom storage. C# equivalent: [code]Tweens.Property[/code].
## [param getter] receives the target; [param setter] receives the target and value.
## Optional [param interpolator] receives from, to, and eased weight; weight may overshoot [code][0, 1][/code].
## Optional [param validator] receives a value and returns an error string, empty on success.
static func custom(getter: Callable, setter: Callable, to: Variant, seconds: float = 0.0,
		interpolator: Callable = Callable(), validator: Callable = Callable()) -> TweensGdDefinition:
	var definition := TweensGdDefinition.new()
	var adapter := CallableAdapter.new()
	adapter.getter = getter
	adapter.setter = setter
	adapter.interpolator = interpolator
	adapter.validator = validator
	definition.adapter = adapter
	definition.to_value = to
	definition.duration = seconds
	return definition

## Creates a reusable definition for a [ShaderMaterial] uniform named [param parameter].
## The endpoint must match the uniform's declared type. [code]null[/code] captures its current value.
static func shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0,
		easing: int = Types.Ease.LINEAR, delay: float = 0.0) -> TweensGdDefinition:
	var definition := TweensGdDefinition.named(^"", &"", TYPE_NIL, to, seconds, easing, delay)
	var adapter := ShaderAdapter.new()
	adapter.parameter = parameter
	definition.adapter = adapter
	return definition

## Creates a reusable definition for an instance uniform on a [CanvasItem] or [GeometryInstance3D].
## Uses the instance override, or the shader default when no override exists.
static func instance_shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0,
		easing: int = Types.Ease.LINEAR, delay: float = 0.0) -> TweensGdDefinition:
	var definition := shader_parameter(parameter, to, seconds, easing, delay)
	definition.adapter.instance_uniform = true
	return definition

# Playback: one handle, a parallel group, or sequential awaits.

## Starts one definition and returns its playback handle. C# equivalent: [code]target.Tween(definition)[/code].
## Snapshots configuration and captures the current value before any delay. Nodes must be in the tree
## and own their playback. Other Objects need an in-tree Node or [SceneTree] as [param owner].
## A rejected start returns an ended handle with [code]Reason.FAILED[/code] and a nonempty [code]error[/code].
static func play(target: Variant, definition: TweensGdDefinition, owner: Variant = null) -> TweensGdHandle:
	return TweensGdRunner.play(target, definition, owner)

## Starts an Array of definitions in parallel on one target and returns their group handle.
## Each member honors its own timing and modes; ownership follows [method play].
## Invalid Array entries reject the group before any start. A rejected start cancels earlier members
## and prevents later starts. C# node equivalent: [code]node.Tween(first, second, ...)[/code].
## C# resource playback uses separate starts combined with [code]Group.Of[/code].
static func play_all(target: Variant, definitions: Variant, owner: Variant = null) -> TweensGdGroup:
	return TweensGdRunner.play_all(target, definitions, owner)

## Groups existing playback handles, including handles on different targets. Starts no new tweens.
## [param members] must be a nonempty Array. If a member stops early or fails, its siblings are cancelled.
## C# equivalent: [code]Group.Of(handle_a, handle_b)[/code]; also available as [method TweensGdGroup.of].
static func group(members: Variant) -> TweensGdGroup:
	return TweensGdGroup.of(members)

## Cancels active tweens owned by [param owner], optionally including descendant owners.
## C# equivalent: [code]owner.CancelTweens(includeChildren)[/code].
static func cancel_tweens(owner: Node, include_children: bool = false) -> void:
	TweensGdRunner.cancel_tweens(owner, include_children)
