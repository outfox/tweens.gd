// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "definition.hpp"

#include "easing.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>

namespace godot {

using namespace tweens;

namespace {

bool is_null(const Variant &p_value) {
	return p_value.get_type() == Variant::NIL;
}

bool invalid_callable(const Callable &p_callable) {
	return !p_callable.is_null() && !p_callable.is_valid();
}

// Resource.duplicate() crashes after a same-process 2dog 4.7.2.91 restart, so copy the public data explicitly.
Ref<Curve> copy_curve(const Ref<Curve> &p_curve) {
	Ref<Curve> result;
	result.instantiate();
	result->set_min_domain(MIN(p_curve->get_min_domain(), 0.0));
	result->set_max_domain(MAX(p_curve->get_max_domain(), 1.0));
	result->set_min_domain(p_curve->get_min_domain());
	result->set_max_domain(p_curve->get_max_domain());
	result->set_min_value(MIN(p_curve->get_min_value(), 0.0));
	result->set_max_value(MAX(p_curve->get_max_value(), 1.0));
	result->set_min_value(p_curve->get_min_value());
	result->set_max_value(p_curve->get_max_value());
	result->set_bake_resolution(p_curve->get_bake_resolution());
	for (int32_t index = 0; index < p_curve->get_point_count(); index++) {
		result->add_point(p_curve->get_point_position(index), p_curve->get_point_left_tangent(index),
				p_curve->get_point_right_tangent(index), p_curve->get_point_left_mode(index), p_curve->get_point_right_mode(index));
	}
	return result;
}

} // namespace

void TweenSettings::set_property(const NodePath &p_property) {
	property = p_property;
	property_path = p_property.get_as_property_path();
}

String TweenSettings::validate() const {
	if (keyframe_curve.is_valid()) {
		if (!keyframe_curve->get_error().is_empty()) return keyframe_curve->get_error();
		if (!is_null(from_value) || !is_null(to_value) || !is_null(by_value) || factor_from != 1 || factor_to != 1 || !is_null(delta_from) || !is_null(delta_to))
			return "Keyframe curves do not accept relative endpoints, endpoint factors or deltas.";
	}
	if (color_space < 0 || color_space > 2 || alpha_mode < 0 || alpha_mode > 1 || color_encoding < 0 || color_encoding > 1) {
		return "Unknown color space, alpha mode or color encoding.";
	}
	if (adapter.is_valid() && !property.is_empty()) {
		return "Choose either an adapter or a property path.";
	}
	if (!is_null(to_value) && !is_null(by_value)) {
		return "Choose either to_value or by_value.";
	}
	if (is_null(by_value) && (factor_by != 1.0 || !is_null(delta_by))) {
		return "factor_by and delta_by need a by_value.";
	}
	if (!is_null(by_value) && (factor_to != 1.0 || !is_null(delta_to))) {
		return "factor_to and delta_to do not apply with by_value.";
	}
	for (const double factor : { factor_from, factor_to, factor_by, factor_duration, delta_duration, factor_delay, delta_delay }) {
		if (!Math::is_finite(factor)) {
			return "Factors and deltas must be finite.";
		}
	}
	const double seconds = effective_duration();
	const double wait = effective_delay();
	for (const double time : { duration, seconds, offset, ping_pong_interval, repeat_interval }) {
		if (!Math::is_finite(time) || time < 0.0) {
			return "Timing must be finite and nonnegative.";
		}
	}
	if (!Math::is_finite(delay) || !Math::is_finite(wait)) {
		return "Delay must be finite.";
	}
	if (offset > seconds) {
		return "Offset must not exceed duration.";
	}
	if (repeats < INFINITE_REPEATS) {
		return "Repeats must be -1 or nonnegative.";
	}
	if (!Math::is_finite(skew) || skew < 0.0 || skew > 1.0) {
		return "Skew must be finite and in [0, 1].";
	}
	if (!Math::is_finite(weks) || weks < 0.0 || weks > 1.0) {
		return "Weks must be finite and in [0, 1].";
	}
	if (blend_type < TweensGdEasing::BLEND_MAKIMA || blend_type > TweensGdEasing::BLEND_LINEAR || !Math::is_finite(blend) || blend < 0.0 || blend > 1.0) {
		return "Invalid easing blend: use a known method and width in [0, 1].";
	}
	if (!is_known_ease(ease)) {
		return "Unknown easing function.";
	}
	if (fill < FILL_NONE || fill > FILL_BOTH) {
		return "Unknown fill flags.";
	}
	if (curve.is_valid() && !ease_function.is_null()) {
		return "Choose either curve or ease_function.";
	}
	for (const Callable *callback : { &ease_function, &on_add, &on_start, &on_update, &on_end, &on_cancel, &on_finally }) {
		if (invalid_callable(*callback)) {
			return "A configured Callable is invalid.";
		}
	}
	const double span = seconds + (ping_pong ? seconds + ping_pong_interval : 0.0) + repeat_interval;
	if (!Math::is_finite(span + wait)) {
		return "Timeline is too long.";
	}
	if (repeats == INFINITE_REPEATS) {
		if (span == 0.0) {
			return "An infinite tween needs a nonzero cycle duration.";
		}
	} else if (!Math::is_finite(span * (double(repeats) + 1.0) - repeat_interval + wait)) {
		return "Timeline is too long.";
	}
	return String();
}

TweenSettings *TweenSettings::snapshot() const {
	TweenSettings *result = memnew(TweenSettings(*this));
	if (adapter.is_valid()) {
		// Scalar adapter configuration is copied by the adapter; captured objects remain shared.
		result->adapter = adapter->call(tweens::names().copy);
	}
	if (curve.is_valid()) {
		result->curve = copy_curve(curve);
	}
	return result;
}

void TweensGdDefinition::_bind_methods() {
	ClassDB::bind_method(D_METHOD("copy"), &TweensGdDefinition::copy);
	ClassDB::bind_method(D_METHOD("through", "curve"), &TweensGdDefinition::through);
	ClassDB::bind_method(D_METHOD("validate"), &TweensGdDefinition::validate);
	ClassDB::bind_static_method("TweensGdDefinition",
			D_METHOD("named", "path", "target_class", "value_type", "to", "seconds", "easing", "delay"), &TweensGdDefinition::named);

#define BIND_SETTING(m_info, m_name) \
	ClassDB::bind_method(D_METHOD("set_" #m_name, "value"), &TweensGdDefinition::set_##m_name); \
	ClassDB::bind_method(D_METHOD("get_" #m_name), &TweensGdDefinition::get_##m_name); \
	ADD_PROPERTY(m_info, "set_" #m_name, "get_" #m_name);
#define VARIANT_INFO(m_name) \
	PropertyInfo(Variant::NIL, #m_name, PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT | PROPERTY_USAGE_NIL_IS_VARIANT)

	BIND_SETTING(PropertyInfo(Variant::NODE_PATH, "property"), property);
	BIND_SETTING(PropertyInfo(Variant::OBJECT, "adapter", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT, "RefCounted"), adapter);
	BIND_SETTING(PropertyInfo(Variant::STRING_NAME, "target_class"), target_class);
	BIND_SETTING(PropertyInfo(Variant::INT, "value_type"), value_type);
	BIND_SETTING(VARIANT_INFO(from_value), from_value);
	BIND_SETTING(VARIANT_INFO(to_value), to_value);
	BIND_SETTING(VARIANT_INFO(by_value), by_value);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "factor_from"), factor_from);
	BIND_SETTING(VARIANT_INFO(delta_from), delta_from);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "factor_to"), factor_to);
	BIND_SETTING(VARIANT_INFO(delta_to), delta_to);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "factor_by"), factor_by);
	BIND_SETTING(VARIANT_INFO(delta_by), delta_by);
	BIND_SETTING(VARIANT_INFO(initial_value), initial_value);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "duration"), duration);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "factor_duration"), factor_duration);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "delta_duration"), delta_duration);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "delay"), delay);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "factor_delay"), factor_delay);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "delta_delay"), delta_delay);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "offset"), offset);
	BIND_SETTING(PropertyInfo(Variant::INT, "repeats"), repeats);
	BIND_SETTING(PropertyInfo(Variant::BOOL, "ping_pong"), ping_pong);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "ping_pong_interval"), ping_pong_interval);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "repeat_interval"), repeat_interval);
	BIND_SETTING(PropertyInfo(Variant::INT, "fill"), fill);
	BIND_SETTING(PropertyInfo(Variant::INT, "ease"), ease);
	BIND_SETTING(PropertyInfo(Variant::INT, "color_space"), color_space);
	BIND_SETTING(PropertyInfo(Variant::INT, "alpha_mode"), alpha_mode);
	BIND_SETTING(PropertyInfo(Variant::INT, "color_encoding"), color_encoding);
	BIND_SETTING(PropertyInfo(Variant::INT, "blend_type"), blend_type);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "blend"), blend);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "skew"), skew);
	BIND_SETTING(PropertyInfo(Variant::FLOAT, "weks"), weks);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "ease_function"), ease_function);
	BIND_SETTING(PropertyInfo(Variant::OBJECT, "curve", PROPERTY_HINT_RESOURCE_TYPE, "Curve"), curve);
	BIND_SETTING(PropertyInfo(Variant::OBJECT, "keyframe_curve", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT, "TweensGdKeyframeCurve"), keyframe_curve);
	BIND_SETTING(PropertyInfo(Variant::BOOL, "suppress_callbacks_when_target_invalid"), suppress_callbacks_when_target_invalid);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_add"), on_add);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_start"), on_start);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_update"), on_update);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_end"), on_end);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_cancel"), on_cancel);
	BIND_SETTING(PropertyInfo(Variant::CALLABLE, "on_finally"), on_finally);
