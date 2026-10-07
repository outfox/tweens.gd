using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>A button that lifts on hover and dips while pressed, like the website's buttons.</summary>
[GlobalClass]
public partial class PillButton : Button
{
    private TweenInstance? lift;
    private TweenInstance? press;

    public override void _Ready()
    {
        OffsetTransformEnabled = true;
        OffsetTransformPivotRatio = new Vector2(0.5f, 0.5f);
        MouseEntered += () => Lift(Disabled ? 0 : -2);
        MouseExited += () => Lift(0);
        ButtonDown += () => Press(0.94f, 0.12, Out.Quad);
        ButtonUp += () => Press(1, 0.35, Out.Back);
    }

    private void Lift(float y)
    {
        lift?.Cancel();
        lift = this.TweenOffsetTransformPositionY(y, 0.35, Out.Back);
    }

    private void Press(float scale, double seconds, EaseType ease)
    {
        press?.Cancel();
        press = this.TweenOffsetTransformScale(new Vector2(scale, scale), seconds, ease);
    }
}
