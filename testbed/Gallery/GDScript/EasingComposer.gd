# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

# Each family's In and Out curve, in the order of the scene's family lists.
const FAMILIES = [
	[In.NONE, Out.NONE], [In.LINEAR, Out.LINEAR], [In.SINE, Out.SINE], [In.QUAD, Out.QUAD], [In.CUBIC, Out.CUBIC],
	[In.QUART, Out.QUART], [In.QUINT, Out.QUINT], [In.EXPO, Out.EXPO], [In.CIRC, Out.CIRC], [In.BACK, Out.BACK],
	[In.ELASTIC, Out.ELASTIC], [In.BOUNCE, Out.BOUNCE], [In.SMOOTH_STEP, Out.SMOOTH_STEP],
	[In.SMOOTHER_STEP, Out.SMOOTHER_STEP], [In.BACK10, Out.BACK10], [In.BACK20, Out.BACK20], [In.BACK30, Out.BACK30],
	[In.BACK40, Out.BACK40], [In.BACK50, Out.BACK50], [In.ELASTIC10, Out.ELASTIC10], [In.ELASTIC20, Out.ELASTIC20],
	[In.ELASTIC30, Out.ELASTIC30], [In.ELASTIC40, Out.ELASTIC40], [In.ELASTIC50, Out.ELASTIC50],
	[In.BOUNCE10, Out.BOUNCE10], [In.BOUNCE20, Out.BOUNCE20], [In.BOUNCE30, Out.BOUNCE30], [In.BOUNCE40, Out.BOUNCE40],
	[In.BOUNCE50, Out.BOUNCE50], [In.JUMP, Out.JUMP], [In.JUMP10, Out.JUMP10], [In.JUMP20, Out.JUMP20],
	[In.JUMP30, Out.JUMP30], [In.JUMP40, Out.JUMP40], [In.JUMP50, Out.JUMP50]
]
var preview: TweensGdHandle

func animate() -> void:
	targets.entry.item_selected.connect(func(_index): refresh())
	targets.exit.item_selected.connect(func(_index): refresh())
	targets.skew.value_changed.connect(func(_value): refresh())
	targets.blend.item_selected.connect(func(_index): refresh())
	targets.width.value_changed.connect(func(_value): refresh())
	refresh()
	# A separate lifetime wait survives preview restarts and also settles on engine shutdown.
	var lifetime := Tweens.value(0.0, 1.0, seconds)
	lifetime.repeats = Tweens.INFINITE
	await Tweens.play(stage, lifetime).wait()
	preview = null

func refresh() -> void:
	if preview: preview.cancel()
	var entry_family: Array = FAMILIES[targets.entry.get_selected_id()]
	var exit_family: Array = FAMILIES[targets.exit.get_selected_id()]
	var a: int = entry_family[0]
	var b: int = exit_family[1]
	var ease: int = a | b
	var split: float = targets.skew.value
	var method: int = targets.blend.selected
	var width: float = targets.width.value
	var single: bool = not a or not b or a == exit_family[0]
	targets.skew.editable = a != 0 and b != 0
	single = single or split == 0.0 or split == 1.0
	targets.blend.disabled = single
	targets.width.editable = not single
	var entry_pair: int = a | entry_family[1] if b else a
	var exit_pair: int = exit_family[0] | b if a else b
	targets.entryCurve.points = sample(entry_pair, split, method, width, 0.0, 1.0 if not b else split) if a else PackedVector2Array()
	targets.exitCurve.points = sample(exit_pair, split, method, width, 0.0 if not a else split, 1.0) if b else PackedVector2Array()
	targets.resultCurve.points = sample(ease, split, method, width)
	targets.fit_preview.call()
	var left := -200.0 + 400.0 * (split - width * minf(split,1.0-split))
	var right := -200.0 + 400.0 * (split + width * minf(split,1.0-split))
	targets.region.polygon = PackedVector2Array([Vector2(left, -110), Vector2(right, -110), Vector2(right, 85), Vector2(left, 85)])
	targets.region.visible = not single
	targets.recipe.text = "%s | %s    ·    Skew %.2f    ·    %s %d%%" % [targets.entry.get_item_text(targets.entry.selected), targets.exit.get_item_text(targets.exit.selected), split, targets.blend.get_item_text(method), roundi(width*100)]
	targets.tracer.position = Vector2(-200, 66)
	var motion := Tweens.position_2d_x(200.0, seconds, ease)
	# For example: In.SINE | Out.CUBIC, or InOut.SINE.
	motion.from_value = -200.0
	motion.skew = split
	motion.blend_type = method
	motion.blend = width
	motion.repeats = Tweens.INFINITE
	motion.repeat_interval = 0.35
	motion.on_update = func(handle, x):
		targets.tracer.position = Vector2(-200.0 + 400.0 * handle.progress, 66.0 - 132.0 * ((x + 200.0) / 400.0))
	preview = Tweens.play(targets.ball, motion)

func sample(ease: int, split: float, method: int, width: float, start: float = 0.0, end: float = 1.0) -> PackedVector2Array:
	var points := PackedVector2Array()
	for i in range(241):
		var t := start + (end-start)*i/240.0
		points.append(Vector2(-200.0 + 400.0 * t, 66.0 - 132.0 * Tweens.Easing.evaluate(ease, t, method, width, split)))
	return points
