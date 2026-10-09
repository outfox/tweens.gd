// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "keyframe_curve.hpp"
#include "color_interpolation.hpp"
#include "interpolation.hpp"
#include "easing.hpp"
#include <godot_cpp/core/class_db.hpp>
#include <algorithm>
#include <cmath>
#include <limits>

namespace godot {

namespace {
double finite_slope(double value) {
	return std::clamp(value, -std::numeric_limits<double>::max(), std::numeric_limits<double>::max());
}
double slope(double from, double to, double width) {
	const double difference = to - from;
	return finite_slope(std::isfinite(difference) ? difference / width : to / width - from / width);
}
} // namespace

void TweensGdKeyframeCurve::_bind_methods() {
	ClassDB::bind_static_method("TweensGdKeyframeCurve", D_METHOD("create", "values", "stops", "interpolation", "modes", "eases", "color_space", "alpha_mode", "color_encoding"),
			&TweensGdKeyframeCurve::create, DEFVAL(0), DEFVAL(PackedInt64Array()), DEFVAL(PackedInt64Array()), DEFVAL(0), DEFVAL(0), DEFVAL(0));
	ClassDB::bind_method(D_METHOD("capture_start", "initial"), &TweensGdKeyframeCurve::capture_start);
	ClassDB::bind_method(D_METHOD("sample", "progress"), &TweensGdKeyframeCurve::sample);
	ClassDB::bind_method(D_METHOD("get_error"), &TweensGdKeyframeCurve::get_error);
	ClassDB::bind_method(D_METHOD("needs_start"), &TweensGdKeyframeCurve::needs_start);
	ADD_PROPERTY(PropertyInfo(Variant::STRING, "error"), "", "get_error");
}

Ref<TweensGdKeyframeCurve> TweensGdKeyframeCurve::create(const Array &p_values, const PackedFloat64Array &p_stops,
		int64_t p_interpolation, const PackedInt64Array &p_modes, const PackedInt64Array &p_eases,
		int64_t p_space, int64_t p_alpha, int64_t p_encoding) {
	Ref<TweensGdKeyframeCurve> result;
	result.instantiate();
	auto &curve = *result.ptr();
	curve.mode = p_interpolation; curve.color_space = p_space; curve.alpha_mode = p_alpha; curve.color_encoding = p_encoding;
	if (p_values.is_empty() || p_values.size() != p_stops.size() || (!p_modes.is_empty() && p_modes.size() != p_values.size())
			|| (!p_eases.is_empty() && p_eases.size() != p_values.size())) {
		curve.error = "A curve needs equally sized nonempty values and stops; modes and eases must match when supplied.";
		return result;
	}
	if (p_interpolation < 0 || p_interpolation > 2 || p_space < 0 || p_space > 2 || p_alpha < 0 || p_alpha > 1 || p_encoding < 0 || p_encoding > 1) {
		curve.error = "Unknown curve interpolation or color policy.";
		return result;
	}
	for (int64_t i = 0; i < p_values.size(); i++) {
		Key key;
		key.at = p_stops[i]; key.value = p_values[i];
		key.mode = p_modes.is_empty() ? -1 : p_modes[i];
		key.ease = p_eases.is_empty() ? 0 : p_eases[i];
		if (!std::isfinite(key.at) || key.at < 0 || key.at > 100 || (i > 0 && key.at <= p_stops[i - 1])
				|| key.mode < -1 || key.mode > 3 || (key.at == 0 && key.mode != -1) || (key.mode == 3 && !tweens::is_known_ease(key.ease))) {
			curve.error = "Invalid or duplicate key stop or arriving interpolation.";
			return result;
		}
		curve.keys.push_back(key);
	}
	if (curve.keys.back().at < 100) {
		Key hold = curve.keys.back(); hold.at = 100; hold.mode = 2;
		curve.keys.push_back(hold);
	}
	curve.type = curve.keys[0].value.get_type();
	curve.prepare();
	return result;
}

void TweensGdKeyframeCurve::prepare() {
	dimensions = type == Variant::VECTOR2 ? 2 : type == Variant::VECTOR3 ? 3
			: type == Variant::VECTOR4 || type == Variant::RECT2 || type == Variant::COLOR || type == Variant::QUATERNION ? 4 : 1;
	for (auto &key : keys) {
		if (!TweensGdInterpolation::supported(key.value) || !TweensGdInterpolation::finite(key.value)
				|| !TweensGdInterpolation::compatible(keys[0].value, key.value)) {
			error = "Keyframe values must be finite, supported and of compatible types."; return;
		}
		switch (type) {
			case Variant::FLOAT: case Variant::INT: key.c[0] = double(key.value); break;
			case Variant::VECTOR2: { const Vector2 v = key.value; key.c[0] = v.x; key.c[1] = v.y; break; }
			case Variant::VECTOR3: { const Vector3 v = key.value; for (int c = 0; c < 3; c++) key.c[c] = v[c]; break; }
			case Variant::VECTOR4: { const Vector4 v = key.value; for (int c = 0; c < 4; c++) key.c[c] = v[c]; break; }
			case Variant::RECT2: { const Rect2 v = key.value; key.c[0] = v.position.x; key.c[1] = v.position.y; key.c[2] = v.size.x; key.c[3] = v.size.y; break; }
			case Variant::COLOR: { const Vector4 v = encode_color(key.value, color_space, alpha_mode, color_encoding); for (int c = 0; c < 4; c++) key.c[c] = v[c]; break; }
			case Variant::QUATERNION: {
				const Quaternion v = key.value;
				if (v.length_squared() == 0 || !std::isfinite(v.length_squared())) {
					error = "Quaternion keys must have finite, nonzero squared length."; return;
				}
				key.value = v.normalized(); break;
			}
			default: break;
		}
		for (int c = 0; c < dimensions; c++) {
			if (!std::isfinite(key.c[c])) { error = "Prepared keyframe components must be finite."; return; }
		}
	}
	tangents();
}

void TweensGdKeyframeCurve::tangents() {
	const size_t n = keys.size();
	if (n < 2 || type == Variant::QUATERNION) return;
	std::vector<double> d(n + 3);
	for (int c = 0; c < dimensions; c++) {
		for (size_t i = 0; i < n - 1; i++) d[i + 2] = slope(keys[i].c[c], keys[i + 1].c[c], keys[i + 1].at - keys[i].at);
		d[1] = n == 2 ? d[2] : finite_slope(2 * d[2] - d[3]); d[0] = finite_slope(2 * d[1] - d[2]);
		d[n + 1] = n == 2 ? d[2] : finite_slope(2 * d[n] - d[n - 1]); d[n + 2] = finite_slope(2 * d[n + 1] - d[n]);
		for (size_t i = 0; i < n; i++) {
			const double scale = std::max(1.0, std::max(std::max(std::abs(d[i]), std::abs(d[i + 1])), std::max(std::abs(d[i + 2]), std::abs(d[i + 3]))));
			const double left = std::abs(d[i + 1] / scale - d[i] / scale) + 0.5 * std::abs(d[i + 1] / scale + d[i] / scale);
			const double right = std::abs(d[i + 3] / scale - d[i + 2] / scale) + 0.5 * std::abs(d[i + 3] / scale + d[i + 2] / scale);
			const double weight = left + right == 0 ? 0 : left / (left + right);
			double m = d[i + 1] * (1 - weight) + d[i + 2] * weight;
			if (i > 0 && i < n - 1 && ((d[i + 1] > 0) - (d[i + 1] < 0)) != ((d[i + 2] > 0) - (d[i + 2] < 0))) m = 0;
			keys[i].m[c] = m;
		}
		for (size_t i = 0; i < n - 1; i++) {
			const double delta = d[i + 2];
			const int sign = (delta > 0) - (delta < 0);
			double a = std::max(0.0, sign * keys[i].m[c]);
			double b = std::max(0.0, sign * keys[i + 1].m[c]);
			const double largest = std::max(std::numeric_limits<double>::denorm_min(), std::max(a, b));
			const double x = a / largest, y = b / largest;
			const double length = std::sqrt(x * x + y * y);
			if (largest / 3 > std::abs(delta) / length) {
				a = (3 * x / length) * std::abs(delta); b = (3 * y / length) * std::abs(delta);
			}
			keys[i].m[c] = sign * a; keys[i + 1].m[c] = sign * b;
		}
	}
}

Ref<TweensGdKeyframeCurve> TweensGdKeyframeCurve::capture_start(const Variant &p_initial) {
	if (!needs_start() || !error.is_empty()) return Ref<TweensGdKeyframeCurve>(this);
	Ref<TweensGdKeyframeCurve> result;
	result.instantiate();
	auto &curve = *result.ptr();
	curve.keys = keys; curve.type = type; curve.mode = mode;
	curve.color_space = color_space; curve.alpha_mode = alpha_mode; curve.color_encoding = color_encoding;
	Key start; start.value = p_initial;
	curve.keys.insert(curve.keys.begin(), start);
	curve.prepare();
	return result;
}

Variant TweensGdKeyframeCurve::decode(const double *v) const {
	switch (type) {
		case Variant::FLOAT: return v[0];
		case Variant::INT: {
			const double rounded = std::round(v[0]);
			return rounded >= 9223372036854775807.0 ? INT64_MAX : rounded <= -9223372036854775808.0 ? INT64_MIN : int64_t(rounded);
		}
		case Variant::VECTOR2: return Vector2(v[0], v[1]);
		case Variant::VECTOR3: return Vector3(v[0], v[1], v[2]);
		case Variant::VECTOR4: return Vector4(v[0], v[1], v[2], v[3]);
		case Variant::RECT2: return Rect2(v[0], v[1], v[2], v[3]);
		case Variant::COLOR: return decode_color(Vector4(v[0], v[1], v[2], v[3]), color_space, alpha_mode, color_encoding);
		default: return Variant();
	}
}

Variant TweensGdKeyframeCurve::sample(double p_progress) const {
	if (!error.is_empty() || needs_start() || keys.empty() || !std::isfinite(p_progress)) return Variant();
	const double stop = p_progress * 100;
	const auto found = std::lower_bound(keys.begin(), keys.end(), p_progress, [](const Key &key, double progress) { return key.at / 100 < progress; });
	if (found != keys.end() && found->at / 100 == p_progress) return found->value;
	const size_t i = found == keys.begin() ? 0 : found == keys.end() ? keys.size() - 2 : size_t(found - keys.begin() - 1);
	const auto &a = keys[i]; const auto &b = keys[i + 1];
	const double width = b.at - a.at;
	double u = (stop - a.at) / width;
	const int64_t segment_mode = b.mode == -1 ? mode : b.mode;
	const auto ease = [&b](double t) { return TweensGdEasing::evaluate(b.ease, t); };
	if (type == Variant::QUATERNION) {
		double weight = u;
		if (segment_mode == 2) weight = u < 1 ? 0 : 1;
		else if (segment_mode == 3) weight = u < 0 ? u * (ease(0.0001) - ease(0)) / 0.0001
				: u > 1 ? 1 + (u - 1) * (ease(1) - ease(0.9999)) / 0.0001 : ease(u);
		return Quaternion(a.value).slerp(Quaternion(b.value), weight).normalized();
	}
	double value[4] = {};
	if (segment_mode == 3 && stop >= 0 && stop <= 100) u = ease(u);
	for (int c = 0; c < dimensions; c++) {
		if (stop < 0 || stop > 100) {
			const auto &end = stop < 0 ? keys.front() : keys.back();
			double derivative = segment_mode == 0 ? end.m[c] : segment_mode == 2 ? 0 : slope(a.c[c], b.c[c], width);
			if (segment_mode == 3) derivative *= stop < 0 ? (ease(0.0001) - ease(0)) / 0.0001 : (ease(1) - ease(0.9999)) / 0.0001;
			value[c] = end.c[c] + derivative * (stop - end.at);
		} else if (segment_mode == 2) value[c] = a.c[c];
		else if (segment_mode != 0) value[c] = a.c[c] * (1 - u) + b.c[c] * u;
		else {
			const double u2 = u * u, u3 = u2 * u;
			// Local scaling protects Hermite arithmetic without losing small values in distant segments.
			const double scale = std::max(1.0, std::max(std::max(std::abs(a.c[c]), std::abs(b.c[c])), std::max(std::abs(a.m[c]), std::abs(b.m[c]))));
			const double sampled = a.c[c] / scale * (2 * u3 - 3 * u2 + 1) + a.m[c] / scale * (u3 - 2 * u2 + u) * width
					+ b.c[c] / scale * (-2 * u3 + 3 * u2) + b.m[c] / scale * (u3 - u2) * width;
			value[c] = std::clamp(sampled, std::min(a.c[c] / scale, b.c[c] / scale), std::max(a.c[c] / scale, b.c[c] / scale)) * scale;
		}
	}
	return decode(value);
}
} // namespace godot
