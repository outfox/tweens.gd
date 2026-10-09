# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene setup is in ColorInterpolation.cs. These are comparison policies, not library defaults.
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
	if weight == 0.0: return from
	if weight == 1.0: return to
	var a := coordinates(from, space)
	var b := coordinates(to, space)
	var alpha := lerpf(from.a, to.a, weight)
	if premultiply:
		a *= from.a
		b *= to.a
	var value := a.lerp(b, weight)
	if premultiply:
		value = Vector3.ZERO if alpha == 0.0 else value / alpha
	if space == 2:
		value = from_oklab(value)
	var result := Color(value.x, value.y, value.z, alpha)
	return result if space == 0 else result.linear_to_srgb()

func coordinates(color: Color, space: int) -> Vector3:
	if space != 0:
		color = color.srgb_to_linear()
	var value := Vector3(color.r, color.g, color.b)
	return to_oklab(value) if space == 2 else value

# Björn Ottosson's linear-sRGB/OKLab matrices: https://bottosson.github.io/posts/oklab/
func cube_root(value: float) -> float:
	return signf(value) * pow(absf(value), 1.0 / 3.0)

func to_oklab(rgb: Vector3) -> Vector3:
	var l := cube_root(0.4122214708 * rgb.x + 0.5363325363 * rgb.y + 0.0514459929 * rgb.z)
	var m := cube_root(0.2119034982 * rgb.x + 0.6806995451 * rgb.y + 0.1073969566 * rgb.z)
	var s := cube_root(0.0883024619 * rgb.x + 0.2817188376 * rgb.y + 0.6299787005 * rgb.z)
	return Vector3(0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
		1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
		0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s)

func from_oklab(lab: Vector3) -> Vector3:
	var l := lab.x + 0.3963377774 * lab.y + 0.2158037573 * lab.z
	var m := lab.x - 0.1055613458 * lab.y - 0.0638541728 * lab.z
	var s := lab.x - 0.0894841775 * lab.y - 1.2914855480 * lab.z
	l *= l * l
	m *= m * m
	s *= s * s
	return Vector3(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
		-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
		-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s)
