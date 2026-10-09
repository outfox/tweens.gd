# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
const Suite = preload("res://tests.gd")
var host: Suite
var factories := T.new()

class Probe extends T.Adapter:
	var events: Array = []
	var path := ^"amount"
	var fail_prepare := false
	var fail_write := false
	var fail_release := false
	func prepare(_target: Object) -> String:
		events.append("prepare")
		return "prepare failed" if fail_prepare else ""
	func read(target: Object) -> Variant:
		events.append("read")
		return target.get_indexed(path)
	func write(target: Object, value: Variant) -> String:
		events.append("write")
		if fail_write: return "write failed"
		target.set_indexed(path, value)
		return ""
	func restore(target: Object, initial: Variant) -> String:
		events.append("restore")
		return super.restore(target, initial)
	func release() -> String:
		events.append("release")
		return "release failed" if fail_release else ""

# copy() is an overridable hook that runs before the start checks its target.
class FreeingCopy extends T.Adapter:
	var victim: Object
	func copy() -> TweensGdAdapter:
		if is_instance_valid(victim): victim.free()
		return super.copy()

class RejectingCopy extends Probe:
	var on_copy: Callable
	var copied_refs: Array[WeakRef] = []
	func copy() -> TweensGdAdapter:
		var snapshot := super.copy()
		copied_refs.append(weakref(snapshot))
		on_copy.call()
		return snapshot

class Box extends RefCounted:
	var amount := 2.0
	var tint := Color(0, 0, 0, 0)
	var count := 2
	var text := "a"
	var on_write: Callable
	var observed: float:
		get: return amount
		set(value):
			amount = value
			if on_write.is_valid(): on_write.call()

class ColorProbe extends T.Adapter:
	var events: Array = []
	var custom := false
	func prepare(_target: Object) -> String:
		events.append(["prepare", color_space, alpha_mode, color_encoding])
		return ""
	func read(target: Object) -> Variant:
		events.append(["read", color_space, alpha_mode, color_encoding])
		return target.get("tint")
	func write(target: Object, value: Variant) -> String:
		target.set("tint", value)
		return ""
	func interpolate(from: Variant, to: Variant, weight: float) -> Variant:
		if not custom: return super.interpolate(from, to, weight)
		var a: Color = from
		var b: Color = to
		return a.lerp(b, weight * weight)

func run(owner: Suite) -> bool:
	host = owner
	_custom()
	_colors()
	_curves()
	_catalog()
	return true

func check(condition: bool, message: String) -> void:
	host.check(condition, message)

func _colors() -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var adapter := ColorProbe.new()
	var definition := TweensGdDefinition.new()
	definition.adapter = adapter
	definition.color_space = T.ColorSpace.LINEAR_RGB
	definition.alpha_mode = T.AlphaMode.STRAIGHT
	definition.color_encoding = T.ColorEncoding.LINEAR_RGB
	definition.to_value = Color.RED
	definition.duration = 1.0
	var h := scheduler.add(box, definition)
	scheduler.update(0.5)
	var prepared: Array = adapter.events[0]
	var captured: Array = adapter.events[1]
	check(prepared == ["prepare", 2, 1, 1] and captured == ["read", 2, 1, 1],
		"definition color policy reaches prepare and initial read")
	h.cancel()
	adapter.custom = true
	definition.to_value = null
	definition.by_value = Color(1, 0, 0, 0)
	box.tint = Color(0, 0, 0, 0)
	var relative := scheduler.add(box, definition)
	scheduler.update(0.5)
	check(box.tint.r == 0.25, "relative color dispatches subclass interpolate override")
	relative.cancel()
	definition.factor_by = 2.0
	box.tint = Color(0, 0, 0, 0)
	var factored := scheduler.add(box, definition)
	scheduler.update(0.5)
	check(box.tint.r == 1.0, "color factors dispatch subclass interpolate override")
	factored.cancel()
	adapter.custom = false
	definition.factor_by = 1.0
	box.tint = Color(0, 0, 0, 0)
	var base := scheduler.add(box, definition)
	scheduler.update(0.5)
	check(box.tint.r == 0.5, "base relative colors retain RGBA arithmetic")
	base.cancel()

