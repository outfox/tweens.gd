// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "handle.hpp"

#include "awaiting.hpp"
#include "chain.hpp"
#include "easing.hpp"
#include "interpolation.hpp"
#include "scheduler.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>
#include <godot_cpp/variant/signal.hpp>

#include <cmath>

namespace godot {

using namespace tweens;

void TweensGdHandle::_bind_methods() {
	ClassDB::bind_static_method("TweensGdHandle", D_METHOD("rejected", "message"), &TweensGdHandle::rejected);
	ClassDB::bind_method(D_METHOD("get_target"), &TweensGdHandle::get_target);
	ClassDB::bind_method(D_METHOD("get_state"), &TweensGdHandle::get_state);
	ClassDB::bind_method(D_METHOD("get_completion_reason"), &TweensGdHandle::get_completion_reason);
	ClassDB::bind_method(D_METHOD("get_error"), &TweensGdHandle::get_error);
	ClassDB::bind_method(D_METHOD("get_end"), &TweensGdHandle::get_end);
	ClassDB::bind_method(D_METHOD("get_value"), &TweensGdHandle::get_value);
	ClassDB::bind_method(D_METHOD("get_progress"), &TweensGdHandle::get_progress);
	ClassDB::bind_method(D_METHOD("is_terminal"), &TweensGdHandle::is_terminal);
	ClassDB::bind_method(D_METHOD("is_settled"), &TweensGdHandle::is_settled);
	ClassDB::bind_method(D_METHOD("is_paused"), &TweensGdHandle::is_paused);
	ClassDB::bind_method(D_METHOD("set_paused", "paused"), &TweensGdHandle::set_paused);
	ClassDB::bind_method(D_METHOD("pause"), &TweensGdHandle::pause);
	ClassDB::bind_method(D_METHOD("resume"), &TweensGdHandle::resume);
	ClassDB::bind_method(D_METHOD("cancel"), &TweensGdHandle::cancel);
	ClassDB::bind_method(D_METHOD("wait", "cancellation"), &TweensGdHandle::wait, DEFVAL(Variant()));

	ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "target", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT, "Object"), "", "get_target");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "state"), "", "get_state");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "completion_reason"), "", "get_completion_reason");
	ADD_PROPERTY(PropertyInfo(Variant::STRING, "error"), "", "get_error");
	ADD_PROPERTY(PropertyInfo(Variant::NIL, "end", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT | PROPERTY_USAGE_NIL_IS_VARIANT), "", "get_end");
	ADD_PROPERTY(PropertyInfo(Variant::NIL, "value", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT | PROPERTY_USAGE_NIL_IS_VARIANT), "", "get_value");
	ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "progress"), "", "get_progress");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_terminal"), "", "is_terminal");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_settled"), "", "is_settled");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_paused"), "set_paused", "is_paused");

	ADD_SIGNAL(MethodInfo("ended", PropertyInfo(Variant::INT, "reason")));
}

Ref<TweensGdHandle> TweensGdHandle::rejected(const String &p_message) {
	Ref<TweensGdHandle> handle;
	handle.instantiate();
	handle->state = STATE_FAULTED;
	handle->reason = REASON_FAILED;
	handle->error = p_message.is_empty() ? String("The tween could not be started.") : p_message;
	handle->settled = true;
	return handle;
}

Ref<TweensGdHandle> TweensGdHandle::start(TweensGdScheduler *p_scheduler, const Variant &p_target, TweenSettings *p_options,
		Node *p_owner, SceneTree *p_tree, const PlaybackPolicy &p_policy) {
	Ref<TweensGdHandle> handle;
	handle.instantiate();
	TweensGdHandle &h = *handle.ptr();
	h.scheduler_id = ObjectID(p_scheduler->get_instance_id());
	h.target = p_target;
	h.target_object = p_target.get_validated_object();
	h.target_id = ObjectID(h.target_object->get_instance_id());
	h.target_is_node = Object::cast_to<Node>(h.target_object) != nullptr;
	if (p_owner != nullptr) {
		h.owner = p_owner;
		h.owner_id = ObjectID(p_owner->get_instance_id());
		h.target_is_owner = h.owner_id == h.target_id;
	}
	if (p_tree != nullptr) {
		h.tree = p_tree;
		h.tree_id = ObjectID(p_tree->get_instance_id());
	}
	h.options = p_options;
	const TweenSettings &options = *p_options;
	h.clock.configure(options);
	h.has_clock = true;
	h.mode = p_policy.process_mode;
	h.pause_mode = p_policy.pause_mode;
	h.unscaled = p_policy.use_unscaled_time;
	return handle;
}

