using Godot;

namespace tutorial;

/// <summary>The remote's lane: a rail to the endpoint, filled up to the sprite, with a glow once it arrives.</summary>
public partial class RemoteLane : Node2D
{
    public Vector2 From { get; set; } = new(128, 128);
    public Vector2 To { get; set; } = new(1024, 128);
    public Node2D? Sprite { get; set; }
    public bool Arrived { get; set; }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (Stage.Of(this) is not { } stage || Sprite is null || !IsInstanceValid(Sprite))
            return;
        DrawLine(From, To, stage.GetThemeColor("outline", "Tutorial"), 4);
        DrawLine(From, Sprite.Position with { Y = From.Y }, stage.GetThemeColor("accent", "Tutorial"), 4);
        DrawCircle(To, 8, stage.GetThemeColor("outline", "Tutorial"), antialiased: true);
        if (Arrived)
            for (var ring = 4; ring > 0; ring--)
                DrawCircle(Sprite.Position, 64 + ring * 9, stage.GetThemeColor("mint", "Tutorial") with { A = 0.07f },
                    antialiased: true);
    }
}
