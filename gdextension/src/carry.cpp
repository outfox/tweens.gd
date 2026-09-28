// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "carry.hpp"

namespace godot {
namespace carry {

static CarryStamp current;

CarryStamp enter(const CarryStamp &p_stamp) {
	const CarryStamp previous = current;
	current = p_stamp;
	return previous;
}

void leave(const CarryStamp &p_previous) {
	current = p_previous;
}

double credit(ObjectID p_scheduler, int64_t p_mode, bool p_unscaled, int64_t p_tick) {
	if (!current.present || current.scheduler != p_scheduler || current.mode != p_mode || current.unscaled != p_unscaled ||
			current.tick != p_tick) {
		return 0.0;
	}
	return current.seconds;
}

bool is_active() {
	return current.present;
}

} // namespace carry
} // namespace godot
