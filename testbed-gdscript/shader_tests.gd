# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
const AdapterTests = preload("adapter_tests.gd")
var host: Node

func check(condition: bool, message: String) -> void:
	host.check(condition, message)

func material(code: String) -> ShaderMaterial:
	var shader := Shader.new()
	shader.code = code
	var result := ShaderMaterial.new()
	result.shader = shader
	return result

func watched(shader: Shader) -> bool:
	for connection in shader.changed.get_connections():
		if connection.callable.get_method() == "_shader_changed": return true
	return false

func run(owner: Node) -> bool:
	host = owner
	_materials()
	if DisplayServer.get_name() != "headless":
		await _rendering()
	return true

func _materials() -> void:
	var scheduler := TweensGdScheduler.new()
	var m := material("shader_type canvas_item; uniform float amount = 0.25; uniform int count = 1; uniform vec2 v2; uniform vec3 v3; uniform vec4 v4; uniform vec4 tint : source_color; uniform bool flag;")
	var compare := AdapterTests.new()
	var values := [[&"amount", 2.0, 4.0], [&"count", 2, 4], [&"v2", Vector2.ONE, Vector2(3, 5)],
		[&"v3", Vector3.ONE, Vector3(3, 5, 7)], [&"v4", Vector4.ONE, Vector4(3, 5, 7, 9)],
		[&"tint", Color(0, 0, 0, 0), Color(1, 1, 1, 1)]]
	for entry in values:
		m.set_shader_parameter(entry[0], entry[1])
		var definition := T.shader_parameter(entry[0], entry[2], 1.0)
		definition.fill = T.Fill.NONE
		var h := scheduler.add(m, definition)
		scheduler.update(0.0)
		check(not h.is_terminal and watched(m.shader), "shader metadata binding: " + entry[0])
		scheduler.update(0.5)
		var expected: Variant = TweensGdInterpolation.interpolate(entry[1], entry[2], 0.5, typeof(entry[1]))
		check(compare._close(expected, m.get_shader_parameter(entry[0])), "shader midpoint: " + entry[0])
		scheduler.update(0.5)
		check(h.completion_reason == T.Reason.COMPLETED and compare._close(entry[1], m.get_shader_parameter(entry[0])), "shader explicit override restored: " + entry[0])
		check(not watched(m.shader), "shader subscriptions released: " + entry[0])
	m.set_shader_parameter(&"amount", 0.25)
	scheduler.add(m, T.shader_parameter(&"amount", null, 1.0).with_by(0.5))
	scheduler.update(0.5)
	m.set_shader_parameter(&"amount", 1.0)
	scheduler.update(0.5)
	check(is_equal_approx(m.get_shader_parameter(&"amount"), 1.25), "shader by_value keeps outside changes")
	scheduler.add(m, T.shader_parameter(&"amount", null, 1.0).with_by(-1.0).with_fill(T.Fill.NONE))
	scheduler.update(0.5)
	check(is_equal_approx(m.get_shader_parameter(&"amount"), 0.75), "shader by_value moves the override")
	scheduler.update(0.5)
	check(is_equal_approx(m.get_shader_parameter(&"amount"), 1.25), "shader by_value takes its offset back out")
	for invalid in [T.shader_parameter(&"missing", 1.0), T.shader_parameter(&"amount", 1),
		T.shader_parameter(&"count", 1.0), T.shader_parameter(&"v4", Color.WHITE),
		T.shader_parameter(&"tint", Vector4.ONE), T.shader_parameter(&"flag", true),
		T.shader_parameter(&"amount", NAN), T.shader_parameter(&"count", 2147483648),
		T.shader_parameter(&"amount").with_by(NAN), T.shader_parameter(&"count").with_by(1.0)]:
		var rejected := scheduler.add(m, invalid)
		scheduler.update(0.0)
		check(rejected.is_settled and rejected.completion_reason == T.Reason.FAILED, "invalid shader definition is rejected")
		check(not watched(m.shader), "rejected shader preparation cleans up")
	var missing := scheduler.add(ShaderMaterial.new(), T.shader_parameter(&"amount", 1.0))
	scheduler.update(0.0)
	check(missing.is_settled, "missing shader is rejected")
	var no_default := material("shader_type canvas_item; uniform float amount = 0.25;")
	var default_handle := scheduler.add(no_default, T.shader_parameter(&"amount", 1.0))
	scheduler.update(0.0)
	if DisplayServer.get_name() == "headless":
		check(default_handle.completion_reason == T.Reason.FAILED, "dummy renderer never invents unavailable shader defaults")
	default_handle.cancel()
	var original := m.shader
	var changed := scheduler.add(m, T.shader_parameter(&"amount", 4.0, 1.0))
	scheduler.update(0.0)
	check(watched(original), "live shader is watched")
	original.emit_changed()
	scheduler.update(0.5)
	check(changed.completion_reason == T.Reason.FAILED and not watched(original), "shader change faults and releases binding")
	var replacement := scheduler.add(m, T.shader_parameter(&"amount", 4.0, 1.0))
	scheduler.update(0.0)
	m.shader = no_default.shader
	scheduler.update(0.5)
	check(replacement.completion_reason == T.Reason.FAILED and not watched(original), "shader replacement faults before writing")
	m.shader = original
	var restore := T.shader_parameter(&"amount", 4.0, 1.0)
	restore.fill = T.Fill.NONE
	restore.on_update = func(_h, _v): original.emit_changed()
	var restoring := scheduler.add(m, restore)
	scheduler.update(1.0)
	check(restoring.completion_reason == T.Reason.FAILED, "shader binding is checked before restoration too")
	var cancelled := scheduler.add(m, T.shader_parameter(&"amount", 8.0, 1.0))
	scheduler.update(0.5)
	var last: Variant = m.get_shader_parameter(&"amount")
	cancelled.cancel()
	check(m.get_shader_parameter(&"amount") == last and not watched(original), "shader cancellation retains sample and releases watch")
	scheduler.dispose()

