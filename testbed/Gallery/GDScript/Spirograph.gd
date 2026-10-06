# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const TRAIL_LENGTH = 420

func animate() -> void:
	var pulse = cycle(Tweens.scale_2d([1.35, 1.35], 0.6 * tempo))
	pulse.ease = InOut.SINE
	pulse.repeat_interval = 0.0
	pulse.ping_pong_interval = 0.0
	var revolution = Tweens.value(0.0, 1.0, 7.0 * tempo)
	revolution.repeats = Tweens.INFINITE
	revolution.on_update = func(_handle, progress): draw(progress)
	await Tweens.group([
		Tweens.play(targets.sun, pulse),
		Tweens.play(stage, revolution),
	]).wait()

func draw(progress: float) -> void:
	var a = progress * TAU
	var inner_joint = Vector2.from_angle(a * 2.0) * 58.0
	var inner_tip = inner_joint + Vector2.from_angle(-a * 8.0) * 26.0
	var outer_joint = Vector2.from_angle(-a) * 76.0
	var outer_tip = outer_joint + Vector2.from_angle(a * 11.0) * 12.0
	targets.innerArm.points = PackedVector2Array([Vector2.ZERO, inner_joint, inner_tip])
	targets.outerArm.points = PackedVector2Array([Vector2.ZERO, outer_joint, outer_tip])
	targets.innerPen.position = inner_tip
	targets.outerPen.position = outer_tip
	extend(targets.innerTrail, inner_tip)
	extend(targets.outerTrail, outer_tip)

func extend(trail: Line2D, tip: Vector2) -> void:
	trail.add_point(tip)
	while trail.get_point_count() > TRAIL_LENGTH:
		trail.remove_point(0)

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
