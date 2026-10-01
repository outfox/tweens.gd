# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted
const T = preload("res://addons/tweens_gd/tweens.gd")

class Box extends RefCounted:
	var amount := 0.0

func run(suite: Object) -> bool:
	var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/chains.json"))
	for test in data.cases:
		var scheduler := TweensGdScheduler.new()
		var box := Box.new()
		var trace: Array[String] = []
		var definitions: Array = []
		for i in test.entries.size():
			var entry: Dictionary = test.entries[i]
			var definition := T.property(^"amount", entry.to, entry.duration, 0, entry.get("delay", 0.0))
			definition.from_value = entry.get("from", null)
			for field in ["offset", "repeats", "repeat_interval", "ping_pong_interval", "use_ping_pong"]:
				if entry.has(field): definition.set(field, entry[field])
			definition.on_add = func(_h): trace.append("add:%d" % i)
			definition.on_start = func(_h): trace.append("start:%d" % i)
			definition.on_end = func(_h):
				trace.append("end:%d" % i)
				if entry.has("end_set"): box.amount = entry.end_set
			definitions.append(definition)
		var chain := scheduler.add_chain(box, definitions)
		suite.check(trace.is_empty(), test.name + " factory has no playback hooks")
		suite.check(chain.pending_count == definitions.size(), test.name + " pending count")
		suite.check(scheduler.active_count == 1, test.name + " root count")
		suite.near(chain.duration, test.duration, test.name + " duration")
		for sample in test.samples:
			if sample.has("set"): box.amount = sample.set
			else: scheduler.update(sample.delta)
			suite.near(box.amount, sample.value, test.name)
			if sample.has("trace"): suite.check(trace == sample.trace, test.name + " chronological callbacks: " + str(trace))
		suite.check(chain.is_settled and chain.end == T.Reason.COMPLETED, test.name + " completes")
		suite.check(scheduler.active_count == 0, test.name + " root removed")
		scheduler.dispose()
	for delta in [1.0 / 120.0, 1.0 / 40.0, 0.3, 10.0]:
		var scheduler := TweensGdScheduler.new()
		var box := Box.new()
		var chain := scheduler.add_chain(box, [T.property(^"amount", 10.0, 0.15),
				T.property(^"amount", 20.0, 0.1), T.property(^"amount", 30.0, 0.1)])
		var time := 0.0
		while time < chain.duration:
			scheduler.update(delta)
			time += delta
		suite.check(chain.is_settled, "linked entries drain at " + str(delta))
		suite.near(box.amount, 30.0, "linked endpoint")
		scheduler.dispose()
	_controls(suite)
	_edges(suite)
	_lifetime(suite)
	_waits(suite)
	_priority_pause(suite)
	return true

func _controls(suite: Object) -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var state := {"leaf": null, "adds": 0}
	var definition := T.property(^"amount", 10.0, 1.0)
	definition.on_add = func(_h): state.adds += 1
	definition.on_start = func(h):
		state.leaf = h
		h.pause()
	var chain := scheduler.add_chain(box, [definition, T.property(^"amount", 20.0, 1.0)])
	scheduler.update(10.0)
	suite.check(chain.is_paused and chain.elapsed == 0.0, "leaf pause stops the whole Chain at its boundary")
	suite.near(box.amount, 0.0, "pause before first write")
	state.leaf.resume()
	scheduler.update(0.5)
	suite.near(box.amount, 5.0, "resume discards unused frame delta")
	suite.check(state.adds == 1, "resume does not replay on_add")
	state.leaf.cancel()
	suite.check(chain.end == T.Reason.CANCELLED and chain.pending_count == 0, "leaf cancellation stops pending links")
	box.amount = 0.0
	var solo := scheduler.add(box, T.property(^"amount", 10.0, 1.0, 0, -0.25))
	box.amount = 2.0
	scheduler.update(0.0)
	suite.near(box.amount, 4.0, "negative standalone delay pre-rolls captured state")
	scheduler.update(0.75)
	suite.check(solo.is_settled, "negative standalone completes")
	var invalid := scheduler.add_chain(box, [T.property(^"amount", 1.0, 1.0).with_repeats(T.INFINITE), definition])
	suite.check(invalid.end == T.Reason.FAILED, "successor after infinite anchor rejects")
	scheduler.dispose()



