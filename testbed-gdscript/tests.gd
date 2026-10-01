# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends Node

const T = preload("res://addons/tweens_gd/tweens.gd")
# White-box: the runner registers itself on the SceneTree under these metadata keys.
const RUNNER_KEY := &"_tweens_gd_runner"
const CLOSING_KEY := &"_tweens_gd_closing"
const ErrorCollector = preload("error_collector.gd")
const Benchmark = preload("benchmark.gd")
const ChainTests = preload("chain_tests.gd")
const GroupTests = preload("group_tests.gd")
const AdapterTests = preload("adapter_tests.gd")
const ShaderTests = preload("shader_tests.gd")
const CoordinationTests = preload("coordination_tests.gd")
const FXTests = preload("fx_tests.gd")

var finished := false
var trace_runs := false
var checks := 0
var failures: Array[String] = []
var _collector := ErrorCollector.new()
var _wait_results: Array[int] = []
var _sequence_handles: Array = []

class PropertyProbe extends Node:
	var when_written: Callable
	var when_read: Callable
	var amount: float = 0.0:
		set(next):
			amount = next
			if when_written.is_valid(): when_written.call()
		get:
			if when_read.is_valid(): when_read.call()
			return amount

class Holder extends RefCounted:
	var amount := 0.0
	var count := 0
	var turn := Quaternion.IDENTITY
	var corners := Vector4.ZERO

class ResourceProbe extends RefCounted:
	var when_read: Callable
	var amount: float:
		get:
			when_read.call()
			return 0.0

# Lists "dynamic" only when asked to, but always answers for it.
class DynamicProbe extends RefCounted:
	var listed := false
	func _get_property_list() -> Array[Dictionary]:
		var properties: Array[Dictionary] = []
		if listed: properties.append({"name": "dynamic", "type": TYPE_FLOAT, "usage": PROPERTY_USAGE_DEFAULT})
		return properties
	func _get(property: StringName) -> Variant:
		return 1.0 if property == &"dynamic" else null

func _ready() -> void:
	if OS.has_feature("tweens_test_export") or "--run-tests" in OS.get_cmdline_user_args(): _run_standalone.call_deferred()

func _run_standalone() -> void:
	get_tree().create_timer(20.0).timeout.connect(func():
		if not finished:
			push_error("GDScript suite timed out.")
			get_tree().quit(1))
	await run_tests()
	for failure in failures: printerr(failure)
	print("GDScript: %d checks, %d failures." % [checks, failures.size()])
	if OS.has_feature("web"):
		JavaScriptBridge.eval("window.tweensTestResult = " + JSON.stringify({"checks": checks, "failures": failures}) + ";", true)
		return
	get_tree().quit(0 if failures.is_empty() else 1)

func benchmark() -> String:
	OS.add_logger(_collector)
	var json := Benchmark.run(self)
	var errors := _collector.take_errors()
	OS.remove_logger(_collector)
	for message in errors: printerr(message)
	return json if errors.is_empty() else ""

func check(condition: bool, message: String) -> void:
	checks += 1
	if not condition: failures.append(message)

func near(actual: float, expected: float, message: String) -> void:
	check(absf(actual - expected) <= 0.000001, "%s: got %s, expected %s" % [message, actual, expected])

func run_tests() -> void:
	if trace_runs: print("suite: logger")
	OS.add_logger(_collector)
	for test in [_conformance, _validation, _snapshots_and_fill, _factories_and_with, _array_endpoints, _relative, _adjustments,
			_leg_exponents, _interpolation,
			_callbacks, _setter_reentrancy, _lifetime, _pause_and_lanes, _detected_faults, _reference_cleanup]:
		if trace_runs: print("suite: " + test.get_method())
		check(test.call() == true, "Test returned normally: " + test.get_method())
	check(ChainTests.new().run(self), "chain suite returned normally")
	if trace_runs: print("suite: groups and adapters")
	await _await_independent_roots()
	check(await GroupTests.new().run(self) == true, "group suite returned normally")
	check(AdapterTests.new().run(self), "adapter suite returned normally")
	check(FXTests.new().run(self), "FX suite returned normally")
	check(await ShaderTests.new().run(self), "shader suite returned normally")
	check(await CoordinationTests.new().run(self), "coordination suite returned normally")
	await _automatic_runner()
	await _rejected_starts()
	failures.append_array(_collector.take_errors())
	OS.remove_logger(_collector)
	finished = true

func _conformance() -> bool:
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/timelines.json"))
	for test in data.cases:
		var definition := TweensGdDefinition.new()
		for key in test.options: definition.set(key, test.options[key])
		check(definition.validate().is_empty(), test.name + " validates")
		var clock := TweensGdPlayback.create(definition)
		if test.has("local_time"): clock.sample_at(test.local_time)
		for sample in test.samples:
			clock.advance(sample.delta)
			near(clock.progress, sample.progress, test.name)
			check(clock.state == int(sample.state), test.name + " state")
			if sample.has("cycle"): near(clock.cycle, sample.cycle, test.name + " cycle")
	for sample in data.easing:
		near(T.Easing.evaluate(int(sample.ease), sample.t), sample.value, "ease %d" % sample.ease)
	for ease in T.Ease.values():
		near(T.Easing.evaluate(ease, 0.0), 0.0, "ease start")
		near(T.Easing.evaluate(ease, 1.0), 1.0, "ease end")
		check(is_nan(T.Easing.evaluate(ease, NAN)), "ease %d keeps NaN progress detectable" % ease)
	_composed_easing()
	var infinite := T.value(0.0, 1.0, 1.0)
	infinite.repeats = T.INFINITE
	var huge := TweensGdPlayback.create(infinite)
	huge.advance(1.7976931348623157e308)
	huge.advance(1.7976931348623157e308)
	check(not huge.completed and is_finite(huge.progress), "huge infinite deltas saturate")
	return true


