// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "playback_options.hpp"
#include <godot_cpp/core/class_db.hpp>

namespace godot {
String PlaybackPolicy::validate() const {
	return process_mode < tweens::LANE_PROCESS || process_mode > tweens::LANE_PHYSICS ||
					pause_mode < tweens::PAUSE_BOUND || pause_mode > tweens::PAUSE_ALWAYS
			? String("Unknown process or pause mode.")
			: String();
}
void TweensGdPlaybackOptions::_bind_methods() {
#define POLICY(name, type) \
	ClassDB::bind_method(D_METHOD("set_" #name, "value"), &TweensGdPlaybackOptions::set_##name); \
	ClassDB::bind_method(D_METHOD("get_" #name), &TweensGdPlaybackOptions::get_##name); \
	ADD_PROPERTY(PropertyInfo(type, #name), "set_" #name, "get_" #name);
	POLICY(process_mode, Variant::INT)
	POLICY(pause_mode, Variant::INT)
	POLICY(use_unscaled_time, Variant::BOOL)
#undef POLICY
}
} //namespace godot
