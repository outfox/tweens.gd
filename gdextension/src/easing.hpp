// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// SPDX-FileCopyrightText: 2020 Jeffrey Lanters
// Adapted from unity-tweens; see THIRD-PARTY-NOTICES.md.
#pragma once

#include <godot_cpp/classes/ref_counted.hpp>

namespace godot {

class TweensGdEasing : public RefCounted {
	GDCLASS(TweensGdEasing, RefCounted)

protected:
	static void _bind_methods();

public:
	enum Ease : int64_t {
		LINEAR = 0,
		SINE_IN = 10,
		SINE_OUT,
		SINE_IN_OUT,
		QUAD_IN = 20,
		QUAD_OUT,
		QUAD_IN_OUT,
		CUBIC_IN = 30,
		CUBIC_OUT,
		CUBIC_IN_OUT,
		QUART_IN = 40,
		QUART_OUT,
		QUART_IN_OUT,
		QUINT_IN = 50,
		QUINT_OUT,
		QUINT_IN_OUT,
		EXPO_IN = 60,
		EXPO_OUT,
		EXPO_IN_OUT,
		CIRC_IN = 70,
		CIRC_OUT,
		CIRC_IN_OUT,
		BACK_IN = 80,
		BACK_OUT,
		BACK_IN_OUT,
		ELASTIC_IN = 90,
		ELASTIC_OUT,
		ELASTIC_IN_OUT,
		BOUNCE_IN = 100,
		BOUNCE_OUT,
		BOUNCE_IN_OUT,
		SMOOTH_STEP = 110,
		SMOOTHER_STEP = 120,
	};

	// Unknown easing functions return NaN.
	static double evaluate(int64_t p_ease, double p_progress, int64_t p_blend_type = 0, double p_blend = 0.2);
};

namespace tweens {
// A legacy Ease or at most one In flag and one Out flag; used by definition validation.
bool is_known_ease(int64_t p_ease);
} // namespace tweens

} // namespace godot
