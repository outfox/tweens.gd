# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted
const T = preload("res://addons/tweens_gd/tweens.gd")
const WARMUP := 120
const SAMPLES := 240

static func run(owner: Node) -> String:
	var results: Array = []
	for count in [1000, 10000]:
		for profile in [
			["linear", T.Ease.LINEAR, T.BlendType.HERMITE],
			["legacy_sine", T.Ease.SINE_IN_OUT, T.BlendType.HERMITE],
			["paired_sine", T.InOut.SINE, T.BlendType.HERMITE],
			["mixed_hermite", T.In.QUAD | T.Out.CUBIC, T.BlendType.HERMITE],
			["mixed_smoothstep", T.In.QUAD | T.Out.CUBIC, T.BlendType.SMOOTH_STEP],
			["mixed_linear", T.In.QUAD | T.Out.CUBIC, T.BlendType.LINEAR],
			["back30_bounce20", T.In.BACK30 | T.Out.BOUNCE20, T.BlendType.HERMITE],
			["solo_jump30", T.Out.JUMP30, T.BlendType.HERMITE],
		]: results.append(_measure(owner, count, profile))
	return JSON.stringify({"engine": Engine.get_version_info(), "os": OS.get_name(),
		"cpu": OS.get_processor_name(), "debug_build": OS.is_debug_build(),
		"warmup": WARMUP, "samples": SAMPLES, "delta_seconds": 1.0/60.0,
		"duration_seconds": 1.0, "blend": 0.2, "results": results})

static func _measure(owner: Node, count: int, profile: Array) -> Dictionary:
	var targets: Array[Node2D] = []
	for i in range(count):
		var target := Node2D.new()
		owner.add_child(target)
		targets.append(target)
	var definition := T.property(^"position", Vector2(100, 200), 1.0, profile[1])
	definition.from_value = Vector2.ZERO
	definition.repeats = T.INFINITE
	definition.blend_type = profile[2]
	definition.blend = 0.2
	var scheduler := TweensGdScheduler.new()
	var start := Time.get_ticks_usec()
	for i in range(count):
		definition.offset = float(i)/count
		scheduler.add(targets[i], definition)
	var create_us := Time.get_ticks_usec()-start
	var samples: Array[int] = []
	for frame in range(WARMUP+SAMPLES):
		start = Time.get_ticks_usec()
		scheduler.update(1.0/60.0)
		if frame >= WARMUP: samples.append(Time.get_ticks_usec()-start)
	samples.sort()
	scheduler.dispose()
	for target in targets: target.free()
	return {"backend": "gdscript", "count": count, "profile": profile[0],
		"median_update_us": samples[SAMPLES/2], "p95_update_us": samples[int(SAMPLES*0.95)-1],
		"create_us": create_us}
