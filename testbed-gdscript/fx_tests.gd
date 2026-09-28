# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")

class Target extends RefCounted:
	var amount := 10.0
	var position := Vector2(4, 5)
	var rotation := Quaternion.IDENTITY

func run(host: Node) -> bool:
	host.near(T.FX.punch(2, 2).call(0.125), 1.53125, "FX punch sample")
	host.near(T.FX.punch().call(0), 0, "FX punch starts at rest")
	host.near(T.FX.punch(2.3, 1, 2, 0.25).call(1), 0, "FX punch ends at rest for any frequency")
	host.near(T.FX.punch(6, 1, 2, 0.25).call(0), 1, "FX phase can give immediate displacement")
	host.near(T.FX.punch(6, 1, 2, 0.25, 0.1).call(0), 0, "FX attack removes initial displacement")
	host.near(T.FX.shake().call(0), 0, "FX shake starts at rest by default")
	host.near(T.FX.shake(0, 3, 0, 0, 2, 0).call(0), -3, "FX immediate shake envelope")
	host.near(T.FX.shake().call(1), 0, "FX shake ends at rest")
	host.near(T.FX.breathe().call(0.5), 1, "FX breathe peak")
	host.near(T.FX.breathe().call(1), 0, "FX breathe full cycle")
	host.near(T.FX.breathe(0.5).call(1), 1, "FX breathe preserves nonintegral cycles")
	var envelope := T.FX.attack_release(0.2)
	host.near(envelope.call(0), 0, "FX attack starts at zero")
	host.near(envelope.call(0.2), 1, "FX attack duration")
	host.near(envelope.call(0.1), 0.5, "FX attack midpoint")
	host.near(envelope.call(0.6), 0.25, "FX release has independent duration")
	host.near(envelope.call(1), 0, "FX envelope ends at zero")
	host.check(absf(envelope.call(0.1999) - envelope.call(0.2001)) < 0.00001, "FX continuous envelope join")
	host.near(T.FX.decay().call(0.25), 0.5625, "FX decay")
	host.near(T.FX.decay().call(-1), 1, "FX clamps early progress")
	host.near(T.FX.decay().call(2), 0, "FX clamps late progress")
	var noise := T.FX.shake(8, 3, -17, -2.5, 2, 0)
	var expected: float = noise.call(0.375)
	for i in range(100, -1, -1): host.check(absf(noise.call(float(i) / 100)) <= 3, "FX bounded noise")
	host.near(noise.call(0.375), expected, "FX sample order independent")
	host.near(T.FX.shake(8, 3, -17, -2.5, 2, 0).call(0.375), expected, "FX reproducible seed")
	host.check(T.FX.shake(8, 3, 18, -2.5, 2, 0).call(0.375) != expected, "FX seed varies noise")
	host.check(T.FX.shake(8, 3, -17, -2.25, 2, 0).call(0.375) != expected, "FX offset varies noise")
	host.check(absf(noise.call(0.375 - 0.00001) - noise.call(0.375 + 0.00001)) < 0.001, "FX continuous lattice boundary")
	var a := Vector3(2, -3, 0)
	var f := Vector3(2, 5, 8)
	var p := Vector3(0, 0.25, 0.5)
	var o := Vector3(-2.5, 7.25, 11)
	var punch: Vector3 = T.FX.punch_3d(a, f, 2, p).call(0.125)
	var shake: Vector3 = T.FX.shake_3d(a, f, -17, o).call(0.125)
	var breathe: Vector3 = T.FX.breathe_3d(a, f, p).call(0.125)
	host.near(punch.y, T.FX.punch(5, -3, 2, 0.25).call(0.125), "FX axis phase/frequency/amplitude")
	host.near(shake.y, T.FX.shake(5, -3, -17, 7.25).call(0.125), "FX axis noise offset")
	host.near(breathe.y, T.FX.breathe(5, -3, 0.25).call(0.125), "FX axis breathe")
	for v in [punch, shake, breathe]: host.near(v.z, 0, "FX zero amplitude locks axis")
	host.check(T.FX.punch_2d(Vector2(2, -3), Vector2(2, 5), 2, Vector2(0, 0.25)).call(0.125) == Vector2(punch.x, punch.y), "FX punch 2D")
	host.check(T.FX.shake_2d(Vector2(2, -3), Vector2(2, 5), -17, Vector2(-2.5, 7.25)).call(0.125) == Vector2(shake.x, shake.y), "FX shake 2D")
	host.check(T.FX.breathe_2d(Vector2(2, -3), Vector2(2, 5), Vector2(0, 0.25)).call(0.125) == Vector2(breathe.x, breathe.y), "FX breathe 2D")
	var independent: Vector3 = T.FX.shake_3d(Vector3.ONE).call(0.37)
	host.check(independent.x != independent.y and independent.y != independent.z, "FX default independent axes")
	for sample in [T.FX.punch_quaternion(a), T.FX.shake_quaternion(a), T.FX.breathe_quaternion(a)]:
		host.check(sample.call(0).is_equal_approx(Quaternion.IDENTITY), "FX quaternion starts at identity")
		host.check(sample.call(1).is_equal_approx(Quaternion.IDENTITY), "FX quaternion ends at identity")
		for i in range(101): host.check(sample.call(float(i) / 100).is_normalized(), "FX unit quaternion")
	host.check(T.FX.breathe_quaternion(Vector3(0, 0.6, 0)).call(0.5).is_equal_approx(Quaternion(Vector3.UP, 0.6)), "FX angular units are radians")
	host.check(T.FX.shake_quaternion(Vector3.ZERO).call(0.3) == Quaternion.IDENTITY, "FX zero rotation vector")
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/fx.json"))
	for row in data.shake:
		var sample := T.FX.shake(row.frequency, row.amplitude, row.seed, row.offset, row.decay, row.attack)
		host.check(absf(sample.call(row.t) - row.value) < 0.00001, "FX cross-language noise fixture")
	_runtime(host)
	# Keep expected factory errors out of the suite's script-error collector.
	host.failures.append_array(host._collector.take_errors())
	var invalid := [T.FX.punch(-1), T.FX.shake(12, NAN), T.FX.shake(12, 1, 0, INF),
		T.FX.breathe(1, 1, NAN), T.FX.attack_release(1), T.FX.decay(0), T.FX.shake(12, 1, 2147483648)]
	for sample in invalid: host.check(sample.is_null(), "FX invalid factory returns empty callable")
	host.check(host._collector.take_errors().size() == invalid.size(), "FX invalid factories report errors")
	host.check(is_nan(T.FX.punch().call(NAN)), "FX nonfinite progress remains detectable")
	return true