func _composed_easing() -> void:
	# The global easing classes' constants, by name.
	var entries := _constants(In)
	var exits := _constants(Out)
	var pairs := _constants(InOut)
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/easing.json"))
	for sample in data.cases:
		var a: int = entries[String(sample["in"]).replace("Step", "_Step").to_upper()]
		var b: int = exits[String(sample["out"]).replace("Step", "_Step").to_upper()]
		near(T.Easing.evaluate(a | b, pow(sample.progress, sample.skew), _constants(BlendType)[String(sample.get("blendType", "Makima")).to_snake_case().to_upper()], sample.get("blend", 0.2)), sample.expected, "shared composed sample %s | %s" % [sample["in"], sample["out"]])
	for family in pairs:
		check(pairs[family] == (entries[family] | exits[family]), "matching ease alias")
		if family.begins_with("BACK") or family.begins_with("ELASTIC") or family.begins_with("BOUNCE") or family.begins_with("JUMP"): continue
		var legacy: int = T.Ease[family if family in ["LINEAR", "SMOOTH_STEP", "SMOOTHER_STEP"] else family + "_IN_OUT"]
		for i in range(1001):
			check(T.Easing.evaluate(pairs[family], i / 1000.0) == T.Easing.evaluate(legacy, i / 1000.0), "matching pair preserves conventional InOut")
	for entry_name in entries:
		var entry: int = entries[entry_name]
		for exit_name in exits:
			var exit: int = exits[exit_name]
			var combined: int = entry | exit
			near(T.Easing.evaluate(combined, -1.0), 0.0, "composed start")
			near(T.Easing.evaluate(combined, 2.0), 1.0, "composed end")
			check(is_nan(T.Easing.evaluate(combined, NAN)), "composed NaN")
			if entry and exit: near(T.Easing.evaluate(combined, 0.5), 0.5, "half legs meet at midpoint")
			for i in range(101):
				var t := i / 100.0
				var a := T.Easing.evaluate(entry | exits[entry_name], t) if exit else T.Easing.evaluate(entry, t)
				var b := T.Easing.evaluate(entries[exit_name] | exit, t) if entry else T.Easing.evaluate(exit, t)
				var expected := lerpf(a, b, smoothstep(0.4, 0.6, t))
				if entry == 0: expected = b
				if exit == 0: expected = a
				near(T.Easing.evaluate(combined, t, BlendType.SMOOTH_STEP), expected, "smoothstep comparison")
				var actual := T.Easing.evaluate(combined, t)
				check(is_finite(actual), "finite default composition")
				if exit == 0 or (entry and t <= 0.4): near(actual, a, "original In half")
				if entry == 0 or (exit and t >= 0.6): near(actual, b, "original Out half")
	for base in ["BACK", "ELASTIC", "JUMP"]:
		check(entries[base] == entries[base + "10"] and exits[base] == exits[base + "10"] and pairs[base] == pairs[base + "10"], "10 percent aliases")
		for percent in [10, 20, 30, 40, 50]:
			var family: String = base + str(percent)
			var solo_low := 0.0
			var solo_high := 1.0
			var pair_low := 0.0
			var pair_high := 1.0
			for i in range(10001):
				var t := i / 10000.0
				var a := T.Easing.evaluate(entries[family], t)
				var b := T.Easing.evaluate(exits[family], t)
				var pair := T.Easing.evaluate(pairs[family], t)
				near(a, 1.0 - T.Easing.evaluate(exits[family], 1.0-t), "overshoot mirror")
				solo_low = minf(solo_low, a)
				solo_high = maxf(solo_high, b)
				pair_low = minf(pair_low, pair)
				pair_high = maxf(pair_high, pair)
			for peak in [-solo_low, solo_high-1.0, -pair_low, pair_high-1.0]:
				near(peak, percent/100.0, "named overshoot peak " + family)
	check(In.BOUNCE10 == In.BOUNCE and Out.BOUNCE10 == Out.BOUNCE and InOut.BOUNCE10 == InOut.BOUNCE, "bounce 10 aliases")
	for percent in [10, 20, 30, 40, 50]:
		var family := "BOUNCE" + str(percent)
		for paired in [false, true]:
			var values := PackedFloat64Array()
			for i in range(10001):
				values.append(T.Easing.evaluate(pairs[family] if paired else exits[family], (0.5 if paired else 0.0) + (0.5 if paired else 1.0)*i/10000.0))
			var depths := PackedFloat64Array()
			for i in range(1, values.size()-1):
				if values[i] < values[i-1] and values[i] <= values[i+1]: depths.append(1.0-values[i])
			check(depths.size() == 3, "three diminishing rebounds " + family)
			for i in range(depths.size()): near(depths[i], percent/100.0/pow(4.0, i), "named first rebound " + family)
		for i in range(1001):
			var t := i/1000.0
			near(T.Easing.evaluate(entries[family], t), 1.0-T.Easing.evaluate(exits[family], 1.0-t), "bounce mirror")
			for curve in [entries[family], exits[family], pairs[family]]:
				var y := T.Easing.evaluate(curve, t)
				check(y >= -0.000001 and y <= 1.000001, "bounce stays inside endpoints")
	near(T.Easing.evaluate(T.Ease.BOUNCE_OUT, 6.0/11.0), 0.75, "legacy first rebound remains 25 percent")
	near(T.Easing.evaluate(T.Ease.BOUNCE_IN_OUT, 17.0/22.0), 0.875, "legacy paired rebound unchanged")
	for percent in [10, 20, 30, 40, 50]:
		var family := "JUMP" + str(percent)
		for paired in [false, true]:
			var values := PackedFloat64Array()
			for i in range(10001):
				values.append(T.Easing.evaluate(pairs[family] if paired else exits[family], (0.5 if paired else 0.0) + (0.5 if paired else 1.0)*i/10000.0))
			var peaks := PackedInt32Array()
			var landed := false
			for i in range(1, values.size()-1):
				if values[i] > values[i-1] and values[i] >= values[i+1]: peaks.append(i)
				if values[i] >= 1.0: landed = true
				if landed: check(values[i] >= 0.999999, "jump stays above target after reaching it")
			check(peaks.size() == 3, "three jump peaks " + family)
			for i in range(peaks.size()):
				near(values[peaks[i]]-1.0, percent/100.0/pow(4.0, i), "named jump peak " + family)
				if i > 0:
					var landing := INF
					for j in range(peaks[i-1], peaks[i]+1): landing = minf(landing, values[j])
					check(absf(landing-1.0) < 0.001, "jump lands between peaks")
			if not peaks.is_empty():
				for i in range(1, peaks[0]+1): check(values[i] >= values[i-1], "direct jump launch")
	for side in [entries, exits]:
		var curves: Array = side.values()
		for i in range(curves.size()):
			if curves[i] == 0: continue
			for j in range(i+1, curves.size()):
				if curves[j] != 0 and curves[i] != curves[j]:
					check(is_nan(T.Easing.evaluate(curves[i] | curves[j], 0.5)), "reject distinct same-leg curves")
			for legacy in T.Ease.values():
				if legacy != 0: check(is_nan(T.Easing.evaluate(curves[i] | legacy, 0.5)), "reject legacy flag mix")
	for width in [-0.1, 1.1, INF, NAN]:
		check(is_nan(T.Easing.evaluate(InOut.SINE, 0.5, BlendType.HERMITE, width)), "invalid blend width")
		check(not T.value(0.0, 1.0, 1.0).with_blend(width).validate().is_empty(), "validate width")
	for method in [-1, BlendType.LINEAR + 1, 99]:
		check(is_nan(T.Easing.evaluate(InOut.SINE, 0.5, method)), "invalid blend method")
		check(not T.value(0.0, 1.0, 1.0).with_blend_type(method).validate().is_empty(), "validate blend")
	near(T.value(0.0, 1.0, 1.0).blend, 0.2, "default blend width")
	check(T.value(0.0, 1.0, 1.0).blend_type == BlendType.MAKIMA, "default blend method")
	near(T.Easing.evaluate(In.QUAD | Out.CUBIC, 0.45), 0.4041956521739131, "default evaluator blend")
	near(T.Easing.evaluate(In.QUAD | Out.CUBIC, 0.45, BlendType.HERMITE), 0.40125, "hermite evaluator blend")
	var custom := T.value(0.0, 1.0, 1.0, In.QUAD | Out.CUBIC).with_blend_type(BlendType.LINEAR).with_blend(0.4)
	var custom_scheduler := TweensGdScheduler.new()
	var custom_handle := custom_scheduler.add(self, custom)
	custom.blend = 1.0
	custom_scheduler.update(0.45)
	near(custom_handle.value, T.Easing.evaluate(custom.ease, 0.45, BlendType.LINEAR, 0.4), "blend settings snapshot")
	for elastic in [In.ELASTIC, Out.ELASTIC, In.BOUNCE50, Out.BOUNCE50, In.BOUNCE20 | Out.BOUNCE40,
			In.JUMP50, Out.JUMP, Out.JUMP20, Out.JUMP30, Out.JUMP40, Out.JUMP50, In.JUMP20 | Out.JUMP40]:
		var elastic_handle := custom_scheduler.add(self, T.value(0.0, 1.0, 1.0, elastic))
		custom_scheduler.update(0.3)
		near(elastic_handle.value, T.Easing.evaluate(elastic, 0.3), "calibrated playback matches sampler")
	custom_scheduler.dispose()
	for invalid in [In.SINE | In.BACK, Out.SINE | Out.BACK,
			In.BACK20 | In.ELASTIC50, Out.BACK50 | Out.ELASTIC,
			In.BOUNCE20 | In.BOUNCE50, Out.BOUNCE40 | Out.ELASTIC50,
			In.SINE | T.Ease.BACK_OUT, 1 << 62, -1]:
		check(is_nan(T.Easing.evaluate(invalid, 0.5)), "reject invalid easing flags")
		check(not T.value(0.0, 1.0, 1.0, invalid).validate().is_empty(), "validate invalid flags")
	var scheduler := TweensGdScheduler.new()
	var target := Holder.new()
	var combined: int = In.QUAD | Out.CUBIC
	var definition := T.property(^"amount", 1.0, 2.0, combined)
	definition.skew = 2.0
	definition.weks = 0.5
	definition.use_ping_pong = true
	var handle := scheduler.add(target, definition)
	scheduler.update(1.0)
	near(target.amount, 0.125, "skew before blended easing")
	scheduler.update(2.0)
	near(target.amount, T.Easing.evaluate(combined, sqrt(0.5)), "independent return skew")
	scheduler.update(1.0)
	near(target.amount, 0.0, "composed return endpoint")
	check(handle.completion_reason == T.Reason.COMPLETED, "composed playback completes")
	check(T.position_2d_x(1.0, 1.0, combined).ease == combined, "catalog accepts flags")
	scheduler.dispose()


