// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "carry.hpp"
#include "interpolation.hpp"
#include "playback.hpp"

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/scene_tree.hpp>

namespace godot {

class TweensGdCancellation;
class TweensGdScheduler;

// A single playback. Await end, including after playback has ended.
class TweensGdHandle : public RefCounted {
	GDCLASS(TweensGdHandle, RefCounted)

	friend class TweensGdScheduler;
	friend class TweensGdGroupWatcher;

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
	CarryStamp stamp;
	bool target_is_node = false;
	bool target_is_owner = false;
	// Settings are immutable until finish(), so the per-frame path tests plain flags.
	TypedLerp lerp;
	bool typed = false;
	bool has_property = false;
	bool has_update = false;
	bool has_start = false;
	bool has_ease_function = false;
	bool has_curve = false;

	void initialize_playback();
	void advance(double p_delta);
	void advance_inner(double p_delta);
	Variant interpolate_values(const Variant &p_from, const Variant &p_to, double p_weight);
	bool follow();
	void apply(Variant p_sample, bool p_restoring = false);
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
	CarryStamp completion_stamp() const;
	void release_options();

protected:
	static void _bind_methods();

public:
	~TweensGdHandle();

	// A rejected start is already settled and owns no target, callbacks or scheduler.
	static Ref<TweensGdHandle> rejected(const String &p_message);
	// Takes ownership of p_options.
	static Ref<TweensGdHandle> start(TweensGdScheduler *p_scheduler, const Variant &p_target, TweenSettings *p_options,
			const Variant &p_initial, Node *p_owner, SceneTree *p_tree);

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
	bool is_paused() const { return paused; }
	void set_paused(bool p_paused);
	const CarryStamp &get_stamp() const { return stamp; }
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
