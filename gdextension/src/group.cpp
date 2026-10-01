// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "group.hpp"

#include "awaiting.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/templates/hash_set.hpp>
#include <godot_cpp/variant/signal.hpp>

namespace godot {

using namespace tweens;

void TweensGdGroupWatcher::_bind_methods() {
	ADD_SIGNAL(MethodInfo("ended", PropertyInfo(Variant::INT, "reason")));
}

void TweensGdGroupWatcher::setup(const TypedArray<TweensGdHandle> &p_members, const String &p_rejection) {
	if (!p_rejection.is_empty() || p_members.is_empty()) {
		settled = true;
		reason = REASON_FAILED;
		errors.push_back(!p_rejection.is_empty() ? p_rejection : String("A group needs at least one tween."));
		return;
	}
	remaining = p_members.size();
	pending.resize(remaining);
	members.resize(remaining);
	// Subscribe to every pending member before processing any settled failures.
	const Variant keep_alive = this;
	for (uint32_t index = 0; index < remaining; index++) {
		pending[index] = true;
		const Ref<TweensGdHandle> member = p_members[index];
		members[index] = ObjectID(member->get_instance_id());
		if (!member->is_settled()) {
			member->connect(names().ended, callable_mp(this, &TweensGdGroupWatcher::deliver).bind(keep_alive, int64_t(index)));
		}
	}
	for (uint32_t index = 0; index < members.size(); index++) {
		const Ref<TweensGdHandle> member = p_members[index];
		if (member->is_settled()) {
			accept(index);
		}
	}
}

void TweensGdGroupWatcher::deliver(int64_t p_reason, const Variant &p_keep_alive, int64_t p_index) {
	accept(uint32_t(p_index));
}

void TweensGdGroupWatcher::accept(uint32_t p_index) {
	// Cancellation can recursively settle members that construction also visits.
	if (!pending[p_index]) {
		return;
	}
	const Ref<TweensGdGroupWatcher> keep(this);
	pending[p_index] = false;
	remaining--;
	TweensGdHandle *member = Object::cast_to<TweensGdHandle>(ObjectDB::get_instance(members[p_index]));
	if (member != nullptr) {
		if (!member->get_error().is_empty()) {
			errors.push_back(member->get_error());
		}
		if (member->get_completion_reason() != REASON_COMPLETED) {
			if (reason == REASON_NONE) {
				reason = member->get_completion_reason();
			}
			if (!stopping) {
				stopping = true;
				for (const ObjectID &id : members) {
					TweensGdHandle *sibling = Object::cast_to<TweensGdHandle>(ObjectDB::get_instance(id));
					if (sibling != nullptr && !sibling->is_terminal()) {
						sibling->cancel();
					}
				}
			}
		}
	}
	if (remaining == 0 && !settled) {
		settled = true;
		if (reason == REASON_NONE) {
			reason = REASON_COMPLETED;
		}
		emit_signal(names().ended, reason);
		disconnect_all(this, names().ended);
	}
}

void TweensGdGroup::_bind_methods() {
	ClassDB::bind_static_method("TweensGdGroup", D_METHOD("of", "tweens"), &TweensGdGroup::of);
	ClassDB::bind_static_method("TweensGdGroup", D_METHOD("rejected", "message"), &TweensGdGroup::rejected);
	ClassDB::bind_method(D_METHOD("get_members"), &TweensGdGroup::get_members);
	ClassDB::bind_method(D_METHOD("is_settled"), &TweensGdGroup::is_settled);
	ClassDB::bind_method(D_METHOD("is_terminal"), &TweensGdGroup::is_settled);
	ClassDB::bind_method(D_METHOD("get_completion_reason"), &TweensGdGroup::get_completion_reason);
	ClassDB::bind_method(D_METHOD("get_end"), &TweensGdGroup::get_end);
	ClassDB::bind_method(D_METHOD("get_error"), &TweensGdGroup::get_error);
	ClassDB::bind_method(D_METHOD("get_errors"), &TweensGdGroup::get_errors);
	ClassDB::bind_method(D_METHOD("is_paused"), &TweensGdGroup::is_paused);
	ClassDB::bind_method(D_METHOD("set_paused", "paused"), &TweensGdGroup::set_paused);
	ClassDB::bind_method(D_METHOD("_get_watcher"), &TweensGdGroup::get_watcher);
	ClassDB::bind_method(D_METHOD("pause"), &TweensGdGroup::pause);
	ClassDB::bind_method(D_METHOD("resume"), &TweensGdGroup::resume);
	ClassDB::bind_method(D_METHOD("cancel"), &TweensGdGroup::cancel);
	ClassDB::bind_method(D_METHOD("wait", "cancellation"), &TweensGdGroup::wait, DEFVAL(Variant()));

	ADD_PROPERTY(PropertyInfo(Variant::ARRAY, "members", PROPERTY_HINT_ARRAY_TYPE, "TweensGdHandle"), "", "get_members");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_terminal"), "", "is_terminal");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_settled"), "", "is_settled");
	ADD_PROPERTY(PropertyInfo(Variant::INT, "completion_reason"), "", "get_completion_reason");
	ADD_PROPERTY(PropertyInfo(Variant::NIL, "end", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_DEFAULT | PROPERTY_USAGE_NIL_IS_VARIANT), "", "get_end");
	ADD_PROPERTY(PropertyInfo(Variant::STRING, "error"), "", "get_error");
	ADD_PROPERTY(PropertyInfo(Variant::ARRAY, "errors", PROPERTY_HINT_ARRAY_TYPE, "String"), "", "get_errors");
	ADD_PROPERTY(PropertyInfo(Variant::BOOL, "is_paused"), "set_paused", "is_paused");
	ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "_watcher", PROPERTY_HINT_NONE, "", PROPERTY_USAGE_NONE, "TweensGdGroupWatcher"), "",
			"_get_watcher");

	ADD_SIGNAL(MethodInfo("ended", PropertyInfo(Variant::INT, "reason")));
}

