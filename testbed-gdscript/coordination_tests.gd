# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
var host: Node

func record_wait(source, token, results: Array) -> void:
	results.append(await source.wait(token))

func continuation(source, token, scheduler, results: Array) -> void:
	await source.wait(token)
	results.append(scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 1.0)))

func run(owner: Node) -> bool:
	host = owner
	var scheduler := T.Scheduler.new()
	var a := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.5))
	var group := T.group([a])
	var token := T.Cancellation.new()
	var results: Array = []
	var uninterrupted: Array = []
	record_wait(a, token, results)
	record_wait(group, token, results)
	record_wait(a, null, uninterrupted)
	token.cancel()
	token.cancel()
	host.check(results == [T.Reason.WAIT_CANCELLED, T.Reason.WAIT_CANCELLED], "token cancels only its handle and group waits")
	host.check(not a.is_terminal and not group.is_settled and uninterrupted.is_empty(), "wait cancellation leaves playback and other waiters active")
	host.check(token.cancelled.get_connections().is_empty(), "cancelled waits disconnect token observers")
	host.check(await a.wait(token) == T.Reason.WAIT_CANCELLED, "already-cancelled token returns immediately")
	scheduler.update(0.5)
	host.check(uninterrupted == [T.Reason.COMPLETED], "uncancelled waiter resumes on playback completion")
	host.check(await a.wait(token) == T.Reason.COMPLETED, "already-settled playback wins over token cancellation")
	var completing := T.Cancellation.new()
	var b := scheduler.add(RefCounted.new(), T.value(0.0, 1.0, 0.25))
	var after: Array = []
	continuation(b, completing, scheduler, after)
	scheduler.update(0.5)
	host.check(completing.cancelled.get_connections().is_empty(), "successful wait releases token subscription")
	scheduler.update(0.0)
	host.near(after[0].value, 0.25, "token-aware waits preserve continuation overshoot")
	var definitions := [T.value(0.0, 1.0, 1.0), T.value(0.0, 2.0, 2.0)]
	var batch := scheduler.add_all(RefCounted.new(), definitions)
	scheduler.update(2.0)
	host.check(await batch.wait() == T.Reason.COMPLETED and batch.members.size() == 2, "manual multi-start groups every definition")
	var starts: Array = []
	var definition := T.value(0.0, 1.0, 1.0)
	definition.on_add = func(_h): starts.append(true)
	var invalid := scheduler.add_all(RefCounted.new(), [definition, null])
	host.check(invalid.is_settled and invalid.completion_reason == T.Reason.FAILED and starts.is_empty(), "multi-start validates array before running callbacks")
	var failed := scheduler.add_all(RefCounted.new(), [definition, T.value(0.0, 1.0, -1.0), definition])
	host.check(failed.is_settled and failed.completion_reason == T.Reason.FAILED and starts.size() == 1, "failed multi-start cancels preceding siblings and stops starting definitions")
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
	host.check(await automatic.wait() == T.Reason.COMPLETED and node.position == Vector2(4, 8) and node.modulate.a == 0.25, "automatic multi-start applies named helpers")
	var runner = host.get_tree().get_meta(T.Runner.META_KEY)
	runner.free()
	node.free()
	return true
