# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var pulse = options(Tweens.scale_2d(Vector2(2.4, 2.4), 1.2), Tweens.Ease.QUART_OUT, Vector2.ONE)
	pulse.repeats = Tweens.INFINITE
	var fade = options(Tweens.modulate_alpha(0.0, 1.2), Tweens.Ease.QUAD_IN, 1.0)
	fade.repeats = Tweens.INFINITE
	await Tweens.group([
		Tweens.play(targets.camera, cycle(Tweens.camera_2d_zoom(Vector2(1.8, 1.8), seconds))),
		Tweens.play(targets.camera, cycle(Tweens.camera_2d_offset(Vector2(90, 25), seconds))),
		Tweens.play(targets.beacon, pulse),
		Tweens.play(targets.beacon, fade),
	]).wait()
