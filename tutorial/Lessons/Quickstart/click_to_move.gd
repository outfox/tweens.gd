extends Sprite2D

@export var seconds := 1.2
@export var spin := false
var easing := Out.BACK

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		Tweens.play(self, Tweens.position_2d(event.position, seconds, easing))
		if spin:
			Tweens.play(self, Tweens.rotation_2d(rotation + TAU, seconds, InOut.SMOOTHER_STEP))
