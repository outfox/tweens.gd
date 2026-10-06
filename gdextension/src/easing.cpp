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
constexpr int64_t COMPOSITION_MASK = ~int64_t(63);
// Preserve original flags. Jump uses two-of-four codes in bits 58-61 (In)
// and 62/63/6/7 (Out); combining different codes fails single_or_missing.
int64_t leg_bits(int64_t bits, bool out) {
	const int64_t curves = ((bits >> (out ? 21 : 8)) & LEG_MASK) | (((bits >> (out ? 42 : 34)) & 255) << 13)
		| (((bits >> (out ? 54 : 50)) & 15) << 21);
	const int64_t jump = out ? ((bits >> 62) & 3) | ((bits >> 4) & 12) : (bits >> 58) & 15;
	int slot;
	switch (jump) {
		case 0: return curves;
		case 3: slot = 25; break;
		case 5: slot = 26; break;
		case 6: slot = 27; break;
		case 9: slot = 28; break;
		case 10: slot = 29; break;
		default: return curves | (int64_t(3) << 30);
	}
	return curves | (int64_t(1) << slot);
}

// Peak-calibrated parameters for 10%, 20%, ... 50% over the full tween range.
constexpr double BACK_SOLO[] = {1.701540198866824, 2.5923889015162995, 3.3940516581445603, 4.155744652639195, 4.894859521133737};
constexpr double BACK_PAIRED[] = {2.5923889015162995, 4.155744652639195, 5.619622918334311, 7.042439379340937, 8.44353560159325};
constexpr double ELASTIC_SOLO_DECAY = 17.553423501870573;
// Solo damping relaxes after the main swing; paired legs use half their former frequency.
// Reproduce the calibrated peaks with scripts/calibrate-elastic.mjs.
constexpr double ELASTIC_SOLO_TAIL = 8.0;
constexpr double ELASTIC_PAIR_DECAY = 7.537490798899971;
constexpr double ELASTIC_SOLO_PERIOD = 0.43031056027706766;
constexpr double ELASTIC_PAIR_PERIOD = 0.6732985463200285;
constexpr double ELASTIC_SOLO_KICK[] = {-0.2974298881021775, 0.5992618094300022, 1.036207742895828, 1.3991518140146244, 1.730459189003298};
constexpr double ELASTIC_PAIR_KICK[] = {0.054242444203084675, 0.9078807808396336, 1.4611442537053763, 1.9533326533438204, 2.419656309841953};

const double BOUNCE_SOLO_ROOT[] = {std::sqrt(0.1), std::sqrt(0.2), std::sqrt(0.3), std::sqrt(0.4), std::sqrt(0.5)};
const double BOUNCE_PAIR_ROOT[] = {std::sqrt(0.2), std::sqrt(0.4), std::sqrt(0.6), std::sqrt(0.8), 1.0};

const double JUMP_SOLO_LAUNCH[] = {std::sqrt(1.1), std::sqrt(1.2), std::sqrt(1.3), std::sqrt(1.4), std::sqrt(1.5)};
const double JUMP_PAIR_LAUNCH[] = {std::sqrt(1.2), std::sqrt(1.4), std::sqrt(1.6), std::sqrt(1.8), std::sqrt(2.0)};

bool single_or_missing(int64_t bits) {
	return (bits & (bits - 1)) == 0;
}

// Slot order matches EaseLegs.cs and the public flags in ease_in.gd and ease_out.gd.
int leg_slot(int64_t bits) {
	int slot = 0;
	while ((bits >>= 1) != 0) ++slot;
	return slot;
}
int back_level(int slot) { return slot == 8 ? 0 : slot >= 13 && slot <= 16 ? slot - 12 : -1; }
int elastic_level(int slot) { return slot == 9 ? 0 : slot >= 17 && slot <= 20 ? slot - 16 : -1; }
int bounce_level(int slot) { return slot == 10 ? 0 : slot >= 21 && slot <= 24 ? slot - 20 : -1; }
int jump_level(int slot) { return slot >= 25 && slot <= 29 ? slot - 25 : -1; }
int64_t legacy_leg(int slot, bool out) {
	if (slot == 0) return TweensGdEasing::LINEAR;
	if (slot == 11) return TweensGdEasing::SMOOTH_STEP;
	if (slot == 12) return TweensGdEasing::SMOOTHER_STEP;
	return slot * 10 + (out ? 1 : 0);
}

