# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
const Suite = preload("res://tests.gd")

func run(suite: Suite) -> bool:
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/keyframes.json"))
	for test: Dictionary in data.cases:
		var values: Array = test.values
		var stops: Array = test.stops
		var mode: int = test.mode
		var modes: Array = test.get("modes", [])
		var eases: Array = test.get("eases", [])
		var curve := TweensGdKeyframeCurve.create(values, PackedFloat64Array(stops), mode, PackedInt64Array(modes), PackedInt64Array(eases))
		suite.check(curve.error.is_empty(), "shared curve validates: " + str(test.name))
		if test.has("initial"): curve = curve.capture_start(test.initial)
		for sample: Array in test.samples:
			var at: float = sample[0]
			var actual: float = curve.sample(at)
			var expected: float = sample[1]
			var scale: float = test.get("sample_scale", 1.0)
			suite.check(absf(actual / scale - expected / scale) <= 0.00001, "shared curve: %s at %s got %s expected %s" % [test.name, at, actual, expected])
	for test: Dictionary in data.colors:
		var from := _color(test.from)
		var to := _color(test.to)
		var space: int = test.space
		var alpha: int = test.alpha
		var encoding: int = test.encoding
		var at: float = test.at
		var curve := TweensGdKeyframeCurve.create([from, to], PackedFloat64Array([0, 100]), 1,
			PackedInt64Array(), PackedInt64Array(), space, alpha, encoding)
		var actual: Color = curve.sample(at)
		var expected := _color(test.expected)
		suite.check(_close(actual, expected), "shared color curve: " + str(test.name))
		var ordinary: Color = TweensGdInterpolation.interpolate_color(from, to, at, space, alpha, encoding)
		suite.check(_close(ordinary, expected), "shared color tween: " + str(test.name))
	_playback(suite)
	_validation(suite)
	_numeric_validation(suite)
	var hidden := Color(1, 0, 0, 0)
	var fractional := TweensGdKeyframeCurve.create([Color.WHITE, hidden, Color.BLUE], PackedFloat64Array([0, 0.23, 100]))
	var exact: Color = fractional.sample(0.23 / 100.0)
	suite.check(exact == hidden, "fractional color keys preserve hidden RGB exactly")
	var turn := TweensGdKeyframeCurve.create([Quaternion.IDENTITY, Quaternion(Vector3.UP, 1.0)], PackedFloat64Array([0, 100]), 1,
		PackedInt64Array([-1, 3]), PackedInt64Array([0, T.Ease.QUAD_IN]))
	for at: float in [-0.1, 0.0, 0.5, 1.0, 1.1]:
		var actual: Quaternion = turn.sample(at)
		var weight := _quad_weight(at)
		suite.check(absf(actual.dot(Quaternion(Vector3.UP, weight))) > 0.99999, "quaternion ease and endpoint tangent")
	return true

static func _quad_weight(at: float) -> float:
	# Finite differences for t² at the endpoints: h at zero, 2-h at one.
	const H = 0.0001
	if at < 0: return at * H
	if at > 1: return 1.0 + (at - 1.0) * (2.0 - H)
	return at * at

func _numeric_validation(suite: Suite) -> void:
	for weight: float in [0.0, 0.5, 1.0]:
		for policy: Array in [[-1, 0, 0], [3, 0, 0], [0, -1, 0], [0, 2, 0], [0, 0, -1], [0, 0, 2]]:
			var space: int = policy[0]
			var alpha: int = policy[1]
			var encoding: int = policy[2]
			suite.check(TweensGdInterpolation.interpolate_color(Color.RED, Color.BLUE, weight, space, alpha, encoding) == null,
				"color helper rejects unknown policies before endpoint shortcuts")
		for invalid: Color in [Color(NAN, 0, 0, 1), Color(0, INF, 0, 1), Color(0, 0, NAN, 1), Color(0, 0, 0, INF)]:
			suite.check(TweensGdInterpolation.interpolate_color(invalid, Color.BLUE, weight) == null, "color helper rejects invalid from")
			suite.check(TweensGdInterpolation.interpolate_color(Color.RED, invalid, weight) == null, "color helper rejects invalid to")
	for weight: float in [NAN, INF, -INF]:
		suite.check(TweensGdInterpolation.interpolate_color(Color.RED, Color.BLUE, weight) == null, "color helper rejects nonfinite weights")
	suite.check(TweensGdInterpolation.interpolate_color(Color.RED, Color.BLUE, 1e308, 1, 1) == null,
		"color helper rejects decoded overflow")
	for rotation: Quaternion in [Quaternion(1e30, 0, 0, 1), Quaternion(1e-30, 0, 0, 0)]:
		var invalid := TweensGdKeyframeCurve.create([rotation, Quaternion.IDENTITY], PackedFloat64Array([0, 100]))
		suite.check(not invalid.error.is_empty(), "quaternion curves reject nonfinite or zero squared lengths")
	var huge_color := TweensGdKeyframeCurve.create([Color(1e30, 1e30, 1e30), Color.BLUE], PackedFloat64Array([0, 100]))
	suite.check(not huge_color.error.is_empty(), "color curves reject overflow during working-space conversion")
	var scheduler := TweensGdScheduler.new()
	var target := Node.new()
	suite.add_child(target)
	target.set_meta("amount", 3000000000)
	var animation := T.keyframes({100: {"metadata/amount": 5000000000}}, 1.0, T.Ease.LINEAR, T.Interpolation.LINEAR)
	var group := animation.play_on(scheduler, target)
	scheduler.update(0.5)
	var amount: int = target.get_meta("amount")
	suite.check(amount == 4000000000, "Int64 channels capture and sample beyond Int32 range")
	scheduler.update(0.5)
	amount = target.get_meta("amount")
	suite.check(amount == 5000000000 and group.completion_reason == T.Reason.COMPLETED, "Int64 batch completes")
	var limits := TweensGdKeyframeCurve.create([-9223372036854775807 - 1, 9223372036854775807], PackedFloat64Array([0, 100]), 1)
	var low: int = limits.sample(0)
	var high: int = limits.sample(1)
	suite.check(low == -9223372036854775807 - 1 and high == 9223372036854775807, "Int64 keys stay exact")
	low = limits.sample(-0.1)
	high = limits.sample(1.1)
	suite.check(low == -9223372036854775807 - 1 and high == 9223372036854775807, "Int64 overshoot saturates")
	target.free()