#undef VARIANT_INFO
#undef BIND_SETTING

	ClassDB::bind_method(D_METHOD("with_from", "value"), &TweensGdDefinition::with_from);
	ClassDB::bind_method(D_METHOD("with_to", "value"), &TweensGdDefinition::with_to);
	ClassDB::bind_method(D_METHOD("with_by", "value"), &TweensGdDefinition::with_by);
	ClassDB::bind_method(D_METHOD("with_initial_value", "value"), &TweensGdDefinition::with_initial_value);
	ClassDB::bind_method(D_METHOD("with_factor_from", "factor"), &TweensGdDefinition::with_factor_from);
	ClassDB::bind_method(D_METHOD("with_delta_from", "value"), &TweensGdDefinition::with_delta_from);
	ClassDB::bind_method(D_METHOD("with_factor_to", "factor"), &TweensGdDefinition::with_factor_to);
	ClassDB::bind_method(D_METHOD("with_delta_to", "value"), &TweensGdDefinition::with_delta_to);
	ClassDB::bind_method(D_METHOD("with_factor_by", "factor"), &TweensGdDefinition::with_factor_by);
	ClassDB::bind_method(D_METHOD("with_delta_by", "value"), &TweensGdDefinition::with_delta_by);
	ClassDB::bind_method(D_METHOD("with_duration", "seconds"), &TweensGdDefinition::with_duration);
	ClassDB::bind_method(D_METHOD("with_factor_duration", "factor"), &TweensGdDefinition::with_factor_duration);
	ClassDB::bind_method(D_METHOD("with_delta_duration", "seconds"), &TweensGdDefinition::with_delta_duration);
	ClassDB::bind_method(D_METHOD("with_delay", "seconds"), &TweensGdDefinition::with_delay);
	ClassDB::bind_method(D_METHOD("with_factor_delay", "factor"), &TweensGdDefinition::with_factor_delay);
	ClassDB::bind_method(D_METHOD("with_delta_delay", "seconds"), &TweensGdDefinition::with_delta_delay);
	ClassDB::bind_method(D_METHOD("with_offset", "seconds"), &TweensGdDefinition::with_offset);
	ClassDB::bind_method(D_METHOD("with_repeats", "count"), &TweensGdDefinition::with_repeats);
	ClassDB::bind_method(D_METHOD("with_ping_pong", "enabled"), &TweensGdDefinition::with_ping_pong, DEFVAL(true));
	ClassDB::bind_method(D_METHOD("with_ping_pong_interval", "seconds"), &TweensGdDefinition::with_ping_pong_interval);
	ClassDB::bind_method(D_METHOD("with_repeat_interval", "seconds"), &TweensGdDefinition::with_repeat_interval);
	ClassDB::bind_method(D_METHOD("with_fill", "mode"), &TweensGdDefinition::with_fill);
	ClassDB::bind_method(D_METHOD("with_ease", "easing"), &TweensGdDefinition::with_ease);
	ClassDB::bind_method(D_METHOD("with_color_space", "space"), &TweensGdDefinition::with_color_space);
	ClassDB::bind_method(D_METHOD("with_alpha_mode", "mode"), &TweensGdDefinition::with_alpha_mode);
	ClassDB::bind_method(D_METHOD("with_color_encoding", "encoding"), &TweensGdDefinition::with_color_encoding);
	ClassDB::bind_method(D_METHOD("with_blend_type", "mode"), &TweensGdDefinition::with_blend_type);
	ClassDB::bind_method(D_METHOD("with_blend", "blend"), &TweensGdDefinition::with_blend);
	ClassDB::bind_method(D_METHOD("with_skew", "split"), &TweensGdDefinition::with_skew);
	ClassDB::bind_method(D_METHOD("with_weks", "split"), &TweensGdDefinition::with_weks);
	ClassDB::bind_method(D_METHOD("with_ease_function", "function"), &TweensGdDefinition::with_ease_function);
	ClassDB::bind_method(D_METHOD("with_curve", "shape"), &TweensGdDefinition::with_curve);
	ClassDB::bind_method(D_METHOD("with_suppress_callbacks_when_target_invalid", "enabled"),
			&TweensGdDefinition::with_suppress_callbacks_when_target_invalid, DEFVAL(true));
	ClassDB::bind_method(D_METHOD("with_on_add", "callback"), &TweensGdDefinition::with_on_add);
	ClassDB::bind_method(D_METHOD("with_on_start", "callback"), &TweensGdDefinition::with_on_start);
	ClassDB::bind_method(D_METHOD("with_on_update", "callback"), &TweensGdDefinition::with_on_update);
	ClassDB::bind_method(D_METHOD("with_on_end", "callback"), &TweensGdDefinition::with_on_end);
	ClassDB::bind_method(D_METHOD("with_on_cancel", "callback"), &TweensGdDefinition::with_on_cancel);
	ClassDB::bind_method(D_METHOD("with_on_finally", "callback"), &TweensGdDefinition::with_on_finally);
}