double back_in(double t, int level, bool paired) {
	if (t == 0.0 || t == 1.0) return t;
	const double s = (paired ? BACK_PAIRED : BACK_SOLO)[level];
	return (s + 1.0)*t*t*t - s*t*t;
}
double elastic_scale(double kick, bool paired) {
	const double omega = Math::TAU/(paired ? ELASTIC_PAIR_PERIOD : ELASTIC_SOLO_PERIOD);
	const double residual = std::pow(2.0, -(paired ? ELASTIC_PAIR_DECAY : ELASTIC_SOLO_DECAY-ELASTIC_SOLO_TAIL))
		*(std::cos(omega)-kick*std::sin(omega));
	return 1.0/(1.0-residual);
}
double elastic_out(double t, int level, bool paired) {
	if (t == 0.0 || t == 1.0) return t;
	const double angle = Math::TAU*t/(paired ? ELASTIC_PAIR_PERIOD : ELASTIC_SOLO_PERIOD);
	const double kick = (paired ? ELASTIC_PAIR_KICK : ELASTIC_SOLO_KICK)[level];
	const double exponent = -(paired ? ELASTIC_PAIR_DECAY : ELASTIC_SOLO_DECAY)*t+(paired ? 0.0 : ELASTIC_SOLO_TAIL*t*t);
	return elastic_scale(kick, paired)*(1.0-std::pow(2.0,exponent)*(std::cos(angle)-kick*std::sin(angle)));
}
// Constant acceleration; three rebound depths h, h/4, h/16, with flight times
// 2*sqrt(h), sqrt(h), sqrt(h)/2 after the unit-duration initial fall.
double bounce_leg_out(double t, int level, bool paired) {
	if (t == 0.0 || t == 1.0) return t;
	const double h = (level+1)*(paired ? 0.2 : 0.1);
	const double r = (paired ? BOUNCE_PAIR_ROOT : BOUNCE_SOLO_ROOT)[level];
	double u = t*(1.0+3.5*r);
	if (u < 1.0) return u*u;
	if (u < 1.0+2.0*r) { u -= 1.0+r; return 1.0-h+u*u; }
	if (u < 1.0+3.0*r) { u -= 1.0+2.5*r; return 1.0-h/4.0+u*u; }
	u -= 1.0+3.25*r;
	return 1.0-h/16.0+u*u;
}
// Launch to the first peak, then rebound twice above the target.
double jump_leg_out(double t, int level, bool paired) {
	if (t == 0.0 || t == 1.0) return t;
	const double h = (level+1)*(paired ? 0.2 : 0.1);
	const double r = (paired ? BOUNCE_PAIR_ROOT : BOUNCE_SOLO_ROOT)[level];
	const double a = (paired ? JUMP_PAIR_LAUNCH : JUMP_SOLO_LAUNCH)[level];
	double u = t*(a+2.5*r);
	if (u < a+r) { u -= a; return 1.0+h-u*u; }
	if (u < a+2.0*r) { u -= a+1.5*r; return 1.0+h/4.0-u*u; }
	u -= a+2.25*r;
	return 1.0+h/16.0-u*u;
}
double family_leg(int slot, bool out, double t, bool paired = false) {
	const int back = back_level(slot), elastic = elastic_level(slot), bounce = bounce_level(slot), jump = jump_level(slot);
	if (back >= 0) return out ? 1.0-back_in(1.0-t, back, paired) : back_in(t, back, paired);
	if (elastic >= 0) return out ? elastic_out(t, elastic, paired) : 1.0-elastic_out(1.0-t, elastic, paired);
	if (bounce >= 0) return out ? bounce_leg_out(t, bounce, paired) : 1.0-bounce_leg_out(1.0-t, bounce, paired);
	if (jump >= 0) return out ? jump_leg_out(t, jump, paired) : 1.0-jump_leg_out(1.0-t, jump, paired);
	return TweensGdEasing::evaluate(legacy_leg(slot, out), t);
}
double family_pair(int slot, double t) {
	if (back_level(slot) >= 0 || elastic_level(slot) >= 0 || bounce_level(slot) >= 0 || jump_level(slot) >= 0)
		return t < 0.5 ? family_leg(slot, false, 2.0*t, true)/2.0 : 0.5+family_leg(slot, true, 2.0*t-1.0, true)/2.0;
	const int64_t legacy = legacy_leg(slot, false);
	return TweensGdEasing::evaluate(legacy >= 10 && legacy <= 100 ? legacy+2 : legacy, t);
}

