using Godot;

namespace tutorial;

/// <summary>Forwards input to the lesson, except the mouse wheel, which scrolls the page instead.</summary>
[GlobalClass]
public partial class StageView : SubViewportContainer
{
    public override bool _PropagateInputEvent(InputEvent @event)
        => @event is not InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown
            or MouseButton.WheelLeft or MouseButton.WheelRight };
}
