using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// The tutorial's ferret. It rises in, squashes from its feet when clicked or when it lands, and its nametag swings
/// to a new tilt each time, like the website's.
/// </summary>
public partial class Ferret : Control
{
    private const float Tilt = -6;

    private Control tag = null!;
    private float tilt = Tilt;
    private TweenInstance? squashY;
    private TweenInstance? squashX;
    private TweenInstance? swing;
    private TweenInstance? sway;

    public override void _Ready()
    {
        tag = GetNode<Control>("%Nametag");
        OffsetTransformEnabled = true;
        OffsetTransformPivotRatio = new Vector2(0.5f, 1);
        tag.OffsetTransformEnabled = true;
        tag.OffsetTransformPivotRatio = new Vector2(0.5f, 0);
        tag.Rotation = Mathf.DegToRad(Tilt);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;
        AcceptEvent();
        Bounce();
    }

    /// <summary>Rises a short way into place, as if it had been on its way already, then lands.</summary>
    public async void Enter()
    {
        OffsetTransformPosition = new Vector2(0, 40);
        Modulate = Colors.Transparent;
        this.TweenModulateAlpha(1, 0.25, Out.Quad);
        if (await this.TweenOffsetTransformPositionY(0, 0.6, Out.Quart).End == Reason.Completed)
            Bounce();
    }

    /// <summary>A faint squash and stretch, and a swing of the nametag; 1 swings its bottom left first.</summary>
    public void Bounce(int direction = 1)
    {
        squashY?.Cancel();
        squashX?.Cancel();
        OffsetTransformScale = Vector2.One;
        squashY = this.Tween(new ControlOffsetTransformScaleYTween
            { By = -0.018f, Duration = 0.5, EaseFunction = Tweens.FX.Punch(frequency: 1.5f) });
        squashX = this.Tween(new ControlOffsetTransformScaleXTween
            { By = 0.009f, Duration = 0.5, EaseFunction = Tweens.FX.Punch(frequency: 1.5f) });

        // The tag settles at a new resting tilt, at least 2.5° from the last, swinging past it on the way.
        var last = tilt;
        while (Mathf.Abs(tilt - last) < 2.5f)
            tilt = (float)GD.RandRange(-10.0, -2.0);
        swing?.Cancel();
        sway?.Cancel();
        tag.OffsetTransformRotation = 0;
        swing = tag.TweenRotation(Mathf.DegToRad(tilt), 0.7, InOut.SmoothStep);
        sway = tag.Tween(new ControlOffsetTransformRotationTween
            { By = Mathf.DegToRad(8 * direction), Duration = 0.8, EaseFunction = Tweens.FX.Punch(frequency: 2.5f) });
    }
}
