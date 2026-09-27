# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdDefinition
extends RefCounted
## Reusable configuration. Each start copies it; null endpoints use the captured value.

const Types = preload("types.gd")
const Adapter = preload("adapter.gd")

var property: NodePath
var adapter: Adapter
## Optional native class/value constraints used by named helpers.
var target_class: StringName
var value_type: int = TYPE_NIL
var from_value: Variant = null
var to_value: Variant = null
## Initial value for callback-only tweens (an empty property path).
var initial_value: Variant = 0.0
var duration: float = 0.0
var delay: float = 0.0
var offset: float = 0.0
var repeats: int = 0
var use_ping_pong: bool = false
var ping_pong_interval: float = 0.0
var repeat_interval: float = 0.0
var fill: int = Types.Fill.RETAIN_FINAL_VALUE
var ease: int = Types.Ease.LINEAR
var skew: float = 1.0
var ease_function: Callable
var curve: Curve
var process_mode: int = Types.Process.PROCESS
var pause_mode: int = Types.Pause.BOUND
var use_unscaled_time: bool = false
var suppress_callbacks_when_target_invalid: bool = false
## Callbacks take the handle, except on_update(handle, value).
var on_add: Callable
var on_start: Callable
var on_update: Callable
var on_end: Callable
var on_cancel: Callable
var on_finally: Callable

func copy() -> TweensGdDefinition:
	var result := get_script().new() as TweensGdDefinition
	result.property = property
	result.adapter = adapter.copy() if adapter != null else null
	result.target_class = target_class
	result.value_type = value_type
	result.from_value = from_value
	result.to_value = to_value
	result.initial_value = initial_value
	result.duration = duration
	result.delay = delay
	result.offset = offset
	result.repeats = repeats
	result.use_ping_pong = use_ping_pong
	result.ping_pong_interval = ping_pong_interval
	result.repeat_interval = repeat_interval
	result.fill = fill
	result.ease = ease
	result.skew = skew
	result.ease_function = ease_function
	result.process_mode = process_mode
	result.pause_mode = pause_mode
	result.use_unscaled_time = use_unscaled_time
	result.suppress_callbacks_when_target_invalid = suppress_callbacks_when_target_invalid
	result.on_add = on_add
	result.on_start = on_start
	result.on_update = on_update
	result.on_end = on_end
	result.on_cancel = on_cancel
	result.on_finally = on_finally
	if curve != null:
		# Resource.duplicate() crashes after a same-process 2dog 4.7.2.91 restart.
		# Copy the public curve data explicitly; no native extension is needed.
		result.curve = Curve.new()
		result.curve.min_domain = minf(curve.min_domain, 0.0)
		result.curve.max_domain = maxf(curve.max_domain, 1.0)
		result.curve.min_domain = curve.min_domain
		result.curve.max_domain = curve.max_domain
		result.curve.min_value = minf(curve.min_value, 0.0)
		result.curve.max_value = maxf(curve.max_value, 1.0)
		result.curve.min_value = curve.min_value
		result.curve.max_value = curve.max_value
		result.curve.bake_resolution = curve.bake_resolution
		for index in range(curve.point_count):
			result.curve.add_point(curve.get_point_position(index), curve.get_point_left_tangent(index),
				curve.get_point_right_tangent(index), curve.get_point_left_mode(index), curve.get_point_right_mode(index))
	return result

