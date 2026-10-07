# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const TUMBLES = [Vector3(PI / 2.0, 0, 0), Vector3(0, PI / 2.0, PI / 2.0), Vector3(0, 0, -PI / 2.0)]
const SHADES = [BLUE, AMBER, MINT]
var landings := 0

func animate() -> void:
	while await jump():
		pass

func jump() -> bool:
	var air := 0.36 * tempo
	if not await reshape(Vector3(1.35, 0.62, 1.35), 0.3, Out.SINE):
		return false
	if not await reshape(Vector3(0.72, 1.42, 0.72), 0.09, Out.QUAD):
		return false
	Tweens.play(targets.tumble, options(Tweens.rotation_3d(TUMBLES[landings % TUMBLES.size()], air * 2.0), InOut.CUBIC, Vector3.ZERO))
	if not await flight(1.0, Vector3.ONE, air, Out.QUAD):
		return false
	if not await flight(-0.6, Vector3(0.84, 1.24, 0.84), air, In.QUAD):
		return false
	targets.tumble.rotation = Vector3.ZERO
	land()
	if not await reshape(Vector3(1.5, 0.55, 1.5), 0.06, Out.QUAD):
		return false
	return await reshape(Vector3.ONE, 0.8, Tweens.Ease.ELASTIC_OUT)

func reshape(scale: Vector3, duration: float, easing: int) -> bool:
	return await Tweens.play(targets.feet, options(Tweens.scale_3d(scale, duration * tempo), easing)).wait() == Tweens.Reason.COMPLETED

func flight(height: float, scale: Vector3, duration: float, easing: int) -> bool:
	return await Tweens.group([
		Tweens.play(targets.feet, options(Tweens.position_3d_y(height, duration), easing)),
		Tweens.play(targets.feet, options(Tweens.scale_3d(scale, duration), easing)),
	]).wait() == Tweens.Reason.COMPLETED

func land() -> void:
	var vertical := options(Tweens.camera_3d_v_offset(0.06, 0.4 * tempo), InOut.LINEAR, 0.0)
	vertical.ease_function = shake
	var horizontal := options(Tweens.camera_3d_h_offset(0.035, 0.4 * tempo), InOut.LINEAR, 0.0)
	horizontal.ease_function = func(w): return shake(minf(1.0, w * 1.3))
	var color: Color = SHADES[landings % SHADES.size()]
	landings += 1
	await Tweens.group([
		Tweens.play(targets.jelly, Tweens.material_albedo_color(color, 0.3 * tempo), stage),
		Tweens.play(targets.wave, options(Tweens.scale_3d([2.6, 1, 2.6], 0.7 * tempo), Out.QUART, [0.9, 1, 0.9])),
		Tweens.play(targets.ripple, options(Tweens.material_albedo_alpha(0.0, 0.7 * tempo), In.QUAD, 0.9), stage),
		Tweens.play(targets.camera, vertical),
		Tweens.play(targets.camera, horizontal),
	]).wait()

func options(definition: TweensGdDefinition, easing := InOut.LINEAR, from: Variant = null,
		delay := 0.0) -> TweensGdDefinition:
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition

func shake(progress: float) -> float:
	return sin(progress * 42.0) * (1.0 - progress) * (1.0 - progress)
