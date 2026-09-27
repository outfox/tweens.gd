# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	await Tweens.group([
		Tweens.play(targets.particles, cycle(Tweens.cpu_particles_2d_spread(75.0, seconds))),
		Tweens.play(targets.particles, cycle(Tweens.cpu_particles_2d_gravity(Vector2(15, -55), seconds))),
		Tweens.play(targets.particles, cycle(Tweens.cpu_particles_2d_color(AMBER, seconds))),
		Tweens.play(targets.particles, cycle(Tweens.position_2d_y(-30.0, seconds))),
	]).wait()