// Derivative of a paired half profile; symmetric about the midpoint.
double pair_slope(int64_t family, double time) {
	const double t = MIN(time, 1.0 - time), x = 2.0 * t;
	const int back = back_level(int(family)), elastic = elastic_level(int(family)), bounce = bounce_level(int(family));
	if (back >= 0) { const double s = BACK_PAIRED[back]; return 3.0*(s+1.0)*x*x - 2.0*s*x; }
	if (elastic >= 0) {
		const double u = 1.0-x, omega = Math::TAU/ELASTIC_PAIR_PERIOD, decay = ELASTIC_PAIR_DECAY*std::log(2.0), kick = ELASTIC_PAIR_KICK[elastic];
		return elastic_scale(kick,true)*std::pow(2.0, -ELASTIC_PAIR_DECAY*u)*((decay+kick*omega)*std::cos(omega*u)+(omega-kick*decay)*std::sin(omega*u));
	}
	if (bounce >= 0) {
		const double r = BOUNCE_PAIR_ROOT[bounce], scale = 1.0+3.5*r;
		double u = (1.0-x)*scale;
		if (u >= 1.0+3.0*r) u -= 1.0+3.25*r;
		else if (u >= 1.0+2.0*r) u -= 1.0+2.5*r;
		else if (u >= 1.0) u -= 1.0+r;
		return 2.0*scale*u;
	}
	const int jump = jump_level(int(family));
	if (jump >= 0) {
		const double r = BOUNCE_PAIR_ROOT[jump], a = JUMP_PAIR_LAUNCH[jump], scale = a+2.5*r;
		const double u = (1.0-x)*scale;
		const double center = u < a+r ? a : u < a+2.0*r ? a+1.5*r : a+2.25*r;
		return 2.0*scale*(center-u);
	}
	switch (family) {
		case 1: return Math::PI * std::sin(Math::PI * t) / 2.0;
		case 2: return 4.0 * t;
		case 3: return 12.0 * t * t;
		case 4: return 32.0 * t * t * t;
		case 5: return 80.0 * t * t * t * t;
		case 6: return 10.0 * std::log(2.0) * std::pow(2.0, 20.0 * t - 10.0);
		case 7: return x / std::sqrt(1.0 - x * x);
		case 11: return 6.0 * t * (1.0 - t);
		case 12: return 30.0 * t * t * (1.0 - t) * (1.0 - t);
		default: return 1.0;
	}
}

// Linear time/value scaling moves the authored split; larger legs approach their solo profiles.
double split_profile(int family, double t, double split) {
    if (split == 0.0) return family_leg(family, true, t);
    if (split == 1.0) return family_leg(family, false, t);
    if (split == 0.5) return family_pair(family, t);
    if (t == 0.0 || t == 1.0) return t;
    if (t <= split) {
        const double x = t / split, mix = MAX(0.0, 2.0 * split - 1.0);
        return split * ((1.0 - mix) * 2.0 * family_pair(family, x / 2.0) + mix * family_leg(family, false, x));
    }
    const double span = 1.0 - split, x = (t - split) / span, mix = MAX(0.0, 2.0 * span - 1.0);
    return split + span * ((1.0 - mix) * (2.0 * family_pair(family, 0.5 + x / 2.0) - 1.0) + mix * family_leg(family, true, x));
}

