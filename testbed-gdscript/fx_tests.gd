# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
const Suite = preload("res://tests.gd")
var host: Suite

class Target extends RefCounted:
	var amount := 10.0
	var position := Vector2(4, 5)
	var rotation := Quaternion.IDENTITY

# FX Callables return Variants; these wrappers convert them for the suite's typed checks.
func check(condition: Variant, message: String) -> void:
	var passed: bool = condition
	host.check(passed, message)

func near(actual: Variant, expected: Variant, message: String) -> void:
	var actual_value: float = actual
	var expected_value: float = expected
	host.near(actual_value, expected_value, message)

func run(owner: Suite) -> bool:
	host = owner
	near(T.FX.punch(2, 2).call(0.125), 1.53125, "FX punch sample")
	near(T.FX.punch().call(0), 0, "FX punch starts at rest")
	near(T.FX.punch(2.3, 1, 2, 0.25).call(1), 0, "FX punch ends at rest for any frequency")
	near(T.FX.punch(6, 1, 2, 0.25).call(0), 1, "FX phase can give immediate displacement")
	near(T.FX.punch(6, 1, 2, 0.25, 0.1).call(0), 0, "FX attack removes initial displacement")
	near(T.FX.shake().call(0), 0, "FX shake starts at rest by default")
	near(T.FX.shake(0, 3, 0, 0, 2, 0).call(0), -3, "FX immediate shake envelope")
	near(T.FX.shake().call(1), 0, "FX shake ends at rest")
	near(T.FX.breathe().call(0.5), 1, "FX breathe peak")
	near(T.FX.breathe().call(1), 0, "FX breathe full cycle")
	near(T.FX.breathe(0.5).call(1), 1, "FX breathe preserves nonintegral cycles")
	var envelope := T.FX.attack_release(0.2)
	near(envelope.call(0), 0, "FX attack starts at zero")
	near(envelope.call(0.2), 1, "FX attack duration")
	near(envelope.call(0.1), 0.5, "FX attack midpoint")
	near(envelope.call(0.6), 0.25, "FX release has independent duration")
	near(envelope.call(1), 0, "FX envelope ends at zero")
	var before_join: float = envelope.call(0.1999)
	var after_join: float = envelope.call(0.2001)
	check(absf(before_join - after_join) < 0.00001, "FX continuous envelope join")
	near(T.FX.decay().call(0.25), 0.5625, "FX decay")
	near(T.FX.decay().call(-1), 1, "FX clamps early progress")
	near(T.FX.decay().call(2), 0, "FX clamps late progress")
	var noise := T.FX.shake(8, 3, -17, -2.5, 2, 0)
	var expected: float = noise.call(0.375)
	for i in range(100, -1, -1):
		var bounded: float = noise.call(float(i) / 100)
		check(absf(bounded) <= 3, "FX bounded noise")
	near(noise.call(0.375), expected, "FX sample order independent")
	near(T.FX.shake(8, 3, -17, -2.5, 2, 0).call(0.375), expected, "FX reproducible seed")
	check(T.FX.shake(8, 3, 18, -2.5, 2, 0).call(0.375) != expected, "FX seed varies noise")
	check(T.FX.shake(8, 3, -17, -2.25, 2, 0).call(0.375) != expected, "FX offset varies noise")
	var below: float = noise.call(0.375 - 0.00001)
	var above: float = noise.call(0.375 + 0.00001)
	check(absf(below - above) < 0.001, "FX continuous lattice boundary")
	var a := Vector3(2, -3, 0)
	var f := Vector3(2, 5, 8)
	var p := Vector3(0, 0.25, 0.5)
	var o := Vector3(-2.5, 7.25, 11)
	var punch: Vector3 = T.FX.punch_3d(a, f, 2, p).call(0.125)
	var shake: Vector3 = T.FX.shake_3d(a, f, -17, o).call(0.125)
	var breathe: Vector3 = T.FX.breathe_3d(a, f, p).call(0.125)
	near(punch.y, T.FX.punch(5, -3, 2, 0.25).call(0.125), "FX axis phase/frequency/amplitude")
	near(shake.y, T.FX.shake(5, -3, -17, 7.25).call(0.125), "FX axis noise offset")
	near(breathe.y, T.FX.breathe(5, -3, 0.25).call(0.125), "FX axis breathe")
	for v: Vector3 in [punch, shake, breathe]: near(v.z, 0, "FX zero amplitude locks axis")
	check(T.FX.punch_2d(Vector2(2, -3), Vector2(2, 5), 2, Vector2(0, 0.25)).call(0.125) == Vector2(punch.x, punch.y), "FX punch 2D")
	check(T.FX.shake_2d(Vector2(2, -3), Vector2(2, 5), -17, Vector2(-2.5, 7.25)).call(0.125) == Vector2(shake.x, shake.y), "FX shake 2D")
	check(T.FX.breathe_2d(Vector2(2, -3), Vector2(2, 5), Vector2(0, 0.25)).call(0.125) == Vector2(breathe.x, breathe.y), "FX breathe 2D")
	var independent: Vector3 = T.FX.shake_3d(Vector3.ONE).call(0.37)
	check(independent.x != independent.y and independent.y != independent.z, "FX default independent axes")
	for sample: Callable in [T.FX.punch_quaternion(a), T.FX.shake_quaternion(a), T.FX.breathe_quaternion(a)]:
		var start: Quaternion = sample.call(0)
		check(start.is_equal_approx(Quaternion.IDENTITY), "FX quaternion starts at identity")
		var finish: Quaternion = sample.call(1)
		check(finish.is_equal_approx(Quaternion.IDENTITY), "FX quaternion ends at identity")
		for i in range(101):
			var unit: Quaternion = sample.call(float(i) / 100)
			check(unit.is_normalized(), "FX unit quaternion")
	var radians: Quaternion = T.FX.breathe_quaternion(Vector3(0, 0.6, 0)).call(0.5)
	check(radians.is_equal_approx(Quaternion(Vector3.UP, 0.6)), "FX angular units are radians")
	check(T.FX.shake_quaternion(Vector3.ZERO).call(0.3) == Quaternion.IDENTITY, "FX zero rotation vector")
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/fx.json"))
	for row: Dictionary in data.shake:
		var frequency: float = row.frequency
		var amplitude: float = row.amplitude
		var noise_seed: int = row.seed
		var offset: float = row.offset
		var decay: float = row.decay
		var attack: float = row.attack
		var sample := T.FX.shake(frequency, amplitude, noise_seed, offset, decay, attack)
		var sampled: float = sample.call(row.t)
		var fixture: float = row.value
		check(absf(sampled - fixture) < 0.00001, "FX cross-language noise fixture")
	_runtime()
	# Keep expected factory errors out of the suite's script-error collector.
	host.failures.append_array(host._collector.take_errors())
	var invalid := [T.FX.punch(-1), T.FX.shake(12, NAN), T.FX.shake(12, 1, 0, INF),
		T.FX.breathe(1, 1, NAN), T.FX.attack_release(1), T.FX.decay(0), T.FX.shake(12, 1, 2147483648)]
	for sample: Callable in invalid: check(sample.is_null(), "FX invalid factory returns empty callable")
	check(host._collector.take_errors().size() == invalid.size(), "FX invalid factories report errors")
	var nonfinite: float = T.FX.punch().call(NAN)
	check(is_nan(nonfinite), "FX nonfinite progress remains detectable")
	return true

