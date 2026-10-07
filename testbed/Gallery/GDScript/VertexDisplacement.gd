# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.play(targets.deformed, cycle(Tweens.instance_shader_parameter(&"amplitude", 0.22, seconds))).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
