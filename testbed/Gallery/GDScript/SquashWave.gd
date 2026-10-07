# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var handles: Array = []
	for i in targets.pills.size():
		var pill: Polygon2D = targets.pills[i]
		var definitions := [
			Tweens.scale_2d_y(2.6, seconds * 0.5),
			Tweens.scale_2d_x(0.62, seconds * 0.5),
			Tweens.polygon_2d_color(pill.color.lightened(0.45), seconds * 0.5),
		]
		for definition in definitions:
			cycle(definition, i * 0.07 * tempo)
			definition.repeat_interval = 0.1
			definition.ping_pong_interval = 0.05
			handles.append(Tweens.play(pill, definition))
	await Tweens.group(handles).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
