// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "awaiting.hpp"

#include "common.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/signal.hpp>

namespace godot {

using namespace tweens;

void TweensGdCancellation::_bind_methods() {
	ClassDB::bind_method(D_METHOD("is_cancelled"), &TweensGdCancellation::is_cancelled);
	ClassDB::bind_method(D_METHOD("cancel"), &TweensGdCancellation::cancel);
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_cancelled"), "", "is_cancelled");
	ADD_SIGNAL(MethodInfo("cancelled"));
}

void TweensGdCancellation::cancel() {
	if (!require_main_thread() || cancelled) {
		return;
	}
	cancelled = true;
	emit_signal(names().cancelled);
	disconnect_all(this, names().cancelled);
}

void TweensGdAwaiting::_bind_methods() {
	ADD_SIGNAL(MethodInfo("finished", PropertyInfo(Variant::INT, "reason")));
}

Variant TweensGdAwaiting::wait_for(Object *p_source, const Ref<TweensGdCancellation> &p_token) {
	if (p_token.is_valid() && p_token->is_cancelled()) {
		return REASON_WAIT_CANCELLED;
	}
	Ref<TweensGdAwaiting> waiter;
	waiter.instantiate();
	waiter->source = p_source;
	waiter->token = p_token;
	// The bound reference keeps the waiter alive until either connection fires.
	const Variant keep_alive = waiter;
	waiter->source_callback = callable_mp(waiter.ptr(), &TweensGdAwaiting::on_source_ended).bind(keep_alive);
	p_source->connect(names().ended, waiter->source_callback);
	if (p_token.is_valid()) {
		waiter->token_callback = callable_mp(waiter.ptr(), &TweensGdAwaiting::on_cancelled).bind(keep_alive);
		p_token->connect(names().cancelled, waiter->token_callback);
	}
	return Signal(waiter.ptr(), names().finished);
}

void TweensGdAwaiting::on_source_ended(int64_t p_reason, const Variant &p_keep_alive) {
	complete(p_reason);
}

void TweensGdAwaiting::on_cancelled(const Variant &p_keep_alive) {
	complete(REASON_WAIT_CANCELLED);
}

void TweensGdAwaiting::complete(int64_t p_reason) {
	if (done) {
		return;
	}
	done = true;
	const Ref<TweensGdAwaiting> keep(this);
	Object *object = source;
	if (object != nullptr && object->is_connected(names().ended, source_callback)) {
		object->disconnect(names().ended, source_callback);
	}
	if (token.is_valid() && token->is_connected(names().cancelled, token_callback)) {
		token->disconnect(names().cancelled, token_callback);
	}
	source_callback = Callable();
	token_callback = Callable();
	source = Variant();
	token.unref();
	emit_signal(names().finished, p_reason);
}

} // namespace godot
