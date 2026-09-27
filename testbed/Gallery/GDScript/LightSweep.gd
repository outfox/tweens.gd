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
