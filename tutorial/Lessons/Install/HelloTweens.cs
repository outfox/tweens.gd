using Godot;
using tweens.gd;

public partial class HelloTweens : Sprite2D
{
    public override void _Ready()
    {
        Scale = Vector2.Zero;
        this.TweenScale(1, 0.6, Out.Back);
    }
}