func _constants(script: Script) -> Dictionary:
	return script.get_script_constant_map()

func _activated_add(scheduler: TweensGdScheduler, target: Variant, definition: TweensGdDefinition, owner: Variant = null) -> TweensGdHandle:
	var handle := scheduler.add(target, definition, owner)
	scheduler.update(0.0)
	return handle

func _validation() -> bool:
	var scheduler := TweensGdScheduler.new()
	var target := RefCounted.new()
	for field in ["duration", "delay", "offset", "ping_pong_interval", "repeat_interval"]:
		for invalid in ([INF, NAN] if field == "delay" else [-1.0, INF, NAN]):
			var definition := T.value(0.0, 1.0, 1.0)
			definition.set(field, invalid)
			check(_activated_add(scheduler, target, definition).completion_reason == T.Reason.FAILED, "reject invalid " + field)
	for pair in [["offset", 2.0], ["repeats", -2], ["skew", 0.0], ["skew", INF],
			["fill", 4], ["ease", 999]]:
		var definition := T.value(0.0, 1.0, 1.0)
		definition.set(pair[0], pair[1])
		check(_activated_add(scheduler, target, definition).completion_reason == T.Reason.FAILED, "reject " + pair[0])
	for field in ["skew", "weks"]:
		for invalid in [0.0, -1.0, INF, -INF, NAN]:
			var probe := ResourceProbe.new()
			probe.when_read = func(): check(false, "invalid exponent must not read target")
			var definition := T.property(^"amount", 1.0, 1.0)
			definition.set(field, invalid)
			check(_activated_add(scheduler, probe, definition).completion_reason == T.Reason.FAILED, "reject invalid " + field)
	var zero := T.value(0.0, 1.0)
	zero.repeats = T.INFINITE
	check(_activated_add(scheduler, target, zero).completion_reason == T.Reason.FAILED, "reject zero infinite timeline")
	check(_activated_add(scheduler, target, T.value(Vector2.ZERO, Color.WHITE, 1.0)).completion_reason == T.Reason.FAILED, "reject mixed types")
	check(_activated_add(scheduler, target, T.value([], [], 1.0)).completion_reason == T.Reason.FAILED, "reject shared mutable endpoints")
	check(_activated_add(scheduler, target, T.value(Quaternion(0, 0, 0, 0), Quaternion.IDENTITY)).completion_reason == T.Reason.FAILED, "reject zero quaternion")
	check(_activated_add(scheduler, target, T.property(^"missing", 1.0)).completion_reason == T.Reason.FAILED, "reject missing property")
	check(scheduler.active_count == 0, "invalid starts do not register work")
	var listing := DynamicProbe.new()
	listing.listed = true
	var dynamic := _activated_add(scheduler, listing, T.property(^"dynamic", 2.0, 1.0))
	check(dynamic.completion_reason != T.Reason.FAILED, "an instance's own property list admits its property")
	dynamic.cancel()
	check(_activated_add(scheduler, DynamicProbe.new(), T.property(^"dynamic", 2.0, 1.0)).completion_reason == T.Reason.FAILED,
		"another instance's property list does not admit a property")
	var valid := _activated_add(scheduler, target, T.value(0.0, 1.0, 1.0))
	scheduler.update(-0.1)
	near(valid.value, 0.0, "invalid update does not advance")
	scheduler.dispose()
	check(valid.completion_reason == T.Reason.RUNNER_DISPOSED, "dispose settles handles")
	check(_activated_add(scheduler, target, T.value(0.0, 1.0)).completion_reason == T.Reason.FAILED, "disposed scheduler rejects starts")
	return true

func _snapshots_and_fill() -> bool:
	if trace_runs: print("snapshots: nodes")
	var scheduler := TweensGdScheduler.new()
	var first := Node2D.new()
	var second := Node2D.new()
	add_child(first)
	add_child(second)
	first.position.x = 2.0
	second.position.x = 4.0
	var definition := T.property(^"position:x", 10.0, 1.0)
	if trace_runs: print("snapshots: add")
	var a := scheduler.add(first, definition)
	var b := scheduler.add(second, definition)
	definition.to_value = 100.0
	definition.duration = 100.0
	if trace_runs: print("snapshots: update")
	scheduler.update(0.5)
	near(first.position.x, 6.0, "first captures own initial value")
	near(second.position.x, 7.0, "second captures own initial value")
	a.cancel()
	b.cancel()
	if trace_runs: print("snapshots: fill")
	var updates: Array = []
	var fill := T.property(^"position:x", 20.0, 1.0)
	fill.delay = 0.5
	fill.from_value = 1.0
	fill.fill = T.Fill.APPLY_FROM_DURING_DELAY
	fill.on_update = func(_h, v): updates.append(v)
	var filled := scheduler.add(first, fill)
	near(first.position.x, 6.0, "factory defers fill")
	scheduler.update(0.0)
	near(first.position.x, 1.0, "delay fills at activation")
	scheduler.update(1.5)
	check(updates == [1.0, 1.0, 20.0, 6.0], "completion samples endpoint then restores captured value")
	check(filled.completion_reason == T.Reason.COMPLETED, "fill completes")
	if trace_runs: print("snapshots: curve")
	var curve := Curve.new()
	curve.add_point(Vector2(0.0, 0.0))
	curve.add_point(Vector2(1.0, 1.0))
	var curved := T.value(0.0, 1.0, 1.0)
	curved.curve = curve
	var c := scheduler.add(self, curved)
	var expected := curve.sample(0.5)
	curve.set_point_value(1, 0.0)
	scheduler.update(0.5)
	near(c.value, expected, "curve is duplicated per playback")
	var skew := T.value(0.0, 1.0, 1.0)
	skew.skew = 2.0
	var d := scheduler.add(self, skew)
	scheduler.update(0.5)
	near(d.value, 0.25, "skew precedes easing")
	scheduler.dispose()
	first.free()
	second.free()
	return true

