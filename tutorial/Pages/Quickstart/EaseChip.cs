using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>A toggle chip for one ease, with a sparkline of its curve that wears the chip's state color.</summary>
[Tool]
public partial class EaseChip : PillButton
{
    private const float Width = 26;
    private const float Height = 18;

    /// <summary>The ease as its raw value, the same in C# and GDScript.</summary>
    [Export] public long Ease { get; set; }

    public EaseType EaseType => (EaseType)Ease;

    public override void _Ready()
    {
        // A blank icon reserves the sparkline's room; _Draw paints the curve there.
        var blank = Image.CreateEmpty((int)Width, (int)Height, false, Image.Format.Rgba8);
        Icon = ImageTexture.CreateFromImage(blank);
        ToggleMode = true;
        if (!Engine.IsEditorHint())
            base._Ready();
    }

    public override void _Draw()
    {
        var style = GetThemeStylebox("normal");
        var origin = new Vector2(style.GetMargin(Side.Left), (Size.Y - Height) / 2);
        var color = GetThemeColor(ButtonPressed ? "icon_pressed_color" : IsHovered() ? "icon_hover_color" : "icon_normal_color");
        // Room for the 30% overshoot of Back and Elastic.
        var points = new Vector2[49];
        for (var i = 0; i < points.Length; i++)
        {
            var t = i / 48f;
            points[i] = origin + new Vector2(t * Width, Height - 2 - Easing.Evaluate(EaseType, t) * (Height - 5));
        }
        DrawPolyline(points, color, 1.6f, true);
    }
}
