# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var progress = cycle(Tweens.path_follow_2d_progress_ratio(1.0, seconds))
	var offset = cycle(Tweens.path_follow_2d_v_offset(20.0, seconds))
	var handles: Array = []
	for i in targets.echoes.size():
		var trailing_progress = progress.copy()
		var trailing_offset = offset.copy()
		trailing_progress.delay = (i + 1) * 0.07
		trailing_offset.delay = trailing_progress.delay
		handles.append(Tweens.play(targets.echoes[i], trailing_progress))
		handles.append(Tweens.play(targets.echoes[i], trailing_offset))
	handles.append(Tweens.play(targets.leader, progress))
	handles.append(Tweens.play(targets.leader, offset))
	handles.append(Tweens.play(targets.ship, cycle(Tweens.scale_2d([1.6, 1.6], seconds))))
	await Tweens.group(handles).wait()

func cycle(definition, delay: float = 0.0):
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