void TweensGdHandle::bind_values(const Variant &p_initial) {
	auto &h = *this;
	const TweenSettings &options = *h.options;
	h.adapter = options.adapter;
	if (h.adapter.is_valid()) {
		h.adapter->set(names().captured_type, int64_t(p_initial.get_type()));
	}
	h.initial = p_initial;
	h.value = p_initial;
	h.from = options.from_value.get_type() == Variant::NIL ? p_initial : options.from_value;
	h.to = options.to_value.get_type() == Variant::NIL ? p_initial : options.to_value;
	if (options.by_value.get_type() != Variant::NIL) {
		h.relative = true;
		// Callback-only definitions have nothing to read back, so they add to the captured start.
		h.follows = options.from_value.get_type() == Variant::NIL && (h.adapter.is_valid() || !options.property.is_empty());
		h.ping_pong = options.use_ping_pong;
		h.by = options.by_value;
		h.zero = TweensGdInterpolation::zero(p_initial.get_type());
		h.origin = h.from;
		h.applied = h.zero;
	}
	h.has_property = !options.property.is_empty();
	h.has_update = !options.on_update.is_null();
	h.has_start = !options.on_start.is_null();
	h.has_ease_function = !options.ease_function.is_null();
	h.has_curve = options.curve.is_valid();
	h.typed = h.adapter.is_null() && !h.relative && h.lerp.prepare(h.from, h.to, p_initial.get_type());
}
void TweensGdHandle::bind_lifetime() {
	if (check_target() && is_alive(owner_id)) {
		owner_exit = callable_mp(this, &TweensGdHandle::owner_exiting);
		owner->connect(names().tree_exiting, owner_exit);
	}
}

TweensGdChain *TweensGdHandle::coordinator() const {
	return Object::cast_to<TweensGdChain>(ObjectDB::get_instance(coordinator_id));
}
bool TweensGdHandle::is_paused() const {
	auto *chain = coordinator();
	return chain != nullptr && !coordinator_root ? chain->is_paused() : paused;
}

Object *TweensGdHandle::get_target() const {
	return is_alive(target_id) ? target_object : nullptr;
}

Node *TweensGdHandle::get_owner_node() const {
	return is_alive(owner_id) ? owner : nullptr;
}

Variant TweensGdHandle::get_end() {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	return settled ? Variant(reason) : Variant(Signal(this, names().ended));
}

void TweensGdHandle::set_paused(bool p_paused) {
	if (require_main_thread() && !is_terminal()) {
		auto *chain = coordinator();
		if (chain != nullptr && !coordinator_root) {
			chain->root->set_paused(p_paused);
			return;
		}
		paused = p_paused;
	}
}

void TweensGdHandle::cancel() {
	if (require_main_thread() && !is_terminal()) {
		auto *chain = coordinator();
		if (chain != nullptr && !coordinator_root) {
			chain->cancel();
			return;
		}
		finish(REASON_CANCELLED);
	}
}

Variant TweensGdHandle::wait(const Ref<TweensGdCancellation> &p_cancellation) {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	if (settled) {
		return reason;
	}
	return TweensGdAwaiting::wait_for(this, p_cancellation);
}

