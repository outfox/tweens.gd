# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	var progress := cycle(Tweens.path_follow_3d_progress_ratio(1.0, seconds * 1.5))
	var offset := cycle(Tweens.path_follow_3d_v_offset(0.35, seconds))
	var handles: Array = []
	for i in targets.echoes.size():
		var trailing_progress := progress.copy()
		var trailing_offset := offset.copy()
		trailing_progress.delay = (i + 1) * 0.08
		trailing_offset.delay = trailing_progress.delay
		handles.append(Tweens.play(targets.echoes[i], trailing_progress))
		handles.append(Tweens.play(targets.echoes[i], trailing_offset))
	handles.append(Tweens.play(targets.leader, progress))
	handles.append(Tweens.play(targets.leader, offset))
	await Tweens.group(handles).wait()

func cycle(definition: TweensGdDefinition, delay: float = 0.0) -> TweensGdDefinition:
	definition.ease = InOut.CUBIC
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition
