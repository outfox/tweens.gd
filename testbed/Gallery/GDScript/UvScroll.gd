# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.material, cycle(Tweens.material_uv1_offset_x(1.0, seconds * 2.0)), stage),
		Tweens.play(targets.material, cycle(Tweens.material_uv1_scale(Vector3(2.5, 2.5, 1), seconds * 2.0)), stage),
	]).wait()