func _leg_exponents() -> bool:
	for exponents in [[1.0, 1.0, 0.25, 0.25], [2.0, 2.0, 0.0625, 0.0625],
			[0.5, 0.5, 0.5, 0.5], [2.0, 1.0, 0.0625, 0.25],
			[1.0, 2.0, 0.25, 0.0625], [2.0, 0.5, 0.0625, 0.5]]:
		for easing in ["linear", "quad", "custom", "curve"]:
			var scheduler := TweensGdScheduler.new()
			var definition := T.value(0.0, 1.0, 1.0).with_ping_pong().with_skew(exponents[0]).with_weks(exponents[1])
			var curve := Curve.new()
			curve.add_point(Vector2.ZERO)
			curve.add_point(Vector2(0.5, 0.8))
			curve.add_point(Vector2.ONE)
			if easing == "quad": definition.ease = T.Ease.QUAD_OUT
			elif easing == "custom": definition.ease_function = func(t): return t + 1.0
			elif easing == "curve": definition.curve = curve
			var expected_curve := [curve.sample(exponents[2]), curve.sample(exponents[3])]
			var handle := scheduler.add(self, definition)
			definition.skew = 3.0
			definition.weks = 3.0
			curve.clear_points()
			for index in range(2):
				scheduler.update(0.25 if index == 0 else 1.5)
				var t: float = exponents[2 + index]
				var expected := t
				if easing == "quad": expected = 1.0 - (1.0 - t) * (1.0 - t)
				elif easing == "custom": expected = t + 1.0
				elif easing == "curve": expected = expected_curve[index]
				near(handle.progress, 0.25, "raw progress is unchanged")
				near(handle.value, expected, "leg exponent precedes " + easing)
			scheduler.update(0.25)
			near(handle.value, 1.0 if easing == "custom" else 0.0, "return endpoint")
			check(handle.completion_reason == T.Reason.COMPLETED, "ping-pong completes")
			scheduler.dispose()
	for relative in [false, true]:
		var scheduler := TweensGdScheduler.new()
		var definition := T.value(0.0, null if relative else 1.0, 1.0).with_ping_pong().with_skew(2.0).with_weks(0.5)
		if relative: definition.by_value = 1.0
		definition.delay = 0.5
		definition.offset = 0.25
		definition.ping_pong_interval = 0.5
		definition.repeat_interval = 0.5
		definition.repeats = 2
		var handle := scheduler.add(self, definition)
		for sample in [[0.25, 0.0], [0.25, 0.0625], [0.875, 1.0], [1.125, 0.5],
				[0.25, 0.0], [0.25, 0.0], [0.25, 0.0], [0.25, 0.0625],
				[2.0, 0.5], [3.0, 0.5], [0.25, 0.0]]:
			scheduler.update(sample[0])
			near(handle.value, sample[1], "leg selection through intervals, repeats and jumps")
		check(handle.completion_reason == T.Reason.COMPLETED, "repeats complete")
		scheduler.dispose()
	var scheduler := TweensGdScheduler.new()
	var forward := scheduler.add(self, T.value(0.0, 1.0, 1.0).with_repeats(2).with_skew(2.0).with_weks(0.5))
	scheduler.update(1.25)
	near(forward.value, 0.0625, "weks does not affect forward repeats")
	for ping_pong in [false, true]:
		var instant := scheduler.add(self, T.value(0.0, 1.0).with_ping_pong(ping_pong).with_skew(0.5).with_weks(2.0))
		scheduler.update(0.0)
		near(instant.value, 0.0 if ping_pong else 1.0, "zero-duration endpoint")
	scheduler.dispose()
	return true

func _factories_and_with() -> bool:
	var plain := T.position_2d()
	check(plain.skew == 1.0 and plain.weks == 1.0, "independent identity exponents")
	check(plain.to_value == null and plain.duration == 0.0 and plain.ease == T.Ease.LINEAR and plain.delay == 0.0,
		"helper defaults")
	for definition in [T.position_2d(Vector2(4, 2), 0.5, T.Ease.CUBIC_OUT, 0.25),
			T.property(^"position", Vector2(4, 2), 0.5, T.Ease.CUBIC_OUT, 0.25),
			T.shader_parameter(&"amount", Vector2(4, 2), 0.5, T.Ease.CUBIC_OUT, 0.25),
			T.instance_shader_parameter(&"amount", Vector2(4, 2), 0.5, T.Ease.CUBIC_OUT, 0.25)]:
		check(definition.to_value == Vector2(4, 2) and definition.duration == 0.5 and definition.ease == T.Ease.CUBIC_OUT
			and definition.delay == 0.25, "factory takes to, seconds, easing and delay")
	var counter := T.value(1.0, 2.0, 0.5, T.Ease.CUBIC_OUT, 0.25)
	check(counter.from_value == 1.0 and counter.initial_value == 1.0 and counter.ease == T.Ease.CUBIC_OUT
		and counter.delay == 0.25, "value factory takes easing and delay")

	var base := T.value(0.0, 1.0, 1.0)
	base.target_class = &"Kept"
	var curve := Curve.new()
	var callback := func(_h): pass
	for case in [["with_from", 2.0, "from_value"], ["with_to", 3.0, "to_value"], ["with_by", 5.0, "by_value"],
			["with_initial_value", 4.0, "initial_value"], ["with_duration", 2.0, "duration"],
			["with_factor_from", 0.5, "factor_from"], ["with_delta_from", 1.5, "delta_from"],
			["with_factor_to", 2.0, "factor_to"], ["with_delta_to", 2.5, "delta_to"],
			["with_factor_by", -1.0, "factor_by"], ["with_delta_by", 3.5, "delta_by"],
			["with_factor_duration", 3.0, "factor_duration"], ["with_delta_duration", 0.5, "delta_duration"],
			["with_factor_delay", 2.0, "factor_delay"], ["with_delta_delay", 0.25, "delta_delay"],
			["with_delay", 0.5, "delay"], ["with_offset", 0.25, "offset"], ["with_repeats", 3, "repeats"],
			["with_ping_pong", true, "use_ping_pong"], ["with_ping_pong_interval", 0.1, "ping_pong_interval"],
			["with_repeat_interval", 0.2, "repeat_interval"], ["with_fill", T.Fill.BOTH, "fill"],
			["with_ease", T.Ease.BACK_OUT, "ease"], ["with_skew", 2.0, "skew"],
			["with_weks", 0.5, "weks"],
			["with_ease_function", callback, "ease_function"], ["with_curve", curve, "curve"],
			["with_suppress_callbacks_when_target_invalid", true, "suppress_callbacks_when_target_invalid"],
			["with_on_add", callback, "on_add"], ["with_on_start", callback, "on_start"],
			["with_on_update", callback, "on_update"], ["with_on_end", callback, "on_end"],
			["with_on_cancel", callback, "on_cancel"], ["with_on_finally", callback, "on_finally"]]:
		var before: Variant = base.get(case[2])
		var changed: TweensGdDefinition = base.call(case[0], case[1])
		check(changed != base and changed.get(case[2]) == case[1] and changed.target_class == &"Kept",
			case[0] + " returns a changed copy")
		check(base.get(case[2]) == before, case[0] + " leaves the original unchanged")
	var chained := base.with_delay(0.5).with_to(3.0)
	check(chained.delay == 0.5 and chained.to_value == 3.0 and base.delay == 0.0 and base.to_value == 1.0,
		"with_ calls chain on copies")

	var scheduler := TweensGdScheduler.new()
	var first := Node2D.new()
	var second := Node2D.new()
	add_child(first)
	add_child(second)
	var move := T.position_2d_x(10.0, 1.0)
	scheduler.add(first, move)
	scheduler.add(second, move.with_duration(2.0))
	scheduler.update(0.5)
	near(first.position.x, 5.0, "shared definition keeps its duration")
	near(second.position.x, 2.5, "with_duration varies one start")
	scheduler.dispose()
	first.free()
	second.free()
	return true

