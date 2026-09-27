# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.cube, cycle(Tweens.global_quaternion_3d(Quaternion.from_euler(Vector3(0.5, 2.5, 0.8)), seconds))),
		Tweens.play(targets.cube, cycle(Tweens.scale_3d(Vector3(1.4, 0.7, 1.1), seconds))),
	]).wait()