func _runtime(host: Node) -> void:
	var scheduler := TweensGdScheduler.new()
	var target := Target.new()
	scheduler.add(target, T.property(^"amount", null, 1).with_by(2.0).with_ease_function(T.FX.punch(2)))
	var sample := T.FX.shake_2d(Vector2(8, 4))
	var baseline := target.position
	var clock := T.value(0.0, 1.0, 1)
	clock.on_update = func(_handle, t): target.position = baseline + sample.call(t)
	scheduler.add(target, clock)
	var turn := T.FX.punch_quaternion(Vector3(0, 0.3, 0))
	scheduler.add(target, T.custom(func(obj): return obj.rotation, func(obj, value): obj.rotation = value,
		null, 1, func(from, _to, t): return from * turn.call(t)))
	scheduler.update(0.125)
	host.near(target.amount, 11.53125, "FX scalar works as easing")
	host.check(target.position == baseline + sample.call(0.125), "FX vector works through callback")
	host.check(target.rotation.is_equal_approx(turn.call(0.125)), "FX quaternion works through custom interpolator")
	scheduler.update(1)
	host.near(target.amount, 10, "FX scalar returns to baseline")
	host.check(target.position == baseline, "FX vector returns to baseline")
	host.check(target.rotation == Quaternion.IDENTITY, "FX quaternion returns to baseline")
	scheduler.dispose()
