# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var handles: Array = []
	for target in [targets.shape, targets.shadow]:
		handles.append(Tweens.play(target, cycle(Tweens.skew_2d(0.5, seconds))))
		handles.append(Tweens.play(target, cycle(Tweens.rotation_2d(PI, seconds))))
		handles.append(Tweens.play(target, cycle(Tweens.scale_2d_x(1.8, seconds))))
		handles.append(Tweens.play(target, cycle(Tweens.scale_2d_y(0.6, seconds))))
	await Tweens.group(handles).wait()
