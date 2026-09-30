// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/callable.hpp>

namespace godot {

// Deterministic progress-to-offset factories. Scalars work as ease_function; vectors and quaternions are
// samplers for on_update/custom interpolators. Frequency is per tween.
// Invalid configuration reports an error and returns an empty Callable.
class TweensGdFX : public RefCounted {
	GDCLASS(TweensGdFX, RefCounted)

protected:
	static void _bind_methods();

public:
	static Callable punch(double p_frequency, double p_amplitude, double p_decay, double p_phase, double p_attack);
	// Quintic-interpolated seeded value noise. Offset is a noise coordinate, not a time delay.
	static Callable shake(double p_frequency, double p_amplitude, int64_t p_seed, double p_offset, double p_decay, double p_attack);
	// Raised cosine from zero to amplitude and back. Phase is in cycles.
	static Callable breathe(double p_frequency, double p_amplitude, double p_phase);
	static Callable decay(double p_power);
	// Attack takes this fraction of the duration, release the rest. Zero attack starts at one.
	static Callable attack_release(double p_attack, double p_decay);
	static Callable punch_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, double p_decay, const Vector2 &p_phase, double p_attack);
	static Callable punch_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, double p_decay, const Vector3 &p_phase, double p_attack);
	static Callable shake_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, int64_t p_seed, const Vector2 &p_offset, double p_decay,
			double p_attack);
	static Callable shake_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, int64_t p_seed, const Vector3 &p_offset, double p_decay,
			double p_attack);
	static Callable breathe_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, const Vector2 &p_phase);
	static Callable breathe_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, const Vector3 &p_phase);
	// Quaternion samplers turn rotation vectors (amplitude in radians) into unit rotation offsets.
	// Apply in local space as baseline * sample.call(t); identity is the neutral offset.
	static Callable punch_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, double p_decay, const Vector3 &p_phase,
			double p_attack);
	static Callable shake_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, int64_t p_seed, const Vector3 &p_offset,
			double p_decay, double p_attack);
	static Callable breathe_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, const Vector3 &p_phase);
};

} // namespace godot