Ref<TweensGdGroup> TweensGdGroup::create(const TypedArray<TweensGdHandle> &p_members, const String &p_rejection) {
	Ref<TweensGdGroup> group;
	group.instantiate();
	group->members = p_members.duplicate();
	group->watcher.instantiate();
	group->watcher->setup(group->members, p_rejection);
	if (!group->watcher->settled) {
		group->watcher->connect(names().ended, callable_mp(group.ptr(), &TweensGdGroup::on_ended));
	}
	return group;
}

Ref<TweensGdGroup> TweensGdGroup::reject_group(const String &p_message) {
	report(p_message);
	return create(TypedArray<TweensGdHandle>(), p_message);
}

Ref<TweensGdGroup> TweensGdGroup::rejected(const String &p_message) {
	return create(TypedArray<TweensGdHandle>(), p_message.is_empty() ? String("The group could not be created.") : p_message);
}

Ref<TweensGdGroup> TweensGdGroup::of(const Variant &p_tweens) {
	if (!is_main_thread()) {
		return reject_group("Use tweens.gd on Godot's main thread.");
	}
	if (p_tweens.get_type() != Variant::ARRAY || Array(p_tweens).is_empty()) {
		return reject_group("A group needs a nonempty Array of tween handles.");
	}
	const Array tweens = p_tweens;
	TypedArray<TweensGdHandle> unique;
	HashSet<uint64_t> seen;
	for (int64_t index = 0; index < tweens.size(); index++) {
		const Variant &entry = tweens[index];
		TweensGdHandle *member = Object::cast_to<TweensGdHandle>(entry.get_validated_object());
		if (member == nullptr) {
			return reject_group("Every group member must be a tween handle.");
		}
		const uint64_t id = member->get_instance_id();
		if (seen.has(id)) {
			continue;
		}
		seen.insert(id);
		unique.push_back(member);
	}
	return create(unique, String());
}

Variant TweensGdGroup::get_end() {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	return is_settled() ? Variant(get_completion_reason()) : Variant(Signal(this, names().ended));
}

String TweensGdGroup::get_error() const {
	return String("\n").join(PackedStringArray(watcher->errors));
}

bool TweensGdGroup::is_paused() const {
	bool active = false;
	for (int64_t index = 0; index < members.size(); index++) {
		const Ref<TweensGdHandle> member = members[index];
		if (member->is_terminal()) {
			continue;
		}
		active = true;
		if (!member->is_paused()) {
			return false;
		}
	}
	return active;
}

void TweensGdGroup::set_paused(bool p_paused) {
	if (!require_main_thread()) {
		return;
	}
	const Ref<TweensGdGroup> keep(this);
	for (int64_t index = 0; index < members.size(); index++) {
		const Ref<TweensGdHandle> member = members[index];
		if (!member->is_terminal()) {
			member->set_paused(p_paused);
		}
	}
}

void TweensGdGroup::cancel() {
	if (!require_main_thread()) {
		return;
	}
	// Member callbacks and waiters may drop the last reference to this group.
	const Ref<TweensGdGroup> keep(this);
	for (int64_t index = 0; index < members.size(); index++) {
		const Ref<TweensGdHandle> member = members[index];
		member->cancel();
	}
}

Variant TweensGdGroup::wait(const Ref<TweensGdCancellation> &p_cancellation) {
	if (!require_main_thread()) {
		return REASON_FAILED;
	}
	if (is_settled()) {
		return get_completion_reason();
	}
	return TweensGdAwaiting::wait_for(this, p_cancellation);
}

void TweensGdGroup::on_ended(int64_t p_reason) {
	// A finishing wait releases the group it kept alive, often the last reference to a temporary group.
	const Ref<TweensGdGroup> keep(this);
	emit_signal(names().ended, p_reason);
	disconnect_all(this, names().ended);
}

} // namespace godot
