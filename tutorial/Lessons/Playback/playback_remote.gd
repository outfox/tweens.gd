extends Node2D
## The remote's buttons call these functions; ended reports how the movement ended.

signal ended(reason: int)

@export var sprite: Sprite2D
@export var to := Vector2(1024, 128)

var movement: TweensGdHandle

func start() -> void:
	movement = Tweens.play(sprite, Tweens.position_2d(to, 2.4, InOut.SMOOTHER_STEP))
	ended.emit(await movement.end)

func pause() -> void:
	movement.pause()

func resume() -> void:
	movement.resume()

func cancel() -> void:
	movement.cancel()
