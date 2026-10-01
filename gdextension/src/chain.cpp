// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "chain.hpp"
#include "awaiting.hpp"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/signal.hpp>

namespace godot {
using namespace tweens;
void TweensGdChain::_bind_methods() {
	ClassDB::bind_static_method("TweensGdChain", D_METHOD("rejected", "message"), &TweensGdChain::rejected);
#define INSPECT(name, type) \
	ClassDB::bind_method(D_METHOD("get_" #name), &TweensGdChain::get_##name); \
	ADD_PROPERTY(PropertyInfo(type, #name), "", "get_" #name);
	INSPECT(completion_reason, Variant::INT)
	INSPECT(error, Variant::STRING)
	INSPECT(errors, Variant::ARRAY)
	INSPECT(elapsed, Variant::FLOAT)
	INSPECT(duration, Variant::FLOAT)
	INSPECT(entry_count, Variant::INT)
	INSPECT(active_count, Variant::INT)
	INSPECT(pending_count, Variant::INT)
#undef INSPECT
	ClassDB::bind_method(D_METHOD("get_end"), &TweensGdChain::get_end);
	ADD_PROPERTY(PropertyInfo(Variant::NIL, "end", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT | PROPERTY_USAGE_NIL_IS_VARIANT), "", "get_end");
	ClassDB::bind_method(D_METHOD("is_terminal"), &TweensGdChain::is_terminal);
	ClassDB::bind_method(D_METHOD("is_settled"), &TweensGdChain::is_settled);
	ClassDB::bind_method(D_METHOD("is_paused"), &TweensGdChain::is_paused);
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_terminal"), "", "is_terminal");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_settled"), "", "is_settled");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_paused"), "", "is_paused");
	ClassDB::bind_method(D_METHOD("pause"), &TweensGdChain::pause);
	ClassDB::bind_method(D_METHOD("resume"), &TweensGdChain::resume);
	ClassDB::bind_method(D_METHOD("cancel"), &TweensGdChain::cancel);
	ClassDB::bind_method(D_METHOD("wait", "cancellation"), &TweensGdChain::wait, DEFVAL(Ref<TweensGdCancellation>()));
	ADD_SIGNAL(MethodInfo("ended", PropertyInfo(Variant::INT, "reason")));
}
Ref<TweensGdChain> TweensGdChain::rejected(const String &p_error) {
	Ref<TweensGdChain> chain;
	chain.instantiate();
	chain->root = TweensGdHandle::rejected(p_error);
	return chain;
}
void TweensGdChain::advance(double p_delta) {
	const Ref<TweensGdChain> keep(this);
	root->depth++;
	plan->advance(p_delta);
	root->end_operation();
}
void TweensGdChain::stop(int64_t p_reason) {
	if (plan) {
		plan->stop(p_reason);
	}
}
void TweensGdChain::on_ended(int64_t p_reason) {
	const Ref<TweensGdChain> keep(this);
	emit_signal(names().ended, p_reason);
	disconnect_all(this, names().ended);
}
TypedArray<String> TweensGdChain::get_errors() const {
	if (!diagnostics.is_empty()) {
		return diagnostics.duplicate();
	}
	TypedArray<String> errors;
	if (!root->get_error().is_empty()) {
		errors.push_back(root->get_error());
	}
	return errors;
}
Variant TweensGdChain::get_end() {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	return is_settled() ? Variant(get_completion_reason()) : Variant(Signal(this, names().ended));
}
Variant TweensGdChain::wait(const Ref<TweensGdCancellation> &p_cancellation) {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	return is_settled() ? Variant(get_completion_reason()) : TweensGdAwaiting::wait_for(this, p_cancellation);
}
} //namespace godot
