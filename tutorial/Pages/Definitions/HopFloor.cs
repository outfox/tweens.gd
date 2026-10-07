using Godot;

namespace tutorial;

/// <summary>The floor under the hopping icons, with a shadow that fades as each rises, and each one's delay.</summary>
public partial class HopFloor : Node2D
{
    private const float Floor = 312;
    private const float Rest = 248;

    public Node2D[] Icons { get; set; } = [];
    public double Stagger { get; set; } = 0.2;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (Stage.Of(this) is not { } stage)
            return;
        var outline = stage.GetThemeColor("outline", "Tutorial");
        DrawLine(new Vector2(0, Floor), new Vector2(stage.World.X, Floor), outline, 3);
        var font = stage.GetThemeFont("font", "ValueLabel");
        for (var i = 0; i < Icons.Length; i++)
        {
            if (!IsInstanceValid(Icons[i]))
                continue;
            var x = Icons[i].Position.X;
            var lift = Rest - Icons[i].Position.Y;
            // The shadow stays on the floor and fades as its icon rises.
            DrawSetTransform(new Vector2(x, Floor), 0, new Vector2(52, 8) * (1 - lift / 320));
            DrawCircle(Vector2.Zero, 1, outline with { A = Mathf.Clamp(0.8f * (1 - lift / 200), 0, 1) }, antialiased: true);
            DrawSetTransform(Vector2.Zero);
            var text = $"{i * Stagger:0.00} s";
            var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, 24);
            DrawString(font, new Vector2(x - size.X / 2, Floor + 34), text, HorizontalAlignment.Left, -1, 24,
                new Color("#a9bfff"));
        }
    }
}
