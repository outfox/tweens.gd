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
constexpr int64_t LEG_MASK = (int64_t(1) << 13) - 1;
constexpr int64_t COMPOSITION_MASK = (LEG_MASK << 8) | (LEG_MASK << 21);

bool single_or_missing(int64_t bits) {
	return (bits & (bits - 1)) == 0;
}

// Slot order matches the public In/Out flags in types.gd and EaseLegs.cs.
int64_t legacy_leg(int64_t bits, bool out) {
	int slot = 0;
	while ((bits >>= 1) != 0) ++slot;
	if (slot == 0) return TweensGdEasing::LINEAR;
	if (slot == 11) return TweensGdEasing::SMOOTH_STEP;
	if (slot == 12) return TweensGdEasing::SMOOTHER_STEP;
	return slot * 10 + (out ? 1 : 0);
}

// Conventional half profiles, including the authored InOut tuning of Back and Elastic.
int64_t legacy_pair(int64_t bits) {
	const int64_t entry = legacy_leg(bits, false);
	return entry >= TweensGdEasing::SINE_IN && entry <= TweensGdEasing::BOUNCE_IN ? entry + 2 : entry;
}

// Derivative of a conventional half profile; symmetric about the midpoint.
double pair_slope(int64_t family, double time) {
	const double t = MIN(time, 1.0 - time), x = 2.0 * t;
	switch (family) {
		case TweensGdEasing::SINE_IN_OUT: return Math::PI * std::sin(Math::PI * t) / 2.0;
		case TweensGdEasing::QUAD_IN_OUT: return 4.0 * t;
		case TweensGdEasing::CUBIC_IN_OUT: return 12.0 * t * t;
		case TweensGdEasing::QUART_IN_OUT: return 32.0 * t * t * t;
		case TweensGdEasing::QUINT_IN_OUT: return 80.0 * t * t * t * t;
		case TweensGdEasing::EXPO_IN_OUT: return 10.0 * std::log(2.0) * std::pow(2.0, 20.0 * t - 10.0);
		case TweensGdEasing::CIRC_IN_OUT: return x / std::sqrt(1.0 - x * x);
		case TweensGdEasing::BACK_IN_OUT: return 3.0 * (B + 1.0) * x * x - 2.0 * B * x;
		case TweensGdEasing::ELASTIC_IN_OUT: return -10.0 * std::pow(2.0, 20.0 * t - 10.0) *
			(std::log(2.0) * std::sin((20.0 * t - 11.125) * E) + E * std::cos((20.0 * t - 11.125) * E));
		case TweensGdEasing::BOUNCE_IN_OUT: {
			double v = 1.0 - x;
			if (v >= 2.5 / 2.75) v -= 2.625 / 2.75;
			else if (v >= 2.0 / 2.75) v -= 2.25 / 2.75;
			else if (v >= 1.0 / 2.75) v -= 1.5 / 2.75;
			return 15.125 * v;
		}
		case TweensGdEasing::SMOOTH_STEP: return 6.0 * t * (1.0 - t);
		case TweensGdEasing::SMOOTHER_STEP: return 30.0 * t * t * (1.0 - t) * (1.0 - t);
		default: return 1.0;
	}
}

double hermite(double u, double y0, double y1, double m0, double m1) {
	const double u2 = u * u, u3 = u2 * u;
	return (2.0*u3 - 3.0*u2 + 1.0)*y0 + (u3 - 2.0*u2 + u)*m0
		+ (-2.0*u3 + 3.0*u2)*y1 + (u3 - u2)*m1;
}

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
	ClassDB::bind_static_method("TweensGdEasing", D_METHOD("evaluate", "ease", "progress", "blend_type", "blend"), &TweensGdEasing::evaluate, DEFVAL(0), DEFVAL(0.4));
}

bool tweens::is_known_ease(int64_t p_ease) {
	if (p_ease >= 256 && (p_ease & ~COMPOSITION_MASK) == 0) {
		return single_or_missing((p_ease >> 8) & LEG_MASK) && single_or_missing((p_ease >> 21) & LEG_MASK);
	}
	using Ease = TweensGdEasing::Ease;
	if (p_ease == Ease::LINEAR || p_ease == Ease::SMOOTH_STEP || p_ease == Ease::SMOOTHER_STEP) {
		return true;
	}
	return p_ease >= Ease::SINE_IN && p_ease <= Ease::BOUNCE_IN_OUT && p_ease % 10 <= 2;
}

double TweensGdEasing::evaluate(int64_t p_ease, double p_progress, int64_t p_blend_type, double p_blend) {
	if (p_blend_type < 0 || p_blend_type > 2 || !std::isfinite(p_blend) || p_blend < 0.0 || p_blend > 1.0) return Math::NaN;
	using std::cos;
	using std::pow;
	using std::sin;
	using std::sqrt;
	// NaN progress stays detectable; infinities clamp like any other out-of-range progress.
	if (Math::is_nan(p_progress)) {
		return Math::NaN;
	}
	const double t = CLAMP(p_progress, 0.0, 1.0);
	if (p_ease >= 256) {
		if (!tweens::is_known_ease(p_ease)) return Math::NaN;
		const int64_t entry = (p_ease >> 8) & LEG_MASK;
		const int64_t exit = (p_ease >> 21) & LEG_MASK;
		if (entry == 0) return evaluate(legacy_leg(exit, true), t);
		if (exit == 0) return evaluate(legacy_leg(entry, false), t);
		const int64_t in_pair = legacy_pair(entry), out_pair = legacy_pair(exit);
		const double h = p_blend / 2.0, left = 0.5 - h, right = 0.5 + h;
		if (entry == exit || t <= left) return evaluate(in_pair, t);
		if (t >= right) return evaluate(out_pair, t);
		if (p_blend_type != 0) {
			const double u = (t - left) / p_blend;
			const double w = p_blend_type == 1 ? u*u*(3.0 - 2.0*u) : u;
			return evaluate(in_pair, t)*(1.0-w) + evaluate(out_pair, t)*w;
		}
		const double y0 = evaluate(in_pair, left), y1 = evaluate(out_pair, right);
		const double v0 = pair_slope(in_pair, left), v1 = pair_slope(out_pair, right);
		const double d0 = (0.5-y0)/h, d1 = (y1-0.5)/h;
		// Shared midpoint tangent solves equal acceleration, limited against new reversals.
		const double middle = CLAMP((3.0*(d0+d1)-v0-v1)/4.0, 0.0, 3.0*MAX(0.0, MIN(d0,d1)));
		return t <= 0.5 ? hermite((t-left)/h, y0, 0.5, h*v0, h*middle)
			: hermite((t-0.5)/h, 0.5, y1, h*middle, h*v1);
	}
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
