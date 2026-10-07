extends Node2D

@export var height := 80.0
@export var stagger := 0.2

func _ready() -> void:
	var hop := Tweens.position_2d_y()
	hop.by_value = -height
	hop.duration = 0.25
	hop.ease = Out.QUAD
	hop.ping_pong = true

	var icons := get_children()
	for i in icons.size():
		Tweens.play(icons[i], hop.with_delay(i * stagger))
