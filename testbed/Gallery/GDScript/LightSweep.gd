# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.light, cycle(Tweens.point_light_2d_texture_scale(2.5, seconds))),
		Tweens.play(targets.light, cycle(Tweens.light_energy_2d(2.0, seconds))),
		Tweens.play(targets.light, cycle(Tweens.position_2d_x(100.0, seconds))),
	]).wait()

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.use_ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
