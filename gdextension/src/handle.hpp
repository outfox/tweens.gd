// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "interpolation.hpp"
#include "playback.hpp"
#include "playback_options.hpp"
#include <godot_cpp/templates/local_vector.hpp>
#include <memory>

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/scene_tree.hpp>

namespace godot {

class TweensGdCancellation;
class TweensGdScheduler;
class TweensGdChain;
namespace tweens {
class ExecutionPlan;
}

// A single playback. Await end, including after playback has ended.
class TweensGdHandle : public RefCounted {
	GDCLASS(TweensGdHandle, RefCounted)

	friend class TweensGdScheduler;
	friend class TweensGdGroupWatcher;
	friend class TweensGdChain;
	friend class tweens::ExecutionPlan;

	ObjectID scheduler_id;
	// Holds RefCounted targets like the GDScript implementation's typed Object field did.
	Variant target;
	ObjectID target_id;
	Object *target_object = nullptr;
	ObjectID owner_id;
	Node *owner = nullptr;
	ObjectID tree_id;
	SceneTree *tree = nullptr;
	Callable owner_exit;
	// Owned copy of the definition; released once playback settles.
	TweenSettings *options = nullptr;
	Ref<RefCounted> adapter;
	Timeline clock;
	bool has_clock = false;
	Variant initial;
	Variant from;
	Variant to;
	Variant value;
	// A by_value tween writes origin + applied. Following tweens move origin along with outside changes.
	bool relative = false;
	bool follows = false;
	bool ping_pong = false;
	Variant by;
	Variant zero;
	Variant origin;
	Variant applied;
	int64_t state = tweens::STATE_DELAYED;
	int64_t reason = tweens::REASON_NONE;
	String error;
	int64_t mode = tweens::LANE_PROCESS;
	int64_t pause_mode = tweens::PAUSE_BOUND;
	bool unscaled = false;
	bool started = false;
	bool settled = false;
	bool paused = false;
	int depth = 0;
	ObjectID coordinator_id;
	bool coordinator_root = false;
	bool prepared = false, initialized = false, delay_filled = false;
	bool update_pending = false, restore_prepared = false;
	Variant restore_value;
	double sampled_time = -1.0;
	int sample_phase = 0;
	int binding_phase = 0, binding_index = 0, adjustment_phase = 0;
	Variant binding_initial, binding_adjusted;
	std::unique_ptr<tweens::ExecutionPlan> plan;
	bool target_is_node = false;
	bool target_is_owner = false;
	// Settings are immutable until finish(), so the per-frame path tests plain flags.
	TypedLerp lerp;
	bool typed = false;
	bool has_interpolator = false;
	bool has_property = false;
	bool has_update = false;
	bool has_start = false;
	bool has_ease_function = false;
	bool has_curve = false;

	void initialize_playback();
	void advance(double p_delta);
	void sample_at(double p_local_time);
	void advance_inner(double p_local_time);
	void bind_values(const Variant &p_initial);
	void bind_lifetime();
	TweensGdChain *coordinator() const;
	Variant interpolate_values(const Variant &p_from, const Variant &p_to, double p_weight);
	bool follow();
	void apply(Variant p_sample, bool p_restoring = false);
	void notify_update();
	bool invalid_target() const;
	bool invalid_owner() const;
	bool check_target();
	bool can_advance() const;
	void owner_exiting();
	void invoke(const Callable &p_callback);
	void fail(const String &p_message);
	void finish(int64_t p_reason);
	void end_operation();
	void settle();
	void release_options();

protected:
	static void _bind_methods();

public:
	~TweensGdHandle();

	// A rejected start is already settled and owns no target, callbacks or scheduler.
	static Ref<TweensGdHandle> rejected(const String &p_message);
	// Takes ownership of p_options.
	static Ref<TweensGdHandle> start(TweensGdScheduler *p_scheduler, const Variant &p_target, TweenSettings *p_options,
			Node *p_owner, SceneTree *p_tree, const PlaybackPolicy &p_policy);

	Object *get_target() const;
	int64_t get_state() const { return state; }
	int64_t get_completion_reason() const { return reason; }
	String get_error() const { return error; }
	// The ended signal while running, or the stored reason once settled.
	Variant get_end();
	Variant get_value() const { return value; }
	double get_progress() const { return has_clock ? clock.progress : 0.0; }
	bool is_terminal() const { return state >= tweens::STATE_COMPLETED; }
	bool is_settled() const { return settled; }
	bool is_paused() const;
	void set_paused(bool p_paused);
	int64_t get_mode() const { return mode; }
	bool uses_unscaled_time() const { return unscaled; }
	Node *get_owner_node() const;

	void pause() { set_paused(true); }
	void resume() { set_paused(false); }
	void cancel();
	// Await the result: the reason once settled, otherwise a signal that delivers it.
	Variant wait(const Ref<TweensGdCancellation> &p_cancellation);
};

} // namespace godot