func _custom() -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var definition := T.custom(func(t: Box) -> float: return t.amount, func(t: Box, v: float) -> void: t.amount = v)
	definition.to_value = 10.0
	definition.duration = 1.0
	definition.fill = T.Fill.NONE
	var h := scheduler.add(box, definition)
	scheduler.update(0.5)
	host.near(box.amount, 6.0, "custom getter and setter interpolate")
	scheduler.update(0.5)
	check(h.is_settled and box.amount == 2.0, "custom adapter restores initial value")
	var mixed := T.custom(func(t: Box) -> float: return t.amount, func(t: Box, v: float) -> void: t.amount = v)
	mixed.from_value = 0
	mixed.to_value = 1
	mixed.duration = 1.0
	var floating := scheduler.add(box, mixed)
	scheduler.update(0.25)
	check(typeof(floating.value) == TYPE_FLOAT and box.amount == 0.25, "custom float storage keeps fractional samples with integer endpoints")
	floating.cancel()
	box.amount = 2.0
	var rounded := T.custom(func(t: Box) -> int: return t.count, func(t: Box, v: int) -> void: t.count = v)
	rounded.from_value = 0.5
	rounded.to_value = 3.5
	rounded.duration = 1.0
	var integer := scheduler.add(box, rounded)
	scheduler.update(0.25)
	check(typeof(integer.value) == TYPE_INT and box.count == 1, "custom integer storage rounds samples without truncating endpoints")
	integer.cancel()
	var custom := T.custom(func(t: Box) -> String: return t.text, func(t: Box, v: String) -> void: t.text = v,
		func(a: String, b: String, t: float) -> String: return a if t < 0.5 else b,
		func(v: Variant) -> String: return "" if v is String else "expected String")
	custom.to_value = "b"
	custom.duration = 1.0
	var text := scheduler.add(box, custom)
	scheduler.update(0.75)
	var interpolated: bool = box.text == "b" and text.value == "b"
	check(interpolated, "custom interpolation supports user-validated value types")
	text.cancel()
	var adapter := Probe.new()
	var source := TweensGdDefinition.new()
	source.adapter = adapter
	source.to_value = 8.0
	source.duration = 1.0
	source.fill = T.Fill.NONE
	source.on_finally = func(_h: TweensGdHandle) -> void: adapter.events.append("finally")
	var cloned := scheduler.add(box, source)
	adapter.path = ^"missing"
	scheduler.update(1.0)
	check(cloned.completion_reason == T.Reason.COMPLETED and box.amount == 2.0, "subclass configuration snapshots independently")
	check(adapter.events == ["prepare", "read", "write", "write", "restore", "write", "finally", "release"], "adapter lifecycle and cleanup order")
	adapter.events.clear()
	adapter.path = ^"amount"
	adapter.fail_prepare = true
	var rejected := scheduler.add(box, source)
	scheduler.update(0.0)
	check(rejected.is_settled and rejected.error == "prepare failed" and adapter.events == ["prepare", "release"], "failed preparation releases partial bindings without callbacks")
	adapter.events.clear()
	adapter.fail_prepare = false
	adapter.fail_write = true
	adapter.fail_release = true
	var failed := scheduler.add(box, source)
	scheduler.update(0.5)
	check(failed.is_settled and failed.completion_reason == T.Reason.FAILED, "custom write errors settle playback")
	check(failed.error.contains("write failed") and failed.error.contains("release failed"), "write and release errors are retained")
	check(adapter.events == ["prepare", "read", "write", "finally", "release"], "fault cleanup runs once")
	# Cancelling inside a setter cannot release its active adapter until it returns.
	adapter.events.clear()
	adapter.fail_write = false
	adapter.fail_release = false
	adapter.path = ^"observed"
	var cancelled := scheduler.add(box, source)
	box.on_write = func() -> void:
		cancelled.cancel()
		check(not adapter.events.has("release") and not cancelled.is_settled, "adapter cleanup waits for active setter")
	scheduler.update(0.5)
	var cleaned_up: bool = cancelled.is_settled and adapter.events.back() == "release"
	check(cleaned_up, "reentrant setter cancellation cleans up")
	box.on_write = Callable()
	var stale := Node.new()
	var stale_definition := T.custom(func(t: Box) -> float: return t.amount, stale.set_meta.bind(&"sample").unbind(2))
	stale_definition.to_value = 1.0
	stale.free()
	var stale_handle := scheduler.add(box, stale_definition)
	scheduler.update(0.0)
	check(stale_handle.completion_reason == T.Reason.FAILED, "stale custom Callable rejected before playback")
	var wrong_target := Node2D.new()
	host.add_child(wrong_target)
	var wrong_kind := scheduler.add(wrong_target, T.position_3d(Vector3.ONE))
	scheduler.update(0.0)
	check(wrong_kind.completion_reason == T.Reason.FAILED, "named helpers validate native target class")
	wrong_target.free()
	var doomed := Node2D.new()
	host.add_child(doomed)
	var freeing := FreeingCopy.new()
	freeing.victim = doomed
	var copying := TweensGdDefinition.new()
	copying.adapter = freeing
	copying.target_class = &"Node2D"
	copying.to_value = 1.0
	check(scheduler.add(doomed, copying).completion_reason == T.Reason.FAILED, "a copy() hook that frees the target is rejected")
	for invalidation: String in ["target", "scheduler"]:
		var rejected_scheduler := TweensGdScheduler.new()
		var target := Node.new()
		host.add_child(target)
		var rejecting := RejectingCopy.new()
		rejecting.fail_release = true
		rejecting.on_copy = func() -> void:
			if invalidation == "target": target.free()
			else: rejected_scheduler.dispose()
		var definition_copy := TweensGdDefinition.new()
		definition_copy.adapter = rejecting
		definition_copy.to_value = 1.0
		var diagnostics: Array[String] = []
		check(rejected_scheduler.error_reported.connect(func(message: String) -> void: diagnostics.append(message)) == OK,
			invalidation + " error signal connects")
		var copy_rejected := rejected_scheduler.add(target, definition_copy)
		check(copy_rejected.is_settled and copy_rejected.completion_reason == T.Reason.FAILED, invalidation + " invalidation during copy rejects")
		check(rejecting.events.is_empty(), invalidation + " rejection has no preparation or release hooks")
		check(rejecting.copied_refs.size() == 1 and rejecting.copied_refs[0].get_ref() == null, invalidation + " rejection frees copied adapter reference")
		check(diagnostics == [copy_rejected.error] and rejected_scheduler.last_error == copy_rejected.error, invalidation + " rejection preserves diagnostics")
		if is_instance_valid(target): target.free()
		rejected_scheduler.dispose()
	var wrong := scheduler.add(box, T.property(^"amount", Vector2.ONE))
	scheduler.update(0.0)
	check(wrong.completion_reason == T.Reason.FAILED, "property endpoints remain type checked")
	scheduler.dispose()

