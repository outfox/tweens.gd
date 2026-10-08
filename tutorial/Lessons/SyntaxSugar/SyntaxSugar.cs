using Godot;
using tweens.gd;

// Each method starts one way of writing a motion. The page runs two of them side by side.
public partial class SyntaxSugar : Node2D
{
    public void Structured(Sprite2D sprite)
    {
        var arrive = new Tweens.Position2D((400, 180), 0.6, Out.Cubic);

        sprite.Tween(arrive with { Delay = 0.1 });
    }

    public void FullyStructured(Sprite2D sprite)
    {
        var arrive = new Tweens.Position2D
        {
            To = new (400, 180),
            Duration = 0.6,
            Ease = Out.Cubic,
            Delay = 0.1,
        };

        sprite.Tween(arrive);
    }

    public void Convenient(Sprite2D sprite)
    {
        sprite.TweenPosition((400, 180), 0.6, Out.Cubic, 0.1);
    }

    public void SafeVector(Sprite2D sprite)
    {
        sprite.TweenPosition(new Vector2(400, 180), 0.6);
    }

    public void SafeTuple(Sprite2D sprite)
    {
        sprite.TweenPosition((400, 180), 0.6);
    }

    public void SweetArray(Sprite2D sprite)
    {
        sprite.TweenPosition([400, 180], 0.6);
    }

    public void Clean(Sprite2D sprite)
    {
        var arrive = new Tweens.Position2D
        {
            To = new Vector2(400, 180),
            Duration = 0.6,
            Ease = Out.Cubic,
            Delay = 0.2,
        };
        sprite.Tween(arrive);
    }

    public void Quick(Sprite2D sprite)
    {
        var arrive = new Tweens.Position2D((400, 180), 0.6, Out.Cubic, 0.2);
        sprite.Tween(arrive);
    }

    public void QuickCall(Sprite2D sprite)
    {
        sprite.TweenPosition((400, 180), 0.6, Out.Cubic, 0.2);
    }

    public void StructuredOptions(Sprite2D sprite)
    {
        var pulse = new Tweens.Scale2D(1.2, 0.2) { PingPong = true };
        sprite.Tween(pulse with { Repeats = 2 });
    }

    public void ConvenientOptions(Sprite2D sprite)
    {
        sprite.TweenScale(1.2, 0.2, options =>
        {
            options.PingPong = true;
            options.Repeats = 2;
        });
    }
}
