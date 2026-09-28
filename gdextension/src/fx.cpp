// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "fx.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>
#include <godot_cpp/variant/callable_custom.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include <cmath>

namespace godot {

namespace {

// Matches C#'s finite float configuration range, including when these scalars become vectors.
constexpr double FLOAT_MAX = 3.4028234663852886e38;

double clamp_time(double p_progress) {
	return Math::is_finite(p_progress) ? CLAMP(p_progress, 0.0, 1.0) : Math::NaN;
}

double smooth(double t) {
	return t * t * t * (t * (6.0 * t - 15.0) + 10.0);
}

double lattice(int64_t p_index, int64_t p_seed) {
	int64_t h = ((p_index & 0xfffff) * 374761393 + (p_seed & 0xffffffff) * 668265263) & 0xffffffff;
	h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff;
	h = h ^ (h >> 16);
	return double(h) / 4294967295.0 * 2.0 - 1.0;
}

double noise(double p_position, int64_t p_seed) {
	if (!Math::is_finite(p_position)) {
		return Math::NaN;
	}
	const double cell = std::floor(p_position);
	const int64_t index = int64_t(std::fmod(cell, 1048576.0));
	const double from = lattice(index, p_seed);
	const double to = lattice(index + 1, p_seed);
	return from + (to - from) * smooth(p_position - cell);
}

struct Envelope {
	double attack = 0.0;
	double power = 2.0;

	double sample(double p_progress) const {
		const double t = clamp_time(p_progress);
		if (t == 1.0) {
			return 0.0;
		}
		if (attack == 0.0) {
			return std::pow(1.0 - t, power);
		}
		if (t <= attack) {
			return smooth(t / attack);
		}
		return std::pow(MAX(0.0, 1.0 - smooth((t - attack) / (1.0 - attack))), power);
	}
};

enum class Wave {
	PUNCH,
	SHAKE,
	BREATHE,
	ENVELOPE,
};

struct Channel {
	Wave wave = Wave::ENVELOPE;
	double frequency = 0.0;
	double amplitude = 0.0;
	// Phase in cycles for punch and breathe, the noise coordinate for shake.
	double phase = 0.0;
	int64_t seed = 0;
	Envelope envelope;

	double sample(double p_progress) const {
		const double t = clamp_time(p_progress);
		switch (wave) {
			case Wave::PUNCH:
				return t == 1.0 ? 0.0 : amplitude * std::sin(Math::TAU * (frequency * t + phase)) * envelope.sample(t);
			case Wave::SHAKE:
				return t == 1.0 ? 0.0 : amplitude * noise(phase + frequency * t, seed) * envelope.sample(t);
			case Wave::BREATHE:
				return amplitude * (0.5 - 0.5 * std::cos(Math::TAU * (frequency * t + phase)));
			case Wave::ENVELOPE:
			default:
				return envelope.sample(p_progress);
		}
	}
};

enum class Shape {
	SCALAR,
	VECTOR2,
	VECTOR3,
	QUATERNION,
};

class FXCallable : public CallableCustom {
	Shape shape;
	Channel channels[3];

	static bool compare_equal(const CallableCustom *p_a, const CallableCustom *p_b) { return p_a == p_b; }
	static bool compare_less(const CallableCustom *p_a, const CallableCustom *p_b) { return p_a < p_b; }

	Vector3 sample_vector(double p_progress) const {
		return Vector3(channels[0].sample(p_progress), channels[1].sample(p_progress), channels[2].sample(p_progress));
	}

public:
	FXCallable(Shape p_shape, const Channel &p_x, const Channel &p_y, const Channel &p_z) :
			shape(p_shape), channels{ p_x, p_y, p_z } {}

	uint32_t hash() const override { return uint32_t(reinterpret_cast<uintptr_t>(this) >> 4); }
	String get_as_text() const override { return "TweensGdFX"; }
	CompareEqualFunc get_compare_equal_func() const override { return &FXCallable::compare_equal; }
	CompareLessFunc get_compare_less_func() const override { return &FXCallable::compare_less; }
	bool is_valid() const override { return true; }
	ObjectID get_object() const override { return ObjectID(); }

	int get_argument_count(bool &r_is_valid) const override {
		r_is_valid = true;
		return 1;
	}

