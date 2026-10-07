using Godot;

namespace tutorial;

/// <summary>A round marker: a step number, or a check mark when <see cref="Number"/> is empty.</summary>
[Tool, GlobalClass]
public partial class Dot : Control
{
    private string number = "";
    private bool filled;

    [Export]
    public string Number
    {
        get => number;
        set
        {
            number = value;
            QueueRedraw();
        }
    }

    [Export]
    public bool Filled
    {
        get => filled;
        set
        {
            filled = value;
            QueueRedraw();
        }
    }

    /// <summary>Draws the outline in the muted text color, as the language doors do.</summary>
    [Export] public bool MutedOutline { get; set; }

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged)
            QueueRedraw();
    }

    public override void _Draw()
    {
        var center = Size / 2;
        var radius = Mathf.Min(Size.X, Size.Y) / 2 - 0.5f;
        var ink = GetThemeColor("accent_ink", "Tutorial");
        if (filled)
            DrawCircle(center, radius, ink, antialiased: true);
        else
        {
            DrawCircle(center, radius, GetThemeColor("bg", "Tutorial"), antialiased: true);
            DrawCircle(center, radius, GetThemeColor(MutedOutline ? "muted" : "outline", "Tutorial"), false, 1, true);
        }

        var color = GetThemeColor(filled ? "on_accent" : "muted", "Tutorial");
        if (number.Length > 0)
        {
            var font = GetThemeFont("font", "StepNumber");
            var size = font.GetStringSize(number, HorizontalAlignment.Left, -1, 15);
            DrawString(font, center + new Vector2(-size.X / 2, font.GetAscent(15) / 2 - 2), number,
                HorizontalAlignment.Left, -1, 15, color);
        }
        else if (filled)
            DrawPolyline([center + new Vector2(-radius * 0.42f, 0), center + new Vector2(-radius * 0.1f, radius * 0.32f),
                center + new Vector2(radius * 0.45f, -radius * 0.3f)], color, 2, true);
    }
}
