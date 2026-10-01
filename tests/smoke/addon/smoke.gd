extends Node

func _ready() -> void:
    var target := Node2D.new()
    add_child(target)
    # The easing classes are global in a fresh project, like Tweens.
    var move = Tweens.position_2d([100, 50], 0.05, In.SINE | Out.CUBIC).with_blend_type(BlendType.HERMITE)
    var handle = Tweens.chain(target, [move, Tweens.rotation_2d(1.0, 0.05).with_delay(-0.02)])
    await handle.end
    set_meta("smoke_passed", target.position == Vector2(100, 50) and is_equal_approx(target.rotation, 1.0) and handle.entry_count == 2 and handle.completion_reason == Tweens.Reason.COMPLETED)
    set_meta("smoke_finished", true)
    target.queue_free()