double solo_slope(int family, double t, bool out) {
    if (out) t = 1.0 - t;
    if (family == 11) return 6.0*t*(1.0-t);
    if (family == 12) return 30.0*t*t*(1.0-t)*(1.0-t);
    if (family == 1) return Math::PI/2.0*std::sin(Math::PI/2.0*t);
    if (family == 6) return 10.0*std::log(2.0)*std::pow(2.0,10.0*t-10.0);
    if (family == 7) return t/std::sqrt(1.0-t*t);
    const int back = back_level(family), elastic = elastic_level(family), bounce = bounce_level(family), jump = jump_level(family);
    if (back >= 0) { const double s = BACK_SOLO[back]; return 3.0*(s+1.0)*t*t-2.0*s*t; }
    if (elastic >= 0) {
        const double u = 1.0-t, omega = Math::TAU/ELASTIC_SOLO_PERIOD, decay = (ELASTIC_SOLO_DECAY-2.0*ELASTIC_SOLO_TAIL*u)*std::log(2.0), kick = ELASTIC_SOLO_KICK[elastic];
        return elastic_scale(kick,false)*std::pow(2.0,-ELASTIC_SOLO_DECAY*u+ELASTIC_SOLO_TAIL*u*u)*((decay+kick*omega)*std::cos(omega*u)+(omega-kick*decay)*std::sin(omega*u));
    }
    if (bounce >= 0 || jump >= 0) {
        const int level = jump >= 0 ? jump : bounce;
        const double r = BOUNCE_SOLO_ROOT[level], a = jump >= 0 ? JUMP_SOLO_LAUNCH[level] : 1.0;
        const double scale = jump >= 0 ? a+2.5*r : 1.0+3.5*r;
        double u = (1.0-t)*scale;
        if (jump >= 0) return 2.0*scale*((u < a+r ? a : u < a+2.0*r ? a+1.5*r : a+2.25*r)-u);
        if (u >= 1.0+3.0*r) u -= 1.0+3.25*r;
        else if (u >= 1.0+2.0*r) u -= 1.0+2.5*r;
        else if (u >= 1.0) u -= 1.0+r;
        return 2.0*scale*u;
    }
    return family >= 2 && family <= 5 ? family*std::pow(t,family-1) : 1.0;
}

double split_slope(int family, double t, double split) {
    if (split == 0.5) return pair_slope(family,t);
    const bool out = t > split;
    const double span = out ? 1.0-split : split, x = out ? (t-split)/span : t/span, mix = MAX(0.0,2.0*span-1.0);
    return (1.0-mix)*pair_slope(family,out ? 0.5+x/2.0 : x/2.0)+mix*solo_slope(family,x,out);
}

double hermite(double u, double y0, double y1, double m0, double m1) {
	const double u2 = u * u, u3 = u2 * u;
	return (2.0*u3 - 3.0*u2 + 1.0)*y0 + (u3 - 2.0*u2 + u)*m0
		+ (-2.0*u3 + 3.0*u2)*y1 + (u3 - u2)*m1;
}

// Modified Akima (makima) slope at the midpoint from the four surrounding slopes. The legs' edge
// velocities stand in for the outer secants. Each half stays on its side of 0.5, so the weights never both vanish.
double makima_slope(double s0, double s1, double s2, double s3) {
	const double w1 = std::abs(s3 - s2) + std::abs(s3 + s2) / 2.0, w2 = std::abs(s1 - s0) + std::abs(s1 + s0) / 2.0;
	return (w1 * s1 + w2 * s2) / (w1 + w2);
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
	ClassDB::bind_static_method("TweensGdEasing", D_METHOD("evaluate", "ease", "progress", "blend_type", "blend", "skew"), &TweensGdEasing::evaluate, DEFVAL(0), DEFVAL(tweens::DEFAULT_BLEND), DEFVAL(0.5));
}

