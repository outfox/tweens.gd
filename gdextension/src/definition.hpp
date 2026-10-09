// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "common.hpp"
#include "keyframe_curve.hpp"

#include <godot_cpp/classes/curve.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/node_path.hpp>

namespace godot {

// Plain settings copied into every playback, so later edits never reach running tweens.
struct TweenSettings {
	NodePath property;
	// get_indexed/set_indexed form of property, prepared once instead of on every write.
	NodePath property_path;
	Ref<RefCounted> adapter;
	StringName target_class;
	int64_t value_type = Variant::NIL;
	Variant from_value;
	Variant to_value;
	Variant by_value;
	double factor_from = 1.0;
	Variant delta_from;
	double factor_to = 1.0;
	Variant delta_to;
	double factor_by = 1.0;
	Variant delta_by;
	Variant initial_value = 0.0;
	double duration = 0.0;
	double factor_duration = 1.0;
	double delta_duration = 0.0;
	double delay = 0.0;
	double factor_delay = 1.0;
	double delta_delay = 0.0;
	double offset = 0.0;
	int64_t repeats = 0;
	bool ping_pong = false;
	double ping_pong_interval = 0.0;
	double repeat_interval = 0.0;
	int64_t fill = tweens::FILL_RETAIN_FINAL_VALUE;
	int64_t ease = 0;
	int64_t color_space = 0;
	int64_t alpha_mode = 0;
	int64_t color_encoding = 0;
	int64_t blend_type = 0;
	double blend = tweens::DEFAULT_BLEND;
	double skew = 0.5;
	double weks = 0.5;
	Callable ease_function;
	Ref<Curve> curve;
	Ref<TweensGdKeyframeCurve> keyframe_curve;
	bool suppress_callbacks_when_target_invalid = false;
	Callable on_add;
	Callable on_start;
	Callable on_update;
	Callable on_end;
	Callable on_cancel;
	Callable on_finally;

	double effective_duration() const { return factor_duration * duration + delta_duration; }
	double effective_delay() const { return factor_delay * delay + delta_delay; }
	void set_property(const NodePath &p_property);
	// Returns an empty string on success.
	String validate() const;
	// The playback copy: its own adapter and curve, shared Callables and values. Free it with memdelete().
	TweenSettings *snapshot() const;
};

// Reusable configuration. Each start copies it; null endpoints use the captured value.
class TweensGdDefinition : public RefCounted {
	GDCLASS(TweensGdDefinition, RefCounted)

	TweenSettings settings;

protected:
	static void _bind_methods();

public:
	const TweenSettings &get_settings() const { return settings; }

	Ref<TweensGdDefinition> copy() const;
	String validate() const;
	static Ref<TweensGdDefinition> named(const NodePath &p_path, const StringName &p_target_class, int64_t p_value_type,
			const Variant &p_to, double p_seconds, int64_t p_ease, double p_delay);