void TweensGdHandle::initialize_playback() {
	if (is_terminal() || (initialized && delay_filled)) {
		return;
	}
	depth++;
	if (check_target()) {
		if (binding_phase < 6) {
			prepared = true;
			auto *scheduler = Object::cast_to<TweensGdScheduler>(ObjectDB::get_instance(scheduler_id));
			if (scheduler == nullptr) {
				finish(REASON_RUNNER_DISPOSED);
			} else {
				const String preparation = scheduler->prepare_handle(*this);
				if (!preparation.is_empty() && !is_terminal()) {
					fail(preparation);
				}
			}
		}
		if (check_target() && can_advance() && binding_phase == 6 && !initialized) {
			initialized = true;
			invoke(options->on_add);
		}
		if (check_target() && can_advance() && !delay_filled) {
			delay_filled = true;
			if (clock.inner_delay() > 0.0 && (options->fill & FILL_APPLY_FROM_DURING_DELAY)) {
				apply(from);
			}
		}
	}
	end_operation();
}

void TweensGdHandle::advance(double p_delta) {
	depth++;
	plan->advance(p_delta);
	end_operation();
}

void TweensGdHandle::sample_at(double p_local_time) {
	depth++;
	initialize_playback();
	if (check_target() && can_advance()) {
		notify_update();
		if (check_target() && can_advance()) {
			advance_inner(p_local_time);
		}
	}
	if (!is_terminal() && can_advance()) {
		sampled_time = -1.0;
	}
	end_operation();
}

void TweensGdHandle::advance_inner(double p_local_time) {
	if (sampled_time != p_local_time) {
		sampled_time = p_local_time;
		sample_phase = 0;
		clock.sample_at(p_local_time);
	}
	state = clock.completed ? STATE_PLAYING : clock.state;
	if (!clock.started) {
		return;
	}
	if (!started) {
		started = true;
		if (has_start) {
			invoke(options->on_start);
			if (!check_target() || !can_advance()) {
				return;
			}
		}
	}
	if (sample_phase == 0) {
		double time = CLAMP(clock.progress, 0.0, 1.0);
		const double split = clock.returning ? 1.0 - options->weks : options->skew;
		double weight;
		if (has_curve) {
			weight = options->curve->sample(time);
		} else if (has_ease_function) {
			const Callable function = options->ease_function;
			if (!function.is_valid()) {
				fail("The easing Callable is no longer valid.");
				return;
			}
			const Variant result = function.call(time);
			if (!check_target() || !can_advance()) {
				return;
			}
			const Variant::Type type = result.get_type();
			if (type != Variant::FLOAT && type != Variant::INT) {
				fail("Easing must return a finite number.");
				return;
			}
			weight = result;
		} else {
			weight = TweensGdEasing::evaluate(options->ease, time, options->blend_type, options->blend, split);
		}
		if (!Math::is_finite(weight)) {
			fail("Easing must return a finite number.");
			return;
		}
		Variant sample;
		if (relative) {
			// Everything by_value has added so far: one offset per finished cycle, unless ping-pong brought it back.
			if (!follow() || !can_advance()) {
				return;
			}
			const Variant offset_value = interpolate_values(zero, by, weight);
			if (!check_target() || !can_advance()) {
				return;
			}
			Variant cycles = zero;
			if (!ping_pong && clock.cycle > 0.0) {
				cycles = interpolate_values(zero, by, clock.cycle);
				if (!check_target() || !can_advance()) {
					return;
				}
			}
			if (!TweensGdInterpolation::compatible(initial, offset_value) || !TweensGdInterpolation::compatible(initial, cycles)) {
				fail("Interpolation changed the value type.");
				return;
			}
			applied = TweensGdInterpolation::add(cycles, offset_value);
			sample = TweensGdInterpolation::add(origin, applied);
		} else if (typed) {
			if (!lerp.sample(weight, sample)) {
				fail("Interpolation produced a non-finite value.");
				return;
			}
		} else {
			sample = interpolate_values(from, to, weight);
			// Only adapters run user code while interpolating.
			if (adapter.is_valid() && (!check_target() || !can_advance())) {
				return;
			}
		}
		if (adapter.is_valid()) {
			const Ref<RefCounted> hooks = adapter;
			const String sample_error = hook_error(hooks->call(names().validate_value, sample), "validate_value");
			if (!check_target() || !can_advance()) {
				return;
			}
			if (!sample_error.is_empty() || !TweensGdInterpolation::compatible(initial, sample)) {
				fail(!sample_error.is_empty() ? sample_error : String("Interpolation changed the value type."));
				return;
			}
		} else if (!typed && !TweensGdInterpolation::finite(sample)) {
			fail("Interpolation produced a non-finite value.");
			return;
		}
		sample_phase = 1;
		apply(std::move(sample));
	}
	if (is_terminal() || !can_advance() || !clock.completed) {
		return;
	}
	if (sample_phase == 1) {
		if (!(options->fill & FILL_RETAIN_FINAL_VALUE)) {
			if (!restore_prepared) {
				if (!follow()) {
					return;
				}
				restore_value = follows ? origin : initial;
				restore_prepared = true;
			}
			if (!check_target() || !can_advance()) {
				return;
			}
			apply(restore_value, true);
		}
		sample_phase = 2;
	}
	if (!is_terminal() && can_advance()) {
		finish(REASON_COMPLETED);
	}
}

