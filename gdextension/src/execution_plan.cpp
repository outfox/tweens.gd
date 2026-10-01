// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "execution_plan.hpp"
#include "chain.hpp"
#include <algorithm>
#include <cmath>

namespace godot::tweens {
TweensGdHandle *ExecutionPlan::Entry::leaf() const {
	return Object::cast_to<TweensGdHandle>(ObjectDB::get_instance(id));
}
String ExecutionPlan::compile(const LocalVector<Ref<TweensGdHandle>> &p_leaves, bool p_retain) {
	double anchor = 0.0;
	for (const auto &leaf : p_leaves) {
		if (!std::isfinite(anchor)) {
			return "An infinite entry cannot have a successor.";
		}
		const auto &timing = leaf->clock;
		const double start = anchor + timing.gap();
		const double activation = std::min(anchor, start);
		const double end = start + timing.remaining();
		if (!std::isfinite(start) || !std::isfinite(activation) ||
				(std::isfinite(timing.remaining()) && !std::isfinite(end))) {
			return "The Chain schedule overflows.";
		}
		entries.push_back({ ObjectID(leaf->get_instance_id()), p_retain ? leaf : Ref<TweensGdHandle>(), activation, start, end });
		for (const double time : { activation, start, end }) {
			if (std::isfinite(time)) {
				boundaries.push_back(time);
			}
		}
		cursor = std::min(cursor, activation);
		duration = std::max(duration, end);
		anchor = end;
	}
	boundaries.push_back(cursor);
	std::sort(boundaries.begin(), boundaries.end());
	boundaries.erase(std::unique(boundaries.begin(), boundaries.end()), boundaries.end());
	return String();
}
bool ExecutionPlan::stopped() const {
	return root->is_terminal() || !root->can_advance() || !root->check_target();
}
bool ExecutionPlan::sample(Entry &entry) {
	auto *leaf = entry.leaf();
	const auto &timing = leaf->clock;
	const double local = cursor == entry.end ? timing.inner_delay() + timing.remaining()
			: cursor == entry.start			 ? timing.inner_delay()
											 : cursor - entry.activation;
	leaf->sample_at(local);
	if (leaf != root && leaf->is_terminal() && leaf->reason != REASON_COMPLETED) {
		if (!leaf->error.is_empty()) {
			root->error = leaf->error;
		}
		root->finish(leaf->reason);
	}
	return !stopped();
}
void ExecutionPlan::advance(double p_delta) {
	const double horizon = first ? p_delta : std::min(Timeline::MAX_TIME, cursor + p_delta);
	first = false;
	while (!stopped()) {
		if (!batch) {
			cursor = boundary < boundaries.size() && boundaries[boundary] <= horizon ? boundaries[boundary++] : horizon;
			batch = true;
			phase = 0;
			index = 0;
			reassert_from = entries.size();
		}
		if (phase == 0) {
			while (index < entries.size()) {
				auto &entry = entries[index];
				auto *leaf = entry.leaf();
				if (entry.active && !leaf->is_terminal() && !sample(entry)) {
					return;
				}
				index++;
				if (stopped()) {
					return;
				}
			}
			phase = 1;
			index = 0;
		}
		if (phase == 1) {
			while (index < entries.size()) {
				auto &entry = entries[index];
				auto *leaf = entry.leaf();
				if (!entry.active && entry.activation <= cursor) {
					for (size_t later = index + 1; later < entries.size(); later++) {
						if (entries[later].active && !entries[later].leaf()->is_terminal()) {
							reassert_from = std::min(reassert_from, index + 1);
							break;
						}
					}
					entry.active = entry.activating = true;
				}
				if (entry.activating && !leaf->is_terminal() && !sample(entry)) {
					return;
				}
				entry.activating = false;
				index++;
				if (stopped()) {
					return;
				}
			}
			phase = 2;
			index = reassert_from;
		}
		// Reassert later entries after an older definition's activation write.
		while (index < entries.size()) {
			auto &entry = entries[index];
			if (entry.active && !entry.leaf()->is_terminal() && !sample(entry)) {
				return;
			}
			index++;
			if (stopped()) {
				return;
			}
		}
		batch = false;
		bool complete = true;
		for (auto &entry : entries) {
			complete = complete && entry.leaf()->is_settled();
		}
		if (complete) {
			root->finish(REASON_COMPLETED);
			return;
		}
		if (cursor == horizon) {
			return;
		}
	}
}
void ExecutionPlan::stop(int64_t p_reason) {
	for (auto &entry : entries) {
		auto *leaf = entry.leaf();
		if (leaf != root && !leaf->is_terminal()) {
			leaf->finish(p_reason == REASON_COMPLETED ? REASON_CANCELLED : p_reason);
		}
	}
}
void ExecutionPlan::release() {
	String errors;
	for (auto &entry : entries) {
		auto *leaf = entry.leaf();
		if (leaf != root && !leaf->error.is_empty()) {
			errors += (errors.is_empty() ? String() : String("\n")) + leaf->error;
			if (auto *chain = root->coordinator()) {
				chain->diagnostics.push_back(leaf->error);
			}
		}
	}
	if (!errors.is_empty()) {
		root->error = errors;
		root->state = STATE_FAULTED;
	}
	for (auto &entry : entries) {
		entry.retained.unref();
	}
}
int64_t ExecutionPlan::active_count() const {
	if (root->is_terminal()) {
		return 0;
	}
	int64_t count = 0;
	for (const auto &entry : entries) {
		if (entry.active && !entry.leaf()->is_terminal()) {
			count++;
		}
	}
	return count;
}
int64_t ExecutionPlan::pending_count() const {
	if (root->is_terminal()) {
		return 0;
	}
	int64_t count = 0;
	for (const auto &entry : entries) {
		if (!entry.active) {
			count++;
		}
	}
	return count;
}
} //namespace godot::tweens
