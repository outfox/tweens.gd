// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/core/object_id.hpp>

#include <cstdint>

namespace godot {

// Where and when a playback completed, so a continuation started inside its end signal keeps the overshoot.
struct CarryStamp {
	bool present = false;
	ObjectID scheduler;
	int64_t mode = 0;
	bool unscaled = false;
	int64_t tick = 0;
	double seconds = 0.0;

	bool same_clock(const CarryStamp &p_other) const {
		return scheduler == p_other.scheduler && mode == p_other.mode && unscaled == p_other.unscaled;
	}
};

// Main-thread continuation scope, shared by handles and groups.
namespace carry {

CarryStamp enter(const CarryStamp &p_stamp);
void leave(const CarryStamp &p_previous);
double credit(ObjectID p_scheduler, int64_t p_mode, bool p_unscaled, int64_t p_tick);
bool is_active();

} // namespace carry
} // namespace godot
