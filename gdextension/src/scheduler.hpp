// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "chain.hpp"
#include <variant>

#include <godot_cpp/templates/local_vector.hpp>

namespace godot {

class TweensGdGroup;

// Deterministic manual scheduler. Call dispose() when its lifetime ends.
class TweensGdScheduler : public RefCounted {
	GDCLASS(TweensGdScheduler, RefCounted)

	friend class TweensGdRunner;

	using WorkItem = std::variant<Ref<TweensGdHandle>, Ref<TweensGdChain>>;
	LocalVector<WorkItem> instances;
	static TweensGdHandle *control(const WorkItem &p_item);
	Ref<TweensGdHandle> make_handle(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition,
			const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options, bool p_enroll);
	String last_error;
	bool updating = false;
	bool disposed = false;
	// The runner also prints its diagnostics; manual schedulers only record and signal them.
	bool prints_errors = false;

	Ref<TweensGdHandle> reject(const String &p_message, const TweenSettings *p_snapshot = nullptr);
	String check_endpoint(const TweenSettings &p_snapshot, const Variant &p_initial, const Variant &p_endpoint) const;
	void compact();

protected:
	static void _bind_methods();

public:
	// Node targets bind to themselves. Other Objects can optionally bind to an owner.
	// Always returns a handle. Invalid starts are already settled with Reason.FAILED.
	Ref<TweensGdHandle> add(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition, const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options);
	Ref<TweensGdGroup> add_all(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options);
	// New playback created inside callbacks is first sampled on the next update.
	void update(double p_delta, double p_unscaled_delta, int64_t p_mode);
	void cancel_all();
	void cancel_owner(Node *p_owner, bool p_include_children);
	void dispose();

	String get_last_error() const { return last_error; }
	int64_t get_active_count() const;
	bool is_disposed() const { return disposed; }
	void report_error(const String &p_message);
	String prepare_handle(TweensGdHandle &p_handle);
	Ref<TweensGdChain> add_chain(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner,
			const Ref<TweensGdPlaybackOptions> &p_options);
};

} // namespace godot
