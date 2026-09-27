# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.progress, cycle(Tweens.range_value(100.0, seconds))),
		Tweens.play(targets.swatch, cycle(Tweens.color_rect_color(BLUE, seconds))),
		Tweens.play(targets.swatch, cycle(Tweens.self_modulate_alpha(0.25, seconds))),
	]).wait()