	void set_property(const NodePath &p_value) { settings.set_property(p_value); }
	NodePath get_property() const { return settings.property; }
	void set_adapter(const Ref<RefCounted> &p_value) { settings.adapter = p_value; }
	Ref<RefCounted> get_adapter() const { return settings.adapter; }
	void set_target_class(const StringName &p_value) { settings.target_class = p_value; }
	StringName get_target_class() const { return settings.target_class; }
	void set_value_type(int64_t p_value) { settings.value_type = p_value; }
	int64_t get_value_type() const { return settings.value_type; }
	void set_from_value(const Variant &p_value) { settings.from_value = p_value; }
	Variant get_from_value() const { return settings.from_value; }
	void set_to_value(const Variant &p_value) { settings.to_value = p_value; }
	Variant get_to_value() const { return settings.to_value; }
	void set_by_value(const Variant &p_value) { settings.by_value = p_value; }
	Variant get_by_value() const { return settings.by_value; }
	void set_factor_from(double p_value) { settings.factor_from = p_value; }
	double get_factor_from() const { return settings.factor_from; }
	void set_delta_from(const Variant &p_value) { settings.delta_from = p_value; }
	Variant get_delta_from() const { return settings.delta_from; }
	void set_factor_to(double p_value) { settings.factor_to = p_value; }
	double get_factor_to() const { return settings.factor_to; }
	void set_delta_to(const Variant &p_value) { settings.delta_to = p_value; }
	Variant get_delta_to() const { return settings.delta_to; }
	void set_factor_by(double p_value) { settings.factor_by = p_value; }
	double get_factor_by() const { return settings.factor_by; }
	void set_delta_by(const Variant &p_value) { settings.delta_by = p_value; }
	Variant get_delta_by() const { return settings.delta_by; }
	void set_initial_value(const Variant &p_value) { settings.initial_value = p_value; }
	Variant get_initial_value() const { return settings.initial_value; }
	void set_duration(double p_value) { settings.duration = p_value; }
	double get_duration() const { return settings.duration; }
	void set_factor_duration(double p_value) { settings.factor_duration = p_value; }
	double get_factor_duration() const { return settings.factor_duration; }
	void set_delta_duration(double p_value) { settings.delta_duration = p_value; }
	double get_delta_duration() const { return settings.delta_duration; }
	void set_delay(double p_value) { settings.delay = p_value; }
	double get_delay() const { return settings.delay; }
	void set_factor_delay(double p_value) { settings.factor_delay = p_value; }
	double get_factor_delay() const { return settings.factor_delay; }
	void set_delta_delay(double p_value) { settings.delta_delay = p_value; }
	double get_delta_delay() const { return settings.delta_delay; }
	void set_offset(double p_value) { settings.offset = p_value; }
	double get_offset() const { return settings.offset; }
	void set_repeats(int64_t p_value) { settings.repeats = p_value; }
	int64_t get_repeats() const { return settings.repeats; }
	void set_ping_pong(bool p_value) { settings.ping_pong = p_value; }
	bool get_ping_pong() const { return settings.ping_pong; }
	void set_ping_pong_interval(double p_value) { settings.ping_pong_interval = p_value; }
	double get_ping_pong_interval() const { return settings.ping_pong_interval; }
	void set_repeat_interval(double p_value) { settings.repeat_interval = p_value; }
	double get_repeat_interval() const { return settings.repeat_interval; }
	void set_fill(int64_t p_value) { settings.fill = p_value; }
	int64_t get_fill() const { return settings.fill; }
	void set_ease(int64_t p_value) { settings.ease = p_value; }
	int64_t get_ease() const { return settings.ease; }
	void set_color_space(int64_t p_value) { settings.color_space = p_value; }
	int64_t get_color_space() const { return settings.color_space; }
	void set_alpha_mode(int64_t p_value) { settings.alpha_mode = p_value; }
	int64_t get_alpha_mode() const { return settings.alpha_mode; }
	void set_color_encoding(int64_t p_value) { settings.color_encoding = p_value; }
	int64_t get_color_encoding() const { return settings.color_encoding; }
	void set_blend_type(int64_t p_value) { settings.blend_type = p_value; }
	int64_t get_blend_type() const { return settings.blend_type; }
	void set_blend(double p_value) { settings.blend = p_value; }
	double get_blend() const { return settings.blend; }
	void set_skew(double p_value) { settings.skew = p_value; }
	double get_skew() const { return settings.skew; }
	void set_weks(double p_value) { settings.weks = p_value; }
	double get_weks() const { return settings.weks; }
	void set_ease_function(const Callable &p_value) { settings.ease_function = p_value; }
	Callable get_ease_function() const { return settings.ease_function; }
	void set_curve(const Ref<Curve> &p_value) { settings.curve = p_value; }
	Ref<Curve> get_curve() const { return settings.curve; }
	void set_keyframe_curve(const Ref<TweensGdKeyframeCurve> &p_value) { settings.keyframe_curve = p_value; }
	Ref<TweensGdKeyframeCurve> get_keyframe_curve() const { return settings.keyframe_curve; }
	void set_suppress_callbacks_when_target_invalid(bool p_value) { settings.suppress_callbacks_when_target_invalid = p_value; }
	bool get_suppress_callbacks_when_target_invalid() const { return settings.suppress_callbacks_when_target_invalid; }
	void set_on_add(const Callable &p_value) { settings.on_add = p_value; }
	Callable get_on_add() const { return settings.on_add; }
	void set_on_start(const Callable &p_value) { settings.on_start = p_value; }
	Callable get_on_start() const { return settings.on_start; }
	void set_on_update(const Callable &p_value) { settings.on_update = p_value; }
	Callable get_on_update() const { return settings.on_update; }
	void set_on_end(const Callable &p_value) { settings.on_end = p_value; }
	Callable get_on_end() const { return settings.on_end; }
	void set_on_cancel(const Callable &p_value) { settings.on_cancel = p_value; }
	Callable get_on_cancel() const { return settings.on_cancel; }
	void set_on_finally(const Callable &p_value) { settings.on_finally = p_value; }
	Callable get_on_finally() const { return settings.on_finally; }