class HookAdapter extends T.Adapter:
	var state: Dictionary
	func prepare(_target: Object) -> String:
		state.events.append("prepare")
		if state.hook == "prepare": state.chain.pause()
		return ""
	func read(target: Object) -> Variant:
		state.events.append("read")
		if state.hook == "read": state.chain.pause()
		return target.amount
	func validate_value(value: Variant) -> String:
		if state.hook == "validate" and not state.paused:
			state.paused = true
			state.chain.pause()
		return super.validate_value(value)
	func write(target: Object, value: Variant) -> String:
		target.amount = value
		if state.hook == "write" and not state.paused:
			state.paused = true
			state.chain.pause()
		return ""
	func release() -> String:
		state.events.append("release")
		return "release fault" if state.get("fail_release", false) else ""

func _edges(suite: Object) -> void:
	for hook in ["prepare", "read", "validate", "add", "start", "write", "update", "end"]:
		var scheduler := TweensGdScheduler.new()
		var box := Box.new()
		var state := {"chain": null, "hook": hook, "events": [], "paused": false, "updates": []}
		var adapter := HookAdapter.new()
		adapter.state = state
		var definition := T.property(^"amount", 10.0, 1.0)
		definition.adapter = adapter
		definition.property = ^""
		definition.on_add = func(_h):
			state.events.append("add")
			if hook == "add": state.chain.pause()
		definition.on_start = func(_h):
			state.events.append("start")
			if hook == "start": state.chain.pause()
		definition.on_update = func(_h, value):
			state.updates.append(value)
			if hook == "update" and not state.paused:
				state.paused = true
				state.chain.pause()
		definition.on_end = func(_h):
			state.events.append("end")
			if hook == "end": state.chain.pause()
		var next := T.property(^"amount", 20.0, 1.0)
		next.on_add = func(_h): state.events.append("next")
		var chain := scheduler.add_chain(box, [definition, next])
		state.chain = chain
		scheduler.update(10.0)
		suite.check(chain.is_paused and not state.events.has("next"), hook + " interrupts activation/history")
		if hook == "write": suite.check(state.updates.is_empty(), "writer pause defers update notification")
		chain.resume()
		scheduler.update(10.0)
		suite.check(chain.is_settled and chain.error.is_empty(), hook + " resumes")
		if hook == "write": suite.check(state.updates == [0.0, 10.0], "writer resume publishes pending update without replaying write")
		for event in ["prepare", "read", "add", "start", "end", "release", "next"]:
			suite.check(state.events.count(event) == 1, hook + " consumes " + event + " once")
		state.chain = null
		scheduler.dispose()
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var trace: Array = []
	var a := T.property(^"amount", 1.0, 1.0)
	a.on_update = func(_h, _v): trace.append("solo A")
	var b := T.property(^"amount", 2.0, 1.0)
	b.on_update = func(_h, _v): trace.append("chain")
	var c := T.property(^"amount", 3.0, 1.0)
	c.on_update = func(_h, _v): trace.append("solo B")
	scheduler.add(box, a)
	scheduler.add_chain(box, [b])
	scheduler.add(box, c)
	scheduler.update(0.0)
	suite.check(trace == ["solo A", "chain", "solo B"], "mixed roots preserve enrollment order")
	trace.clear()
	scheduler.update(0.0)
	suite.check(trace == ["solo A", "chain", "solo B"], "zero delta remains a normal sample")
	scheduler.cancel_all()
	var state := {"chain": null, "events": [], "hook": "", "fail_release": true}
	var adapter := HookAdapter.new()
	adapter.state = state
	var broken := T.property(^"amount", 10.0, 1.0)
	broken.adapter = adapter
	broken.property = ^""
	broken.on_update = func(_h, _v):
		state.chain.cancel()
		suite.check(not state.chain.is_settled, "reentrant cancellation defers completion until callback returns")
	var next := T.property(^"amount", 20.0, 1.0)
	next.on_add = func(_h): state.events.append("next")
	var chain := scheduler.add_chain(box, [broken, next])
	state.chain = chain
	scheduler.update(10.0)
	suite.check(chain.is_settled and chain.error.contains("release fault"), "release failure survives Chain cancellation")
	suite.check(state.events == ["prepare", "read", "release"], "pending entries have no preparation or cleanup hooks")
	suite.check(chain.entry_count == 2 and chain.active_count == 0 and chain.pending_count == 0, "terminal Chain retains inspection counts")
	state.chain = null
	scheduler.dispose()

