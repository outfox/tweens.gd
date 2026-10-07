extends Node2D
## Each function starts one way of writing a motion. The page runs two of them side by side.

func structured(sprite: Sprite2D) -> void:
	var arrive := Tweens.position_2d([400, 180], 0.6, Out.CUBIC)

	Tweens.play(sprite, arrive.with_delay(0.1))

func fully_structured(sprite: Sprite2D) -> void:
	var arrive := Tweens.position_2d()
	arrive.to_value = Vector2(400, 180)
	arrive.duration = 0.6
	arrive.ease = Out.CUBIC
	arrive.delay = 0.1

	Tweens.play(sprite, arrive)

func convenient(sprite: Sprite2D) -> void:
	Tweens.play(sprite, Tweens.position_2d([400, 180], 0.6, Out.CUBIC, 0.1))

func safe_vector(sprite: Sprite2D) -> void:
	Tweens.play(sprite, Tweens.position_2d(Vector2(400, 180), 0.6))

func sweet_array(sprite: Sprite2D) -> void:
	Tweens.play(sprite, Tweens.position_2d([400, 180], 0.6))

func clean(sprite: Sprite2D) -> void:
	var arrive := Tweens.position_2d()
	arrive.to_value = Vector2(400, 180)
	arrive.duration = 0.6
	arrive.ease = Out.CUBIC
	arrive.delay = 0.2
	Tweens.play(sprite, arrive)

func quick(sprite: Sprite2D) -> void:
	var arrive := Tweens.position_2d([400, 180], 0.6, Out.CUBIC, 0.2)
	Tweens.play(sprite, arrive)

func quick_call(sprite: Sprite2D) -> void:
	Tweens.play(sprite, Tweens.position_2d([400, 180], 0.6, Out.CUBIC, 0.2))

func structured_options(sprite: Sprite2D) -> void:
	var pulse := Tweens.scale_2d([1.2, 1.2], 0.2)
	pulse.ping_pong = true
	Tweens.play(sprite, pulse.with_repeats(2))

func convenient_options(sprite: Sprite2D) -> void:
	Tweens.play(sprite, Tweens.scale_2d([1.2, 1.2], 0.2).with_ping_pong())
