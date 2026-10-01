// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "definition.hpp"

namespace godot {

// Constant-time timeline, shared behavioral contract with the C# Playback.
struct Timeline {
	static constexpr double MAX_TIME = 1.7976931348623157e308;

	double progress = 0.0;
	// True on the ping-pong return leg and its final hold.
	bool returning = false;
	bool started = false;
	bool completed = false;
	// Index of the current cycle; relative tweens add one full offset per cycle before it.
	double cycle = 0.0;
	int64_t state = tweens::STATE_DELAYED;
	double elapsed = 0.0;

	void configure(const TweenSettings &p_settings);
	void advance(double p_delta);
	void sample_at(double p_local_time);
	double gap() const { return delay; }
	double inner_delay() const { return delay > 0.0 ? delay : 0.0; }
	double remaining() const { return total - offset; }

private:
	double duration = 0.0;
	double delay = 0.0;
	double turn = 0.0;
	double offset = 0.0;
	double span = 0.0;
	double total = 0.0;
	bool ping_pong = false;
	int64_t repeats = 0;
};

// Exposes the timeline to the conformance fixtures.
class TweensGdPlayback : public RefCounted {
	GDCLASS(TweensGdPlayback, RefCounted)

	Timeline timeline;

protected:
	static void _bind_methods();

public:
	static Ref<TweensGdPlayback> create(const Ref<TweensGdDefinition> &p_definition);
	void advance(double p_delta) { timeline.advance(p_delta); }
	double get_progress() const { return timeline.progress; }
	bool is_returning() const { return timeline.returning; }
	bool is_started() const { return timeline.started; }
	bool is_completed() const { return timeline.completed; }
	double get_cycle() const { return timeline.cycle; }
	int64_t get_state() const { return timeline.state; }
	double get_elapsed() const { return timeline.elapsed; }
	void sample_at(double p_local_time) { timeline.sample_at(p_local_time); }
};

} // namespace godot