## Like C#'s `with`: each returns a copy with one setting changed and leaves this definition as it is,
## so a shared definition can vary one start. Chain them: `pop.with_delay(0.1).with_to(target)`.
func with_from(value: Variant) -> TweensGdDefinition: return _with(&"from_value", value)
func with_to(value: Variant) -> TweensGdDefinition: return _with(&"to_value", value)
func with_initial_value(value: Variant) -> TweensGdDefinition: return _with(&"initial_value", value)
func with_duration(seconds: float) -> TweensGdDefinition: return _with(&"duration", seconds)
func with_delay(seconds: float) -> TweensGdDefinition: return _with(&"delay", seconds)
func with_offset(seconds: float) -> TweensGdDefinition: return _with(&"offset", seconds)
func with_repeats(count: int) -> TweensGdDefinition: return _with(&"repeats", count)
func with_ping_pong(enabled: bool = true) -> TweensGdDefinition: return _with(&"use_ping_pong", enabled)
func with_ping_pong_interval(seconds: float) -> TweensGdDefinition: return _with(&"ping_pong_interval", seconds)
func with_repeat_interval(seconds: float) -> TweensGdDefinition: return _with(&"repeat_interval", seconds)
func with_fill(mode: Types.Fill) -> TweensGdDefinition: return _with(&"fill", mode)
func with_ease(easing: Types.Ease) -> TweensGdDefinition: return _with(&"ease", easing)
func with_skew(exponent: float) -> TweensGdDefinition: return _with(&"skew", exponent)
func with_ease_function(function: Callable) -> TweensGdDefinition: return _with(&"ease_function", function)
func with_curve(shape: Curve) -> TweensGdDefinition: return _with(&"curve", shape)
func with_process_mode(mode: Types.Process) -> TweensGdDefinition: return _with(&"process_mode", mode)
func with_pause_mode(mode: Types.Pause) -> TweensGdDefinition: return _with(&"pause_mode", mode)
func with_unscaled_time(enabled: bool = true) -> TweensGdDefinition: return _with(&"use_unscaled_time", enabled)
func with_suppress_callbacks_when_target_invalid(enabled: bool = true) -> TweensGdDefinition:
	return _with(&"suppress_callbacks_when_target_invalid", enabled)
func with_on_add(callback: Callable) -> TweensGdDefinition: return _with(&"on_add", callback)
func with_on_start(callback: Callable) -> TweensGdDefinition: return _with(&"on_start", callback)
func with_on_update(callback: Callable) -> TweensGdDefinition: return _with(&"on_update", callback)
func with_on_end(callback: Callable) -> TweensGdDefinition: return _with(&"on_end", callback)
func with_on_cancel(callback: Callable) -> TweensGdDefinition: return _with(&"on_cancel", callback)
func with_on_finally(callback: Callable) -> TweensGdDefinition: return _with(&"on_finally", callback)

func _with(field: StringName, value: Variant) -> TweensGdDefinition:
	var result := copy()
	result.set(field, value)
	return result

## Returns an empty string on success. No partial playback is created on failure.
func validate() -> String:
	if adapter != null and not property.is_empty(): return "Choose either an adapter or a property path."
	for seconds in [duration, delay, offset, ping_pong_interval, repeat_interval]:
		if not is_finite(seconds) or seconds < 0.0:
			return "Timing must be finite and nonnegative."
	if offset > duration: return "Offset must not exceed duration."
	if repeats < Types.INFINITE: return "Repeats must be -1 or nonnegative."
	if not is_finite(skew) or skew <= 0.0: return "Skew must be finite and positive."
	if not Types.Ease.values().has(ease): return "Unknown easing function."
	if not Types.Process.values().has(process_mode) or not Types.Pause.values().has(pause_mode):
		return "Unknown process or pause mode."
	if fill < 0 or fill > Types.Fill.BOTH: return "Unknown fill flags."
	if curve != null and not ease_function.is_null(): return "Choose either curve or ease_function."
	for callback in [ease_function, on_add, on_start, on_update, on_end, on_cancel, on_finally]:
		if not callback.is_null() and not callback.is_valid(): return "A configured Callable is invalid."
	var span := duration + (duration + ping_pong_interval if use_ping_pong else 0.0) + repeat_interval
	if not is_finite(span + delay): return "Timeline is too long."
	if repeats == Types.INFINITE:
		if span == 0.0: return "An infinite tween needs a nonzero cycle duration."
	elif not is_finite(span * (float(repeats) + 1.0) - repeat_interval + delay):
		return "Timeline is too long."
	return ""
