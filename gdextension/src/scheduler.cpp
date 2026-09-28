// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "scheduler.hpp"

#include "group.hpp"
#include "interpolation.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>

namespace godot {

using namespace tweens;

namespace {

bool is_null(const Variant &p_value) {
	return p_value.get_type() == Variant::NIL;
}

} // namespace

void TweensGdScheduler::_bind_methods() {
	ClassDB::bind_method(D_METHOD("add", "target", "definition", "owner"), &TweensGdScheduler::add, DEFVAL(Variant()));
	ClassDB::bind_method(D_METHOD("add_all", "target", "definitions", "owner"), &TweensGdScheduler::add_all, DEFVAL(Variant()));
	ClassDB::bind_method(D_METHOD("update", "delta", "unscaled_delta", "mode"), &TweensGdScheduler::update, DEFVAL(-1.0),
			DEFVAL(LANE_PROCESS));
	ClassDB::bind_method(D_METHOD("cancel_all"), &TweensGdScheduler::cancel_all);
	ClassDB::bind_method(D_METHOD("cancel_owner", "owner", "include_children"), &TweensGdScheduler::cancel_owner, DEFVAL(false));
	ClassDB::bind_method(D_METHOD("dispose"), &TweensGdScheduler::dispose);
	ClassDB::bind_method(D_METHOD("get_last_error"), &TweensGdScheduler::get_last_error);
	ClassDB::bind_method(D_METHOD("get_active_count"), &TweensGdScheduler::get_active_count);
	ClassDB::bind_method(D_METHOD("is_disposed"), &TweensGdScheduler::is_disposed);
	ClassDB::bind_static_method("TweensGdScheduler", D_METHOD("_has_carry"), &TweensGdScheduler::has_carry);
	ADD_PROPERTY(PropertyInfo(Variant::STRING, "last_error"), "", "get_last_error");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "active_count"), "", "get_active_count");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_disposed"), "", "is_disposed");
	ADD_SIGNAL(MethodInfo("error_reported", PropertyInfo(Variant::STRING, "message")));
}

