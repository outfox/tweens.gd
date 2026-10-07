# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.ribbon, cycle(Tweens.line_2d_width(16.0, seconds))),
		Tweens.play(targets.ribbon, cycle(Tweens.line_2d_default_color(BLUE, seconds))),
		Tweens.play(targets.glow, cycle(Tweens.line_2d_width(40.0, seconds))),
		Tweens.play(targets.glow, cycle(Tweens.line_2d_default_color(Color(BLUE, 0.28), seconds))),
	]).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
