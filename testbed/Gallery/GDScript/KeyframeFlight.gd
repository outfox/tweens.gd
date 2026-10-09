# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene setup is in KeyframeFlight.cs.

func animate() -> void:
	var flight := Tweens.keyframes({
		0: {"scale": 1, "rotation_degrees": 0, "modulate": Color.CORAL},
		35: {"x": 20, "scale": 1.5, "rotation_degrees": -20, "modulate": Color.GOLD},
		65: {"x": 135, "rotation_degrees": 20, "interpolation": Out.QUAD},
		100: {"x": 180, "scale": 1, "rotation_degrees": 0, "modulate": Color(0, 0, 0, 0)},
	}, seconds)
	flight.options.ping_pong = true
	flight.options.repeats = Tweens.INFINITE
	flight.options.repeat_interval = 0.4
	var plays: Array[TweensGdGroup] = []
	for flyer: Node2D in targets.flyers: plays.append(Tweens.animate(flyer, flight))
	for play in plays: await play.wait()