func _array_endpoints() -> bool:
	# Arrays of numbers stand in for the captured vector or color, on every endpoint.
	var scheduler := TweensGdScheduler.new()
	var node := Node2D.new()
	add_child(node)
	var moved := scheduler.add(node, T.position_2d([400, 180], 1.0).with_delta_to([10, 0]))
	scheduler.update(1.0)
	check(node.position == Vector2(410, 180), "arrays are Vector2 endpoints and deltas: " + moved.error)
	scheduler.add(node, T.scale_2d([2, 2], 1.0).with_from([0.5, 0.5]))
	scheduler.update(0.5)
	check(node.scale == Vector2(1.25, 1.25), "arrays are from endpoints")
	scheduler.update(0.5)
	scheduler.add(node, T.scale_2d(null, 1.0).with_by([1, -0.5]))
	scheduler.update(1.0)
	check(node.scale == Vector2(3, 1.5), "arrays are by offsets")
	scheduler.add(node, T.property(^"modulate", [1, 0.5, 0], 1.0))
	scheduler.update(1.0)
	check(node.modulate == Color(1, 0.5, 0), "three components are an opaque color")
	scheduler.add(node, T.modulate([1, 0.5, 0, 0.25], 1.0))
	scheduler.update(1.0)
	check(node.modulate == Color(1, 0.5, 0, 0.25), "four components include alpha")
	var spatial := Node3D.new()
	add_child(spatial)
	scheduler.add(spatial, T.position_3d([1, 2.5, -3], 1.0))
	var holder := Holder.new()
	scheduler.add(holder, T.property(^"corners", [1, 2, 3, 4], 1.0))
	scheduler.update(1.0)
	check(spatial.position == Vector3(1, 2.5, -3), "arrays are Vector3 endpoints")
	check(holder.corners == Vector4(1, 2, 3, 4), "arrays are Vector4 endpoints")
	for rejected in [T.position_2d([1, 2, 3], 1.0), T.position_2d(["1", 2], 1.0), T.property(^"rotation", [1, 2], 1.0)]:
		check(_activated_add(scheduler, node, rejected).completion_reason == T.Reason.FAILED, "arrays must match the captured type")
	node.free()
	spatial.free()
	scheduler.dispose()
	return true

func _relative() -> bool:
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/endpoints.json"))
	for test in data.cases:
		var fixture := TweensGdScheduler.new()
		var probe := Holder.new()
		probe.amount = test.start
		var definition := T.property(^"amount", null)
		for key in ["from", "to", "by"]:
			if test.has(key): definition.set(key + "_value", float(test[key]))
		for key in test.options: definition.set(key, test.options[key])
		var started := fixture.add(probe, definition)
		check(not started.is_terminal, test.name + " starts: " + started.error)
		for sample in test.samples:
			# A "set" sample changes the property from outside the tween.
			if sample.has("set"): probe.amount = sample.set
			else: fixture.update(sample.delta)
			if sample.has("value"): near(probe.amount, sample.value, test.name)
		fixture.dispose()

	var scheduler := TweensGdScheduler.new()
	var holder := Holder.new()
	holder.amount = 1.0
	scheduler.add(holder, T.property(^"amount", null, 1.0).with_by(10.0))
	scheduler.add(holder, T.property(^"amount", null, 2.0).with_by(-4.0))
	scheduler.update(1.0)
	near(holder.amount, 9.0, "relative tweens on one property add up")
	scheduler.update(1.0)
	near(holder.amount, 7.0, "relative tweens end at the sum of their offsets")

	holder.count = 3
	scheduler.add(holder, T.property(^"count", null, 1.0).with_by(5).with_repeats(1))
	var counts: Array = []
	for step in range(4):
		scheduler.update(0.5)
		counts.append(holder.count)
	check(counts == [6, 8, 11, 13], "integer offsets round each step and accumulate: %s" % [counts])

	var start := Quaternion(Vector3.UP, PI / 2.0)
	var turn := Quaternion(Vector3.RIGHT, PI / 2.0)
	holder.turn = start
	scheduler.add(holder, T.property(^"turn", null, 1.0).with_by(turn))
	scheduler.update(0.5)
	check(holder.turn.is_equal_approx(start * Quaternion(Vector3.RIGHT, PI / 4.0)), "quaternion offsets rotate about local axes")
	scheduler.update(0.5)
	check(holder.turn.is_equal_approx(start * turn) and not holder.turn.is_equal_approx(turn * start), "quaternion offsets end at start * by")

	var samples: Array = []
	scheduler.add(self, T.float_value(null, 1.0).with_initial_value(2.0).with_by(10.0).with_on_update(func(_h, v): samples.append(v)))
	scheduler.update(0.5)
	scheduler.update(0.5)
	check(samples == [2.0, 7.0, 12.0], "callback values add to the captured start: %s" % [samples])

	holder.amount = 1.0
	scheduler.add(holder, T.custom(func(t): return t.amount, func(t, v): t.amount = v, null, 1.0).with_by(2.0))
	scheduler.update(0.5)
	holder.amount += 10.0
	scheduler.update(0.5)
	near(holder.amount, 13.0, "adapter tweens keep outside changes")

	var node := Node2D.new()
	add_child(node)
	node.position = Vector2(1, 2)
	scheduler.add(node, T.position_2d_x(null, 1.0).with_by(4.0))
	scheduler.add(node, T.position_2d(null, 1.0).with_by(Vector2(1, 1)))
	scheduler.update(1.0)
	check(node.position == Vector2(6, 3), "component and whole-vector offsets combine: %s" % [node.position])
	node.free()

	for rejected in [T.value(0.0, 1.0, 1.0).with_by(1.0), T.value(0.0, null, 1.0).with_by(Vector2.ONE),
			T.value(0.0, null, 1.0).with_by(NAN), T.value(Quaternion.IDENTITY, null, 1.0).with_by(Quaternion(0, 0, 0, 0)),
			T.custom(func(_t): return Transform2D.IDENTITY, func(_t, _v): pass, null, 1.0, Callable(), func(_v): return "")
				.with_by(Transform2D.IDENTITY)]:
		check(_activated_add(scheduler, holder, rejected).completion_reason == T.Reason.FAILED, "reject invalid by_value")
	scheduler.dispose()
	return true

func _adjustments() -> bool:
	var scheduler := TweensGdScheduler.new()
	var holder := Holder.new()
	var tilt := Quaternion(Vector3.RIGHT, PI / 2.0)
	holder.turn = Quaternion(Vector3.UP, PI / 2.0)
	scheduler.add(holder, T.property(^"turn", null, 1.0).with_factor_to(0.5).with_delta_to(tilt))
	scheduler.update(1.0)
	check(holder.turn.is_equal_approx(Quaternion(Vector3.UP, PI / 4.0) * tilt),
		"quaternion factors scale the angle and deltas rotate locally")

	holder.count = 3
	scheduler.add(holder, T.property(^"count", null, 1.0).with_factor_to(1.5))
	scheduler.update(1.0)
	check(holder.count == 5, "integer factors round away from zero: %s" % holder.count)

	# The offset may reach the adjusted duration, past the unadjusted one.
	holder.amount = 0.0
	var stretched := T.property(^"amount", 10.0, 1.0).with_factor_duration(2.0).with_delta_duration(1.0).with_offset(1.5)
	check(stretched.validate().is_empty(), "offset within the adjusted duration")
	scheduler.add(holder, stretched)
	scheduler.update(0.0)
	near(holder.amount, 5.0, "duration adjustments shape the timeline")

	for rejected in [T.value(0.0, 1.0, 1.0).with_factor_by(2.0), T.value(0.0, 1.0, 1.0).with_delta_by(1.0),
			T.value(0.0, null, 1.0).with_by(1.0).with_factor_to(2.0), T.value(0.0, null, 1.0).with_by(1.0).with_delta_to(1.0),
			T.value(0.0, 1.0, 1.0).with_factor_from(NAN), T.value(0.0, 1.0, 1.0).with_factor_to(INF),
			T.value(0.0, 1.0, 1.0).with_factor_duration(NAN), T.value(0.0, 1.0, 1.0).with_delta_duration(-2.0),
			T.value(0.0, 1.0, 1.0).with_factor_duration(0.5).with_offset(0.75),
			T.value(0.0, 1.0, 1.0).with_factor_delay(NAN),
			T.value(0.0, 1.0, 1.0).with_delta_to(Vector2.ONE), T.value(0.0, 1.0, 1.0).with_delta_from(NAN),
			T.custom(func(_t): return Transform2D.IDENTITY, func(_t, _v): pass, null, 1.0, Callable(), func(_v): return "")
				.with_factor_to(2.0)]:
		check(_activated_add(scheduler, holder, rejected).completion_reason == T.Reason.FAILED, "reject invalid factors and deltas")
	scheduler.dispose()
	return true

