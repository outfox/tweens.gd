# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

func animate() -> void:
	report(0, "1 / Position")
	if await Tweens.play(targets.courier, options(Tweens.position_2d_x(150.0, seconds), Tweens.Ease.CUBIC_IN_OUT)).wait() != Tweens.Reason.COMPLETED:
		return
	report(1, "2 / Position + rotation")
	if await Tweens.group([
		Tweens.play(targets.courier, options(Tweens.position_2d_x(-150.0, seconds), Tweens.Ease.CUBIC_IN_OUT)),
		Tweens.play(targets.courier, options(Tweens.rotation_2d(TAU, seconds), Tweens.Ease.CUBIC_IN_OUT)),
	]).wait() != Tweens.Reason.COMPLETED:
		return
	await report(2, "3 / Complete")

func report(step: int, text: String) -> void:
	targets.status.text = text
	var handles: Array = []
	for i in targets.steps.size():
		handles.append(Tweens.play(targets.steps[i], Tweens.polygon_2d_color(MINT if i <= step else OUTLINE, 0.2)))
	handles.append(Tweens.play(targets.steps[step], options(Tweens.scale_2d(Vector2.ONE, 0.5), Tweens.Ease.ELASTIC_OUT, Vector2(2, 2))))
	await Tweens.group(handles).wait()

func options(definition, easing = Tweens.Ease.LINEAR, from = null, delay: float = 0.0):
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition
