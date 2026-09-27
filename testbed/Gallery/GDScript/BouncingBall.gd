# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const GROUND = 62.0
const APEX = -34.0
const STRIDE = 75.0
const BOUNDS = 151.0
var direction = 1

func animate() -> void:
	while await bounce():
		pass

func bounce() -> bool:
	var ball = targets.ball
	var air = 0.36 * tempo
	if abs(ball.position.x + direction * STRIDE) > BOUNDS:
		direction = -direction
	var destination = ball.position.x + direction * STRIDE
	if await Tweens.play(ball, options(Tweens.scale_2d(Vector2(0.72, 1.32), 0.07 * tempo), Tweens.Ease.QUAD_OUT)).wait() != Tweens.Reason.COMPLETED:
		return false
	travel(destination, air * 2.0)
	if not await flight(APEX, Vector2.ONE, Vector2(0.4, 0.4), air, Tweens.Ease.QUAD_OUT):
		return false
	if not await flight(GROUND, Vector2(0.8, 1.25), Vector2.ONE, air, Tweens.Ease.QUAD_IN):
		return false
	ripple(destination)
	kick_up_dust(destination)
	return await Tweens.play(ball, options(Tweens.scale_2d(Vector2(1.55, 0.55), 0.06 * tempo), Tweens.Ease.QUAD_OUT)).wait() == Tweens.Reason.COMPLETED

func travel(destination: float, duration: float) -> void:
	await Tweens.group([
		Tweens.play(targets.spin, Tweens.rotation_2d(targets.spin.rotation + direction * PI, duration)),
		Tweens.play(targets.ball, Tweens.position_2d_x(destination, duration)),
		Tweens.play(targets.shadow, Tweens.position_2d_x(destination, duration)),
	]).wait()

func flight(height: float, scale: Vector2, shadow_scale: Vector2, duration: float, easing: int) -> bool:
	return await Tweens.group([
		Tweens.play(targets.ball, options(Tweens.position_2d_y(height, duration), easing)),
		Tweens.play(targets.ball, options(Tweens.scale_2d(scale, duration), easing)),
		Tweens.play(targets.shadow, options(Tweens.scale_2d(shadow_scale, duration), easing)),
	]).wait() == Tweens.Reason.COMPLETED

func ripple(x: float) -> void:
	targets.ring.position = Vector2(x, GROUND)
	await Tweens.group([
		Tweens.play(targets.ring, options(Tweens.scale_2d(Vector2(2.2, 2.2), 0.5 * tempo), Tweens.Ease.QUART_OUT, Vector2(0.4, 0.4))),
		Tweens.play(targets.ring, options(Tweens.modulate_alpha(0.0, 0.5 * tempo), Tweens.Ease.QUAD_IN, 1.0)),
	]).wait()

func kick_up_dust(x: float) -> void:
	var handles: Array = []
	for i in targets.dust.size():
		var side = -1 if i % 2 == 0 else 1
		var row = floori(i / 2.0)
		var landing = Vector2(x + side * (26 + row * 16), GROUND - 10 - row * 5)
		var dust = targets.dust[i]
		dust.position = Vector2(x + side * 14, GROUND - 3)
		dust.scale = Vector2.ONE * (1.4 - row * 0.3)
		handles.append(Tweens.play(dust, options(Tweens.position_2d(landing, 0.45 * tempo), Tweens.Ease.QUART_OUT)))
		handles.append(Tweens.play(dust, options(Tweens.modulate_alpha(0.0, 0.45 * tempo), Tweens.Ease.QUAD_IN, 0.9)))
	await Tweens.group(handles).wait()
