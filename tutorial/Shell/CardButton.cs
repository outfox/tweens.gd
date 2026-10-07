using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>A clickable card that lays out its children, unlike a Button. It lifts on hover and can stay selected.
/// </summary>
[Tool, GlobalClass]
public partial class CardButton : PanelContainer
{
    [Signal]
    public delegate void PressedEventHandler();

    private StringName normal = "Card";
    private bool selected;
    private bool hovered;
    private TweenInstance? lift;

    /// <summary>The panel variation at rest. Hover and selection append "Hover" and "Selected".</summary>
    [Export]
    public StringName Normal
    {
        get => normal;
        set
        {
            normal = value;
            Refresh();
        }
    }

    [Export]
    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            Refresh();
        }
    }

    /// <summary>How far the card rises under the pointer, in pixels.</summary>
    [Export] public float Rise { get; set; } = 3;

    public override void _Ready()
    {
        Refresh();
        if (Engine.IsEditorHint())
            return;
        FocusMode = FocusModeEnum.All;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        OffsetTransformEnabled = true;
        MouseEntered += () => Hover(true);
        MouseExited += () => Hover(false);
    }

    public override void _GuiInput(InputEvent @event)
    {
        var click = @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } mouse
            && new Rect2(Vector2.Zero, Size).HasPoint(mouse.Position);
        if (click || @event.IsActionPressed("ui_accept"))
        {
            AcceptEvent();
            EmitSignalPressed();
        }
    }

    private void Hover(bool on)
    {
        hovered = on;
        Refresh();
        HoverChanged(on);
        lift?.Cancel();
        lift = this.TweenOffsetTransformPositionY(on ? -Rise : 0, 0.4, Out.Back);
    }

    /// <summary>Lets a card restyle its contents under the pointer.</summary>
    protected virtual void HoverChanged(bool on)
    {
    }

    private void Refresh()
    {
        var variation = selected ? normal + "Selected" : hovered ? normal + "Hover" : normal;
        ThemeTypeVariation = HasThemeStylebox("panel", variation) ? variation : normal;
    }
}
