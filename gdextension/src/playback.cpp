// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "playback.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>

namespace godot {

using namespace tweens;

void Timeline::configure(const TweenSettings &p_settings) {
	duration = p_settings.effective_duration();
	delay = p_settings.effective_delay();
	turn = p_settings.ping_pong_interval;
	offset = p_settings.offset;
	ping_pong = p_settings.use_ping_pong;
	repeats = p_settings.repeats;
	span = duration + (ping_pong ? turn + duration : 0.0) + p_settings.repeat_interval;
	total = repeats == INFINITE_REPEATS ? Math::INF : span * (double(repeats) + 1.0) - p_settings.repeat_interval;
}

void Timeline::advance(double p_delta) {
	sample_at(MIN(MAX_TIME, elapsed + p_delta));
}

void Timeline::sample_at(double p_local_time) {
	elapsed = p_local_time;
	started = completed = returning = false;
	progress = cycle = 0.0;
	state = STATE_DELAYED;
	if (elapsed < inner_delay()) {
		return;
	}
	started = true;
	const double time = MIN(MAX_TIME, elapsed - inner_delay() + offset);
	if (time >= total) {
		returning = ping_pong;
		progress = ping_pong ? 0.0 : 1.0;
		cycle = double(repeats);
		completed = true;
		state = STATE_COMPLETED;
		return;
	}
	double local = Math::fmod(time, span);
	cycle = Math::round((time - local) / span);
	if (local == 0.0 && time > 0.0) {
		local = span;
		cycle -= 1.0;
	}
	state = STATE_PLAYING;
	returning = ping_pong && local > duration && local >= duration + turn;
	if (duration > 0.0 && local <= duration) {
		progress = local / duration;
	} else if (!ping_pong) {
		progress = 1.0;
		state = STATE_INTERVAL;
	} else if (local < duration + turn) {
		progress = 1.0;
		state = STATE_INTERVAL;
	} else if (duration > 0.0 && local <= duration * 2.0 + turn) {
		progress = 1.0 - (local - duration - turn) / duration;
	} else {
		progress = 0.0;
		state = STATE_INTERVAL;
	}
}

void TweensGdPlayback::_bind_methods() {
	ClassDB::bind_static_method("TweensGdPlayback", D_METHOD("create", "definition"), &TweensGdPlayback::create);
	ClassDB::bind_method(D_METHOD("advance", "delta"), &TweensGdPlayback::advance);
	ClassDB::bind_method(D_METHOD("get_progress"), &TweensGdPlayback::get_progress);
	ClassDB::bind_method(D_METHOD("is_returning"), &TweensGdPlayback::is_returning);
	ClassDB::bind_method(D_METHOD("is_started"), &TweensGdPlayback::is_started);
	ClassDB::bind_method(D_METHOD("is_completed"), &TweensGdPlayback::is_completed);
	ClassDB::bind_method(D_METHOD("get_cycle"), &TweensGdPlayback::get_cycle);
	ClassDB::bind_method(D_METHOD("get_state"), &TweensGdPlayback::get_state);
	ClassDB::bind_method(D_METHOD("get_elapsed"), &TweensGdPlayback::get_elapsed);
	ClassDB::bind_method(D_METHOD("sample_at", "local_time"), &TweensGdPlayback::sample_at);
	ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "progress"), "", "get_progress");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "returning"), "", "is_returning");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "started"), "", "is_started");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "completed"), "", "is_completed");
	ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "cycle"), "", "get_cycle");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "state"), "", "get_state");
	ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "elapsed"), "", "get_elapsed");
}

Ref<TweensGdPlayback> TweensGdPlayback::create(const Ref<TweensGdDefinition> &p_definition) {
	Ref<TweensGdPlayback> result;
	result.instantiate();
	if (p_definition.is_valid()) {
		result->timeline.configure(p_definition->get_settings());
	}
	return result;
}

} // namespace godot
