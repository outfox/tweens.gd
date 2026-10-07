using Godot;

namespace tutorial;

/// <summary>A tilted speech bubble with its tail on one side, pointing at the ferret.</summary>
[Tool]
public partial class SpeechBubble : PanelContainer
{
    private bool tailRight = true;

    [Export]
    public bool TailRight
    {
        get => tailRight;
        set
        {
            tailRight = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // A square turned 45°, half inside the bubble; only its outer edges are outlined.
        var side = tailRight ? 1 : -1;
        var tip = new Vector2(tailRight ? Size.X + 7 : -7, Size.Y / 2);
        var back = tip - new Vector2(side * 9, 0);
        Vector2[] tail = [back + new Vector2(0, -9), tip, back + new Vector2(0, 9)];
        DrawColoredPolygon(tail, GetThemeColor("surface", "Tutorial"));
        DrawPolyline(tail, GetThemeColor("outline", "Tutorial"), 1, true);
    }
}