func _rendering() -> void:
	var scheduler := TweensGdScheduler.new()
	var m := material("shader_type canvas_item; uniform float amount = 0.25; void fragment() { COLOR = vec4(amount, 0.0, 0.0, 1.0); }")
	var viewport := SubViewport.new()
	viewport.size = Vector2i(8, 8)
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	host.add_child(viewport)
	var rect := ColorRect.new()
	rect.size = Vector2(8, 8)
	rect.material = m
	viewport.add_child(rect)
	await host.get_tree().process_frame
	var definition := T.shader_parameter(&"amount", 0.75, 1.0)
	definition.fill = T.Fill.NONE
	var h := scheduler.add(m, definition)
	scheduler.update(0.0)
	host.near(h.value, 0.25, "real renderer captures declared shader default")
	scheduler.update(0.5)
	RenderingServer.force_draw(false)
	var pixel := viewport.get_texture().get_image().get_pixel(4, 4)
	check(absf(pixel.r - 0.5) < 0.01 and pixel.g < 0.01, "material tween changes rendered pixel")
	scheduler.update(0.5)
	check(h.completion_reason == T.Reason.COMPLETED and m.get_shader_parameter(&"amount") == null, "absent material override is removed at restoration")
	m.set_shader_parameter(&"amount", 0.25)
	var explicit := scheduler.add(m, definition)
	scheduler.update(1.0)
	check(explicit.completion_reason == T.Reason.COMPLETED and m.get_shader_parameter(&"amount") != null, "explicit override equal to default stays explicit")
	viewport.free()
	for spatial in [false, true]:
		var code := "shader_type spatial; instance uniform float pulse = 0.25;" if spatial else "shader_type canvas_item; instance uniform float pulse = 0.25;"
		var instance_material := material(code)
		var node: Node = MeshInstance3D.new() if spatial else ColorRect.new()
		if spatial:
			node.mesh = BoxMesh.new()
			node.material_override = instance_material
		else: node.material = instance_material
		host.add_child(node)
		await host.get_tree().process_frame
		var instance_definition := T.instance_shader_parameter(&"pulse", 0.75, 1.0)
		instance_definition.fill = T.Fill.NONE
		var instance := scheduler.add(node, instance_definition)
		scheduler.update(0.0)
		check(not instance.is_terminal, "instance shader starts: " + str(spatial) + " " + instance.error)
		host.near(instance.value if instance.value != null else -1.0, 0.25, "instance shader captures default")
		scheduler.update(0.5)
		host.near(node.get_instance_shader_parameter(&"pulse"), 0.5, "instance shader midpoint")
		scheduler.update(0.5)
		check(instance.completion_reason == T.Reason.COMPLETED and not _has_override(node), "instance override absence restored")
		node.set_instance_shader_parameter(&"pulse", 0.25)
		scheduler.add(node, instance_definition)
		scheduler.update(1.0)
		check(_has_override(node) and node.get_instance_shader_parameter(&"pulse") == 0.25, "explicit instance default preserved")
		var replaced := scheduler.add(node, instance_definition)
		scheduler.update(0.0)
		if spatial: node.material_override = material(code)
		else: node.material = material(code)
		scheduler.update(0.5)
		check(replaced.completion_reason == T.Reason.FAILED, "instance material replacement faults")
		check(not watched(instance_material.shader), "instance watches released after failure")
		node.free()
	# Every effective slot is watched, including inherited materials and next passes.
	var parent := Node2D.new()
	var child := ColorRect.new()
	var canvas_code := "shader_type canvas_item; instance uniform float pulse = 0.25;"
	parent.material = material(canvas_code)
	child.use_parent_material = true
	host.add_child(parent)
	parent.add_child(child)
	await host.get_tree().process_frame
	var inherited := scheduler.add(child, T.instance_shader_parameter(&"pulse", 1.0, 1.0))
	scheduler.update(0.0)
	check(not inherited.is_terminal, "inherited canvas material supplies instance uniform")
	parent.material = material(canvas_code)
	scheduler.update(0.1)
	check(inherited.completion_reason == T.Reason.FAILED, "inherited material replacement faults")
	parent.free()
	for change in ["mesh", "overlay", "next_pass", "shader", "edit"]:
		var mesh := MeshInstance3D.new()
		mesh.mesh = BoxMesh.new()
		var bound := material("shader_type spatial; instance uniform float pulse = 0.25;")
		mesh.material_override = bound
		host.add_child(mesh)
		await host.get_tree().process_frame
		var binding := scheduler.add(mesh, T.instance_shader_parameter(&"pulse", 1.0, 1.0))
		scheduler.update(0.0)
		check(not binding.is_terminal, "instance binding starts before " + change)
		match change:
			"mesh": mesh.mesh = BoxMesh.new()
			"overlay": mesh.material_overlay = StandardMaterial3D.new()
			"next_pass": bound.next_pass = StandardMaterial3D.new()
			"shader": bound.shader = material("shader_type spatial; instance uniform float pulse = 0.25;").shader
			"edit": bound.shader.emit_changed()
		scheduler.update(0.1)
		check(binding.completion_reason == T.Reason.FAILED, "instance binding tracks " + change)
		mesh.free()
	scheduler.dispose()

func _has_override(node: Node) -> bool:
	for property in node.get_property_list():
		if property.name == "instance_shader_parameters/pulse": return bool(property.usage & PROPERTY_USAGE_STORAGE)
	return false
