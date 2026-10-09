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
	ClassDB::bind_method(D_METHOD("add", "target", "definition", "owner", "options"), &TweensGdScheduler::add, DEFVAL(Variant()), DEFVAL(Ref<TweensGdPlaybackOptions>()));
	ClassDB::bind_method(D_METHOD("add_all", "target", "definitions", "owner", "options"), &TweensGdScheduler::add_all, DEFVAL(Variant()), DEFVAL(Ref<TweensGdPlaybackOptions>()));
	ClassDB::bind_method(D_METHOD("update", "delta", "unscaled_delta", "mode"), &TweensGdScheduler::update, DEFVAL(-1.0),
			DEFVAL(LANE_PROCESS));
	ClassDB::bind_method(D_METHOD("cancel_all"), &TweensGdScheduler::cancel_all);
	ClassDB::bind_method(D_METHOD("cancel_owner", "owner", "include_children"), &TweensGdScheduler::cancel_owner, DEFVAL(false));
	ClassDB::bind_method(D_METHOD("dispose"), &TweensGdScheduler::dispose);
	ClassDB::bind_method(D_METHOD("get_last_error"), &TweensGdScheduler::get_last_error);
	ClassDB::bind_method(D_METHOD("get_active_count"), &TweensGdScheduler::get_active_count);
	ClassDB::bind_method(D_METHOD("is_disposed"), &TweensGdScheduler::is_disposed);
	ClassDB::bind_method(D_METHOD("add_chain", "target", "definitions", "owner", "options"), &TweensGdScheduler::add_chain, DEFVAL(Variant()), DEFVAL(Ref<TweensGdPlaybackOptions>()));
	ADD_PROPERTY(PropertyInfo(Variant::STRING, "last_error"), "", "get_last_error");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "active_count"), "", "get_active_count");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_disposed"), "", "is_disposed");
	ADD_SIGNAL(MethodInfo("error_reported", PropertyInfo(Variant::STRING, "message")));
}

Ref<TweensGdHandle> TweensGdScheduler::add(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition, const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options) {
	auto handle = make_handle(p_target, p_definition, p_owner, p_options, true);
	return handle;
}

