# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdCancellation
extends RefCounted
## Cancels individual waits without cancelling their playback. Main-thread only.

signal cancelled
var is_cancelled: bool:
	get: return _cancelled
var _cancelled := false

func cancel() -> void:
	if OS.get_thread_caller_id() != OS.get_main_thread_id():
		push_error("Use tweens.gd on Godot's main thread.")
		return
	if _cancelled: return
	_cancelled = true
	cancelled.emit()
	for connection in cancelled.get_connections(): cancelled.disconnect(connection.callable)
