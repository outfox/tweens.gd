# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var easings := [InOut.LINEAR, InOut.SINE, InOut.CUBIC, InOut.EXPO,
		Tweens.Ease.BACK_IN_OUT, Tweens.Ease.ELASTIC_OUT, Tweens.Ease.BOUNCE_OUT]
	var handles: Array = []
	for lane in targets.racers.size():
		for position in targets.racers[lane].size():
			var movement := cycle(Tweens.position_2d_x(120.0, seconds), position * 0.05)
			movement.ease = easings[lane]
			movement.repeat_interval = 0.3
			movement.ping_pong_interval = 0.3
			handles.append(Tweens.play(targets.racers[lane][position], movement))
	await Tweens.group(handles).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