Ref<TweensGdHandle> TweensGdScheduler::make_handle(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition,
		const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options, bool p_enroll) {
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
	const PlaybackPolicy policy = p_options.is_valid() ? p_options->snapshot() : PlaybackPolicy();
	if (!policy.validate().is_empty()) {
		return reject(policy.validate());
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
	if (disposed) {
		return reject("The scheduler was disposed while copying the definition.");
	}
	// The snapshot runs the adapter's overridable copy(), which can free the target or owner.
	if (lifetime_ended()) {
		return reject("The target or owner became invalid while copying the definition.");
	}
	if (p_definition->get_settings().adapter.is_valid() && snapshot.adapter.is_null()) {
		return reject("The adapter copy must return an adapter.");
	}
	SceneTree *tree = owner != nullptr ? owner->get_tree() : nullptr;
	const auto instance = TweensGdHandle::start(this, p_target, owned.settings, owner, tree, policy);
	owned.settings = nullptr;
	if (p_enroll) {
		// Chain entries are compiled together by add_chain, without individual plans.
		instance->plan = std::make_unique<ExecutionPlan>(instance.ptr());
		LocalVector<Ref<TweensGdHandle>> leaves;
		leaves.push_back(instance);
		const String schedule_error = instance->plan->compile(leaves, false);
		if (!schedule_error.is_empty()) {
			instance->fail(schedule_error);
			return instance;
		}
		instances.push_back(instance);
		instance->bind_lifetime();
	}
	return instance;
}

String TweensGdScheduler::prepare_handle(TweensGdHandle &handle) {
	TweenSettings &settings = *handle.options;
	auto eligible = [&]() { return handle.check_target() && handle.can_advance(); };
	if (handle.binding_phase == 0) {
		handle.adapter = settings.adapter;
		if (!settings.target_class.is_empty() && !handle.target_object->is_class(settings.target_class)) {
			return vformat("This definition requires a %s target.", settings.target_class);
		}
		handle.binding_phase = 1;
		if (settings.adapter.is_valid()) {
			settings.adapter->set("color_space", settings.color_space);
			settings.adapter->set("alpha_mode", settings.alpha_mode);
			settings.adapter->set("color_encoding", settings.color_encoding);
			const String error = hook_error(settings.adapter->call(names().prepare, handle.target), "prepare");
			if (!error.is_empty()) {
				return error;
			}
		}
		if (!eligible()) {
			return String();
		}
	}
	if (handle.binding_phase == 1) {
		handle.binding_phase = 2;
		handle.binding_initial = settings.adapter.is_valid() ? settings.adapter->call(names().read, handle.target)
				: settings.property.is_empty()				 ? settings.initial_value
															 : TweensGdInterpolation::read_property(handle.target_object, settings.property);
		if (!eligible()) {
			return String();
		}
	}
	const Variant &initial = handle.binding_initial;
	const bool adjusts[3] = {
		settings.factor_from != 1.0 || !is_null(settings.delta_from),
		settings.factor_to != 1.0 || !is_null(settings.delta_to),
		settings.factor_by != 1.0 || !is_null(settings.delta_by),
	};
	if (handle.binding_phase == 2) {
		if (settings.adapter.is_null() && !TweensGdInterpolation::supported(initial)) {
			return "The property is missing or its value type is unsupported.";
		}
		if (settings.value_type != Variant::NIL && initial.get_type() != settings.value_type) {
			return "The captured value does not match the definition's value type.";
		}
		for (Variant *endpoint : { &settings.from_value, &settings.to_value, &settings.by_value,
					 &settings.delta_from, &settings.delta_to, &settings.delta_by }) {
			*endpoint = TweensGdInterpolation::coerce(*endpoint, initial.get_type());
		}
		if ((!is_null(settings.by_value) || adjusts[0] || adjusts[1] || adjusts[2]) &&
				is_null(TweensGdInterpolation::zero(initial.get_type()))) {
			return "by_value, factors and deltas need an int, float, vector, Color, Quaternion or Rect2 value.";
		}
		handle.binding_phase = 3;
	}
	if (handle.binding_phase == 3) {
		const Variant *endpoints[] = { &initial, &settings.from_value, &settings.to_value, &settings.by_value,
			&settings.delta_from, &settings.delta_to, &settings.delta_by };
		while (handle.binding_index < 7) {
			const Variant &endpoint = *endpoints[handle.binding_index++];
			if (is_null(endpoint) && !is_null(initial)) {
				continue;
			}
			const String error = check_endpoint(settings, initial, endpoint);
			if (!error.is_empty()) {
				return error;
			}
			if (!eligible()) {
				return String();
			}
		}
		handle.binding_index = 0;
		handle.binding_phase = 4;
	}
	if (handle.binding_phase == 4) {
		Variant *fields[] = { &settings.from_value, &settings.to_value, &settings.by_value };
		const double factors[] = { settings.factor_from, settings.factor_to, settings.factor_by };
		const Variant *deltas[] = { &settings.delta_from, &settings.delta_to, &settings.delta_by };
		while (handle.binding_index < 3) {
			const int i = handle.binding_index;
			if (!adjusts[i]) {
				handle.binding_index++;
				continue;
			}
			if (handle.adjustment_phase == 0) {
				handle.binding_adjusted = is_null(*fields[i]) ? initial : *fields[i];
				handle.adjustment_phase = 1;
				if (factors[i] != 1.0) {
					const Variant zero = TweensGdInterpolation::zero(initial.get_type());
					if (settings.adapter.is_valid()) {
						settings.adapter->set(names().captured_type, int64_t(initial.get_type()));
						handle.binding_adjusted = settings.adapter->call("interpolate_offset", zero, handle.binding_adjusted, factors[i]);
					} else {
						handle.binding_adjusted = TweensGdInterpolation::interpolate_offset(zero, handle.binding_adjusted, factors[i], initial.get_type());
					}
					if (!TweensGdInterpolation::compatible(initial, handle.binding_adjusted)) {
						return "A factor changed the value type.";
					}
				}
				if (!eligible()) {
					return String();
				}
			}
			if (handle.adjustment_phase == 1) {
				if (!is_null(*deltas[i])) {
					handle.binding_adjusted = TweensGdInterpolation::add(handle.binding_adjusted, *deltas[i]);
				}
				handle.adjustment_phase = 2;
				const String error = check_endpoint(settings, initial, handle.binding_adjusted);
				if (!error.is_empty()) {
					return error;
				}
				if (!eligible()) {
					return String();
				}
			}
			*fields[i] = handle.binding_adjusted;
			handle.adjustment_phase = 0;
			handle.binding_index++;
		}
		handle.binding_phase = 5;
	}
	if (handle.binding_phase == 5) {
		handle.bind_values(initial);
		handle.binding_initial = handle.binding_adjusted = Variant();
		handle.binding_phase = 6;
	}
	return String();
}

Ref<TweensGdGroup> TweensGdScheduler::add_all(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options) {
	if (!require_main_thread()) {
		return TweensGdGroup::of(Array::make(TweensGdHandle::rejected("Use tweens.gd on Godot's main thread.")));
	}
	// on_add callbacks may drop the last reference to this scheduler between starts.
	const Ref<TweensGdScheduler> keep(this);
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
		const Ref<TweensGdHandle> handle = add(p_target, definitions[index], p_owner, p_options);
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
	const uint32_t count = instances.size();
	for (uint32_t index = 0; index < count; index++) {
		if (disposed) {
			break;
		}
		// The vector keeps its references for the whole update; callbacks may still move its storage.
		const WorkItem item = instances[index];
		TweensGdHandle *instance = control(item);
		// Lifetime checks run even for paused tweens and the other process lane.
		if (instance->check_target() && instance->mode == p_mode && instance->can_advance()) {
			const double delta = instance->unscaled ? p_unscaled_delta : p_delta;
			if (const auto *chain = std::get_if<Ref<TweensGdChain>>(&item)) {
				(*chain)->advance(delta);
			} else {
				instance->advance(delta);
			}
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
	const LocalVector<WorkItem> snapshot(instances);
	for (const WorkItem &item : snapshot) {
		control(item)->cancel();
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
	// Cancel callbacks can free the owner, so every match is decided before any playback is cancelled.
	LocalVector<Ref<TweensGdHandle>> matches;
	for (const WorkItem &item : instances) {
		TweensGdHandle *instance = control(item);
		Node *instance_owner = instance->get_owner_node();
		if (instance->owner_id == owner_id || (p_include_children && instance_owner != nullptr && p_owner->is_ancestor_of(instance_owner))) {
			matches.push_back(Ref<TweensGdHandle>(instance));
		}
	}
	for (const Ref<TweensGdHandle> &instance : matches) {
		instance->cancel();
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
	const LocalVector<WorkItem> snapshot(instances);
	for (const WorkItem &item : snapshot) {
		control(item)->finish(REASON_RUNNER_DISPOSED);
	}
	if (!updating) {
		instances.clear();
	}
}

int64_t TweensGdScheduler::get_active_count() const {
	int64_t count = 0;
	for (const WorkItem &item : instances) {
		TweensGdHandle *instance = control(item);
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

TweensGdHandle *TweensGdScheduler::control(const WorkItem &p_item) {
	if (const auto *single = std::get_if<Ref<TweensGdHandle>>(&p_item)) {
		return single->ptr();
	}
	return std::get<Ref<TweensGdChain>>(p_item)->root.ptr();
}

Ref<TweensGdChain> TweensGdScheduler::add_chain(const Variant &p_target, const Variant &p_definitions,
		const Variant &p_owner, const Ref<TweensGdPlaybackOptions> &p_options) {
	const Ref<TweensGdScheduler> keep(this);
	if (!require_main_thread()) {
		return TweensGdChain::rejected("Use tweens.gd on Godot's main thread.");
	}
	const auto reject_chain = [&](const String &message) {
		report_error(message);
		return TweensGdChain::rejected(message);
	};
	if (p_definitions.get_type() != Variant::ARRAY || Array(p_definitions).is_empty()) {
		return reject_chain("A Chain needs a nonempty Array of definitions.");
	}
	const Array definitions = p_definitions;
	for (int64_t i = 0; i < definitions.size(); i++) {
		if (Object::cast_to<TweensGdDefinition>(definitions[i].get_validated_object()) == nullptr) {
			return reject_chain("Every Chain entry must be a definition.");
		}
	}
	Ref<TweensGdDefinition> empty;
	empty.instantiate();
	Ref<TweensGdChain> chain;
	chain.instantiate();
	chain->root = make_handle(p_target, empty, p_owner, p_options, false);
	if (chain->root->is_terminal()) {
		return TweensGdChain::rejected(chain->root->error);
	}
	LocalVector<Ref<TweensGdHandle>> leaves;
	for (int64_t i = 0; i < definitions.size(); i++) {
		auto leaf = make_handle(p_target, definitions[i], p_owner, p_options, false);
		if (leaf->is_terminal()) {
			return TweensGdChain::rejected(leaf->error);
		}
		leaf->coordinator_id = ObjectID(chain->get_instance_id());
		leaves.push_back(leaf);
	}
	chain->plan = std::make_unique<ExecutionPlan>(chain->root.ptr());
	const String failure = chain->plan->compile(leaves, true);
	if (!failure.is_empty()) {
		return reject_chain(failure);
	}
	chain->root->coordinator_id = ObjectID(chain->get_instance_id());
	chain->root->coordinator_root = true;
	chain->root->connect(names().ended, callable_mp(chain.ptr(), &TweensGdChain::on_ended));
	instances.push_back(chain);
	chain->root->bind_lifetime();
	return chain;
}

void TweensGdScheduler::compact() {
	uint32_t kept = 0;
	for (uint32_t index = 0; index < instances.size(); index++) {
		if (!control(instances[index])->is_terminal()) {
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
	if (p_endpoint.get_type() == Variant::QUATERNION && (Quaternion(p_endpoint).length_squared() == 0.0
			|| !Math::is_finite(Quaternion(p_endpoint).length_squared()))) {
		return "Quaternion endpoints must have finite, nonzero squared length.";
	}
	return String();
}

Ref<TweensGdHandle> TweensGdScheduler::reject(const String &p_message) {
	String message = p_message;
	report_error(message);
	return TweensGdHandle::rejected(message);
}

} // namespace godot