func _lifetime(suite: Object) -> void:
	var tree := Engine.get_main_loop() as SceneTree
	var node := Sprite2D.new()
	tree.root.add_child(node)
	var scheduler := TweensGdScheduler.new()
	var hooks: Array = []
	var pausing := T.position_2d_x(10.0, 1.0).with_from(0.0).with_offset(0.5)
	pausing.on_start = func(_h): tree.paused = true
	node.position.x = 4.0
	var paused_chain := scheduler.add_chain(node, [pausing])
	scheduler.update(10.0)
	suite.check(node.position.x == 4.0 and paused_chain.elapsed == 0.0, "tree pause from start interrupts before writing")
	tree.paused = false
	scheduler.update(0.25)
	suite.near(node.position.x, 7.5, "tree resume continues consumed start hook")
	scheduler.update(0.25)
	suite.check(paused_chain.is_settled, "tree-paused Chain completes after resume")
	var before := node.get_signal_connection_list("tree_exiting").size()
	var move := T.position_2d([10, 20], 1.0)
	move.on_add = func(_h): hooks.append("move")
	var fade := T.modulate_alpha(0.0, 1.0)
	fade.on_add = func(_h): hooks.append("fade")
	var chain := scheduler.add_chain(node, [move, fade])
	suite.check(hooks.is_empty(), "lifetime Chain enrollment defers leaf hooks")
	suite.check(node.get_signal_connection_list("tree_exiting").size() == before + 1, "one owner subscription per Chain")
	scheduler.update(1.5)
	suite.check(hooks == ["move", "fade"], "heterogeneous leaf hooks activate once in order")
	suite.check(node.position == Vector2(10, 20) and node.modulate.a == 0.5, "heterogeneous value types on one target")
	chain.pause()
	tree.root.remove_child(node)
	suite.check(chain.is_settled and chain.end == T.Reason.OWNER_EXITED, "owner exit settles a paused Chain")
	suite.check(hooks == ["move", "fade"], "owner exit does not replay leaf activation hooks")
	suite.check(node.get_signal_connection_list("tree_exiting").size() == before, "subscription released on settlement")
	node.free()
	var owner := Node.new()
	tree.root.add_child(owner)
	var material := StandardMaterial3D.new()
	material.roughness = 0.0
	var clock := T.playback_options(T.Process.PHYSICS, T.Pause.BOUND, true)
	chain = scheduler.add_chain(material, [T.material_roughness(1.0, 1.0), T.material_metallic(1.0, 1.0)], owner, clock)
	clock.process_mode = T.Process.PROCESS
	scheduler.update(0.5)
	suite.near(material.roughness, 0.0, "root policy snapshot preserves physics lane")
	scheduler.update(0.1, 0.5, T.Process.PHYSICS)
	suite.near(material.roughness, 0.5, "Chain uses one unscaled clock")
	chain.pause()
	tree.root.remove_child(owner)
	suite.check(chain.end == T.Reason.OWNER_EXITED and is_instance_valid(material), "resource Chain follows owner without disposing target")
	owner.free()
	scheduler.dispose()

	# Exercise owner exit without an external controller reference or an update keep-alive.
	scheduler = TweensGdScheduler.new()
	owner = Node.new()
	tree.root.add_child(owner)
	var state := {"events": [], "hook": "", "fail_release": true}
	var adapter := HookAdapter.new()
	adapter.state = state
	var definition := T.property(^"amount", 1.0, 1.0)
	definition.property = ^""
	definition.adapter = adapter
	definition.on_cancel = func(_h): scheduler.dispose()
	chain = scheduler.add_chain(Box.new(), [definition], owner)
	scheduler.update(0.0)
	var weak: WeakRef = weakref(chain)
	chain = null
	tree.root.remove_child(owner)
	suite.check(state.events.has("release") and scheduler.last_error.contains("release fault"), "reentrant owner-exit disposal retains cleanup errors")
	suite.check(weak.get_ref() == null, "settled Chain is released after reentrant scheduler disposal")
	owner.free()

