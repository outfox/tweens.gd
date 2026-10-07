using Godot;

namespace tutorial;

/// <summary>Draws the stage's grid in world pixels, behind the lesson.</summary>
[Tool, GlobalClass]
public partial class StageGrid : Node2D
{
    public override void _Draw()
    {
        if (Stage.Of(this) is not { GridStep: > 0 } stage)
            return;
        var color = stage.GetThemeColor("grid", "Tutorial");
        var width = stage.Zoom;
        for (var x = 0; x <= stage.World.X; x += stage.GridStep)
            DrawLine(new Vector2(x, 0), new Vector2(x, stage.World.Y), color, width);
        for (var y = 0; y <= stage.World.Y; y += stage.GridStep)
            DrawLine(new Vector2(0, y), new Vector2(stage.World.X, y), color, width);
    }
}