	void call(const Variant **p_arguments, int p_argcount, Variant &r_return_value, GDExtensionCallError &r_call_error) const override {
		if (p_argcount != 1) {
			r_call_error.error = p_argcount < 1 ? GDEXTENSION_CALL_ERROR_TOO_FEW_ARGUMENTS : GDEXTENSION_CALL_ERROR_TOO_MANY_ARGUMENTS;
			r_call_error.expected = 1;
			return;
		}
		const Variant &argument = *p_arguments[0];
		if (argument.get_type() != Variant::FLOAT && argument.get_type() != Variant::INT) {
			r_call_error.error = GDEXTENSION_CALL_ERROR_INVALID_ARGUMENT;
			r_call_error.argument = 0;
			r_call_error.expected = Variant::FLOAT;
			return;
		}
		const double progress = argument;
		r_call_error.error = GDEXTENSION_CALL_OK;
		switch (shape) {
			case Shape::SCALAR:
				r_return_value = channels[0].sample(progress);
				return;
			case Shape::VECTOR2:
				r_return_value = Vector2(channels[0].sample(progress), channels[1].sample(progress));
				return;
			case Shape::VECTOR3:
				r_return_value = sample_vector(progress);
				return;
			case Shape::QUATERNION: {
				const Vector3 v = sample_vector(progress);
				const double angle = std::sqrt(double(v.x) * v.x + double(v.y) * v.y + double(v.z) * v.z);
				if (angle == 0.0) {
					r_return_value = Quaternion();
					return;
				}
				const double scale = std::sin(angle * 0.5) / angle;
				r_return_value = Quaternion(v.x * scale, v.y * scale, v.z * scale, std::cos(angle * 0.5)).normalized();
				return;
			}
		}
	}
};

bool valid_wave(double p_frequency, double p_amplitude, double p_offset) {
	if (!Math::is_finite(p_frequency) || p_frequency < 0.0 || p_frequency > FLOAT_MAX || !Math::is_finite(p_amplitude) ||
			std::abs(p_amplitude) > FLOAT_MAX || !Math::is_finite(p_offset) || std::abs(p_offset) > FLOAT_MAX) {
		UtilityFunctions::push_error("tweens.gd FX: frequency must be nonnegative; frequency, amplitude and offset must fit finite floats.");
		return false;
	}
	return true;
}

bool make_envelope(double p_attack, double p_power, Channel &r_channel) {
	if (!Math::is_finite(p_attack) || p_attack < 0.0 || p_attack >= 1.0 || !Math::is_finite(p_power) || p_power <= 0.0) {
		UtilityFunctions::push_error("tweens.gd FX: attack must be in [0, 1) and decay must be finite and positive.");
		return false;
	}
	r_channel.envelope.attack = p_attack;
	r_channel.envelope.power = p_power;
	return true;
}

bool make_punch(double p_frequency, double p_amplitude, double p_decay, double p_phase, double p_attack, Channel &r_channel) {
	if (!valid_wave(p_frequency, p_amplitude, p_phase) || !make_envelope(p_attack, p_decay, r_channel)) {
		return false;
	}
	r_channel.wave = Wave::PUNCH;
	r_channel.frequency = p_frequency;
	r_channel.amplitude = p_amplitude;
	r_channel.phase = p_phase;
	return true;
}

bool make_shake(double p_frequency, double p_amplitude, int64_t p_seed, double p_offset, double p_decay, double p_attack, Channel &r_channel) {
	if (!valid_wave(p_frequency, p_amplitude, p_offset)) {
		return false;
	}
	if (p_seed < -2147483648LL || p_seed > 2147483647LL) {
		UtilityFunctions::push_error("tweens.gd FX: seed must fit a signed 32-bit integer.");
		return false;
	}
	if (!make_envelope(p_attack, p_decay, r_channel)) {
		return false;
	}
	r_channel.wave = Wave::SHAKE;
	r_channel.frequency = p_frequency;
	r_channel.amplitude = p_amplitude;
	r_channel.phase = p_offset;
	r_channel.seed = p_seed;
	return true;
}

bool make_breathe(double p_frequency, double p_amplitude, double p_phase, Channel &r_channel) {
	if (!valid_wave(p_frequency, p_amplitude, p_phase)) {
		return false;
	}
	r_channel.wave = Wave::BREATHE;
	r_channel.frequency = p_frequency;
	r_channel.amplitude = p_amplitude;
	r_channel.phase = p_phase;
	return true;
}

Callable wrap(Shape p_shape, const Channel &p_x, const Channel &p_y = Channel(), const Channel &p_z = Channel()) {
	return Callable(memnew(FXCallable(p_shape, p_x, p_y, p_z)));
}

} // namespace

void TweensGdFX::_bind_methods() {
	const char *name = "TweensGdFX";
	const Vector3 zero3;
	ClassDB::bind_static_method(name, D_METHOD("punch", "frequency", "amplitude", "decay", "phase", "attack"), &TweensGdFX::punch,
			DEFVAL(6.0), DEFVAL(1.0), DEFVAL(2.0), DEFVAL(0.0), DEFVAL(0.0));
	ClassDB::bind_static_method(name, D_METHOD("shake", "frequency", "amplitude", "seed", "offset", "decay", "attack"), &TweensGdFX::shake,
			DEFVAL(12.0), DEFVAL(1.0), DEFVAL(0), DEFVAL(0.0), DEFVAL(2.0), DEFVAL(0.1));
	ClassDB::bind_static_method(name, D_METHOD("breathe", "frequency", "amplitude", "phase"), &TweensGdFX::breathe, DEFVAL(1.0),
			DEFVAL(1.0), DEFVAL(0.0));
	ClassDB::bind_static_method(name, D_METHOD("decay", "power"), &TweensGdFX::decay, DEFVAL(2.0));
	ClassDB::bind_static_method(name, D_METHOD("attack_release", "attack", "power"), &TweensGdFX::attack_release, DEFVAL(0.1),
			DEFVAL(2.0));
	ClassDB::bind_static_method(name, D_METHOD("punch_2d", "amplitude", "frequency", "decay", "phase", "attack"), &TweensGdFX::punch_2d,
			DEFVAL(Vector2(6, 6)), DEFVAL(2.0), DEFVAL(Vector2()), DEFVAL(0.0));
	ClassDB::bind_static_method(name, D_METHOD("punch_3d", "amplitude", "frequency", "decay", "phase", "attack"), &TweensGdFX::punch_3d,
			DEFVAL(Vector3(6, 6, 6)), DEFVAL(2.0), DEFVAL(zero3), DEFVAL(0.0));
	ClassDB::bind_static_method(name, D_METHOD("shake_2d", "amplitude", "frequency", "seed", "offset", "decay", "attack"),
			&TweensGdFX::shake_2d, DEFVAL(Vector2(12, 12)), DEFVAL(0), DEFVAL(Vector2(0, 101.37)), DEFVAL(2.0), DEFVAL(0.1));
	ClassDB::bind_static_method(name, D_METHOD("shake_3d", "amplitude", "frequency", "seed", "offset", "decay", "attack"),
			&TweensGdFX::shake_3d, DEFVAL(Vector3(12, 12, 12)), DEFVAL(0), DEFVAL(Vector3(0, 101.37, 203.71)), DEFVAL(2.0), DEFVAL(0.1));
	ClassDB::bind_static_method(name, D_METHOD("breathe_2d", "amplitude", "frequency", "phase"), &TweensGdFX::breathe_2d,
			DEFVAL(Vector2(1, 1)), DEFVAL(Vector2()));
	ClassDB::bind_static_method(name, D_METHOD("breathe_3d", "amplitude", "frequency", "phase"), &TweensGdFX::breathe_3d,
			DEFVAL(Vector3(1, 1, 1)), DEFVAL(zero3));
	ClassDB::bind_static_method(name, D_METHOD("punch_quaternion", "amplitude", "frequency", "decay", "phase", "attack"),
			&TweensGdFX::punch_quaternion, DEFVAL(Vector3(6, 6, 6)), DEFVAL(2.0), DEFVAL(zero3), DEFVAL(0.0));
	ClassDB::bind_static_method(name, D_METHOD("shake_quaternion", "amplitude", "frequency", "seed", "offset", "decay", "attack"),
			&TweensGdFX::shake_quaternion, DEFVAL(Vector3(12, 12, 12)), DEFVAL(0), DEFVAL(Vector3(0, 101.37, 203.71)), DEFVAL(2.0),
			DEFVAL(0.1));
	ClassDB::bind_static_method(name, D_METHOD("breathe_quaternion", "amplitude", "frequency", "phase"), &TweensGdFX::breathe_quaternion,
			DEFVAL(Vector3(1, 1, 1)), DEFVAL(zero3));
}

Callable TweensGdFX::punch(double p_frequency, double p_amplitude, double p_decay, double p_phase, double p_attack) {
	Channel channel;
	return make_punch(p_frequency, p_amplitude, p_decay, p_phase, p_attack, channel) ? wrap(Shape::SCALAR, channel) : Callable();
}

Callable TweensGdFX::shake(double p_frequency, double p_amplitude, int64_t p_seed, double p_offset, double p_decay, double p_attack) {
	Channel channel;
	return make_shake(p_frequency, p_amplitude, p_seed, p_offset, p_decay, p_attack, channel) ? wrap(Shape::SCALAR, channel) : Callable();
}

Callable TweensGdFX::breathe(double p_frequency, double p_amplitude, double p_phase) {
	Channel channel;
	return make_breathe(p_frequency, p_amplitude, p_phase, channel) ? wrap(Shape::SCALAR, channel) : Callable();
}

Callable TweensGdFX::decay(double p_power) {
	return attack_release(0.0, p_power);
}

Callable TweensGdFX::attack_release(double p_attack, double p_power) {
	Channel channel;
	return make_envelope(p_attack, p_power, channel) ? wrap(Shape::SCALAR, channel) : Callable();
}

Callable TweensGdFX::punch_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, double p_decay, const Vector2 &p_phase, double p_attack) {
	Channel x;
	Channel y;
	const bool valid_x = make_punch(p_frequency.x, p_amplitude.x, p_decay, p_phase.x, p_attack, x);
	const bool valid_y = make_punch(p_frequency.y, p_amplitude.y, p_decay, p_phase.y, p_attack, y);
	return valid_x && valid_y ? wrap(Shape::VECTOR2, x, y) : Callable();
}

