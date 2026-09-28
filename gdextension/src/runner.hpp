// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include "group.hpp"
#include "scheduler.hpp"

#include <godot_cpp/classes/node.hpp>
#include <godot_cpp/classes/scene_tree.hpp>

namespace godot {

// Automatic playback for one SceneTree, attached under its root and advanced in both process lanes.
class TweensGdRunner : public Node {
	GDCLASS(TweensGdRunner, Node)

	Ref<TweensGdScheduler> scheduler;
	uint64_t last_ticks = 0;
	ObjectID tree_id;
	Callable tree_exit;

	SceneTree *get_bound_tree() const;
	void bind_tree(SceneTree *p_tree);
	void attach(const Variant &p_tree);
	void shutdown();
	void tree_exiting();
	static Ref<TweensGdHandle> reject(const String &p_message);

protected:
	static void _bind_methods();
	void _notification(int p_what);

public:
	TweensGdRunner();

	void _process(double p_delta) override;
	void _physics_process(double p_delta) override;
	void _exit_tree() override;

	Ref<TweensGdScheduler> get_scheduler() const { return scheduler; }

	// The runner bound to this tree, or null.
	static TweensGdRunner *find(SceneTree *p_tree);
	static TweensGdRunner *acquire(SceneTree *p_tree);
	// Node targets bind to themselves; Resources/Objects need an explicit owner.
	// Always returns an awaitable handle, including an already-failed handle on rejection.
	static Ref<TweensGdHandle> play(const Variant &p_target, const Ref<TweensGdDefinition> &p_definition, const Variant &p_owner);
	static Ref<TweensGdGroup> play_all(const Variant &p_target, const Variant &p_definitions, const Variant &p_owner);
	static void cancel_tweens(Node *p_owner, bool p_include_children);
};

} // namespace godot
