# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "res://Gallery/GDScript/GalleryAnimation.gd"
# Scene objects are supplied by the matching C# scene setup file.

const POINTS = 10
var squish
var total = 0.0
var shown = 0.0
var random = RandomNumberGenerator.new()

func animate() -> void:
	random.randomize()
	targets.button.pressed.connect(pop)
	while await wait(1.5 * tempo):
		pop()

func pop() -> void:
	squish_button()
	flash()
	for shard in targets.shards:
		throw_shard(shard)
	float_bonus()
	add_to_score()

func squish_button() -> void:
	if squish != null: squish.cancel()
	squish = Tweens.play(targets.button, options(Tweens.control_scale(Vector2(1.3, 0.7), 0.07 * tempo), Tweens.Ease.QUAD_OUT))
	if await Tweens.group([squish]).wait() != Tweens.Reason.COMPLETED:
		return
	squish = Tweens.play(targets.button, options(Tweens.control_scale(Vector2.ONE, 0.8 * tempo), Tweens.Ease.ELASTIC_OUT))
	await Tweens.group([squish]).wait()

func flash() -> void:
	var movement = options(Tweens.control_position(Vector2(9, 5), 0.4 * tempo), Tweens.Ease.LINEAR, Vector2.ZERO)
	movement.ease_function = Tweens.FX.punch(42.0 / TAU)
	await Tweens.group([
		Tweens.play(targets.shaker, movement),
		Tweens.play(targets.burst, options(Tweens.scale_2d(Vector2(2.1, 2.1), 0.6 * tempo), Tweens.Ease.QUART_OUT, Vector2(0.7, 0.7))),
		Tweens.play(targets.burst, options(Tweens.modulate_alpha(0.0, 0.6 * tempo), Tweens.Ease.QUAD_IN, 1.0)),
	]).wait()

func throw_shard(shard: Polygon2D) -> void:
	var flight = 0.9 * tempo
	var angle = random.randf() * PI * 1.2 + PI * 0.9
	var reach = 90.0 + random.randf() * 110.0
	var landing = Vector2(cos(angle) * reach, 70.0 + random.randf() * 20.0)
	var spin = angle + random.randf() * 12.0 - 6.0
	var lift = 1.0 + random.randf()
	shard.position = Vector2.ZERO
	shard.rotation = angle
	shard.modulate = Color.WHITE
	var arc = Tweens.position_2d_y(landing.y, flight)
	arc.ease_function = func(progress): return (2.0 + lift) * progress * progress - (1.0 + lift) * progress
	await Tweens.group([
		Tweens.play(shard, options(Tweens.position_2d_x(landing.x, flight), Tweens.Ease.QUART_OUT)),
		Tweens.play(shard, arc),
		Tweens.play(shard, options(Tweens.rotation_2d(spin, flight), Tweens.Ease.QUAD_OUT)),
		Tweens.play(shard, options(Tweens.modulate_alpha(0.0, 0.3 * tempo), Tweens.Ease.LINEAR, null, 0.6 * tempo)),
	]).wait()

func float_bonus() -> void:
	targets.bonus.modulate = Color.WHITE
	await Tweens.group([
		Tweens.play(targets.bonus, options(Tweens.control_position_y(-95.0, 0.7 * tempo), Tweens.Ease.QUART_OUT, -50.0)),
		Tweens.play(targets.bonus, options(Tweens.modulate_alpha(0.0, 0.3 * tempo), Tweens.Ease.LINEAR, null, 0.4 * tempo)),
	]).wait()

func add_to_score() -> void:
	total += POINTS
	var roll = options(Tweens.value(shown, total, 0.5 * tempo), Tweens.Ease.CUBIC_OUT, shown)
	roll.on_update = func(_handle, value):
		shown = value
		targets.score.text = "%03d" % roundi(value)
	await Tweens.group([
		Tweens.play(stage, roll),
		Tweens.play(targets.score, options(Tweens.control_scale(Vector2.ONE, 0.6 * tempo), Tweens.Ease.ELASTIC_OUT, Vector2(1.45, 1.45))),
	]).wait()

func options(definition, easing = Tweens.Ease.LINEAR, from = null, delay: float = 0.0):
	definition.ease = easing
	definition.from_value = from
	definition.delay = delay
	return definition

func wait(seconds_to_wait: float) -> bool:
	return await Tweens.play(stage, Tweens.value(0.0, 1.0, seconds_to_wait)).wait() == Tweens.Reason.COMPLETED
