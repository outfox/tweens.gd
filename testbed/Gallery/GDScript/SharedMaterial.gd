# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.material, cycle(Tweens.material_albedo_color(AMBER, seconds)), stage),
		Tweens.play(targets.material, cycle(Tweens.material_roughness(0.95, seconds)), stage),
	]).wait()
