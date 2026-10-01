// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once
#include "execution_plan.hpp"
#include <godot_cpp/variant/typed_array.hpp>
#include <memory>

namespace godot {
class TweensGdChain : public RefCounted {
	GDCLASS(TweensGdChain, RefCounted)
	friend class TweensGdScheduler;
	friend class TweensGdHandle;
	friend class tweens::ExecutionPlan;
	Ref<TweensGdHandle> root;
	std::unique_ptr<tweens::ExecutionPlan> plan;
	TypedArray<String> diagnostics;
	void on_ended(int64_t p_reason);
	void advance(double p_delta);
	void stop(int64_t p_reason);

protected:
	static void _bind_methods();

public:
	static Ref<TweensGdChain> rejected(const String &p_error);
	bool is_terminal() const { return root->is_terminal(); }
	bool is_settled() const { return root->is_settled(); }
	bool is_paused() const { return root->is_paused(); }
	int64_t get_completion_reason() const { return root->get_completion_reason(); }
	String get_error() const { return root->get_error(); }
	TypedArray<String> get_errors() const;
	Variant get_end();
	Variant wait(const Ref<TweensGdCancellation> &p_cancellation);
	void pause() { root->pause(); }
	void resume() { root->resume(); }
	void cancel() { root->cancel(); }
	double get_elapsed() const { return plan ? plan->get_elapsed() : 0.0; }
	double get_duration() const { return plan ? plan->get_duration() : 0.0; }
	int64_t get_entry_count() const { return plan ? plan->entry_count() : 0; }
	int64_t get_active_count() const { return plan ? plan->active_count() : 0; }
	int64_t get_pending_count() const { return plan ? plan->pending_count() : 0; }
};
} //namespace godot
