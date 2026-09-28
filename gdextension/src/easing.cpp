// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// SPDX-FileCopyrightText: 2020 Jeffrey Lanters
// Adapted from unity-tweens; see THIRD-PARTY-NOTICES.md.
#include "easing.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>

#include <cmath>

namespace godot {

namespace {

constexpr double A = 1.70158;
constexpr double B = A * 1.525;
constexpr double C = A + 1.0;
constexpr double D = Math::TAU / 3.0;
constexpr double E = Math::TAU / 4.5;

double bounce_out(double t) {
	if (t < 1.0 / 2.75) {
		return 7.5625 * t * t;
	}
	if (t < 2.0 / 2.75) {
		t -= 1.5 / 2.75;
		return 7.5625 * t * t + 0.75;
	}
	if (t < 2.5 / 2.75) {
		t -= 2.25 / 2.75;
		return 7.5625 * t * t + 0.9375;
	}
	t -= 2.625 / 2.75;
	return 7.5625 * t * t + 0.984375;
}

} // namespace

void TweensGdEasing::_bind_methods() {
	ClassDB::bind_static_method("TweensGdEasing", D_METHOD("evaluate", "ease", "progress"), &TweensGdEasing::evaluate);
}

bool tweens::is_known_ease(int64_t p_ease) {
	using Ease = TweensGdEasing::Ease;
	if (p_ease == Ease::LINEAR || p_ease == Ease::SMOOTH_STEP || p_ease == Ease::SMOOTHER_STEP) {
		return true;
	}
	return p_ease >= Ease::SINE_IN && p_ease <= Ease::BOUNCE_IN_OUT && p_ease % 10 <= 2;
}

double TweensGdEasing::evaluate(int64_t p_ease, double p_progress) {
	using std::cos;
	using std::pow;
	using std::sin;
	using std::sqrt;
	// NaN progress stays detectable; infinities clamp like any other out-of-range progress.
	if (Math::is_nan(p_progress)) {
		return Math::NaN;
	}
	const double t = CLAMP(p_progress, 0.0, 1.0);
	switch (p_ease) {
		case LINEAR:
			return t;
		case SMOOTH_STEP:
			return t * t * (3.0 - 2.0 * t);
		case SMOOTHER_STEP:
			return t * t * t * (t * (6.0 * t - 15.0) + 10.0);
		case SINE_IN:
			return 1.0 - cos(t * Math::PI / 2.0);
		case SINE_OUT:
			return sin(t * Math::PI / 2.0);
		case SINE_IN_OUT:
			return -(cos(Math::PI * t) - 1.0) / 2.0;
		case QUAD_IN:
			return t * t;
		case QUAD_OUT:
			return 1.0 - (1.0 - t) * (1.0 - t);
		case QUAD_IN_OUT:
			return t < 0.5 ? 2.0 * t * t : 1.0 - pow(-2.0 * t + 2.0, 2.0) / 2.0;
		case CUBIC_IN:
			return t * t * t;
		case CUBIC_OUT:
			return 1.0 - pow(1.0 - t, 3.0);
		case CUBIC_IN_OUT:
			return t < 0.5 ? 4.0 * t * t * t : 1.0 - pow(-2.0 * t + 2.0, 3.0) / 2.0;
		case QUART_IN:
			return pow(t, 4.0);
		case QUART_OUT:
			return 1.0 - pow(1.0 - t, 4.0);
		case QUART_IN_OUT:
			return t < 0.5 ? 8.0 * pow(t, 4.0) : 1.0 - pow(-2.0 * t + 2.0, 4.0) / 2.0;
		case QUINT_IN:
			return pow(t, 5.0);
		case QUINT_OUT:
			return 1.0 - pow(1.0 - t, 5.0);
		case QUINT_IN_OUT:
			return t < 0.5 ? 16.0 * pow(t, 5.0) : 1.0 - pow(-2.0 * t + 2.0, 5.0) / 2.0;
		case EXPO_IN:
			return t == 0.0 ? 0.0 : pow(2.0, 10.0 * t - 10.0);
		case EXPO_OUT:
			return t == 1.0 ? 1.0 : 1.0 - pow(2.0, -10.0 * t);
		case EXPO_IN_OUT:
			if (t == 0.0 || t == 1.0) {
				return t;
			}
			return t < 0.5 ? pow(2.0, 20.0 * t - 10.0) / 2.0 : (2.0 - pow(2.0, -20.0 * t + 10.0)) / 2.0;
		case CIRC_IN:
			return 1.0 - sqrt(1.0 - t * t);
		case CIRC_OUT:
			return sqrt(1.0 - pow(t - 1.0, 2.0));
		case CIRC_IN_OUT:
			return t < 0.5 ? (1.0 - sqrt(1.0 - pow(2.0 * t, 2.0))) / 2.0 : (sqrt(1.0 - pow(-2.0 * t + 2.0, 2.0)) + 1.0) / 2.0;
		case BACK_IN:
			return C * t * t * t - A * t * t;
		case BACK_OUT:
			return 1.0 + C * pow(t - 1.0, 3.0) + A * pow(t - 1.0, 2.0);
		case BACK_IN_OUT:
			return t < 0.5 ? pow(2.0 * t, 2.0) * ((B + 1.0) * 2.0 * t - B) / 2.0
						   : (pow(2.0 * t - 2.0, 2.0) * ((B + 1.0) * (t * 2.0 - 2.0) + B) + 2.0) / 2.0;
		case ELASTIC_IN:
			return t == 0.0 || t == 1.0 ? t : -pow(2.0, 10.0 * t - 10.0) * sin((t * 10.0 - 10.75) * D);
		case ELASTIC_OUT:
			return t == 0.0 || t == 1.0 ? t : pow(2.0, -10.0 * t) * sin((t * 10.0 - 0.75) * D) + 1.0;
		case ELASTIC_IN_OUT:
			if (t == 0.0 || t == 1.0) {
				return t;
			}
			return t < 0.5 ? -pow(2.0, 20.0 * t - 10.0) * sin((20.0 * t - 11.125) * E) / 2.0
						   : pow(2.0, -20.0 * t + 10.0) * sin((20.0 * t - 11.125) * E) / 2.0 + 1.0;
		case BOUNCE_IN:
			return 1.0 - bounce_out(1.0 - t);
		case BOUNCE_OUT:
			return bounce_out(t);
		case BOUNCE_IN_OUT:
			return t < 0.5 ? (1.0 - bounce_out(1.0 - 2.0 * t)) / 2.0 : (1.0 + bounce_out(2.0 * t - 1.0)) / 2.0;
		default:
			return Math::NaN;
	}
}

} // namespace godot
