// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "common.hpp"

#include <godot_cpp/classes/os.hpp>
#include <godot_cpp/core/memory.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/dictionary.hpp>
#include <godot_cpp/variant/typed_array.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace godot {
namespace tweens {

static Names *cached_names = nullptr;
static uint64_t main_thread_id = 0;

const Names &names() {
	return *cached_names;
}

void initialize() {
	cached_names = memnew(Names);
	Names &n = *cached_names;
	n.ended = "ended";
	n.cancelled = "cancelled";
	n.finished = "finished";
	n.error_reported = "error_reported";
	n.tree_exiting = "tree_exiting";
	n.captured_type = "_captured_type";
	n.copy = "copy";
	n.prepare = "prepare";
	n.read = "read";
	n.write = "write";
	n.restore = "restore";
	n.interpolate = "interpolate";
	n.validate_value = "validate_value";
	n.release = "release";
	n.runner_key = "_tweens_gd_runner";
	n.closing_key = "_tweens_gd_closing";
	main_thread_id = OS::get_singleton()->get_main_thread_id();
}

void uninitialize() {
	memdelete(cached_names);
	cached_names = nullptr;
}

bool is_main_thread() {
	return OS::get_singleton()->get_thread_caller_id() == main_thread_id;
}

bool require_main_thread() {
	if (is_main_thread()) {
		return true;
	}
	UtilityFunctions::push_error("Use tweens.gd on Godot's main thread.");
	return false;
}

bool is_alive(ObjectID p_id) {
	return p_id.is_valid() && gdextension_interface::object_get_instance_from_id(p_id) != nullptr;
}

void report(const String &p_message) {
	UtilityFunctions::push_error("tweens.gd: " + p_message);
}

String hook_error(const Variant &p_result, const char *p_hook) {
	switch (p_result.get_type()) {
		case Variant::NIL:
			return String();
		case Variant::STRING:
		case Variant::STRING_NAME:
			return p_result;
		default:
			return vformat("The adapter's %s method must return an error string (empty on success).", p_hook);
	}
}

void disconnect_all(Object *p_object, const StringName &p_signal) {
	const TypedArray<Dictionary> connections = p_object->get_signal_connection_list(p_signal);
	for (int64_t index = 0; index < connections.size(); index++) {
		const Dictionary connection = connections[index];
		p_object->disconnect(p_signal, connection["callable"]);
	}
}

} // namespace tweens
} // namespace godot
