# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.material, cycle(Tweens.material_emission(Color(0.12, 0.08, 0.3), seconds)), stage),
		Tweens.play(targets.material, cycle(Tweens.material_emission_energy_multiplier(2.0, seconds)), stage),
	]).wait()
