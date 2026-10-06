// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/core/object.hpp>
#include <godot_cpp/core/object_id.hpp>
#include <godot_cpp/variant/string_name.hpp>
#include <godot_cpp/variant/variant.hpp>

namespace godot {

// Values mirror types.gd, which keeps the GDScript enums.
namespace tweens {

inline constexpr int64_t INFINITE_REPEATS = -1;

// Centered join width for mixed In/Out eases; matches Easing.DefaultBlend in C#.
inline constexpr double DEFAULT_BLEND = 0.1;

enum Fill : int64_t {
	FILL_NONE = 0,
	FILL_APPLY_FROM_DURING_DELAY = 1,
	FILL_RETAIN_FINAL_VALUE = 2,
	FILL_BOTH = 3,
};

enum Lane : int64_t {
	LANE_PROCESS = 0,
	LANE_PHYSICS = 1,
};

enum PauseMode : int64_t {
	PAUSE_BOUND = 0,
	PAUSE_SCENE_TREE = 1,
	PAUSE_ALWAYS = 2,
};

enum State : int64_t {
	STATE_DELAYED = 0,
	STATE_PLAYING = 1,
	STATE_INTERVAL = 2,
	STATE_COMPLETED = 3,
	STATE_CANCELLED = 4,
	STATE_FAULTED = 5,
};

enum Reason : int64_t {
	REASON_NONE = -1,
	REASON_COMPLETED = 0,
	REASON_CANCELLED = 1,
	REASON_TARGET_FREED = 2,
	REASON_OWNER_EXITED = 3,
	REASON_RUNNER_DISPOSED = 4,
	REASON_FAILED = 5,
	REASON_WAIT_CANCELLED = 6,
};

// Engine-owned names must not outlive the engine, so they live between initialize() and uninitialize().
struct Names {
	StringName ended;
	StringName cancelled;
	StringName finished;
	StringName error_reported;
	StringName tree_exiting;
	StringName captured_type;
	StringName copy;
	StringName prepare;
	StringName read;
	StringName write;
	StringName restore;
	StringName interpolate;
	StringName validate_value;
	StringName release;
	StringName runner_key;
	StringName closing_key;
};

const Names &names();
void initialize();
void uninitialize();

bool is_main_thread();
// Reports misuse like the GDScript implementation did.
bool require_main_thread();
bool is_alive(ObjectID p_id);
void report(const String &p_message);
// Adapter hooks return an error string; anything else is a broken override.
String hook_error(const Variant &p_result, const char *p_hook);
// Completion is one-shot: observers are released once they have been notified.
void disconnect_all(Object *p_object, const StringName &p_signal);

} // namespace tweens
} // namespace godot
