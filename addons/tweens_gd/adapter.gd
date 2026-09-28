# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdAdapter
extends RefCounted
## Override read/write for custom storage. Each playback receives its own copy.
## Hooks return an empty error string on success. Constructors must accept no arguments.

var _captured_type: int = TYPE_NIL

func copy() -> TweensGdAdapter:
	var result: TweensGdAdapter = get_script().new()
	# Match definition snapshots: scalar configuration is copied; captured objects remain shared.
	for field in get_property_list():
		if field.usage & PROPERTY_USAGE_SCRIPT_VARIABLE: result.set(field.name, get(field.name))
	return result

func prepare(_target: Object) -> String:
	return ""

func read(_target: Object) -> Variant:
	return null

func write(_target: Object, _value: Variant) -> String:
	return "Override the adapter's write method."

func restore(target: Object, initial: Variant) -> String:
	return write(target, initial)

func interpolate(from: Variant, to: Variant, weight: float) -> Variant:
	return TweensGdInterpolation.interpolate(from, to, weight, _captured_type if _captured_type != TYPE_NIL else typeof(from))

func validate_value(value: Variant) -> String:
	if not TweensGdInterpolation.supported(value) or not TweensGdInterpolation.finite(value):
		return "Adapter values must be supported, finite values."
	if typeof(value) == TYPE_QUATERNION and value.length_squared() == 0.0:
		return "Quaternion endpoints must have nonzero length."
	return ""

func release() -> String:
	return ""
