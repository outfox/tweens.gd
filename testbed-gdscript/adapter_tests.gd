# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted

const T = preload("res://addons/tweens_gd/tweens.gd")
var host: Node
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

class Box extends RefCounted:
	var amount := 2.0
	var count := 2
	var text := "a"
	var on_write: Callable
	var observed: float:
		get: return amount
		set(value):
			amount = value
			if on_write.is_valid(): on_write.call()

func run(owner: Node) -> bool:
	host = owner
	_custom()
	_curves()
	_catalog()
	return true

func check(condition: bool, message: String) -> void:
	host.check(condition, message)

func _custom() -> void:
	var scheduler := TweensGdScheduler.new()
	var box := Box.new()
	var definition := T.custom(func(t): return t.amount, func(t, v): t.amount = v, 10.0, 1.0)
	definition.fill = T.Fill.NONE
	var h := scheduler.add(box, definition)
	scheduler.update(0.5)
	host.near(box.amount, 6.0, "custom getter and setter interpolate")
	scheduler.update(0.5)
	check(h.is_settled and box.amount == 2.0, "custom adapter restores initial value")
	var mixed := T.custom(func(t): return t.amount, func(t, v): t.amount = v, 1, 1.0)
	mixed.from_value = 0
	var floating := scheduler.add(box, mixed)
	scheduler.update(0.25)
	check(typeof(floating.value) == TYPE_FLOAT and box.amount == 0.25, "custom float storage keeps fractional samples with integer endpoints")
	floating.cancel()
	box.amount = 2.0
	var rounded := T.custom(func(t): return t.count, func(t, v): t.count = v, 3.5, 1.0)
	rounded.from_value = 0.5
	var integer := scheduler.add(box, rounded)
	scheduler.update(0.25)
	check(typeof(integer.value) == TYPE_INT and box.count == 1, "custom integer storage rounds samples without truncating endpoints")
	integer.cancel()
	var custom := T.custom(func(t): return t.text, func(t, v): t.text = v, "b", 1.0,
		func(a, b, t): return a if t < 0.5 else b,
		func(v): return "" if v is String else "expected String")
	var text := scheduler.add(box, custom)
	scheduler.update(0.75)
	check(box.text == "b" and text.value == "b", "custom interpolation supports user-validated value types")
	text.cancel()
	var adapter := Probe.new()
	var source := TweensGdDefinition.new()
	source.adapter = adapter
	source.to_value = 8.0
	source.duration = 1.0
	source.fill = T.Fill.NONE
	source.on_finally = func(_h): adapter.events.append("finally")
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
	box.on_write = func():
		cancelled.cancel()
		check(not adapter.events.has("release") and not cancelled.is_settled, "adapter cleanup waits for active setter")
	scheduler.update(0.5)
	check(cancelled.is_settled and adapter.events.back() == "release", "reentrant setter cancellation cleans up")
	box.on_write = Callable()
	var stale := Node.new()
	var stale_definition := T.custom(func(t): return t.amount, stale.set_meta.bind(&"sample").unbind(2), 1.0)
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
	var wrong := scheduler.add(box, T.property(^"amount", Vector2.ONE))
	scheduler.update(0.0)
	check(wrong.completion_reason == T.Reason.FAILED, "property endpoints remain type checked")
	scheduler.dispose()

func _create(class_name_: String) -> Object:
	var concrete: Dictionary = {"CanvasItem": "Node2D", "Range": "ProgressBar", "SpriteBase3D": "Sprite3D",
		"GeometryInstance3D": "MeshInstance3D", "Light2D": "PointLight2D", "Light3D": "OmniLight3D", "BaseMaterial3D": "StandardMaterial3D"}
	var target: Object = ClassDB.instantiate(concrete.get(class_name_, class_name_))
	if target is Resource: return target
	if target is Control:
		target.size = Vector2(100, 200)
		target.offset_transform_enabled = true
	if target is Range: target.step = 0
	if target is Label or target is RichTextLabel: target.text = "a".repeat(100)
	if target is Sprite2D:
		target.hframes = 10
		target.region_enabled = true
	if target is AnimatedSprite2D or target is AnimatedSprite3D:
		target.sprite_frames = SpriteFrames.new()
		for i in range(10): target.sprite_frames.add_frame(&"default", null)
	if target is GPUParticles2D or target is GPUParticles3D or target is CPUParticles2D or target is CPUParticles3D:
		target.emitting = false
	if target is PathFollow2D:
		var path := Path2D.new()
		path.curve = Curve2D.new()
		path.curve.add_point(Vector2.ZERO)
		path.curve.add_point(Vector2(100, 0))
		host.add_child(path)
		path.add_child(target)
		target.loop = false
	elif target is PathFollow3D:
		var path := Path3D.new()
		path.curve = Curve3D.new()
		path.curve.add_point(Vector3.ZERO)
		path.curve.add_point(Vector3(100, 0, 0))
		host.add_child(path)
		path.add_child(target)
		target.loop = false
	else: host.add_child(target)
	if target is ScrollContainer:
		target.get_h_scroll_bar().max_value = 1000
		target.get_h_scroll_bar().page = 100
		target.get_v_scroll_bar().max_value = 1000
		target.get_v_scroll_bar().page = 100
	return target

func _nudge(value: float) -> float:
	return 0.5 if value <= 0.0 else value * 0.75

