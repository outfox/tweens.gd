# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdAwaiting
extends RefCounted

const Types = preload("types.gd")
const Cancellation = preload("cancellation.gd")
signal finished(reason: int)
var _source: RefCounted
var _token: Cancellation
var _finished := false

static func wait_for(source: RefCounted, token: Cancellation) -> int:
	if source.is_settled: return source.completion_reason
	if token.is_cancelled: return Types.Reason.WAIT_CANCELLED
	var waiter := TweensGdAwaiting.new()
	return await waiter._wait(source, token)

func _wait(source: RefCounted, token: Cancellation) -> int:
	_source = source
	_token = token
	source.ended.connect(_complete)
	token.cancelled.connect(_cancel)
	return await finished

func _cancel() -> void:
	_complete(Types.Reason.WAIT_CANCELLED)

func _complete(reason: int) -> void:
	if _finished: return
	_finished = true
	if _source.ended.is_connected(_complete): _source.ended.disconnect(_complete)
	if _token.cancelled.is_connected(_cancel): _token.cancelled.disconnect(_cancel)
	_source = null
	_token = null
	finished.emit(reason)
