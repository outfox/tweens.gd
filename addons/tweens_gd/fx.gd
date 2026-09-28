# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted
## Deterministic progress-to-offset factories. Scalars work as ease_function; vectors and
## quaternions are samplers for on_update/custom interpolators. Frequency is per tween.
## Invalid configuration reports an error and returns an empty Callable.

static func punch(frequency: float = 6.0, amplitude: float = 1.0, decay: float = 2.0,
		phase: float = 0.0, attack: float = 0.0) -> Callable:
	if not _validate(frequency, amplitude, phase): return Callable()
	var envelope := attack_release(attack, decay)
	if envelope.is_null(): return Callable()
	return func(progress: float) -> float:
		var t := _time(progress)
		return 0.0 if t == 1.0 else amplitude * sin(TAU * (frequency * t + phase)) * envelope.call(t)

## Quintic-interpolated seeded value noise. Offset is a noise coordinate, not a time delay.
static func shake(frequency: float = 12.0, amplitude: float = 1.0, seed: int = 0,
		offset: float = 0.0, decay: float = 2.0, attack: float = 0.1) -> Callable:
	if not _validate(frequency, amplitude, offset): return Callable()
	if seed < -2147483648 or seed > 2147483647:
		push_error("tweens.gd FX: seed must fit a signed 32-bit integer.")
		return Callable()
	var envelope := attack_release(attack, decay)
	if envelope.is_null(): return Callable()
	return func(progress: float) -> float:
		var t := _time(progress)
		return 0.0 if t == 1.0 else amplitude * _noise(offset + frequency * t, seed) * envelope.call(t)

## Raised cosine from zero to amplitude and back. Phase is in cycles. Noninteger frequencies
## or phases need not end at zero; use an integral frequency and zero phase for seamless repeats.
static func breathe(frequency: float = 1.0, amplitude: float = 1.0, phase: float = 0.0) -> Callable:
	if not _validate(frequency, amplitude, phase): return Callable()
	return func(progress: float) -> float:
		return amplitude * (0.5 - 0.5 * cos(TAU * (frequency * _time(progress) + phase)))

static func decay(power: float = 2.0) -> Callable:
	return attack_release(0.0, power)

## Attack takes this fraction of the duration, release the rest. Zero attack starts at one.
## Positive attack uses quintic ramps; decay must be positive and attack must be in [0, 1).
static func attack_release(attack: float = 0.1, power: float = 2.0) -> Callable:
	if not is_finite(attack) or attack < 0.0 or attack >= 1.0 or not is_finite(power) or power <= 0.0:
		push_error("tweens.gd FX: attack must be in [0, 1) and decay must be finite and positive.")
		return Callable()
	return func(progress: float) -> float:
		var t := _time(progress)
		if t == 1.0: return 0.0
		if attack == 0.0: return pow(1.0 - t, power)
		if t <= attack: return _smooth(t / attack)
		return pow(maxf(0.0, 1.0 - _smooth((t - attack) / (1.0 - attack))), power)

static func punch_2d(amplitude: Vector2, frequency := Vector2(6, 6), decay: float = 2.0,
		phase := Vector2.ZERO, attack: float = 0.0) -> Callable:
	return _combine_2d(punch(frequency.x, amplitude.x, decay, phase.x, attack),
		punch(frequency.y, amplitude.y, decay, phase.y, attack))

static func punch_3d(amplitude: Vector3, frequency := Vector3(6, 6, 6), decay: float = 2.0,
		phase := Vector3.ZERO, attack: float = 0.0) -> Callable:
	return _combine_3d(punch(frequency.x, amplitude.x, decay, phase.x, attack),
		punch(frequency.y, amplitude.y, decay, phase.y, attack), punch(frequency.z, amplitude.z, decay, phase.z, attack))

static func shake_2d(amplitude: Vector2, frequency := Vector2(12, 12), seed: int = 0,
		offset := Vector2(0, 101.37), decay: float = 2.0, attack: float = 0.1) -> Callable:
	return _combine_2d(shake(frequency.x, amplitude.x, seed, offset.x, decay, attack),
		shake(frequency.y, amplitude.y, seed, offset.y, decay, attack))