bool tweens::is_known_ease(int64_t p_ease) {
	if (p_ease != 0 && (p_ease & ~COMPOSITION_MASK) == 0) {
		return single_or_missing(leg_bits(p_ease, false)) && single_or_missing(leg_bits(p_ease, true));
	}
	using Ease = TweensGdEasing::Ease;
	if (p_ease == Ease::LINEAR || p_ease == Ease::SMOOTH_STEP || p_ease == Ease::SMOOTHER_STEP) {
		return true;
	}
	return p_ease >= Ease::SINE_IN && p_ease <= Ease::BOUNCE_IN_OUT && p_ease % 10 <= 2;
}

double TweensGdEasing::evaluate(int64_t p_ease, double p_progress, int64_t p_blend_type, double p_blend, double p_skew) {
	if (p_blend_type < BLEND_MAKIMA || p_blend_type > BLEND_LINEAR || !std::isfinite(p_blend) || p_blend < 0.0 || p_blend > 1.0 || !std::isfinite(p_skew) || p_skew < 0.0 || p_skew > 1.0) return Math::NaN;
	using std::cos;
	using std::pow;
	using std::sin;
	using std::sqrt;
	// NaN progress stays detectable; infinities clamp like any other out-of-range progress.
	if (Math::is_nan(p_progress)) {
		return Math::NaN;
	}
	const double t = CLAMP(p_progress, 0.0, 1.0);
	if (p_ease != 0 && (p_ease & ~COMPOSITION_MASK) == 0) {
		if (!tweens::is_known_ease(p_ease)) return Math::NaN;
		const int64_t entry = leg_bits(p_ease, false), exit = leg_bits(p_ease, true);
		if (entry == 0) return family_leg(leg_slot(exit), true, t);
		if (exit == 0) return family_leg(leg_slot(entry), false, t);
		const int in_pair = leg_slot(entry), out_pair = leg_slot(exit);
		if (p_skew == 0.0) return family_leg(out_pair, true, t);
        if (p_skew == 1.0) return family_leg(in_pair, false, t);
        const double h = p_blend * MIN(p_skew,1.0-p_skew), left = p_skew - h, right = p_skew + h;
		if (entry == exit || t <= left) return split_profile(in_pair, t, p_skew);
		if (t >= right) return split_profile(out_pair, t, p_skew);
		if (p_blend_type == BLEND_SMOOTH_STEP || p_blend_type == BLEND_LINEAR) {
			const double u = (t - left) / (2.0*h);
			const double w = p_blend_type == BLEND_SMOOTH_STEP ? u*u*(3.0 - 2.0*u) : u;
			return split_profile(in_pair, t, p_skew)*(1.0-w) + split_profile(out_pair, t, p_skew)*w;
		}
		const double y0 = split_profile(in_pair, left, p_skew), y1 = split_profile(out_pair, right, p_skew);
		const double v0 = split_slope(in_pair, left, p_skew), v1 = split_slope(out_pair, right, p_skew);
		const double d0 = (p_skew-y0)/h, d1 = (y1-p_skew)/h;
		// Hermite's shared midpoint tangent solves equal acceleration, limited against new reversals.
		const double middle = p_blend_type == BLEND_MAKIMA ? makima_slope(v0, d0, d1, v1)
			: CLAMP((3.0*(d0+d1)-v0-v1)/4.0, 0.0, 3.0*MAX(0.0, MIN(d0,d1)));
		return t <= p_skew ? hermite((t-left)/h, y0, p_skew, h*v0, h*middle)
			: hermite((t-p_skew)/h, p_skew, y1, h*middle, h*v1);
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