Variant TweensGdHandle::interpolate_values(const Variant &p_from, const Variant &p_to, double p_weight) {
	if (adapter.is_valid()) {
		const Ref<RefCounted> hooks = adapter;
		return hooks->call(names().interpolate, p_from, p_to, p_weight);
	}
	return TweensGdInterpolation::interpolate(p_from, p_to, p_weight, initial.get_type());
}

// A value other than the last one written means something else changed the property; keep that change.
bool TweensGdHandle::follow() {
	if (!follows) {
		return true;
	}
	Variant current;
	if (adapter.is_valid()) {
		const Ref<RefCounted> hooks = adapter;
		current = hooks->call(names().read, target);
	} else {
		current = target_object->get_indexed(options->property_path);
	}
	if (!check_target()) {
		return false;
	}
	if (!TweensGdInterpolation::compatible(initial, current)) {
		fail("The property no longer holds a value of its captured type.");
		return false;
	}
	if (current != value) {
		origin = TweensGdInterpolation::remove(current, applied);
	}
	return true;
}

void TweensGdHandle::apply(Variant p_sample, bool p_restoring) {
	// Callers have checked lifetimes after every preceding user callback.
	if (adapter.is_valid()) {
		const Ref<RefCounted> hooks = adapter;
		const String write_error = p_restoring ? hook_error(hooks->call(names().restore, target, p_sample), "restore")
											   : hook_error(hooks->call(names().write, target, p_sample), "write");
		if (!write_error.is_empty()) {
			fail(write_error);
			return;
		}
		value = std::move(p_sample);
		if (!check_target()) {
			return;
		}
	} else if (has_property) {
		target_object->set_indexed(options->property_path, p_sample);
		value = std::move(p_sample);
		// Setters may run script code or emit signals that cancel or free the target.
		if (!check_target()) {
			return;
		}
	} else {
		value = std::move(p_sample);
	}
	update_pending = has_update;
	notify_update();
}

void TweensGdHandle::notify_update() {
	if (!update_pending || !check_target() || !can_advance()) {
		return;
	}
	update_pending = false;
	if (has_update) {
		// The callback can end this playback and clear the settings that own the Callable.
		const Callable update = options->on_update;
		if (!update.is_valid()) {
			fail("A callback Callable is no longer valid.");
			return;
		}
		update.call(this, value);
		check_target();
	}
}

bool TweensGdHandle::invalid_target() const {
	if (!is_alive(target_id)) {
		return true;
	}
	return target_is_node && target_object->is_queued_for_deletion();
}

bool TweensGdHandle::invalid_owner() const {
	if (!owner_id.is_valid()) {
		return false;
	}
	return !is_alive(owner_id) || owner->is_queued_for_deletion() || !owner->is_inside_tree();
}

bool TweensGdHandle::check_target() {
	if (state >= STATE_COMPLETED) {
		return false;
	}
	if (invalid_target()) {
		finish(REASON_TARGET_FREED);
	} else if (!target_is_owner && invalid_owner()) {
		finish(REASON_OWNER_EXITED);
	}
	return state < STATE_COMPLETED;
}