func _record_wait(chain, results: Array, token = null) -> void:
	results.append(await chain.wait(token))

func _continue_chain(chain, scheduler, box) -> void:
	await chain.end
	scheduler.add(box, T.property(^"amount", 10.0, 1.0))

func _waits(suite: Object) -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var chain := scheduler.add_chain(box, [T.property(^"amount", 1.0, 1.0)])
	var results: Array = []
	var token := TweensGdCancellation.new()
	_record_wait(chain, results)
	_record_wait(chain, results)
	_record_wait(chain, results, token)
	token.cancel()
	suite.check(results == [T.Reason.WAIT_CANCELLED] and not chain.is_terminal, "wait-only cancellation leaves Chain running")
	var next_a := Box.new()
	var next_b := Box.new()
	chain.ended.connect(func(_reason): scheduler.add(next_a, T.property(^"amount", 10.0, 1.0)))
	_continue_chain(chain, scheduler, next_b)
	scheduler.update(10.0)
	suite.check(results == [T.Reason.WAIT_CANCELLED, T.Reason.COMPLETED, T.Reason.COMPLETED], "multiple active Chain waiters settle once")
	suite.near(next_a.amount + next_b.amount, 0.0, "signal and await continuations inherit no time")
	scheduler.update(0.25)
	suite.near(next_a.amount, 2.5, "signal-created root starts next update")
	suite.near(next_b.amount, 2.5, "await-created root starts next update")
	_record_wait(chain, results)
	suite.check(results.back() == T.Reason.COMPLETED, "late wait uses cached Chain reason")
	var clock := TweensGdPlayback.create(T.property(^"amount", 1.0, 1.0, 0, 0.5))
	clock.sample_at(2.0)
	clock.sample_at(0.75)
	suite.check(not clock.completed and clock.progress == 0.25, "pure sampler forgets previous completion")
	clock.sample_at(0.25)
	suite.check(not clock.started and clock.state == T.State.DELAYED and clock.progress == 0.0, "pure sampler resets delay state")
	scheduler.dispose()

func _priority_pause(suite: Object) -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var state := {"older_active": false, "paused": false, "writes": 0}
	var a := T.property(^"amount", 10.0, 1.0).with_from(0.0)
	a.on_add = func(_h): state.older_active = true
	var b := T.property(^"amount", 20.0, 2.0, 0, -1.5).with_from(0.0)
	b.on_update = func(h, value):
		state.writes += 1
		if state.older_active and value == 5.0 and not state.paused:
			state.paused = true
			h.pause()
	var chain := scheduler.add_chain(box, [a, b])
	scheduler.update(10.0)
	suite.check(chain.is_paused and box.amount == 5.0 and state.writes == 3, "pause during priority reassertion stops at boundary")
	chain.resume()
	scheduler.update(0.5)
	suite.check(box.amount == 10.0 and state.writes == 4 and chain.elapsed == 0.5, "priority reassertion resumes without repeating consumed write")
	scheduler.update(1.0)
	suite.check(chain.is_settled, "resumed priority phase completes")
	scheduler.dispose()
