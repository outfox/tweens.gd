extends Sprite2D

func _ready() -> void:
	scale = Vector2.ZERO
	Tweens.play(self, Tweens.scale_2d([1, 1], 0.6, Out.BACK))