bool TweensGdHandle::can_advance() const {
	if (is_paused()) {
		return false;
	}
	if (pause_mode == PAUSE_ALWAYS) {
		return true;
	}
	// Called right after check_target(), which has verified a target that is its own owner.
	if (pause_mode == PAUSE_BOUND && owner != nullptr && (target_is_owner || is_alive(owner_id))) {
		return owner->can_process();
	}
	return !is_alive(tree_id) || !tree->is_paused();
}

void TweensGdHandle::owner_exiting() {
	finish(owner_id == target_id && owner->is_queued_for_deletion() ? REASON_TARGET_FREED : REASON_OWNER_EXITED);
}

void TweensGdHandle::invoke(const Callable &p_callback) {
	// Callbacks can end this playback and clear the settings that own the Callable.
	const Callable callback = p_callback;
	if (callback.is_null()) {
		return;
	}
	if (!callback.is_valid()) {
		fail("A callback Callable is no longer valid.");
		return;
	}
	callback.call(this);
}

void TweensGdHandle::fail(const String &p_message) {
	if (settled) {
		return;
	}
	error = error.is_empty() ? p_message : error + "\n" + p_message;
	if (is_terminal()) {
		state = STATE_FAULTED;
		reason = REASON_FAILED;
	} else {
		finish(REASON_FAILED);
	}
}

void TweensGdHandle::finish(int64_t p_reason) {
	if (is_terminal()) {
		return;
	}
	const Ref<TweensGdHandle> keep(this);
	// Owner-exit callbacks can dispose the scheduler and drop its last Chain reference.
	const Ref<TweensGdChain> keep_coordinator(coordinator_root ? coordinator() : nullptr);
	reason = p_reason;
	state = p_reason == REASON_COMPLETED ? STATE_COMPLETED : STATE_CANCELLED;
	if (p_reason == REASON_FAILED) {
		state = STATE_FAULTED;
	}
	if (coordinator_root) {
		auto *chain = coordinator();
		if (chain != nullptr) {
			chain->stop(p_reason);
		}
	}
	const bool suppress = !initialized || options == nullptr || (options->suppress_callbacks_when_target_invalid && (invalid_target() || invalid_owner() || p_reason == REASON_TARGET_FREED || p_reason == REASON_OWNER_EXITED));
	if (!suppress) {
		if (p_reason == REASON_COMPLETED) {
			invoke(options->on_end);
		} else if (p_reason != REASON_FAILED) {
			invoke(options->on_cancel);
		}
		invoke(options->on_finally);
	}
	if (!owner_exit.is_null() && is_alive(owner_id) && owner->is_connected(names().tree_exiting, owner_exit)) {
		owner->disconnect(names().tree_exiting, owner_exit);
	}
	owner_exit = Callable();
	has_update = false;
	has_start = false;
	has_ease_function = false;
	has_curve = false;
	if (depth == 0) {
		settle();
	}
}

void TweensGdHandle::end_operation() {
	depth--;
	if (is_terminal() && depth == 0) {
		settle();
	}
}

void TweensGdHandle::release_options() {
	if (options != nullptr) {
		memdelete(options);
		options = nullptr;
	}
}

TweensGdHandle::~TweensGdHandle() {
	release_options();
}

void TweensGdHandle::settle() {
	if (settled) {
		return;
	}
	const Ref<TweensGdHandle> keep(this);
	if (coordinator_root) {
		auto *chain = coordinator();
		if (chain != nullptr && chain->plan) {
			chain->plan->release();
		}
	}
	// Released only now, once reentrant setters and callbacks have returned.
	release_options();
	// Wait until reentrant setters/interpolators have returned before releasing bindings.
	if (prepared && adapter.is_valid()) {
		const Ref<RefCounted> hooks = adapter;
		adapter.unref();
		const String cleanup = hook_error(hooks->call(names().release), "release");
		if (!cleanup.is_empty()) {
			fail(cleanup);
		}
	}
	settled = true;
	TweensGdScheduler *scheduler = Object::cast_to<TweensGdScheduler>(ObjectDB::get_instance(scheduler_id));
	if (!error.is_empty() && scheduler != nullptr && (coordinator_root || !coordinator_id.is_valid())) {
		scheduler->report_error(error);
	}
	emit_signal(names().ended, reason);
	disconnect_all(this, names().ended);
}

} // namespace godot
