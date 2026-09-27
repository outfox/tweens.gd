# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var handles: Array = []
	for i in targets.echoes.size():
		add_polygon(handles, targets.echoes[i], (targets.echoes.size() - i) * 0.08)
	add_polygon(handles, targets.polygon, 0.0)
	await Tweens.group(handles).wait()

func add_polygon(handles: Array, target: Polygon2D, delay: float) -> void:
	handles.append(Tweens.play(target, cycle(Tweens.polygon_2d_color(AMBER, seconds), delay)))
	handles.append(Tweens.play(target, cycle(Tweens.polygon_2d_offset(Vector2(40, 0), seconds), delay)))
	handles.append(Tweens.play(target, cycle(Tweens.rotation_2d(PI, seconds * 2.0), delay)))