Ref<TweensGdHandle> TweensGdScheduler::add(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition, const Variant &p_owner) {
	if (!require_main_thread()) {
		return TweensGdHandle::rejected("Use tweens.gd on Godot's main thread.");
	}
	const Ref<TweensGdScheduler> keep(this);
	last_error = String();
	if (disposed) {
		return reject("The scheduler is disposed.");
	}
	if (p_target.get_validated_object() == nullptr) {
		return reject("The target is invalid.");
	}
	if (p_definition.is_null()) {
		return reject("A definition is required.");
	}
	const String validation = p_definition->validate();
	if (!validation.is_empty()) {
		return reject(validation);
	}
	const bool has_owner = !is_null(p_owner);
	Object *target = p_target.get_validated_object();
	Node *owner = nullptr;
	if (has_owner) {
		owner = Object::cast_to<Node>(p_owner.get_validated_object());
		if (owner == nullptr) {
			return reject("The owner must be a valid Node.");
		}
	}
	if (Node *node = Object::cast_to<Node>(target)) {
		if (has_owner && owner->get_instance_id() != node->get_instance_id()) {
			return reject("Node targets use their own lifetime.");
		}
		owner = node;
	}
	const ObjectID target_id(target->get_instance_id());
	const ObjectID owner_id = owner != nullptr ? ObjectID(owner->get_instance_id()) : ObjectID();
	const auto lifetime_ended = [&]() {
		return !is_alive(target_id) ||
				(owner != nullptr && (!is_alive(owner_id) || !owner->is_inside_tree() || owner->is_queued_for_deletion()));
	};
	if (owner != nullptr && (!owner->is_inside_tree() || owner->is_queued_for_deletion())) {
		return reject("The owner must be alive and inside the scene tree.");
	}
	// The handle takes ownership; rejected starts free the copy on return.
	struct Owned {
		TweenSettings *settings;
		~Owned() {
			if (settings != nullptr) {
				memdelete(settings);
			}
		}
	} owned{ p_definition->get_settings().snapshot() };
	TweenSettings &snapshot = *owned.settings;
	if (!snapshot.target_class.is_empty() && !target->is_class(snapshot.target_class)) {
		return reject(vformat("This definition requires a %s target.", snapshot.target_class));
	}
	if (snapshot.adapter.is_valid()) {
		const String preparation = hook_error(snapshot.adapter->call(names().prepare, p_target), "prepare");
		if (!preparation.is_empty()) {
			return reject(preparation, &snapshot);
		}
	}
	if (lifetime_ended()) {
		return reject("The target or owner became invalid during adapter preparation.", &snapshot);
	}
	Variant initial;
	if (snapshot.adapter.is_valid()) {
		initial = snapshot.adapter->call(names().read, p_target);
	} else {
		initial = snapshot.property.is_empty() ? snapshot.initial_value : TweensGdInterpolation::read_property(target, snapshot.property);
	}
	if (lifetime_ended()) {
		return reject("The target or owner became invalid while reading the property.", &snapshot);
	}
	if (snapshot.adapter.is_null() && !TweensGdInterpolation::supported(initial)) {
		return reject("The property is missing or its value type is unsupported.");
	}
	if (snapshot.value_type != Variant::NIL && initial.get_type() != snapshot.value_type) {
		return reject("The captured value does not match the definition's value type.", &snapshot);
	}
	const bool adjusts[3] = {
		snapshot.factor_from != 1.0 || !is_null(snapshot.delta_from),
		snapshot.factor_to != 1.0 || !is_null(snapshot.delta_to),
		snapshot.factor_by != 1.0 || !is_null(snapshot.delta_by),
	};
	if ((!is_null(snapshot.by_value) || adjusts[0] || adjusts[1] || adjusts[2]) && is_null(TweensGdInterpolation::zero(initial.get_type()))) {
		return reject("by_value, factors and deltas need an int, float, vector, Color, Quaternion or Rect2 value.", &snapshot);
	}
	for (const Variant *endpoint : { &initial, &snapshot.from_value, &snapshot.to_value, &snapshot.by_value, &snapshot.delta_from,
				 &snapshot.delta_to, &snapshot.delta_by }) {
		if (is_null(*endpoint) && !is_null(initial)) {
			continue;
		}
		const String endpoint_error = check_endpoint(snapshot, initial, *endpoint);
		if (!endpoint_error.is_empty()) {
			return reject(endpoint_error, &snapshot);
		}
	}
	// Apply factor * value + delta once. An adjusted endpoint becomes explicit, so an adjusted start is fixed.
	Variant *fields[3] = { &snapshot.from_value, &snapshot.to_value, &snapshot.by_value };
	const double factors[3] = { snapshot.factor_from, snapshot.factor_to, snapshot.factor_by };
	const Variant *deltas[3] = { &snapshot.delta_from, &snapshot.delta_to, &snapshot.delta_by };
	for (int index = 0; index < 3; index++) {
		if (!adjusts[index]) {
			continue;
		}
		Variant adjusted = is_null(*fields[index]) ? initial : *fields[index];
		if (factors[index] != 1.0) {
			const Variant zero = TweensGdInterpolation::zero(initial.get_type());
			if (snapshot.adapter.is_valid()) {
				snapshot.adapter->set(names().captured_type, int64_t(initial.get_type()));
				adjusted = snapshot.adapter->call(names().interpolate, zero, adjusted, factors[index]);
			} else {
				adjusted = TweensGdInterpolation::interpolate(zero, adjusted, factors[index], initial.get_type());
			}
			if (!TweensGdInterpolation::compatible(initial, adjusted)) {
				return reject("A factor changed the value type.", &snapshot);
			}
		}
		if (!is_null(*deltas[index])) {
			adjusted = TweensGdInterpolation::add(adjusted, *deltas[index]);
		}
		const String adjusted_error = check_endpoint(snapshot, initial, adjusted);
		if (!adjusted_error.is_empty()) {
			return reject(adjusted_error, &snapshot);
		}
		*fields[index] = adjusted;
	}
	// Custom validation can reenter and invalidate the target/owner too.
	if (lifetime_ended()) {
		return reject("The target or owner became invalid during validation.", &snapshot);
	}
	SceneTree *tree = owner != nullptr ? owner->get_tree() : nullptr;
	const Ref<TweensGdHandle> instance = TweensGdHandle::start(this, p_target, owned.settings, initial, owner, tree);
	owned.settings = nullptr;
	instance->clock.elapsed = carry::credit(ObjectID(get_instance_id()), instance->mode, instance->unscaled, ticks[instance->mode]);
	if (disposed) {
		instance->finish(REASON_RUNNER_DISPOSED);
		return instance;
	}
	instances.push_back(instance);
	instance->initialize_playback();
	return instance;
}

Ref<TweensGdGroup> TweensGdScheduler::add_all(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner) {
	if (!require_main_thread()) {
		return TweensGdGroup::of(Array::make(TweensGdHandle::rejected("Use tweens.gd on Godot's main thread.")));
	}
	if (p_definitions.get_type() != Variant::ARRAY || Array(p_definitions).is_empty()) {
		return TweensGdGroup::of(Array::make(reject("At least one tween definition is required.")));
	}
	const Array definitions = p_definitions;
	for (int64_t index = 0; index < definitions.size(); index++) {
		if (Object::cast_to<TweensGdDefinition>(definitions[index].get_validated_object()) == nullptr) {
			return TweensGdGroup::of(Array::make(reject("Every entry must be a tween definition.")));
		}
	}
	Array handles;
	for (int64_t index = 0; index < definitions.size(); index++) {
		const Ref<TweensGdHandle> handle = add(p_target, definitions[index], p_owner);
		handles.push_back(handle);
		if (handle->get_completion_reason() == REASON_FAILED) {
			break;
		}
	}
	return TweensGdGroup::of(handles);
}