Callable TweensGdFX::punch_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, double p_decay, const Vector3 &p_phase, double p_attack) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_punch(p_frequency.x, p_amplitude.x, p_decay, p_phase.x, p_attack, x);
	const bool valid_y = make_punch(p_frequency.y, p_amplitude.y, p_decay, p_phase.y, p_attack, y);
	const bool valid_z = make_punch(p_frequency.z, p_amplitude.z, p_decay, p_phase.z, p_attack, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::VECTOR3, x, y, z) : Callable();
}

Callable TweensGdFX::shake_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, int64_t p_seed, const Vector2 &p_offset, double p_decay,
		double p_attack) {
	Channel x;
	Channel y;
	const bool valid_x = make_shake(p_frequency.x, p_amplitude.x, p_seed, p_offset.x, p_decay, p_attack, x);
	const bool valid_y = make_shake(p_frequency.y, p_amplitude.y, p_seed, p_offset.y, p_decay, p_attack, y);
	return valid_x && valid_y ? wrap(Shape::VECTOR2, x, y) : Callable();
}

Callable TweensGdFX::shake_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, int64_t p_seed, const Vector3 &p_offset, double p_decay,
		double p_attack) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_shake(p_frequency.x, p_amplitude.x, p_seed, p_offset.x, p_decay, p_attack, x);
	const bool valid_y = make_shake(p_frequency.y, p_amplitude.y, p_seed, p_offset.y, p_decay, p_attack, y);
	const bool valid_z = make_shake(p_frequency.z, p_amplitude.z, p_seed, p_offset.z, p_decay, p_attack, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::VECTOR3, x, y, z) : Callable();
}

