// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/node_path.hpp>

namespace godot {

// Value rules shared by playback, adapters and the start validation.
class TweensGdInterpolation : public RefCounted {
	GDCLASS(TweensGdInterpolation, RefCounted)

protected:
	static void _bind_methods();

public:
	static bool supported(const Variant &p_value);
	static bool compatible(const Variant &p_initial, const Variant &p_endpoint);
	static bool finite(const Variant &p_value);
	// Arrays of numbers stand in for vectors and colors: [x, y], [r, g, b] or [r, g, b, a]. Anything else is kept.
	static Variant coerce(const Variant &p_value, Variant::Type p_type);
	static Variant interpolate(const Variant &p_from, const Variant &p_to, double p_weight, int64_t p_value_type);
	// The offset that changes nothing, or null if by_value does not support the type.
	static Variant zero(int64_t p_value_type);
	// Quaternion offsets rotate about the value's own (local) axes.
	static Variant add(const Variant &p_value, const Variant &p_offset);
	static Variant remove(const Variant &p_value, const Variant &p_offset);
	// Paths reach properties on the target and value components, never another Object.
	static Variant read_property(Object *p_target, const NodePath &p_path);

	static void clear_caches();
};

// Endpoints kept as native values, so playback without an adapter samples without Variant conversions.
// TweensGdInterpolation::interpolate() samples through it too, so both paths share one result.
class TypedLerp {
	Variant::Type type = Variant::NIL;
	bool exact_from = false;
	bool exact_to = false;
	int64_t from_integer = 0;
	int64_t to_integer = 0;
	double from_scalar = 0.0;
	double to_scalar = 0.0;
	// Vector2/3/4, Color and Rect2 (position, size) components in declaration order.
	real_t from_components[4] = {};
	real_t to_components[4] = {};
	Quaternion from_rotation;
	Quaternion to_rotation;

public:
	bool prepare(const Variant &p_from, const Variant &p_to, Variant::Type p_type);
	// Returns false when the sample is not finite.
	bool sample(double p_weight, Variant &r_value) const;
};

} // namespace godot