func _interpolation() -> bool:
	var scheduler := TweensGdScheduler.new()
	var target := RefCounted.new()
	for pair in [[0.0, 10.0, 5.0], [0, 3, 2], [Vector2.ZERO, Vector2(2, 4), Vector2(1, 2)],
			[Vector3.ZERO, Vector3(2, 4, 6), Vector3(1, 2, 3)], [Vector4.ZERO, Vector4(2, 4, 6, 8), Vector4(1, 2, 3, 4)],
			[Color(0, 0, 0, 0), Color.WHITE, Color(0.5, 0.5, 0.5, 0.5)],
			[Rect2(0, 0, 0, 0), Rect2(2, 4, 6, 8), Rect2(1, 2, 3, 4)]]:
		var h := scheduler.add(target, T.value(pair[0], pair[1], 1.0))
		scheduler.update(0.5)
		check(h.value == pair[2], "interpolate type %s: got %s, expected %s (state %s, error %s)" % [type_string(typeof(pair[0])), h.value, pair[2], h.state, h.error])
		h.cancel()
	var extreme := scheduler.add(target, T.value(-1.7e308, 1.7e308, 1.0))
	scheduler.update(0.0)
	check(extreme.value == -1.7e308, "endpoints near opposite double limits keep their start")
	scheduler.update(0.5)
	check(extreme.completion_reason != T.Reason.FAILED and extreme.value == 0.0, "endpoints near opposite double limits interpolate")
	extreme.cancel()
	var wide := scheduler.add(target, T.value(Vector2(-3.0e38, 1.0), Vector2(3.0e38, 1.0), 1.0))
	scheduler.update(0.5)
	check(wide.completion_reason != T.Reason.FAILED and wide.value == Vector2(0.0, 1.0), "vector components near opposite float limits interpolate")
	wide.cancel()
	var rotation := scheduler.add(target, T.value(Quaternion.IDENTITY, -Quaternion(Vector3.UP, PI / 2.0), 1.0))
	scheduler.update(0.5)
	check(rotation.value.is_equal_approx(Quaternion(Vector3.UP, PI / 4.0)), "quaternion uses shortest path")
	var integer := T.value(0, 9223372036854775807, 1.0)
	integer.ease_function = func(_t): return 2.0
	var saturated := scheduler.add(target, integer)
	scheduler.update(1.0)
	check(saturated.value == 9223372036854775807, "integer overshoot saturates without wrapping")
	var node := Node2D.new()
	add_child(node)
	scheduler.add(node, T.property(^"position:x", 10.0, 1.0))
	scheduler.add(node, T.property(^"position:y", 20.0, 1.0))
	scheduler.update(0.5)
	check(node.position == Vector2(5, 10), "component writes compose")
	scheduler.add(node, T.property(^"position:x", 100.0, 0.0))
	scheduler.update(0.0)
	near(node.position.x, 100.0, "insertion order determines last write")
	var material := StandardMaterial3D.new()
	var resource_tween := scheduler.add(material, T.property(^"roughness", 0.0, 1.0), node)
	scheduler.update(0.5)
	near(material.roughness, 0.5, "resource property writes")
	check(resource_tween != null, "resource tween uses separate owner")
	node.free()
	scheduler.dispose()
	return true

func _callbacks() -> bool:
	var scheduler := TweensGdScheduler.new()
	var events: Array[String] = []
	var definition := T.value(0.0, 1.0, 1.0)
	definition.on_add = func(_h): events.append("add")
	definition.on_start = func(_h): events.append("start")
	definition.on_update = func(_h, _v): events.append("update")
	definition.on_end = func(h):
		check(h.is_terminal, "terminal state visible in on_end")
		events.append("end")
		h.cancel()
	definition.on_finally = func(_h): events.append("finally")
	var handle := scheduler.add(self, definition)
	handle.ended.connect(func(_r): events.append("signal"))
	scheduler.update(2.0)
	handle.cancel()
	check(events == ["add", "start", "update", "update", "end", "finally", "signal"], "terminal ordering/idempotence")
	events.clear()
	var cancel := T.value(0.0, 1.0)
	cancel.on_add = func(h): h.cancel()
	cancel.on_cancel = func(_h): events.append("cancel")
	cancel.on_finally = func(_h): events.append("finally")
	var cancelled := scheduler.add(self, cancel)
	scheduler.update(0.0)
	check(cancelled.is_settled and events == ["cancel", "finally"], "cancel during on_add settles")
	var spawned: Array = []
	var parent := T.value(0.0, 1.0, 1.0)
	parent.on_update = func(h, _v):
		spawned.append(scheduler.add(self, T.value(0.0, 10.0, 1.0)))
		h.cancel()
		scheduler.update(99.0)
	scheduler.add(self, parent)
	scheduler.update(0.5)
	check(spawned.size() == 1 and spawned[0].value == null, "callback additions wait for next tick")
	check(scheduler.last_error.contains("Recursive"), "recursive updates rejected")
	scheduler.update(0.5)
	near(spawned[0].value, 5.0, "spawned tween gets next delta")
	var dispose := T.value(0.0, 1.0)
	dispose.on_update = func(_h, _v): scheduler.dispose()
	scheduler.add(self, dispose)
	scheduler.update(0.0)
	check(scheduler.active_count == 0, "dispose during update cleans up all work")
	return true

func _lifetime() -> bool:
	var scheduler := TweensGdScheduler.new()
	var node := Node2D.new()
	add_child(node)
	var h := scheduler.add(node, T.property(^"position:x", 10.0, 1.0))
	h.pause()
	remove_child(node)
	check(h.completion_reason == T.Reason.OWNER_EXITED and h.is_settled, "paused owner exit settles immediately")
	node.free()
	var queued := Node2D.new()
	add_child(queued)
	var events: Array = []
	var def := T.property(^"position:x", 10.0, 1.0)
	def.suppress_callbacks_when_target_invalid = true
	def.on_cancel = func(_h): events.append("cancel")
	def.on_finally = func(_h): events.append("finally")
	var dying := scheduler.add(queued, def)
	dying.pause()
	queued.queue_free()
	scheduler.update(0.5, 0.5, T.Process.PHYSICS)
	check(dying.completion_reason == T.Reason.TARGET_FREED, "queued target checked across lanes and pause")
	near(queued.position.x, 0.0, "queued target never written")
	check(events.is_empty(), "invalid-target callback suppression")
	var object := Object.new()
	var freed := scheduler.add(object, T.value(0.0, 1.0, 1.0))
	object.free()
	scheduler.update(0.0)
	check(freed.completion_reason == T.Reason.TARGET_FREED, "freed Object detected")
	var owner := Node.new()
	add_child(owner)
	var resource := Gradient.new()
	var bound := scheduler.add(resource, T.value(0.0, 1.0, 1.0), owner)
	owner.free()
	check(bound.completion_reason == T.Reason.OWNER_EXITED, "resource follows owner lifetime")
	var parent := Node.new()
	var other := Node.new()
	add_child(parent)
	add_child(other)
	scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 1.0).with_on_cancel(func(_h): parent.free()), parent)
	var unrelated := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 1.0), other)
	scheduler.cancel_owner(parent, true)
	check(not unrelated.is_terminal, "cancel_owner survives a callback that frees the owner")
	other.free()
	scheduler.dispose()
	return true

