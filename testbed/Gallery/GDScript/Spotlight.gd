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
