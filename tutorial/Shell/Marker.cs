using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>The endpoint of the last click: a ring that pops in, in the color of the call's endpoint.</summary>
public partial class Marker : Node2D
{
    [Export] public Color Color { get; set; } = new("#f2bc74");

    /// <summary>The ring's radius in world pixels.</summary>
    [Export] public float Radius { get; set; } = 18;

    public void Pop(Vector2 at)
    {
        Position = at;
        Visible = true;
        Scale = new Vector2(0.2f, 0.2f);
        this.TweenScale(1, 0.5, Out.Back);
    }

    public override void _Draw()
    {
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 48, Color, Radius / 6, true);
        DrawCircle(Vector2.Zero, Radius / 5, Color, antialiased: true);
    }
}
