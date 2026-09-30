extends Node

func _ready() -> void:
    var target := Node2D.new()
    add_child(target)
    # The easing classes are global in a fresh project, like Tweens.
    var move = Tweens.position_2d([100, 50], 0.05, In.SINE | Out.CUBIC).with_blend_type(BlendType.HERMITE)
    var handle = Tweens.play(target, move)
    await handle.end
    set_meta("smoke_passed", target.position == Vector2(100, 50) and handle.completion_reason == Tweens.Reason.COMPLETED)
    set_meta("smoke_finished", true)
    target.queue_free()
