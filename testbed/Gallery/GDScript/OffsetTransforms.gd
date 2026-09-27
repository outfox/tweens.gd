# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_position(Vector2(0, -22), seconds))),
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_rotation(0.18, seconds))),
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_scale(Vector2(1.13, 1.13), seconds))),
	]).wait()
