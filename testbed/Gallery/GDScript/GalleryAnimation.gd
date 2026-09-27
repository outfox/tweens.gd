# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends RefCounted
## Shared gallery lifecycle and scene bindings. Animation helpers live in each example.
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