void TweensGdScheduler::update(double p_delta, double p_unscaled_delta, int64_t p_mode) {
	if (!require_main_thread()) {
		return;
	}
	// Signal listeners and tween callbacks may drop the last reference to this scheduler.
	const Ref<TweensGdScheduler> keep(this);
	if (disposed) {
		report_error("The scheduler is disposed.");
		return;
	}
	if (updating) {
		report_error("Recursive scheduler updates are not supported.");
		return;
	}
	if (p_unscaled_delta == -1.0) {
		p_unscaled_delta = p_delta;
	}
	if (!Math::is_finite(p_delta) || p_delta < 0.0 || !Math::is_finite(p_unscaled_delta) || p_unscaled_delta < 0.0 ||
			(p_mode != LANE_PROCESS && p_mode != LANE_PHYSICS)) {
		report_error("Update requires finite nonnegative deltas and a valid process mode.");
		return;
	}
	updating = true;
	ticks[p_mode] += 1;
	const uint32_t count = instances.size();
	for (uint32_t index = 0; index < count; index++) {
		if (disposed) {
			break;
		}
		// The vector keeps its references for the whole update; callbacks may still move its storage.
		TweensGdHandle *instance = instances[index].ptr();
		// Lifetime checks run even for paused tweens and the other process lane.
		if (instance->check_target() && instance->mode == p_mode && instance->can_advance()) {
			instance->advance(instance->unscaled ? p_unscaled_delta : p_delta);
		}
	}
	updating = false;
	compact();
}

void TweensGdScheduler::cancel_all() {
	if (!require_main_thread()) {
		return;
	}
	const Ref<TweensGdScheduler> keep(this);
	const LocalVector<Ref<TweensGdHandle>> snapshot(instances);
	for (const Ref<TweensGdHandle> &instance : snapshot) {
		instance->cancel();
	}
	if (!updating) {
		compact();
	}
}

void TweensGdScheduler::cancel_owner(Node *p_owner, bool p_include_children) {
	if (!require_main_thread() || p_owner == nullptr) {
		return;
	}
	const Ref<TweensGdScheduler> keep(this);
	const ObjectID owner_id(p_owner->get_instance_id());
	const LocalVector<Ref<TweensGdHandle>> snapshot(instances);
	for (const Ref<TweensGdHandle> &instance : snapshot) {
		Node *instance_owner = instance->get_owner_node();
		if (instance->owner_id == owner_id || (p_include_children && instance_owner != nullptr && p_owner->is_ancestor_of(instance_owner))) {
			instance->cancel();
		}
	}
	if (!updating) {
		compact();
	}
}

void TweensGdScheduler::dispose() {
	if (!require_main_thread() || disposed) {
		return;
	}
	const Ref<TweensGdScheduler> keep(this);
	disposed = true;
	const LocalVector<Ref<TweensGdHandle>> snapshot(instances);
	for (const Ref<TweensGdHandle> &instance : snapshot) {
		instance->finish(REASON_RUNNER_DISPOSED);
	}
	if (!updating) {
		instances.clear();
	}
}

int64_t TweensGdScheduler::get_active_count() const {
	int64_t count = 0;
	for (const Ref<TweensGdHandle> &instance : instances) {
		if (!instance->is_terminal()) {
			count++;
		}
	}
	return count;
}

void TweensGdScheduler::report_error(const String &p_message) {
	last_error = p_message;
	emit_signal(names().error_reported, p_message);
	if (prints_errors) {
		report(p_message);
	}
}

bool TweensGdScheduler::has_carry() {
	return carry::is_active();
}

void TweensGdScheduler::compact() {
	uint32_t kept = 0;
	for (uint32_t index = 0; index < instances.size(); index++) {
		if (!instances[index]->is_terminal()) {
			if (kept != index) {
				instances[kept] = instances[index];
			}
			kept++;
		}
	}
	instances.resize(kept);
}

String TweensGdScheduler::check_endpoint(const TweenSettings &p_snapshot, const Variant &p_initial, const Variant &p_endpoint) const {
	if (p_snapshot.adapter.is_valid()) {
		const String value_error = hook_error(p_snapshot.adapter->call(names().validate_value, p_endpoint), "validate_value");
		if (!value_error.is_empty()) {
			return value_error;
		}
	} else if (!TweensGdInterpolation::finite(p_endpoint)) {
		return "Endpoints must be finite and match the property's value type.";
	}
	if (!TweensGdInterpolation::compatible(p_initial, p_endpoint)) {
		return "Endpoints must match the captured value's type.";
	}
	if (p_endpoint.get_type() == Variant::QUATERNION && Quaternion(p_endpoint).length_squared() == 0.0) {
		return "Quaternion endpoints must have nonzero length.";
	}
	return String();
}

Ref<TweensGdHandle> TweensGdScheduler::reject(const String &p_message, const TweenSettings *p_snapshot) {
	String message = p_message;
	if (p_snapshot != nullptr && p_snapshot->adapter.is_valid()) {
		const String cleanup = hook_error(p_snapshot->adapter->call(names().release), "release");
		if (!cleanup.is_empty()) {
			message += "\n" + cleanup;
		}
	}
	report_error(message);
	return TweensGdHandle::rejected(message);
}

} // namespace godot