func _setter_reentrancy() -> bool:
	var scheduler := TweensGdScheduler.new()
	var target := PropertyProbe.new()
	add_child(target)
	var h := scheduler.add(target, T.property(^"amount", 10.0, 1.0))
	target.when_written = h.cancel
	scheduler.update(0.5)
	check(h.completion_reason == T.Reason.CANCELLED, "setter can cancel its own tween")
	near(h.value, 0.0, "start-boundary sample remains inspectable after setter cancellation")
	target.when_written = Callable()
	var exit := T.property(^"amount", 10.0, 1.0)
	exit.on_start = func(instance): instance.target.get_parent().remove_child(instance.target)
	var exited := scheduler.add(target, exit)
	scheduler.update(0.5)
	check(exited.completion_reason == T.Reason.OWNER_EXITED, "on_start exit prevents property write")
	near(target.amount, 0.0, "no write after on_start exit")
	add_child(target)
	target.when_read = func(): remove_child(target)
	var removed_during_read := scheduler.add(target, T.property(^"amount", 10.0, 1.0))
	scheduler.update(0.0)
	check(removed_during_read.completion_reason == T.Reason.OWNER_EXITED, "getter invalidation interrupts activation")
	target.when_read = Callable()
	target.free()
	var holder: Array = []
	var ease := T.value(0.0, 1.0, 1.0)
	ease.ease_function = func(t):
		holder[0].cancel()
		return t
	holder.append(scheduler.add(self, ease))
	scheduler.update(0.5)
	check(holder[0].is_settled and holder[0].value == 0.0, "easing can cancel without later writes")
	holder.clear()
	var disposal := T.value(0.0, 1.0, 1.0)
	disposal.on_cancel = func(_h): scheduler.cancel_all()
	var first := scheduler.add(self, disposal)
	var second := scheduler.add(self, T.value(0.0, 1.0, 1.0))
	scheduler.dispose()
	check(first.is_settled and second.is_settled, "reentrant cancel_all during disposal settles all handles")
	return true

func _pause_and_lanes() -> bool:
	var scheduler := TweensGdScheduler.new()
	var node := Node.new()
	add_child(node)
	node.process_mode = Node.PROCESS_MODE_DISABLED
	var bound := scheduler.add(node, T.value(0.0, 1.0, 1.0))
	var tree_bound := scheduler.add(node, T.value(0.0, 1.0, 1.0), null, T.playback_options(T.Process.PROCESS, T.Pause.SCENE_TREE))
	var always := scheduler.add(node, T.value(0.0, 1.0, 1.0), null, T.playback_options(T.Process.PHYSICS, T.Pause.ALWAYS, true))
	scheduler.update(0.25, 0.5)
	check(bound.value == null, "bound pause defers activation")
	near(tree_bound.value, 0.25, "tree policy ignores owner process mode")
	check(always.value == null, "other lane defers activation")
	get_tree().paused = true
	scheduler.update(0.25, 0.5, T.Process.PHYSICS)
	near(always.value, 0.5, "always physics uses unscaled delta while paused")
	always.pause()
	scheduler.update(0.25, 0.5, T.Process.PHYSICS)
	near(always.value, 0.5, "instance pause wins")
	always.resume()
	scheduler.update(0.25)
	near(tree_bound.value, 0.25, "tree pause blocks scene-tree policy")
	get_tree().paused = false
	node.process_mode = Node.PROCESS_MODE_INHERIT
	node.set_process(false)
	scheduler.update(0.25)
	near(bound.value, 0.25, "set_process false does not disable bound tween")
	node.free()
	scheduler.dispose()
	return true

func _detected_faults() -> bool:
	var scheduler := TweensGdScheduler.new()
	var events: Array = []
	var def := T.value(0.0, 1.0, 1.0)
	def.ease_function = func(_t): return NAN
	def.on_finally = func(_h): events.append("finally")
	var faulty := scheduler.add(self, def)
	var healthy := scheduler.add(self, T.value(0.0, 1.0, 1.0))
	scheduler.update(1.0)
	check(faulty.state == T.State.FAULTED and faulty.completion_reason == T.Reason.FAILED, "invalid ease faults handle")
	check(faulty.is_settled and not faulty.error.is_empty() and events == ["finally"], "fault cleanup and diagnosis")
	check(healthy.completion_reason == T.Reason.COMPLETED, "fault does not stop other tweens")
	var callback_owner := Node.new()
	var stale := T.value(0.0, 1.0, 1.0)
	stale.on_start = callback_owner.set_process.bind(true).unbind(1)
	var h := scheduler.add(self, stale)
	callback_owner.free()
	scheduler.update(1.0)
	check(h.state == T.State.FAULTED and h.is_settled, "invalid Callable is detected")
	scheduler.dispose()
	return true

func _reference_cleanup() -> bool:
	var scheduler := TweensGdScheduler.new()
	var h := scheduler.add(self, T.value(0.0, 1.0, 1.0))
	var weak_handle: WeakRef = weakref(h)
	h = null
	scheduler.dispose()
	check(weak_handle.get_ref() == null, "scheduler releases handles on dispose")
	var weak_scheduler: WeakRef = weakref(scheduler)
	scheduler = null
	check(weak_scheduler.get_ref() == null, "no scheduler/handle ownership cycle")
	var retained_scheduler := TweensGdScheduler.new()
	var retained := retained_scheduler.add(self, T.value(0.0, 1.0))
	retained_scheduler.update(0.0)
	var weak_retained_scheduler: WeakRef = weakref(retained_scheduler)
	retained_scheduler = null
	check(weak_retained_scheduler.get_ref() == null and retained.is_settled, "finished handle does not retain scheduler")
	var observer_scheduler := TweensGdScheduler.new()
	var observed := observer_scheduler.add(self, T.value(0.0, 1.0))
	_capture_in_observer(observed)
	var weak_observed: WeakRef = weakref(observed)
	observer_scheduler.update(0.0)
	observed = null
	check(weak_observed.get_ref() == null, "completion releases observer closures capturing handle")
	observer_scheduler.dispose()
	return true

func _capture_in_observer(handle) -> void:
	handle.ended.connect(func(_reason): handle.cancel())

func _record_wait(handle) -> void:
	_wait_results.append(await handle.wait())

func _sequence(scheduler) -> void:
	var first = scheduler.add(self, T.value(0.0, 1.0, 0.25))
	_sequence_handles.append(first)
	await first.wait()
	_sequence_handles.append(scheduler.add(self, T.value(0.0, 1.0, 1.0)))

func _await_independent_roots() -> void:
	var scheduler := TweensGdScheduler.new()
	var h := scheduler.add(self, T.value(0.0, 1.0, 1.0))
	_record_wait(h)
	_record_wait(h)
	check(_wait_results.is_empty(), "await suspends before completion")
	scheduler.update(1.0)
	check(_wait_results == [T.Reason.COMPLETED, T.Reason.COMPLETED], "multiple waiters resume")
	check(await h.wait() == T.Reason.COMPLETED, "late await returns cached reason")
	_sequence(scheduler)
	scheduler.update(0.5)
	check(_sequence_handles.size() == 2, "await continuation starts next step")
	check(_sequence_handles[1].value == null, "continuation defers capture until next update")
	scheduler.update(0.0)
	near(_sequence_handles[1].value, 0.0, "continuation starts with no inherited time")
	var cancel := scheduler.add(self, T.value(0.0, 1.0, 1.0))
	_record_wait(cancel)
	cancel.cancel()
	check(_wait_results.back() == T.Reason.CANCELLED, "cancellation resumes waiters")
	scheduler.dispose()
	_sequence_handles.clear()