static func _color(value: Variant) -> Color:
	var components: Array = value
	var r: float = components[0]
	var g: float = components[1]
	var b: float = components[2]
	var a: float = components[3]
	return Color(r, g, b, a)

static func _close(a: Color, b: Color) -> bool:
	return absf(a.r - b.r) + absf(a.g - b.g) + absf(a.b - b.b) + absf(a.a - b.a) < 0.00001

func _playback(suite: Suite) -> void:
	var scheduler := TweensGdScheduler.new()
	var a := Node2D.new()
	var b := Node2D.new()
	suite.add_child(a)
	suite.add_child(b)
	var definition := T.keyframes({50: {"x": 10}, 100: {"x": 20}}, 1.0, T.Ease.LINEAR, T.Interpolation.LINEAR)
	var first := definition.play_on(scheduler, a)
	var second := definition.play_on(scheduler, b)
	a.position = Vector2(2, 3)
	b.position = Vector2(6, 7)
	scheduler.update(0.25)
	suite.check(a.position == Vector2(6, 3) and b.position == Vector2(8, 7), "sparse captures are independent and happen on activation")
	first.pause()
	scheduler.update(0.25)
	suite.check(a.position.x == 6 and b.position.x == 10, "batch pause is independent")
	first.resume()
	scheduler.update(0.75)
	suite.check(first.is_terminal() and second.is_terminal() and a.position.x == 20, "reused batches finish")
	var arrays := T.keyframes({"position": [Vector2.ZERO, Vector2(10, 20)], "scale": [1, 2], "modulate": [Color.RED, Color(0, 0, 0, 0)]})
	arrays.options.ping_pong = true
	arrays.options.delay = 0.5
	var group := arrays.play_on(scheduler, a)
	scheduler.update(1.0)
	suite.check(a.position == Vector2(5, 10) and a.scale == Vector2(1.5, 1.5), "array channels share timing")
	suite.check(_close(a.modulate, Color(1, 0, 0, 0.5)), "keyframes default to premultiplied OKLab")
	scheduler.update(1.5)
	suite.check(a.position == Vector2.ZERO and group.is_terminal(), "ping pong returns to first key")
	var again := definition.play_on(scheduler, a)
	again.cancel()
	suite.check(again.is_terminal(), "batch cancellation settles all channels")
	a.free()
	b.free()

func _validation(suite: Suite) -> void:
	var scheduler := TweensGdScheduler.new()
	var target := Node2D.new()
	suite.add_child(target)
	for keys: Dictionary in [{}, {"x": [1]}, {"x": [0, NAN]}, {"x": [0, 1], "position": [Vector2.ZERO, Vector2.ONE]},
			{"rotation": [0, 1], "rotation_degrees": [0, 90]}, {0: {"x": 0, "interpolation": 1}},
			{-1: {"x": 1}}, {101: {"x": 1}}, {50: {"x": 1, "position:x": 2}}]:
		suite.check(not T.keyframes(keys).validate().is_empty(), "invalid definition rejected: " + str(keys))
	for path: String in ["missing", "position:z", "material:resource_name", "position:"]:
		var invalid := T.keyframes({"x": [0, 10], path: [0, 1]})
		var rejected := invalid.play_on(scheduler, target)
		suite.check(rejected.is_terminal() and rejected.completion_reason == T.Reason.FAILED, "invalid binding rejects entire batch: " + path)
		suite.check(scheduler.active_count == 0 and target.position == Vector2.ZERO, "invalid batch starts nothing")
	var endpoints := T.keyframes({"x": [0, 1]})
	endpoints.options.to_value = 2
	suite.check(not endpoints.validate().is_empty(), "keyframes reject external endpoints")
	suite.check(T.keyframes({"x": [0, 1]}).play_on(null, target).is_terminal(), "missing scheduler is rejected")
	target.free()