static func shake_3d(amplitude: Vector3, frequency := Vector3(12, 12, 12), seed: int = 0,
		offset := Vector3(0, 101.37, 203.71), decay: float = 2.0, attack: float = 0.1) -> Callable:
	return _combine_3d(shake(frequency.x, amplitude.x, seed, offset.x, decay, attack),
		shake(frequency.y, amplitude.y, seed, offset.y, decay, attack), shake(frequency.z, amplitude.z, seed, offset.z, decay, attack))

static func breathe_2d(amplitude: Vector2, frequency := Vector2.ONE, phase := Vector2.ZERO) -> Callable:
	return _combine_2d(breathe(frequency.x, amplitude.x, phase.x), breathe(frequency.y, amplitude.y, phase.y))

static func breathe_3d(amplitude: Vector3, frequency := Vector3.ONE, phase := Vector3.ZERO) -> Callable:
	return _combine_3d(breathe(frequency.x, amplitude.x, phase.x), breathe(frequency.y, amplitude.y, phase.y),
		breathe(frequency.z, amplitude.z, phase.z))

## Quaternion samplers turn rotation vectors (amplitude in radians) into unit rotation offsets.
## Apply in local space as baseline * sample.call(t); identity is the neutral offset.
static func punch_quaternion(amplitude: Vector3, frequency := Vector3(6, 6, 6), decay: float = 2.0,
		phase := Vector3.ZERO, attack: float = 0.0) -> Callable:
	return _rotation(punch_3d(amplitude, frequency, decay, phase, attack))

static func shake_quaternion(amplitude: Vector3, frequency := Vector3(12, 12, 12), seed: int = 0,
		offset := Vector3(0, 101.37, 203.71), decay: float = 2.0, attack: float = 0.1) -> Callable:
	return _rotation(shake_3d(amplitude, frequency, seed, offset, decay, attack))

static func breathe_quaternion(amplitude: Vector3, frequency := Vector3.ONE, phase := Vector3.ZERO) -> Callable:
	return _rotation(breathe_3d(amplitude, frequency, phase))

static func _combine_2d(x: Callable, y: Callable) -> Callable:
	if x.is_null() or y.is_null(): return Callable()
	return func(t: float) -> Vector2: return Vector2(x.call(t), y.call(t))

static func _combine_3d(x: Callable, y: Callable, z: Callable) -> Callable:
	if x.is_null() or y.is_null() or z.is_null(): return Callable()
	return func(t: float) -> Vector3: return Vector3(x.call(t), y.call(t), z.call(t))

static func _rotation(sample: Callable) -> Callable:
	if sample.is_null(): return Callable()
	return func(t: float) -> Quaternion:
		var v: Vector3 = sample.call(t)
		var angle := sqrt(float(v.x) * v.x + float(v.y) * v.y + float(v.z) * v.z)
		if angle == 0.0: return Quaternion.IDENTITY
		var scale := sin(angle * 0.5) / angle
		return Quaternion(v.x * scale, v.y * scale, v.z * scale, cos(angle * 0.5)).normalized()

static func _validate(frequency: float, amplitude: float, offset: float) -> bool:
	# Match C#'s finite float configuration range, including when these scalars become vectors.
	if not is_finite(frequency) or frequency < 0.0 or frequency > 3.4028234663852886e38 \
			or not is_finite(amplitude) or absf(amplitude) > 3.4028234663852886e38 \
			or not is_finite(offset) or absf(offset) > 3.4028234663852886e38:
		push_error("tweens.gd FX: frequency must be nonnegative; frequency, amplitude and offset must fit finite floats.")
		return false
	return true

static func _time(t: float) -> float:
	return clampf(t, 0.0, 1.0) if is_finite(t) else NAN

static func _smooth(t: float) -> float:
	return t * t * t * (t * (6.0 * t - 15.0) + 10.0)

static func _noise(position: float, seed: int) -> float:
	if not is_finite(position): return NAN
	var cell := floorf(position)
	var index := int(fmod(cell, 1048576.0))
	return lerpf(_lattice(index, seed), _lattice(index + 1, seed), _smooth(position - cell))

static func _lattice(index: int, seed: int) -> float:
	var h := ((index & 0xfffff) * 374761393 + (seed & 0xffffffff) * 668265263) & 0xffffffff
	h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff
	h = h ^ (h >> 16)
	return float(h) / 4294967295.0 * 2.0 - 1.0
