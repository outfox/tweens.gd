// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "handle.hpp"

#include <godot_cpp/templates/local_vector.hpp>
#include <godot_cpp/variant/typed_array.hpp>

namespace godot {

// Handles retain this coordinator through their signal bindings, but it only weakly references them.
// Fail-fast cancellation survives dropping the group, without a group/handle cycle or keeping a discarded scheduler alive.
class TweensGdGroupWatcher : public RefCounted {
	GDCLASS(TweensGdGroupWatcher, RefCounted)

	friend class TweensGdGroup;

	bool settled = false;
	int64_t reason = tweens::REASON_NONE;
	TypedArray<String> errors;
	LocalVector<ObjectID> members;
	LocalVector<bool> pending;
	uint32_t remaining = 0;
	bool stopping = false;
	CarryStamp stamp;
	bool same_clock = true;

	void setup(const TypedArray<TweensGdHandle> &p_members, const String &p_rejection);
	void deliver(int64_t p_reason, const Variant &p_keep_alive, int64_t p_index);
	void accept(uint32_t p_index);
	void include_stamp(const CarryStamp &p_stamp);

protected:
	static void _bind_methods();
};

// Existing tweens that finish as one step. Await end, including after settlement.
class TweensGdGroup : public RefCounted {
	GDCLASS(TweensGdGroup, RefCounted)

	TypedArray<TweensGdHandle> members;
	Ref<TweensGdGroupWatcher> watcher;

	void on_ended(int64_t p_reason);
	static Ref<TweensGdGroup> reject_group(const String &p_message);

protected:
	static void _bind_methods();

public:
	// Invalid input returns an already-settled FAILED group without touching members.
	// Repeated handles are included once, preserving their first occurrence's order.
	static Ref<TweensGdGroup> of(const Variant &p_tweens);
	// A settled FAILED group, like TweensGdHandle.rejected().
	static Ref<TweensGdGroup> rejected(const String &p_message);
	static Ref<TweensGdGroup> create(const TypedArray<TweensGdHandle> &p_members, const String &p_rejection);

	TypedArray<TweensGdHandle> get_members() const { return members.duplicate(); }
	bool is_settled() const { return watcher->settled; }
	int64_t get_completion_reason() const { return watcher->reason; }
	Variant get_end();
	String get_error() const;
	TypedArray<String> get_errors() const { return watcher->errors.duplicate(); }
	bool is_paused() const;
	void set_paused(bool p_paused);
	Ref<TweensGdGroupWatcher> get_watcher() const { return watcher; }

	void pause() { set_paused(true); }
	void resume() { set_paused(false); }
	void cancel();
	Variant wait(const Ref<TweensGdCancellation> &p_cancellation);
};

} // namespace godot
