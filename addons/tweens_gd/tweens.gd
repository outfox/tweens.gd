# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGd
extends "catalog.gd"
## Pure GDScript entry point. No editor plugin, autoload or native extension needed.

const Types = preload("types.gd")
const Definition = preload("definition.gd")
const Handle = preload("handle.gd")
const Group = preload("group.gd")
const Scheduler = preload("scheduler.gd")
const Easing = preload("easing.gd")
const Runner = preload("runner.gd")
const Adapter = preload("adapter.gd")
const CallableAdapter = preload("callable_adapter.gd")
const ShaderAdapter = preload("shader_adapter.gd")
const Cancellation = preload("cancellation.gd")
const INFINITE := Types.INFINITE
const Fill = Types.Fill
const Process = Types.Process
const Pause = Types.Pause
const State = Types.State
const Reason = Types.Reason
const Ease = Types.Ease

## Configure once, then play the definition on any compatible target.
static func property(path: NodePath, to: Variant, seconds: float = 0.0) -> Definition:
	var definition := Definition.new()
	definition.property = path
	definition.to_value = to
	definition.duration = seconds
	return definition

static func value(from: Variant, to: Variant, seconds: float = 0.0) -> Definition:
	var definition := Definition.new()
	definition.initial_value = from
	definition.from_value = from
	definition.to_value = to
	definition.duration = seconds
	return definition

## Callables receive (target), (target, value), and optionally (from, to, weight).
static func custom(getter: Callable, setter: Callable, to: Variant, seconds: float = 0.0,
		interpolator: Callable = Callable(), validator: Callable = Callable()) -> Definition:
	var definition := Definition.new()
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
static func play_all(target: Variant, definitions: Variant, owner: Variant = null) -> Group:
	if OS.get_thread_caller_id() != OS.get_main_thread_id(): return Group.of([])
	if not definitions is Array or definitions.is_empty(): return Group.of([])
	for definition in definitions:
		if not definition is Definition: return Group.of([_reject("Every entry must be a tween definition.")])
	var handles: Array[Handle] = []
	for definition in definitions:
		handles.append(play(target, definition, owner))
		if handles.back().completion_reason == Reason.FAILED: break
	return Group.of(handles)

static func shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0) -> Definition:
	var definition := Definition.new()
	var adapter := ShaderAdapter.new()
	adapter.parameter = parameter
	definition.adapter = adapter
	definition.to_value = to
	definition.duration = seconds
	return definition

static func instance_shader_parameter(parameter: StringName, to: Variant = null, seconds: float = 0.0) -> Definition:
	var definition := shader_parameter(parameter, to, seconds)
	definition.adapter.instance_uniform = true
	return definition

## Node targets bind to themselves; Resources/Objects need an explicit owner.
## Always returns an awaitable handle, including an already-failed handle on rejection.
# Variant at the boundary lets us reject freed objects inside the function;
# an Object/Node parameter would make Godot abort the call before returning a handle.
static func play(target: Variant, definition: Definition, owner: Variant = null) -> Handle:
	if OS.get_thread_caller_id() != OS.get_main_thread_id():
		return _reject("Use tweens.gd on Godot's main thread.")
	if typeof(target) != TYPE_OBJECT or not is_instance_valid(target):
		return _reject("The target is invalid.")
	if is_instance_valid(owner) and owner is SceneTree: owner = owner.root
	if target is Node and typeof(owner) == TYPE_NIL: owner = target
	if typeof(owner) != TYPE_OBJECT or not is_instance_valid(owner) or not owner is Node:
		return _reject("Automatic playback needs an owner inside the scene tree.")
	if not owner.is_inside_tree() or owner.is_queued_for_deletion():
		return _reject("Automatic playback needs an owner inside the scene tree.")
	if owner.get_tree().has_meta(Runner.CLOSING_KEY):
		return _reject("The scene tree is shutting down.")
	return _runner(owner.get_tree()).scheduler.add(target, definition, owner)

## Groups existing playback handles; interrupted members cancel their siblings.
static func group(members: Variant) -> Group:
	return Group.of(members)

static func _reject(message: String) -> Handle:
	push_error("tweens.gd: " + message)
	return Handle.rejected(message)

static func cancel_tweens(owner: Node, include_children: bool = false) -> void:
	if OS.get_thread_caller_id() != OS.get_main_thread_id(): return
	if not is_instance_valid(owner) or not owner.is_inside_tree(): return
	var tree := owner.get_tree()
	var runner = tree.get_meta(Runner.META_KEY) if tree.has_meta(Runner.META_KEY) else null
	if is_instance_valid(runner): runner.scheduler.cancel_owner(owner, include_children)

static func _runner(tree: SceneTree) -> Runner:
	var existing = tree.get_meta(Runner.META_KEY) if tree.has_meta(Runner.META_KEY) else null
	if is_instance_valid(existing) and not existing.is_queued_for_deletion() and not existing.scheduler.is_disposed:
		return existing
	var runner := Runner.new()
	tree.set_meta(Runner.META_KEY, runner)
	runner.bind_tree(tree)
	return runner
