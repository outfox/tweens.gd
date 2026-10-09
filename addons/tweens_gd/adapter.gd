# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdAdapter
extends RefCounted
## Reads, writes, and interpolates values in custom storage.
##
## Override [method read] and [method write], and optionally [method interpolate].
## Each playback receives its own [method copy]. Hooks that return [String] use an empty
## string for success and a message for failure. Constructors must accept no arguments.
## C# equivalent: the protected operations of [code]TweenDefinition[/code].

var _captured_type: int = TYPE_NIL
## Working color coordinates: OKLab=0, sRGB=1, linear RGB=2. Copied from the definition at activation.
var color_space := 0
## Premultiplied=0, straight=1. Copied from the definition at activation.
var alpha_mode := 0
## Target API RGB encoding: sRGB=0, linear=1. Copied from the definition at activation.
var color_encoding := 0

## Copies configuration for one playback. Captured Objects and Callables remain shared.
## Override to copy custom mutable data or omit transient playback state.
## Allocate playback bindings in [method prepare]. An unprepared copy can be discarded
## without calling [method release]; its RefCounted references are freed normally.
func copy() -> TweensGdAdapter:
	var script: GDScript = get_script()
	var result: TweensGdAdapter = script.new()
	# Match definition snapshots: scalar configuration is copied; captured objects remain shared.
	for field in get_property_list():
		var usage: int = field["usage"]
		var field_name: StringName = field["name"]
		if usage & PROPERTY_USAGE_SCRIPT_VARIABLE: result.set(field_name, get(field_name))
	return result

## Prepares bindings before the initial read. Return an error string, empty on success.
func prepare(_target: Object) -> String:
	return ""

## Reads the current value. Called at start and during relative playback that follows the target.
func read(_target: Object) -> Variant:
	return null

## Writes an interpolated value. Return an error string, empty on success.
func write(_target: Object, _value: Variant) -> String:
	return "Override the adapter's write method."

## Restores captured state on natural completion without RETAIN_FINAL_VALUE. Defaults to [method write].
func restore(target: Object, initial: Variant) -> String:
	return write(target, initial)

## Interpolates endpoints using eased [param weight], which may overshoot [code][0, 1][/code].
func interpolate(from: Variant, to: Variant, weight: float) -> Variant:
	if typeof(from) == TYPE_COLOR:
		var a: Color = from
		var b: Color = to
		return TweensGdInterpolation.interpolate_color(a, b, weight, color_space, alpha_mode, color_encoding)
	return TweensGdInterpolation.interpolate(from, to, weight, _captured_type if _captured_type != TYPE_NIL else typeof(from))

## Relative offsets and factors use Godot components rather than perceptual color coordinates.
func interpolate_offset(from: Variant, to: Variant, weight: float) -> Variant:
	if typeof(from) == TYPE_COLOR:
		var a: Color = from
		var b: Color = to
		return a.lerp(b, weight)
	return interpolate(from, to, weight)

## Validates endpoints and sampled values. Return an error string, empty on success.
func validate_value(value: Variant) -> String:
	if not TweensGdInterpolation.supported(value) or not TweensGdInterpolation.finite(value):
		return "Adapter values must be supported, finite values."
	if typeof(value) == TYPE_QUATERNION:
		var rotation: Quaternion = value
		if rotation.length_squared() == 0.0: return "Quaternion endpoints must have nonzero length."
	return ""

## Releases bindings owned by this playback copy once preparation was attempted, including
## after failed preparation. Copies discarded before preparation do not call this hook.
## Return an error string, empty on success. Captured shared Objects must remain usable.
func release() -> String:
	return ""
