# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name Tweens
extends "catalog.gd"
## GDScript entry point. Playback runs in the bundled tweens_gd GDExtension, whose classes
## (TweensGdDefinition, TweensGdHandle, TweensGdGroup, TweensGdScheduler, ...) are global.

const Types = preload("types.gd")
const Easing = preload("easing.gd")
const FX = preload("fx.gd")
const Adapter = preload("adapter.gd")
const CallableAdapter = preload("callable_adapter.gd")
const ShaderAdapter = preload("shader_adapter.gd")
const INFINITE := Types.INFINITE
const Fill = Types.Fill
const Process = Types.Process
const Pause = Types.Pause
const State = Types.State
const Reason = Types.Reason
const BlendType = Types.BlendType
const Ease = Types.Ease
const In = Types.In
const Out = Types.Out
const InOut = Types.InOut

## Configure once, then play the definition on any compatible target.
static func property(path: NodePath, to: Variant, seconds: float = 0.0, easing: int = Types.Ease.LINEAR,
		delay: float = 0.0) -> TweensGdDefinition:
	return TweensGdDefinition.named(path, &"", TYPE_NIL, to, seconds, easing, delay)

static func value(from: Variant, to: Variant, seconds: float = 0.0, easing: int = Types.Ease.LINEAR,
		delay: float = 0.0) -> TweensGdDefinition:
	var definition := TweensGdDefinition.named(^"", &"", TYPE_NIL, to, seconds, easing, delay)
	definition.initial_value = from
	definition.from_value = from
	return definition

## Callables receive (target), (target, value), and optionally (from, to, weight).
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

## Start several definitions on one target, then control/await them as a group.
static func play_all(target: Variant, definitions: Variant, owner: Variant = null) -> TweensGdGroup:
	return TweensGdRunner.play_all(target, definitions, owner)

static func shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0,
		easing: int = Types.Ease.LINEAR, delay: float = 0.0) -> TweensGdDefinition:
	var definition := TweensGdDefinition.named(^"", &"", TYPE_NIL, to, seconds, easing, delay)
	var adapter := ShaderAdapter.new()
	adapter.parameter = parameter
	definition.adapter = adapter
	return definition

static func instance_shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0,
		easing: int = Types.Ease.LINEAR, delay: float = 0.0) -> TweensGdDefinition:
	var definition := shader_parameter(parameter, to, seconds, easing, delay)
	definition.adapter.instance_uniform = true
	return definition

## Node targets bind to themselves; Resources/Objects need an explicit owner.
## Always returns an awaitable handle, including an already-failed handle on rejection.
static func play(target: Variant, definition: TweensGdDefinition, owner: Variant = null) -> TweensGdHandle:
	return TweensGdRunner.play(target, definition, owner)

## Groups existing playback handles; interrupted members cancel their siblings.
static func group(members: Variant) -> TweensGdGroup:
	return TweensGdGroup.of(members)

static func cancel_tweens(owner: Node, include_children: bool = false) -> void:
	TweensGdRunner.cancel_tweens(owner, include_children)
