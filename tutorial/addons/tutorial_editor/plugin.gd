@tool
extends EditorPlugin


class SceneDebugger extends EditorDebuggerPlugin:

	func _has_capture(prefix: String) -> bool:
		return prefix == "tutorial"


	func _capture(message: String, data: Array, _session_id: int) -> bool:
		if message != "tutorial:open_scene":
			return false
		if data.size() == 1 and data[0] is String:
			var path: String = data[0]
			if path.begins_with("res://Lessons/") and path.ends_with(".tscn") and ResourceLoader.exists(path, "PackedScene"):
				EditorInterface.open_scene_from_path.call_deferred(path)
				EditorInterface.set_main_screen_editor.call_deferred("2D")
				EditorInterface.get_base_control().get_window().grab_focus.call_deferred()
		return true


var scene_debugger := SceneDebugger.new()


func _enter_tree() -> void:
	add_debugger_plugin(scene_debugger)


func _exit_tree() -> void:
	remove_debugger_plugin(scene_debugger)
