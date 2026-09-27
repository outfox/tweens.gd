# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.material, cycle(Tweens.shader_parameter(&"tint", AMBER, seconds)), stage),
		Tweens.play(targets.material, cycle(Tweens.shader_parameter(&"offset", Vector2(0.25, 0.33), seconds)), stage),
	]).wait()
