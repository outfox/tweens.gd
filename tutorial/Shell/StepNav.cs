using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>Five numbered dots on a line. The current step's dot fills, and its ring slides to the next one.</summary>
[Tool, GlobalClass]
public partial class StepNav : Control
{
    private const float Radius = 14;
    private const float Spacing = 46;

    [Signal]
    public delegate void SelectedEventHandler(int step);

    private int current;
    private int hovered = -1;
    private float ring;
    private float ringAlpha;
    private TweenInstance? slide;
    private TweenInstance? fade;

    public override Vector2 _GetMinimumSize() => new(Spacing * (Steps.All.Length - 1) + Radius * 2 + 4, Radius * 2 + 8);

    public override void _Ready()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseExited += () => SetHovered(-1);
    }

    /// <summary>Marks a step as current, from 1; zero hides the ring.</summary>
    public void Select(int step)
    {
        var appearing = current == 0;
        current = step;
        fade?.Cancel();
        fade = this.TweenFloat(step > 0 ? 1 : 0, 0.25, options =>
        {
            options.From = ringAlpha;
            options.OnUpdate = (_, alpha) =>
            {
                ringAlpha = alpha;
                QueueRedraw();
            };
        });
        if (step == 0)
            return;
        slide?.Cancel();
        if (appearing)
            ring = step;
        slide = this.TweenFloat(step, 0.55, options =>
        {
            options.From = ring;
            options.Ease = Out.Back;
            options.OnUpdate = (_, at) =>
            {
                ring = at;
                QueueRedraw();
            };
        });
    }

    private Vector2 Center(float step) => new(Radius + 2 + (step - 1) * Spacing, Size.Y / 2);

    private int StepAt(Vector2 position)
    {
        for (var step = 1; step <= Steps.All.Length; step++)
            if (position.DistanceTo(Center(step)) <= Radius + 4)
                return step;
        return -1;
    }

    private void SetHovered(int step)
    {
        if (hovered == step)
            return;
        hovered = step;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
            SetHovered(StepAt(motion.Position));
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click
            && StepAt(click.Position) is > 0 and var step)
        {
            AcceptEvent();
            EmitSignalSelected(step);
        }
    }

    public override string _GetTooltip(Vector2 atPosition)
        => StepAt(atPosition) is > 0 and var step ? $"{step} · {Steps.All[step - 1].Label}" : "";

    public override void _Draw()
    {
        var outline = GetThemeColor("outline", "Tutorial");
        var accent = GetThemeColor("accent", "Tutorial");
        var font = GetThemeFont("font", "StepNumber");
        DrawLine(Center(1), Center(Steps.All.Length), outline, 2);
        for (var step = 1; step <= Steps.All.Length; step++)
        {
            var center = Center(step);
            var on = step == current;
            var fill = on ? accent : step == hovered ? GetThemeColor("raised", "Tutorial") : GetThemeColor("bg", "Tutorial");
            DrawCircle(center, Radius, fill, antialiased: true);
            if (!on)
                DrawCircle(center, Radius, outline, false, 1, true);
            var number = step.ToString();
            var color = on ? GetThemeColor("on_accent", "Tutorial") : GetThemeColor(step == hovered ? "text" : "muted", "Tutorial");
            var size = font.GetStringSize(number, HorizontalAlignment.Left, -1, 14);
            DrawString(font, center + new Vector2(-size.X / 2, size.Y / 2 - 4), number, HorizontalAlignment.Left, -1, 14, color);
        }
        if (ringAlpha > 0)
            DrawCircle(Center(ring), Radius + 4, accent with { A = ringAlpha }, false, 2, true);
    }
}