Ref<TweensGdDefinition> TweensGdDefinition::copy() const {
	Ref<TweensGdDefinition> result;
	result.instantiate();
	TweenSettings *copied = settings.snapshot();
	result->settings = std::move(*copied);
	memdelete(copied);
	return result;
}

Ref<TweensGdDefinition> TweensGdDefinition::through(const Ref<TweensGdKeyframeCurve> &p_curve) const {
	if (p_curve.is_null()) return Ref<TweensGdDefinition>();
	Ref<TweensGdDefinition> result = copy();
	result->settings.from_value = Variant();
	result->settings.to_value = Variant();
	result->settings.keyframe_curve = p_curve;
	return result;
}

String TweensGdDefinition::validate() const {
	return settings.validate();
}

Ref<TweensGdDefinition> TweensGdDefinition::named(const NodePath &p_path, const StringName &p_target_class, int64_t p_value_type,
		const Variant &p_to, double p_seconds, int64_t p_ease, double p_delay) {
	Ref<TweensGdDefinition> result;
	result.instantiate();
	TweenSettings &target = result->settings;
	target.set_property(p_path);
	target.target_class = p_target_class;
	target.value_type = p_value_type;
	target.to_value = p_to;
	target.duration = p_seconds;
	target.ease = p_ease;
	target.delay = p_delay;
	return result;
}