	// Like C#'s `with`: each returns a copy with one setting changed and leaves this definition as it is.
	Ref<TweensGdDefinition> with_from(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_to(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_by(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_initial_value(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_factor_from(double p_factor) const;
	Ref<TweensGdDefinition> with_delta_from(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_factor_to(double p_factor) const;
	Ref<TweensGdDefinition> with_delta_to(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_factor_by(double p_factor) const;
	Ref<TweensGdDefinition> with_delta_by(const Variant &p_value) const;
	Ref<TweensGdDefinition> with_duration(double p_seconds) const;
	Ref<TweensGdDefinition> with_factor_duration(double p_factor) const;
	Ref<TweensGdDefinition> with_delta_duration(double p_seconds) const;
	Ref<TweensGdDefinition> with_delay(double p_seconds) const;
	Ref<TweensGdDefinition> with_factor_delay(double p_factor) const;
	Ref<TweensGdDefinition> with_delta_delay(double p_seconds) const;
	Ref<TweensGdDefinition> with_offset(double p_seconds) const;
	Ref<TweensGdDefinition> with_repeats(int64_t p_count) const;
	Ref<TweensGdDefinition> with_ping_pong(bool p_enabled) const;
	Ref<TweensGdDefinition> with_ping_pong_interval(double p_seconds) const;
	Ref<TweensGdDefinition> with_repeat_interval(double p_seconds) const;
	Ref<TweensGdDefinition> with_fill(int64_t p_mode) const;
	Ref<TweensGdDefinition> with_ease(int64_t p_easing) const;
	Ref<TweensGdDefinition> with_color_space(int64_t p_space) const;
	Ref<TweensGdDefinition> with_alpha_mode(int64_t p_mode) const;
	Ref<TweensGdDefinition> with_color_encoding(int64_t p_encoding) const;
	Ref<TweensGdDefinition> with_blend_type(int64_t p_mode) const;
	Ref<TweensGdDefinition> with_blend(double p_blend) const;
	Ref<TweensGdDefinition> with_skew(double p_split) const;
	Ref<TweensGdDefinition> with_weks(double p_split) const;
	Ref<TweensGdDefinition> with_ease_function(const Callable &p_function) const;
	Ref<TweensGdDefinition> with_curve(const Ref<Curve> &p_shape) const;
	Ref<TweensGdDefinition> with_suppress_callbacks_when_target_invalid(bool p_enabled) const;
	Ref<TweensGdDefinition> with_on_add(const Callable &p_callback) const;
	Ref<TweensGdDefinition> with_on_start(const Callable &p_callback) const;
	Ref<TweensGdDefinition> with_on_update(const Callable &p_callback) const;
	Ref<TweensGdDefinition> with_on_end(const Callable &p_callback) const;
	Ref<TweensGdDefinition> with_on_cancel(const Callable &p_callback) const;
	Ref<TweensGdDefinition> with_on_finally(const Callable &p_callback) const;
};

} // namespace godot
