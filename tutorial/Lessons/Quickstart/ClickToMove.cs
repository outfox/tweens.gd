using Godot;
using tweens.gd;

public partial class ClickToMove : Sprite2D
{
    [Export] public double Seconds { get; set; } = 1.2;
    [Export] public bool Spin { get; set; }
    public EaseType Ease { get; set; } = Out.Back;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } click)
        {
            this.TweenPosition(click.Position, Seconds, Ease);
            if (Spin)
                this.TweenRotation(Rotation + Mathf.Tau, Seconds, InOut.SmootherStep);
        }
    }
}