Callable TweensGdFX::breathe_2d(const Vector2 &p_amplitude, const Vector2 &p_frequency, const Vector2 &p_phase) {
	Channel x;
	Channel y;
	const bool valid_x = make_breathe(p_frequency.x, p_amplitude.x, p_phase.x, x);
	const bool valid_y = make_breathe(p_frequency.y, p_amplitude.y, p_phase.y, y);
	return valid_x && valid_y ? wrap(Shape::VECTOR2, x, y) : Callable();
}

Callable TweensGdFX::breathe_3d(const Vector3 &p_amplitude, const Vector3 &p_frequency, const Vector3 &p_phase) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_breathe(p_frequency.x, p_amplitude.x, p_phase.x, x);
	const bool valid_y = make_breathe(p_frequency.y, p_amplitude.y, p_phase.y, y);
	const bool valid_z = make_breathe(p_frequency.z, p_amplitude.z, p_phase.z, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::VECTOR3, x, y, z) : Callable();
}

Callable TweensGdFX::punch_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, double p_decay, const Vector3 &p_phase,
		double p_attack) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_punch(p_frequency.x, p_amplitude.x, p_decay, p_phase.x, p_attack, x);
	const bool valid_y = make_punch(p_frequency.y, p_amplitude.y, p_decay, p_phase.y, p_attack, y);
	const bool valid_z = make_punch(p_frequency.z, p_amplitude.z, p_decay, p_phase.z, p_attack, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::QUATERNION, x, y, z) : Callable();
}

Callable TweensGdFX::shake_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, int64_t p_seed, const Vector3 &p_offset,
		double p_decay, double p_attack) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_shake(p_frequency.x, p_amplitude.x, p_seed, p_offset.x, p_decay, p_attack, x);
	const bool valid_y = make_shake(p_frequency.y, p_amplitude.y, p_seed, p_offset.y, p_decay, p_attack, y);
	const bool valid_z = make_shake(p_frequency.z, p_amplitude.z, p_seed, p_offset.z, p_decay, p_attack, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::QUATERNION, x, y, z) : Callable();
}

Callable TweensGdFX::breathe_quaternion(const Vector3 &p_amplitude, const Vector3 &p_frequency, const Vector3 &p_phase) {
	Channel x;
	Channel y;
	Channel z;
	const bool valid_x = make_breathe(p_frequency.x, p_amplitude.x, p_phase.x, x);
	const bool valid_y = make_breathe(p_frequency.y, p_amplitude.y, p_phase.y, y);
	const bool valid_z = make_breathe(p_frequency.z, p_amplitude.z, p_phase.z, z);
	return valid_x && valid_y && valid_z ? wrap(Shape::QUATERNION, x, y, z) : Callable();
}

} // namespace godot
