# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene setup is in ColorInterpolation.cs. The comparison uses the library color policies; OKLab + premultiplied is the default.
var clock: TweensGdHandle

func animate() -> void:
	var definition := Tweens.value(0.0, 1.0, seconds)
	definition.ease = Tweens.Ease.LINEAR
	definition.ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.ping_pong_interval = 0.35
	definition.repeat_interval = 0.35
	definition.on_update = func(_handle, value): sample(value)
	clock = Tweens.play(stage, definition)
	var scrub_connection: int = targets.progress.value_changed.connect(scrub)
	var midpoint_connection: int = targets.midpoint.pressed.connect(func(): scrub(0.5))
	var automatic_connection: int = targets.automatic.toggled.connect(func(enabled): clock.set_paused(not enabled))
	if scrub_connection != OK or midpoint_connection != OK or automatic_connection != OK:
		push_error("Could not connect color comparison controls.")
		clock.cancel()
	sample(0.0)
	await clock.wait()

func scrub(value: float) -> void:
	targets.automatic.set_pressed_no_signal(false)
	clock.pause()
	sample(value)

func sample(value: float) -> void:
	targets.progress.set_value_no_signal(value)
	for row in 6:
		for column in 4:
			var color := mix_color(targets.starts[column], targets.ends[column], value, row >> 1, (row & 1) != 0)
			# Independent RGB tint and opacity: both tint endpoints are opaque.
			if column == 3:
				color.a = 1.0 - value
			var swatch: ColorRect = targets.swatches[row * 4 + column]
			swatch.modulate = color
			swatch.tooltip_text = "R %.3f · G %.3f · B %.3f · A %.3f" % [color.r, color.g, color.b, color.a]

func mix_color(from: Color, to: Color, weight: float, space: int, premultiply: bool) -> Color:
	var color_space := Tweens.ColorSpace.SRGB if space == 0 else Tweens.ColorSpace.LINEAR_RGB if space == 1 else Tweens.ColorSpace.OKLAB
	var alpha_mode := Tweens.AlphaMode.PREMULTIPLIED if premultiply else Tweens.AlphaMode.STRAIGHT
	return TweensGdInterpolation.interpolate_color(from, to, weight, color_space, alpha_mode)