// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "runner.hpp"

#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/time.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/core/class_db.hpp>

namespace godot {

using namespace tweens;

void TweensGdRunner::_bind_methods() {
	ClassDB::bind_method(D_METHOD("get_scheduler"), &TweensGdRunner::get_scheduler);
	ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "scheduler", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_NONE, "TweensGdScheduler"), "",
			"get_scheduler");
	const char *name = "TweensGdRunner";
	ClassDB::bind_static_method(name, D_METHOD("find", "tree"), &TweensGdRunner::find);
	ClassDB::bind_static_method(name, D_METHOD("acquire", "tree"), &TweensGdRunner::acquire);
	ClassDB::bind_static_method(name, D_METHOD("play", "target", "definition", "owner"), &TweensGdRunner::play, DEFVAL(Variant()));
	ClassDB::bind_static_method(name, D_METHOD("play_all", "target", "definitions", "owner"), &TweensGdRunner::play_all, DEFVAL(Variant()));
	ClassDB::bind_static_method(name, D_METHOD("cancel_tweens", "owner", "include_children"), &TweensGdRunner::cancel_tweens, DEFVAL(false));
}

TweensGdRunner::TweensGdRunner() {
	set_name("TweensGd");
	set_process_mode(PROCESS_MODE_ALWAYS);
	set_process_priority(1000);
	set_physics_process_priority(1000);
	last_ticks = Time::get_singleton()->get_ticks_usec();
	scheduler.instantiate();
	scheduler->prints_errors = true;
}

void TweensGdRunner::_notification(int p_what) {
	if (p_what == NOTIFICATION_PREDELETE) {
		shutdown();
	}
}

void TweensGdRunner::_process(double p_delta) {
	const uint64_t now = Time::get_singleton()->get_ticks_usec();
	const double unscaled = double(now - last_ticks) / 1000000.0;
	last_ticks = now;
	scheduler->update(p_delta, unscaled, LANE_PROCESS);
}

void TweensGdRunner::_physics_process(double p_delta) {
	scheduler->update(p_delta, 1.0 / double(Engine::get_singleton()->get_physics_ticks_per_second()), LANE_PHYSICS);
}

void TweensGdRunner::_exit_tree() {
	shutdown();
}

SceneTree *TweensGdRunner::get_bound_tree() const {
	return is_alive(tree_id) ? Object::cast_to<SceneTree>(ObjectDB::get_instance(tree_id)) : nullptr;
}

void TweensGdRunner::bind_tree(SceneTree *p_tree) {
	tree_id = ObjectID(p_tree->get_instance_id());
	tree_exit = callable_mp(this, &TweensGdRunner::tree_exiting);
	p_tree->get_root()->connect(names().tree_exiting, tree_exit);
	callable_mp(this, &TweensGdRunner::attach).call_deferred(p_tree);
}

void TweensGdRunner::attach(const Variant &p_tree) {
	SceneTree *tree = Object::cast_to<SceneTree>(p_tree.get_validated_object());
	if (tree == nullptr || find(tree) != this || scheduler->is_disposed()) {
		queue_free();
		return;
	}
	tree_id = ObjectID(tree->get_instance_id());
	tree->get_root()->add_child(this);
}

void TweensGdRunner::shutdown() {
	scheduler->dispose();
	SceneTree *tree = get_bound_tree();
	if (tree == nullptr) {
		return;
	}
	Window *root = tree->get_root();
	if (root != nullptr && root->is_connected(names().tree_exiting, tree_exit)) {
		root->disconnect(names().tree_exiting, tree_exit);
	}
	if (find(tree) == this) {
		tree->remove_meta(names().runner_key);
	}
}

void TweensGdRunner::tree_exiting() {
	if (SceneTree *tree = get_bound_tree()) {
		tree->set_meta(names().closing_key, true);
	}
	shutdown();
	// Also free a runner whose deferred attachment never happened.
	if (!is_inside_tree()) {
		queue_free();
	}
}

TweensGdRunner *TweensGdRunner::find(SceneTree *p_tree) {
	if (p_tree == nullptr || !p_tree->has_meta(names().runner_key)) {
		return nullptr;
	}
	return Object::cast_to<TweensGdRunner>(p_tree->get_meta(names().runner_key).get_validated_object());
}

TweensGdRunner *TweensGdRunner::acquire(SceneTree *p_tree) {
	if (!require_main_thread()) {
		return nullptr;
	}
	if (p_tree == nullptr) {
		report("A runner needs a SceneTree.");
		return nullptr;
	}
	TweensGdRunner *existing = find(p_tree);
	if (existing != nullptr && !existing->is_queued_for_deletion() && !existing->scheduler->is_disposed()) {
		return existing;
	}
	TweensGdRunner *runner = memnew(TweensGdRunner);
	p_tree->set_meta(names().runner_key, runner);
	runner->bind_tree(p_tree);
	return runner;
}

Ref<TweensGdHandle> TweensGdRunner::reject(const String &p_message) {
	report(p_message);
	return TweensGdHandle::rejected(p_message);
}

Ref<TweensGdHandle> TweensGdRunner::play(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition, const Variant &p_owner) {
	if (!is_main_thread()) {
		return reject("Use tweens.gd on Godot's main thread.");
	}
	if (p_target.get_validated_object() == nullptr) {
		return reject("The target is invalid.");
	}
	Variant owner = p_owner;
	if (SceneTree *tree = Object::cast_to<SceneTree>(owner.get_validated_object())) {
		owner = tree->get_root();
	}
	if (owner.get_type() == Variant::NIL && Object::cast_to<Node>(p_target.get_validated_object()) != nullptr) {
		owner = p_target;
	}
	Node *node = Object::cast_to<Node>(owner.get_validated_object());
	if (node == nullptr || !node->is_inside_tree() || node->is_queued_for_deletion()) {
		return reject("Automatic playback needs an owner inside the scene tree.");
	}
	SceneTree *tree = node->get_tree();
	if (tree->has_meta(names().closing_key)) {
		return reject("The scene tree is shutting down.");
	}
	return acquire(tree)->scheduler->add(p_target, p_definition, owner);
}

Ref<TweensGdGroup> TweensGdRunner::play_all(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner) {
	if (!is_main_thread() || p_definitions.get_type() != Variant::ARRAY || Array(p_definitions).is_empty()) {
		return TweensGdGroup::of(Array());
	}
	const Array definitions = p_definitions;
	for (int64_t index = 0; index < definitions.size(); index++) {
		if (Object::cast_to<TweensGdDefinition>(definitions[index].get_validated_object()) == nullptr) {
			return TweensGdGroup::of(Array::make(reject("Every entry must be a tween definition.")));
		}
	}
	Array handles;
	for (int64_t index = 0; index < definitions.size(); index++) {
		const Ref<TweensGdHandle> handle = play(p_target, definitions[index], p_owner);
		handles.push_back(handle);
		if (handle->get_completion_reason() == REASON_FAILED) {
			break;
		}
	}
	return TweensGdGroup::of(handles);
}

void TweensGdRunner::cancel_tweens(Node *p_owner, bool p_include_children) {
	if (!is_main_thread() || p_owner == nullptr || !p_owner->is_inside_tree()) {
		return;
	}
	TweensGdRunner *runner = find(p_owner->get_tree());
	if (runner != nullptr) {
		runner->scheduler->cancel_owner(p_owner, p_include_children);
	}
}

} // namespace godot