func _runtime() -> void:
	var scheduler := TweensGdScheduler.new()
	var target := Target.new()
	@warning_ignore("return_value_discarded")
	scheduler.add(target, T.property(^"amount").with_duration(1).with_by(2.0).with_ease_function(T.FX.punch(2)))
	var sample := T.FX.shake_2d(Vector2(8, 4))
	var baseline := target.position
	var clock := T.value(0.0, 1.0, 1)
	clock.on_update = func(_handle: TweensGdHandle, t: float) -> void: target.position = baseline + sample.call(t)
	@warning_ignore("return_value_discarded")
	scheduler.add(target, clock)
	var turn := T.FX.punch_quaternion(Vector3(0, 0.3, 0))
	@warning_ignore("return_value_discarded")
	scheduler.add(target, T.custom(func(obj: Target) -> Quaternion: return obj.rotation,
		func(obj: Target, value: Quaternion) -> void: obj.rotation = value,
		func(from: Quaternion, _to: Quaternion, t: float) -> Quaternion: return from * turn.call(t)).with_duration(1))
	scheduler.update(0.125)
	near(target.amount, 11.53125, "FX scalar works as easing")
	check(target.position == baseline + sample.call(0.125), "FX vector works through callback")
	var turned: Quaternion = turn.call(0.125)
	check(target.rotation.is_equal_approx(turned), "FX quaternion works through custom interpolator")
	scheduler.update(1)
	near(target.amount, 10, "FX scalar returns to baseline")
	check(target.position == baseline, "FX vector returns to baseline")
	check(target.rotation == Quaternion.IDENTITY, "FX quaternion returns to baseline")
	scheduler.dispose()