func _create(class_name_: String) -> Object:
	var concrete: Dictionary = {"CanvasItem": "Node2D", "Range": "ProgressBar", "SpriteBase3D": "Sprite3D",
		"GeometryInstance3D": "MeshInstance3D", "Light2D": "PointLight2D", "Light3D": "OmniLight3D", "BaseMaterial3D": "StandardMaterial3D"}
	var concrete_name: String = concrete.get(class_name_, class_name_)
	var target: Object = ClassDB.instantiate(concrete_name)
	if target is Resource: return target
	if target is Control:
		var control := target as Control
		control.size = Vector2(100, 200)
		control.offset_transform_enabled = true
	if target is Range: (target as Range).step = 0
	if target is Label or target is RichTextLabel: target.set(&"text", "a".repeat(100))
	if target is Sprite2D:
		var sprite := target as Sprite2D
		sprite.hframes = 10
		sprite.region_enabled = true
	if target is AnimatedSprite2D or target is AnimatedSprite3D:
		var frames := SpriteFrames.new()
		target.set(&"sprite_frames", frames)
		for i in range(10): frames.add_frame(&"default", null)
	if target is GPUParticles2D or target is GPUParticles3D or target is CPUParticles2D or target is CPUParticles3D:
		target.set(&"emitting", false)
	if target is PathFollow2D:
		var follow := target as PathFollow2D
		var path := Path2D.new()
		path.curve = Curve2D.new()
		path.curve.add_point(Vector2.ZERO)
		path.curve.add_point(Vector2(100, 0))
		host.add_child(path)
		path.add_child(follow)
		follow.loop = false
	elif target is PathFollow3D:
		var follow := target as PathFollow3D
		var path := Path3D.new()
		path.curve = Curve3D.new()
		path.curve.add_point(Vector3.ZERO)
		path.curve.add_point(Vector3(100, 0, 0))
		host.add_child(path)
		path.add_child(follow)
		follow.loop = false
	else: host.add_child(target as Node)
	if target is ScrollContainer:
		var scroll := target as ScrollContainer
		scroll.get_h_scroll_bar().max_value = 1000
		scroll.get_h_scroll_bar().page = 100
		scroll.get_v_scroll_bar().max_value = 1000
		scroll.get_v_scroll_bar().page = 100
	return target

