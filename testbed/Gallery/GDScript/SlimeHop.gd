# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const GROUND = 52.0
const APEX = -26.0
const STRIDE = 90.0
const BOUNDS = 136.0
var direction = 1
var hops = 0

func animate() -> void:
	var blink = Tweens.scale_2d_y(0.1, 0.07)
	blink.use_ping_pong = true
	blink.repeats = Tweens.INFINITE
	blink.repeat_interval = 2.2
	blink.delay = 0.9
	var blinking = Tweens.play(targets.eyes, blink)
	while await hop():
		pass
	await blinking.wait()

func hop() -> bool:
	var slime = targets.slime
	var air = 0.32 * tempo
	if abs(slime.position.x + direction * STRIDE) > BOUNDS:
		direction = -direction
	var destination = slime.position.x + direction * STRIDE
	Tweens.play(targets.pupils, options(Tweens.position_2d_x(direction * 3.0, 0.2 * tempo), Tweens.Ease.BACK_OUT))
	if not await reshape(Vector2(1.38, 0.6), 0.32, Tweens.Ease.SINE_OUT):
		return false
	if not await reshape(Vector2(0.68, 1.45), 0.08, Tweens.Ease.QUAD_OUT):
		return false
	travel(destination, air * 2.0)
	if not await flight(APEX, Vector2.ONE, air, Tweens.Ease.QUAD_OUT):
		return false
	if not await flight(GROUND, Vector2(0.82, 1.25), air, Tweens.Ease.QUAD_IN):
		return false
	splash(destination)
	if not await reshape(Vector2(1.6, 0.5), 0.06, Tweens.Ease.QUAD_OUT):
		return false
	return await reshape(Vector2.ONE, 0.75, Tweens.Ease.ELASTIC_OUT)

func reshape(scale: Vector2, duration: float, easing: int) -> bool:
	return await Tweens.play(targets.slime, options(Tweens.scale_2d(scale, duration * tempo), easing)).wait() == Tweens.Reason.COMPLETED

func travel(destination: float, duration: float) -> void:
	var handles = [Tweens.play(targets.slime, options(Tweens.position_2d_x(destination, duration), Tweens.Ease.SINE_IN_OUT))]
	hops += 1
	if hops % 2 == 0:
		handles.append(Tweens.play(targets.body, options(Tweens.rotation_2d(direction * TAU, duration), Tweens.Ease.CUBIC_IN_OUT, 0.0)))
	await Tweens.group(handles).wait()

func flight(height: float, scale: Vector2, duration: float, easing: int) -> bool:
	return await Tweens.group([
		Tweens.play(targets.slime, options(Tweens.position_2d_y(height, duration), easing)),
		Tweens.play(targets.slime, options(Tweens.scale_2d(scale, duration), easing)),
	]).wait() == Tweens.Reason.COMPLETED

func splash(x: float) -> void:
	var handles: Array = []
	var origin = Vector2(x, GROUND - 6.0)
	for i in targets.drops.size():
		var angle = PI + (i + 0.5) / targets.drops.size() * PI
		var landing = origin + Vector2(cos(angle) * 58.0, sin(angle) * 34.0)
		var drop = targets.drops[i]
		drop.position = origin
		drop.scale = Vector2.ONE * (1.0 if i % 2 == 0 else 0.7)
		handles.append(Tweens.play(drop, options(Tweens.position_2d(landing, 0.4 * tempo), Tweens.Ease.QUART_OUT)))
		handles.append(Tweens.play(drop, options(Tweens.modulate_alpha(0.0, 0.4 * tempo), Tweens.Ease.CUBIC_IN, 1.0)))
	await Tweens.group(handles).wait()
