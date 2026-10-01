// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once
#include "handle.hpp"
#include <vector>

namespace godot::tweens {
class ExecutionPlan {
	struct Entry {
		ObjectID id;
		Ref<TweensGdHandle> retained;
		double activation, start, end;
		bool active = false;
		bool activating = false;
		TweensGdHandle *leaf() const;
	};
	TweensGdHandle *root;
	std::vector<Entry> entries;
	std::vector<double> boundaries;
	size_t boundary = 0, index = 0, reassert_from = 0;
	int phase = 0;
	bool first = true, batch = false;
	double cursor = 0.0, duration = 0.0;
	bool sample(Entry &entry);
	bool stopped() const;

public:
	explicit ExecutionPlan(TweensGdHandle *p_root) :
			root(p_root) {}
	String compile(const LocalVector<Ref<TweensGdHandle>> &p_leaves, bool p_retain);
	void advance(double p_delta);
	void stop(int64_t p_reason);
	void release();
	double get_elapsed() const { return cursor > 0.0 ? cursor : 0.0; }
	double get_duration() const { return duration; }
	int64_t entry_count() const { return entries.size(); }
	int64_t active_count() const;
	int64_t pending_count() const;
};
} //namespace godot::tweens