func _nudge(value: float) -> float:
	return 0.5 if value <= 0.0 else value * 0.75

func _perturb(value: Variant) -> Variant:
	match typeof(value):
		TYPE_FLOAT:
			var number: float = value
			return _nudge(number)
		TYPE_INT: return value + 3
		TYPE_VECTOR2:
			var v2: Vector2 = value
			return Vector2(_nudge(v2.x), _nudge(v2.y))
		TYPE_VECTOR3:
			var v3: Vector3 = value
			return Vector3(_nudge(v3.x), _nudge(v3.y), _nudge(v3.z))
		TYPE_VECTOR4:
			var v4: Vector4 = value
			return Vector4(_nudge(v4.x), _nudge(v4.y), _nudge(v4.z), _nudge(v4.w))
		TYPE_COLOR:
			var color: Color = value
			return Color(_nudge(color.r), _nudge(color.g), _nudge(color.b), _nudge(color.a))
		TYPE_QUATERNION:
			var rotation: Quaternion = value
			return (rotation * Quaternion(Vector3.UP, 0.5)).normalized()
		TYPE_RECT2:
			var rect: Rect2 = value
			return Rect2(rect.position + Vector2(0.5, 0.5), rect.size + Vector2(0.5, 0.5))
	return null

func _close(a: Variant, b: Variant) -> bool:
	if typeof(a) != typeof(b): return false
	match typeof(a):
		TYPE_FLOAT:
			var x: float = a
			var y: float = b
			return absf(x - y) <= 0.001 * maxf(1.0, absf(x))
		TYPE_INT: return a == b
		TYPE_QUATERNION:
			var p: Quaternion = a
			var q: Quaternion = b
			return absf(p.normalized().dot(q.normalized())) >= 0.999
		TYPE_RECT2: return _close(a.position, b.position) and _close(a.size, b.size)
		TYPE_COLOR:
			var c: Color = a
			var d: Color = b
			return _close(Vector4(c.r, c.g, c.b, c.a), Vector4(d.r, d.g, d.b, d.a))
		TYPE_VECTOR2:
			var u: Vector2 = a
			var v: Vector2 = b
			return (u - v).length() <= 0.001 * maxf(1.0, u.length())
		TYPE_VECTOR3:
			var u: Vector3 = a
			var v: Vector3 = b
			return (u - v).length() <= 0.001 * maxf(1.0, u.length())
		_:
			var u: Vector4 = a
			var v: Vector4 = b
			return (u - v).length() <= 0.001 * maxf(1.0, u.length())

func _read(scheduler: TweensGdScheduler, target: Object, factory: String) -> Variant:
	var definition: TweensGdDefinition = factories.call(factory)
	var probe: TweensGdHandle = scheduler.add(target, definition)
	scheduler.update(0.0)
	probe.cancel()
	return probe.value

