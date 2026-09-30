# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.spot, cycle(Tweens.spot_angle(52.0, seconds))),
		Tweens.play(targets.spot, cycle(Tweens.light_color_3d(BLUE, seconds))),
		Tweens.play(targets.spot, cycle(Tweens.light_energy_3d(3.0, seconds))),
	]).wait()

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.use_ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
