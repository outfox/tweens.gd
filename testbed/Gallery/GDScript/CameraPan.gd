# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var pulse = options(Tweens.scale_2d([2.4, 2.4], 1.2), Out.QUART, Vector2.ONE)
	pulse.repeats = Tweens.INFINITE
	var fade = options(Tweens.modulate_alpha(0.0, 1.2), In.QUAD, 1.0)
	fade.repeats = Tweens.INFINITE
	await Tweens.group([
		Tweens.play(targets.camera, cycle(Tweens.camera_2d_zoom([1.8, 1.8], seconds))),
		Tweens.play(targets.camera, cycle(Tweens.camera_2d_offset([90, 25], seconds))),
		Tweens.play(targets.beacon, pulse),
		Tweens.play(targets.beacon, fade),
	]).wait()

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition

func options(definition, easing = InOut.LINEAR, from = null, delay: float = 0.0):
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition
