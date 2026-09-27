# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.text, cycle(Tweens.label_visible_ratio(1.0, seconds))),
		Tweens.play(targets.text, cycle(Tweens.self_modulate(AMBER, seconds))),
		Tweens.play(targets.underline, cycle(Tweens.control_scale_x(1.0, seconds))),
		Tweens.play(targets.underline, cycle(Tweens.color_rect_color(AMBER, seconds))),
	]).wait()
