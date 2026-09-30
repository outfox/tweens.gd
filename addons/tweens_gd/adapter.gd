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

## Copies configuration for one playback. Captured Objects and Callables remain shared.
## Override to copy custom mutable data or omit transient playback state.
func copy() -> TweensGdAdapter:
	var result: TweensGdAdapter = get_script().new()
	# Match definition snapshots: scalar configuration is copied; captured objects remain shared.
	for field in get_property_list():
		if field.usage & PROPERTY_USAGE_SCRIPT_VARIABLE: result.set(field.name, get(field.name))
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
	return TweensGdInterpolation.interpolate(from, to, weight, _captured_type if _captured_type != TYPE_NIL else typeof(from))

## Validates endpoints and sampled values. Return an error string, empty on success.
func validate_value(value: Variant) -> String:
	if not TweensGdInterpolation.supported(value) or not TweensGdInterpolation.finite(value):
		return "Adapter values must be supported, finite values."
	if typeof(value) == TYPE_QUATERNION and value.length_squared() == 0.0:
		return "Quaternion endpoints must have nonzero length."
	return ""

## Releases resources owned by this playback copy, including after failed preparation.
## Return an error string, empty on success. Captured shared Objects must remain usable.
func release() -> String:
	return ""
