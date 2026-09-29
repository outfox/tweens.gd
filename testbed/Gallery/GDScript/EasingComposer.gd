# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const FAMILIES = ["NONE", "LINEAR", "SINE", "QUAD", "CUBIC", "QUART", "QUINT", "EXPO",
	"CIRC", "BACK", "ELASTIC", "BOUNCE", "SMOOTH_STEP", "SMOOTHER_STEP"]
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
	var a: int = Tweens.In[FAMILIES[targets.entry.selected]]
	var b: int = Tweens.Out[FAMILIES[targets.exit.selected]]
	var ease: int = a | b
	var exponent: float = targets.skew.value
	var method: int = targets.blend.selected
	var width: float = targets.width.value
	var single: bool = not a or not b or targets.entry.selected == targets.exit.selected
	targets.blend.disabled = single
	targets.width.editable = not single
	var entry_pair: int = a | Tweens.Out[FAMILIES[targets.entry.selected]] if b else a
	var exit_pair: int = Tweens.In[FAMILIES[targets.exit.selected]] | b if a else b
	targets.entryCurve.points = sample(entry_pair, exponent, method, width, 0.0, 1.0 if not b else pow(0.5, 1.0/exponent)) if a else PackedVector2Array()
	targets.exitCurve.points = sample(exit_pair, exponent, method, width, 0.0 if not a else pow(0.5, 1.0/exponent), 1.0) if b else PackedVector2Array()
	targets.resultCurve.points = sample(ease, exponent, method, width)
	var left := -200.0 + 400.0 * pow((1.0-width)/2.0, 1.0 / exponent)
	var right := -200.0 + 400.0 * pow((1.0+width)/2.0, 1.0 / exponent)
	targets.region.polygon = PackedVector2Array([Vector2(left, -110), Vector2(right, -110), Vector2(right, 85), Vector2(left, 85)])
	targets.region.visible = not single
	targets.recipe.text = "%s | %s    ·    Skew %.2f    ·    %s %d%%" % [targets.entry.get_item_text(targets.entry.selected), targets.exit.get_item_text(targets.exit.selected), exponent, targets.blend.get_item_text(method), roundi(width*100)]
	targets.tracer.position = Vector2(-200, 66)
	var motion := Tweens.position_2d_x(200.0, seconds, ease)
	# For example: Tweens.In.SINE | Tweens.Out.CUBIC, or Tweens.InOut.SINE.
	motion.from_value = -200.0
	motion.skew = exponent
	motion.blend_type = method
	motion.blend = width
	motion.repeats = Tweens.INFINITE
	motion.repeat_interval = 0.35
	motion.on_update = func(handle, x):
		targets.tracer.position = Vector2(-200.0 + 400.0 * handle.progress, 66.0 - 132.0 * ((x + 200.0) / 400.0))
	preview = Tweens.play(targets.ball, motion)

func sample(ease: int, exponent: float, method: int, width: float, start: float = 0.0, end: float = 1.0) -> PackedVector2Array:
	var points := PackedVector2Array()
	for i in range(241):
		var t := start + (end-start)*i/240.0
		points.append(Vector2(-200.0 + 400.0 * t, 66.0 - 132.0 * Tweens.Easing.evaluate(ease, pow(t, exponent), method, width)))
	return points
