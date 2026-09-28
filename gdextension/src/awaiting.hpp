// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/ref_counted.hpp>
#include <godot_cpp/variant/callable.hpp>

namespace godot {

// Cancels individual waits without cancelling their playback. Main-thread only.
class TweensGdCancellation : public RefCounted {
	GDCLASS(TweensGdCancellation, RefCounted)

	bool cancelled = false;

protected:
	static void _bind_methods();

public:
	bool is_cancelled() const { return cancelled; }
	void cancel();
};

// One pending wait. It keeps its source alive, like a suspended GDScript coroutine did.
class TweensGdAwaiting : public RefCounted {
	GDCLASS(TweensGdAwaiting, RefCounted)

	Variant source;
	Ref<TweensGdCancellation> token;
	Callable source_callback;
	Callable token_callback;
	bool done = false;

	void on_source_ended(int64_t p_reason, const Variant &p_keep_alive);
	void on_cancelled(const Variant &p_keep_alive);
	void complete(int64_t p_reason);

protected:
	static void _bind_methods();

public:
	// The source must be unsettled and have an ended(reason) signal.
	static Variant wait_for(Object *p_source, const Ref<TweensGdCancellation> &p_token);
};

} // namespace godot
