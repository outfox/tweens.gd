using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>A pause glyph pinned to the sprite's corner while its handle is paused.</summary>
public partial class PauseBadge : Node2D
{
    private TweenInstance? pop;

    public Node2D? Sprite { get; set; }

    public void Display(bool on)
    {
        pop?.Cancel();
        if (on)
            Visible = true;
        pop = this.TweenScale(on ? 1 : 0.3, on ? 0.45 : 0.2, on ? Out.Back : Out.Quad);
        if (!on)
            HideAfterShrinking();
    }

    private async void HideAfterShrinking()
    {
        if (await pop!.End == Reason.Completed)
            Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Sprite is not null && IsInstanceValid(Sprite))
            Position = Sprite.Position + new Vector2(58, -58);
    }

    public override void _Draw()
    {
        var dark = new Color("#231705");
        DrawCircle(Vector2.Zero, 21, Stage.Of(this)?.GetThemeColor("stage", "Tutorial") ?? dark, antialiased: true);
        DrawCircle(Vector2.Zero, 17, new Color("#f2bc74"), antialiased: true);
        DrawRect(new Rect2(-6, -7, 4, 14), dark);
        DrawRect(new Rect2(2, -7, 4, 14), dark);
    }
}
