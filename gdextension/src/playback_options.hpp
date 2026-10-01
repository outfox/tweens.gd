// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once
#include "common.hpp"
#include <godot_cpp/classes/ref_counted.hpp>

namespace godot {
struct PlaybackPolicy {
	int64_t process_mode = tweens::LANE_PROCESS;
	int64_t pause_mode = tweens::PAUSE_BOUND;
	bool use_unscaled_time = false;
	String validate() const;
};

class TweensGdPlaybackOptions : public RefCounted {
	GDCLASS(TweensGdPlaybackOptions, RefCounted)
	PlaybackPolicy policy;

protected:
	static void _bind_methods();

public:
	PlaybackPolicy snapshot() const { return policy; }
	void set_process_mode(int64_t value) { policy.process_mode = value; }
	int64_t get_process_mode() const { return policy.process_mode; }
	void set_pause_mode(int64_t value) { policy.pause_mode = value; }
	int64_t get_pause_mode() const { return policy.pause_mode; }
	void set_use_unscaled_time(bool value) { policy.use_unscaled_time = value; }
	bool get_use_unscaled_time() const { return policy.use_unscaled_time; }
};
} //namespace godot
