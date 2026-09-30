# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var amount = cycle(Tweens.instance_shader_parameter(&"amount", null, seconds))
	var first = amount.copy()
	first.to_value = 0.85
	var second = amount.copy()
	second.to_value = 0.15
	await Tweens.group([
		Tweens.play(targets.first, first),
		Tweens.play(targets.second, second),
	]).wait()

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.use_ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