func _perturb(value: Variant) -> Variant:
	match typeof(value):
		TYPE_FLOAT: return _nudge(value)
		TYPE_INT: return value + 3
		TYPE_VECTOR2: return Vector2(_nudge(value.x), _nudge(value.y))
		TYPE_VECTOR3: return Vector3(_nudge(value.x), _nudge(value.y), _nudge(value.z))
		TYPE_VECTOR4: return Vector4(_nudge(value.x), _nudge(value.y), _nudge(value.z), _nudge(value.w))
		TYPE_COLOR: return Color(_nudge(value.r), _nudge(value.g), _nudge(value.b), _nudge(value.a))
		TYPE_QUATERNION: return (value * Quaternion(Vector3.UP, 0.5)).normalized()
		TYPE_RECT2: return Rect2(value.position + Vector2(0.5, 0.5), value.size + Vector2(0.5, 0.5))
	return null

func _close(a: Variant, b: Variant) -> bool:
	if typeof(a) != typeof(b): return false
	match typeof(a):
		TYPE_FLOAT: return absf(a - b) <= 0.001 * maxf(1.0, absf(a))
		TYPE_INT: return a == b
		TYPE_QUATERNION: return absf(a.normalized().dot(b.normalized())) >= 0.999
		TYPE_RECT2: return _close(a.position, b.position) and _close(a.size, b.size)
		TYPE_COLOR: return _close(Vector4(a.r, a.g, a.b, a.a), Vector4(b.r, b.g, b.b, b.a))
		_: return (a - b).length() <= 0.001 * maxf(1.0, a.length())

func _read(scheduler, target, factory: String) -> Variant:
	var probe: TweensGdHandle = scheduler.add(target, factories.call(factory))
	scheduler.update(0.0)
	probe.cancel()
	return probe.value

func _catalog() -> void:
	var entries: Array = JSON.parse_string(FileAccess.get_file_as_string("res://conformance/adapters.json"))
	check(entries.size() == 331, "catalog covers every concrete C# property/value adapter")
	for entry in entries:
		var scheduler := TweensGdScheduler.new()
		var target := _create(entry.target)
		var definition: TweensGdDefinition = factories.call(entry.name)
		var probe := scheduler.add(target, definition)
		scheduler.update(0.0)
		check(probe.completion_reason != T.Reason.FAILED, entry.name + " starts: " + probe.error)
		if probe.completion_reason != T.Reason.FAILED:
			var initial: Variant = probe.value
			probe.cancel()
			var first: Variant = _perturb(initial)
			if entry.kind != "value":
				scheduler.add(target, factories.call(entry.name, first))
				scheduler.update(0.0)
				initial = _read(scheduler, target, entry.name)
				check(_close(first, initial), entry.name + " writes its native property")
			var to: Variant = _perturb(initial)
			var move: TweensGdDefinition = factories.call(entry.name, to, 1.0)
			var playing := scheduler.add(target, move)
			scheduler.update(0.5)
			var expected: Variant = TweensGdInterpolation.interpolate(initial, to, 0.5, typeof(initial))
			var actual: Variant = playing.value if entry.kind == "value" else _read(scheduler, target, entry.name)
			check(_close(expected, actual), "%s midpoint: expected %s, got %s" % [entry.name, expected, actual])
			scheduler.update(0.5)
			check(playing.completion_reason == T.Reason.COMPLETED and _close(playing.value, to), entry.name + " completes")
			if entry.kind != "value":
				move.to_value = initial
				move.fill = T.Fill.NONE
				var restore := scheduler.add(target, move)
				scheduler.update(1.0)
				check(restore.completion_reason == T.Reason.COMPLETED and _close(_read(scheduler, target, entry.name), to), entry.name + " restores captured property")
		scheduler.dispose()
		if target is PathFollow2D or target is PathFollow3D: target.get_parent().free()
		elif target is Node: target.free()

func _curves() -> void:
	for bounds in [Vector2(-3, -1), Vector2(2, 5), Vector2(0, 1)]:
		var curve := Curve.new()
		curve.min_domain = minf(bounds.x, 0.0)
		curve.max_domain = maxf(bounds.y, 1.0)
		curve.min_domain = bounds.x
		curve.max_domain = bounds.y
		curve.min_value = -2.0
		curve.max_value = 3.0
		curve.bake_resolution = 37
		curve.add_point(Vector2(bounds.x, -1), 0.25, 0.5, Curve.TANGENT_FREE, Curve.TANGENT_LINEAR)
		curve.add_point(Vector2((bounds.x + bounds.y) / 2.0, 2), -0.25, 0.75)
		curve.add_point(Vector2(bounds.y, 1), 1.0, 0.0, Curve.TANGENT_LINEAR, Curve.TANGENT_FREE)
		var definition := TweensGdDefinition.new()
		definition.curve = curve
		var copy := definition.copy().curve
		check(copy != curve and copy.min_domain == curve.min_domain and copy.max_domain == curve.max_domain, "curve snapshot preserves non-default domains")
		check(copy.min_value == -2.0 and copy.max_value == 3.0 and copy.bake_resolution == 37, "curve snapshot preserves range and bake resolution")
		for fraction in [-0.5, 0.0, 0.25, 0.5, 0.75, 1.0, 1.5]:
			var x := lerpf(bounds.x, bounds.y, fraction)
			host.near(copy.sample(x), curve.sample(x), "curve copy preserves mixed tangents and extrapolation")
		curve.set_point_value(1, 0.0)
		host.near(copy.get_point_position(1).y, 2.0, "curve snapshot owns its control points")