func _catalog() -> void:
	var entries: Array = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/adapters.json"))
	check(entries.size() == 331, "catalog covers every concrete C# property/value adapter")
	for entry: Dictionary in entries:
		var factory: String = entry.name
		var target_class: String = entry.target
		var scheduler := TweensGdScheduler.new()
		var target := _create(target_class)
		var definition: TweensGdDefinition = factories.call(factory)
		var probe := scheduler.add(target, definition)
		scheduler.update(0.0)
		check(probe.completion_reason != T.Reason.FAILED, factory + " starts: " + probe.error)
		if probe.completion_reason != T.Reason.FAILED:
			var initial: Variant = probe.value
			probe.cancel()
			var first: Variant = _perturb(initial)
			if entry.kind != "value":
				var write: TweensGdDefinition = factories.call(factory, first)
				@warning_ignore("return_value_discarded")
				scheduler.add(target, write)
				scheduler.update(0.0)
				initial = _read(scheduler, target, factory)
				check(_close(first, initial), factory + " writes its native property")
			var to: Variant = _perturb(initial)
			var move: TweensGdDefinition = factories.call(factory, to, 1.0)
			var playing := scheduler.add(target, move)
			scheduler.update(0.5)
			var expected: Variant = TweensGdInterpolation.interpolate(initial, to, 0.5, typeof(initial))
			var actual: Variant = playing.value if entry.kind == "value" else _read(scheduler, target, factory)
			check(_close(expected, actual), "%s midpoint: expected %s, got %s" % [factory, expected, actual])
			scheduler.update(0.5)
			check(playing.completion_reason == T.Reason.COMPLETED and _close(playing.value, to), factory + " completes")
			if entry.kind != "value":
				move.to_value = initial
				move.fill = T.Fill.NONE
				var restore := scheduler.add(target, move)
				scheduler.update(1.0)
				check(restore.completion_reason == T.Reason.COMPLETED and _close(_read(scheduler, target, factory), to), factory + " restores captured property")
		scheduler.dispose()
		if target is PathFollow2D or target is PathFollow3D: (target as Node).get_parent().free()
		elif target is Node: target.free()

func _curves() -> void:
	for bounds: Vector2 in [Vector2(-3, -1), Vector2(2, 5), Vector2(0, 1)]:
		var curve := Curve.new()
		curve.min_domain = minf(bounds.x, 0.0)
		curve.max_domain = maxf(bounds.y, 1.0)
		curve.min_domain = bounds.x
		curve.max_domain = bounds.y
		curve.min_value = -2.0
		curve.max_value = 3.0
		curve.bake_resolution = 37
		@warning_ignore("return_value_discarded")
		curve.add_point(Vector2(bounds.x, -1), 0.25, 0.5, Curve.TANGENT_FREE, Curve.TANGENT_LINEAR)
		@warning_ignore("return_value_discarded")
		curve.add_point(Vector2((bounds.x + bounds.y) / 2.0, 2), -0.25, 0.75)
		@warning_ignore("return_value_discarded")
		curve.add_point(Vector2(bounds.y, 1), 1.0, 0.0, Curve.TANGENT_LINEAR, Curve.TANGENT_FREE)
		var definition := TweensGdDefinition.new()
		definition.curve = curve
		var copy := definition.copy().curve
		check(copy != curve and copy.min_domain == curve.min_domain and copy.max_domain == curve.max_domain, "curve snapshot preserves non-default domains")
		check(copy.min_value == -2.0 and copy.max_value == 3.0 and copy.bake_resolution == 37, "curve snapshot preserves range and bake resolution")
		for fraction: float in [-0.5, 0.0, 0.25, 0.5, 0.75, 1.0, 1.5]:
			var x := lerpf(bounds.x, bounds.y, fraction)
			host.near(copy.sample(x), curve.sample(x), "curve copy preserves mixed tangents and extrapolation")
		curve.set_point_value(1, 0.0)
		host.near(copy.get_point_position(1).y, 2.0, "curve snapshot owns its control points")
