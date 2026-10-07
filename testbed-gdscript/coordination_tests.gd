# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
const Suite = preload("res://tests.gd")
var host: Suite

# Sources are a TweensGdHandle or a TweensGdGroup, which share no base class with wait() and end.
func record_wait(source: Object, token: TweensGdCancellation, results: Array) -> void:
	results.append(await source.call(&"wait", token))

func continuation(source: TweensGdHandle, token: TweensGdCancellation, scheduler: TweensGdScheduler, results: Array) -> void:
	await source.wait(token)
	results.append(scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 1.0)))

func record_end(source: Object, results: Array) -> void:
	results.append(await source.get(&"end"))

func continue_end(source: Object, scheduler: TweensGdScheduler, results: Array) -> void:
	await source.get(&"end")
	results.append(scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 1.0)))

func check_end() -> void:
	var scheduler := TweensGdScheduler.new()
	for cancel: bool in [false, true]:
		var handle := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.5))
		var group := T.group([handle])
		var results: Array = []
		@warning_ignore("missing_await")
		record_end(handle, results)
		@warning_ignore("missing_await")
		record_end(handle, results)
		@warning_ignore("missing_await")
		record_end(group, results)
		host.check(results.is_empty(), "end suspends pending handle and group waiters")
		if cancel: handle.cancel()
		else: scheduler.update(0.5)
		var expected := T.Reason.CANCELLED if cancel else T.Reason.COMPLETED
		host.check(results == [expected, expected, expected], "end resumes every waiter with the reason")
		var immediate: bool = await handle.end == expected and await group.end == expected
		host.check(immediate, "end returns immediately after settlement")
		host.check(handle.ended.get_connections().is_empty() and group.ended.get_connections().is_empty(), "end releases completed subscriptions")
	var rejected := TweensGdHandle.rejected("test rejection")
	var rejected_reason: int = await rejected.end
	host.check(rejected_reason == T.Reason.FAILED, "rejected start can be awaited through end")
	var rejected_group := TweensGdGroup.rejected("test rejection")
	var rejected_group_reason: int = await rejected_group.end
	host.check(rejected_group_reason == T.Reason.FAILED, "rejected group can be awaited through end")
	for grouped: bool in [false, true]:
		var handle := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.5))
		var after: Array = []
		var source: Object = handle
		if grouped: source = T.group([handle])
		@warning_ignore("missing_await")
		continue_end(source, scheduler, after)
		scheduler.update(0.75)
		scheduler.update(0.0)
		host.check(after.size() == 1, "end continues a sequence")
		var root_value: float = after[0].value
		host.near(root_value, 0.0, "end starts an independent root")
	scheduler.dispose()

func run(owner: Node) -> bool:
	host = owner
	await check_end()
	var scheduler := TweensGdScheduler.new()
	var a := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.5))
	var group := T.group([a])
	var token := TweensGdCancellation.new()
	var results: Array = []
	var uninterrupted: Array = []
	@warning_ignore("missing_await")
	record_wait(a, token, results)
	@warning_ignore("missing_await")
	record_wait(group, token, results)
	@warning_ignore("missing_await")
	record_wait(a, null, uninterrupted)
	token.cancel()
	token.cancel()
	host.check(results == [T.Reason.WAIT_CANCELLED, T.Reason.WAIT_CANCELLED], "token cancels only its handle and group waits")
	host.check(not a.is_terminal and not group.is_settled and uninterrupted.is_empty(), "wait cancellation leaves playback and other waiters active")
	host.check(token.cancelled.get_connections().is_empty(), "cancelled waits disconnect token observers")
	var cancelled_reason: int = await a.wait(token)
	host.check(cancelled_reason == T.Reason.WAIT_CANCELLED, "already-cancelled token returns immediately")
	scheduler.update(0.5)
	host.check(uninterrupted == [T.Reason.COMPLETED], "uncancelled waiter resumes on playback completion")
	var settled_reason: int = await a.wait(token)
	host.check(settled_reason == T.Reason.COMPLETED, "already-settled playback wins over token cancellation")
	var completing := TweensGdCancellation.new()
	var b := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.25))
	var after: Array = []
	@warning_ignore("missing_await")
	continuation(b, completing, scheduler, after)
	scheduler.update(0.5)
	host.check(completing.cancelled.get_connections().is_empty(), "successful wait releases token subscription")
	scheduler.update(0.0)
	var root_value: float = after[0].value
	host.near(root_value, 0.0, "token-aware waits start an independent root")
	var definitions := [T.value(0.0, 1.0, 1.0), T.value(0.0, 2.0, 2.0)]
	var batch := scheduler.add_all(RefCounted.new(), definitions)
	scheduler.update(2.0)
	var batch_reason: int = await batch.wait()
	host.check(batch_reason == T.Reason.COMPLETED and batch.members.size() == 2, "manual multi-start groups every definition")
	var starts: Array = []
	var definition := T.value(0.0, 1.0, 1.0)
	definition.on_add = func(_h: TweensGdHandle) -> void: starts.append(true)
	var invalid := scheduler.add_all(RefCounted.new(), [definition, null])
	host.check(invalid.is_settled and invalid.completion_reason == T.Reason.FAILED and starts.is_empty(), "multi-start validates array before running callbacks")
	var failed := scheduler.add_all(RefCounted.new(), [definition, T.value(0.0, 1.0, -1.0), definition])
	host.check(failed.is_settled and failed.completion_reason == T.Reason.FAILED and starts.is_empty(), "failed multi-start cancels preceding siblings and stops starting definitions")
	scheduler.dispose()
	# Exercise automatic resource playback bound to the SceneTree itself.
	var resource := Gradient.new()
	var tree_bound := T.play(resource, T.value(0.0, 1.0), host.get_tree())
	var node := Node2D.new()
	host.add_child(node)
	var automatic := T.play_all(node, [T.position_2d(Vector2(4, 8)), T.modulate_alpha(0.25)])
	await host.get_tree().process_frame
	await host.get_tree().process_frame
	host.check(tree_bound.is_settled and tree_bound.completion_reason == T.Reason.COMPLETED, "resource playback accepts a SceneTree lifetime")
	var automatic_reason: int = await automatic.wait()
	host.check(automatic_reason == T.Reason.COMPLETED and node.position == Vector2(4, 8) and node.modulate.a == 0.25, "automatic multi-start applies named helpers")
	var runner := TweensGdRunner.find(host.get_tree())
	runner.free()
	node.free()
	return true