#define DEFINE_WITH(m_method, m_type, m_field) \
	Ref<TweensGdDefinition> TweensGdDefinition::m_method(m_type p_value) const { \
		Ref<TweensGdDefinition> result = copy(); \
		result->settings.m_field = p_value; \
		return result; \
	}

DEFINE_WITH(with_from, const Variant &, from_value)
DEFINE_WITH(with_to, const Variant &, to_value)
DEFINE_WITH(with_by, const Variant &, by_value)
DEFINE_WITH(with_initial_value, const Variant &, initial_value)
DEFINE_WITH(with_factor_from, double, factor_from)
DEFINE_WITH(with_delta_from, const Variant &, delta_from)
DEFINE_WITH(with_factor_to, double, factor_to)
DEFINE_WITH(with_delta_to, const Variant &, delta_to)
DEFINE_WITH(with_factor_by, double, factor_by)
DEFINE_WITH(with_delta_by, const Variant &, delta_by)
DEFINE_WITH(with_duration, double, duration)
DEFINE_WITH(with_factor_duration, double, factor_duration)
DEFINE_WITH(with_delta_duration, double, delta_duration)
DEFINE_WITH(with_delay, double, delay)
DEFINE_WITH(with_factor_delay, double, factor_delay)
DEFINE_WITH(with_delta_delay, double, delta_delay)
DEFINE_WITH(with_offset, double, offset)
DEFINE_WITH(with_repeats, int64_t, repeats)
DEFINE_WITH(with_ping_pong, bool, ping_pong)
DEFINE_WITH(with_ping_pong_interval, double, ping_pong_interval)
DEFINE_WITH(with_repeat_interval, double, repeat_interval)
DEFINE_WITH(with_fill, int64_t, fill)
DEFINE_WITH(with_ease, int64_t, ease)
DEFINE_WITH(with_color_space, int64_t, color_space)
DEFINE_WITH(with_alpha_mode, int64_t, alpha_mode)
DEFINE_WITH(with_color_encoding, int64_t, color_encoding)
DEFINE_WITH(with_blend_type, int64_t, blend_type)
DEFINE_WITH(with_blend, double, blend)
DEFINE_WITH(with_skew, double, skew)
DEFINE_WITH(with_weks, double, weks)
DEFINE_WITH(with_ease_function, const Callable &, ease_function)
DEFINE_WITH(with_curve, const Ref<Curve> &, curve)
DEFINE_WITH(with_suppress_callbacks_when_target_invalid, bool, suppress_callbacks_when_target_invalid)
DEFINE_WITH(with_on_add, const Callable &, on_add)
DEFINE_WITH(with_on_start, const Callable &, on_start)
DEFINE_WITH(with_on_update, const Callable &, on_update)
DEFINE_WITH(with_on_end, const Callable &, on_end)
DEFINE_WITH(with_on_cancel, const Callable &, on_cancel)
DEFINE_WITH(with_on_finally, const Callable &, on_finally)

#undef DEFINE_WITH

} // namespace godot
