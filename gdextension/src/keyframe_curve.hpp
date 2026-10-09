// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/variant/array.hpp>
#include <godot_cpp/variant/packed_float64_array.hpp>
#include <godot_cpp/variant/packed_int64_array.hpp>
#include <vector>

namespace godot {

// Immutable prepared keys, shared by definitions and independent plays. Captured starts make a private curve.
class TweensGdKeyframeCurve : public RefCounted {
	GDCLASS(TweensGdKeyframeCurve, RefCounted)
	struct Key {
		double at = 0;
		Variant value;
		double c[4] = {}, m[4] = {};
		int64_t mode = -1, ease = 0;
	};
	std::vector<Key> keys;
	Variant::Type type = Variant::NIL;
	int dimensions = 1;
	int64_t mode = 0, color_space = 0, alpha_mode = 0, color_encoding = 0;
	String error;
	void prepare();
	void tangents();
	Variant decode(const double *p_value) const;

protected:
	static void _bind_methods();

public:
	static Ref<TweensGdKeyframeCurve> create(const Array &p_values, const PackedFloat64Array &p_stops,
			int64_t p_interpolation = 0, const PackedInt64Array &p_modes = PackedInt64Array(),
			const PackedInt64Array &p_eases = PackedInt64Array(), int64_t p_space = 0, int64_t p_alpha = 0, int64_t p_encoding = 0);
	String get_error() const { return error; }
	bool needs_start() const { return !keys.empty() && keys[0].at != 0; }
	Ref<TweensGdKeyframeCurve> capture_start(const Variant &p_initial);
	Variant sample(double p_progress) const;
};
} // namespace godot
