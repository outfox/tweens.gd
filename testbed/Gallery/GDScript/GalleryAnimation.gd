# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted
## Shared playback helpers. Scene objects are built by the C# gallery.
const Tweens = preload("res://addons/tweens_gd/tweens.gd")
const MINT = Color("#79deb4")
const AMBER = Color("#f2bc74")
const BLUE = Color("#8caaff")
const OUTLINE = Color("#2d4057")
signal finished
var stage: Control
var targets: Dictionary
var seconds: float
var tempo: float

func start(scene: Control, bindings: Dictionary, duration: float) -> void:
	stage = scene
	targets = bindings
	seconds = duration
	tempo = seconds / 1.8
	await animate()
	finished.emit()

func animate() -> void:
	pass

func cycle(definition, delay: float = 0.0):
	definition.ease = Tweens.Ease.CUBIC_IN_OUT
	definition.use_ping_pong = true
	definition.repeats = Tweens.INFINITE
	definition.repeat_interval = 0.25
	definition.ping_pong_interval = 0.15
	definition.delay = delay
	return definition

func options(definition, easing = Tweens.Ease.LINEAR, from = null, delay: float = 0.0):
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition

func wait(seconds_to_wait: float) -> bool:
	return await Tweens.play(stage, Tweens.value(0.0, 1.0, seconds_to_wait)).wait() == Tweens.Reason.COMPLETED

func shake(progress: float) -> float:
	return sin(progress * 42.0) * (1.0 - progress) * (1.0 - progress)
