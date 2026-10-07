# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_position([0, -22], seconds))),
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_rotation(0.18, seconds))),
		Tweens.play(targets.featured, cycle(Tweens.control_offset_transform_scale([1.13, 1.13], seconds))),
	]).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