func _automatic_runner() -> void:
	var node := Node2D.new()
	add_child(node)
	var h := T.play(node, T.property(^"position:x", 10.0, 0.0))
	var group := T.group([h, T.play(node, T.property(^"position:y", 20.0, 0.0))])
	var runner = get_tree().get_meta(RUNNER_KEY)
	check(runner == TweensGdRunner.acquire(get_tree()), "one automatic runner per tree")
	check(not runner.is_inside_tree(), "runner attachment is deferred")
	await get_tree().process_frame
	await get_tree().process_frame
	check(runner.is_inside_tree() and runner.process_priority == 1000, "runner attached with correct priority")
	check(h.is_settled and node.position.x == 10.0, "automatic processing completes tween")
	check(await group.wait() == T.Reason.COMPLETED and node.position.y == 20.0, "automatic runner completes and awaits group")
	var child := Node2D.new()
	node.add_child(child)
	var a := T.play(node, T.property(^"position:x", 0.0, 10.0))
	var b := T.play(child, T.property(^"position:x", 10.0, 10.0))
	T.cancel_tweens(node, true)
	check(a.is_settled and b.is_settled, "cancel_tweens includes descendants")
	var disposed := T.play(node, T.property(^"position:x", 1.0, 10.0))
	runner.free()
	check(disposed.completion_reason == T.Reason.RUNNER_DISPOSED, "runner teardown settles work")
	check(not get_tree().has_meta(RUNNER_KEY), "runner removes tree metadata")
	# Disposal before deferred attachment must settle work and remove its metadata too.
	var pending := T.play(node, T.property(^"position:x", 1.0, 10.0))
	var pending_runner = get_tree().get_meta(RUNNER_KEY)
	pending_runner.free()
	check(pending.completion_reason == T.Reason.RUNNER_DISPOSED, "pending runner teardown settles work")
	check(not get_tree().has_meta(RUNNER_KEY), "pending runner removes metadata")
	await get_tree().process_frame
	# A queued runner can be replaced; deleting the old runner must keep the new one.
	var old := T.play(node, T.property(^"position:x", 2.0, 10.0))
	var old_runner = get_tree().get_meta(RUNNER_KEY)
	old_runner.queue_free()
	var replacement := T.play(node, T.property(^"position:x", 3.0, 10.0))
	var replacement_runner = get_tree().get_meta(RUNNER_KEY)
	check(old_runner != replacement_runner, "queued runner is replaced")
	await get_tree().process_frame
	await get_tree().process_frame
	check(old.is_settled and not replacement.is_settled, "replacement outlives old runner")
	check(get_tree().get_meta(RUNNER_KEY) == replacement_runner, "old runner preserves replacement metadata")
	replacement_runner.free()
	node.free()

func _expected_rejection(start: Callable, message: String) -> TweensGdHandle:
	# Keep unrelated errors visible; only consume the expected diagnostic for this call.
	failures.append_array(_collector.take_errors())
	var handle: TweensGdHandle = start.call()
	if handle != null and not handle.is_settled:
		TweensGdRunner.find(get_tree()).scheduler.update(0.0)
	var errors := _collector.take_errors()
	check(handle != null, message + " returns a handle")
	check(errors.size() == 1, message + " reports exactly one error")
	if handle != null and errors.size() == 1:
		check(errors[0].contains(handle.error), message + " preserves diagnostic details")
	return handle

func _rejected_starts() -> void:
	var definition := T.value(0.0, 1.0, -1.0)
	var callbacks: Array = []
	definition.on_add = func(_h): callbacks.append("add")
	definition.on_finally = func(_h): callbacks.append("finally")
	var scheduler := TweensGdScheduler.new()
	var diagnostics: Array[String] = []
	scheduler.error_reported.connect(func(message): diagnostics.append(message))
	var rejected := scheduler.add(self, definition)
	check(rejected.is_terminal and rejected.is_settled, "rejected start settles without a tick")
	check(rejected.state == T.State.FAULTED and rejected.completion_reason == T.Reason.FAILED, "rejection is a failure, not successful playback")
	check(diagnostics == [rejected.error] and scheduler.last_error == rejected.error, "manual rejection retains scheduler diagnostics")
	check(callbacks.is_empty() and scheduler.active_count == 0, "rejection runs no definition callbacks and registers no work")
	check(rejected.target == null and rejected.value == null and rejected.progress == 0.0, "dummy inspection is safe and retains no target")
	var after_signal: Array = []
	rejected.ended.connect(func(reason): after_signal.append(reason))
	rejected.pause()
	rejected.resume()
	rejected.cancel()
	check(await rejected.wait() == T.Reason.FAILED, "await rejected handle completes immediately")
	check(await rejected.wait() == T.Reason.FAILED, "repeated waits return cached failure")
	scheduler.update(1.0)
	check(after_signal.is_empty() and callbacks.is_empty(), "settled rejection cannot run callbacks or end a second time")
	scheduler.dispose()
	check(await scheduler.add(self, definition).wait() == T.Reason.FAILED, "disposed scheduler still returns an awaitable handle")
	var getter_scheduler := TweensGdScheduler.new()
	var owner := Node.new()
	add_child(owner)
	var resource := ResourceProbe.new()
	resource.when_read = func(): owner.free()
	var getter_failure := getter_scheduler.add(resource, T.property(^"amount", 1.0), owner)
	getter_scheduler.update(0.0)
	check(await getter_failure.wait() == T.Reason.OWNER_EXITED, "owner freed during activation interrupts the handle")
	getter_scheduler.dispose()
	var detached := Node2D.new()
	var starts: Array[Callable] = [
		func(): return T.play(null, definition),
		func(): return T.play(42, definition),
		func():
			var dead := Node2D.new()
			dead.free()
			return T.play(dead, definition),
		func():
			var dead_owner := Node.new()
			dead_owner.free()
			return T.play(RefCounted.new(), definition, dead_owner),
		func(): return T.play(detached, definition),
		func(): return T.play(RefCounted.new(), definition),
		func(): return T.play(self, definition),
		func(): return T.play(self, null),
		func(): return T.play(self, T.property(^"missing", 1.0)),
		func(): return T.play(self, T.value(0.0, 1.0), get_tree().root),
	]
	for index in range(starts.size()):
		var failed := _expected_rejection(starts[index], "automatic rejection %d" % index)
		check(failed.is_settled and await failed.wait() == T.Reason.FAILED, "automatic rejection is safely awaitable without a tick")
	check(callbacks.is_empty(), "automatic rejections run no definition callbacks")
	detached.free()
	var runner = get_tree().get_meta(RUNNER_KEY)
	check(runner.scheduler.active_count == 0, "automatic rejection schedules no work")
	runner.free()
	get_tree().set_meta(CLOSING_KEY, true)
	var closing := _expected_rejection(func(): return T.play(self, definition), "closing tree")
	get_tree().remove_meta(CLOSING_KEY)
	check(await closing.wait() == T.Reason.FAILED, "closing tree needs no future tick to settle rejection")
	check(not get_tree().has_meta(RUNNER_KEY), "closing tree rejection creates no runner")
	failures.append_array(_collector.take_errors())
	check(TweensGdRunner.acquire(null) == null, "acquire rejects a missing tree")
	check(_collector.take_errors().size() == 1, "acquire reports a missing tree once")
	# The API rejects worker-thread use before touching scene state, but still returns a handle.
	if OS.has_feature("web"): return # The export deliberately disables thread support.
	var worker := Thread.new()
	var from_worker := _expected_rejection(func():
		worker.start(func(): return T.play(null, null))
		return worker.wait_to_finish(), "worker thread")
	check(await from_worker.wait() == T.Reason.FAILED, "worker-thread rejection is inspectable on main thread")
