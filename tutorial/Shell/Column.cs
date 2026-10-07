using Godot;

namespace tutorial;

/// <summary>Keeps its children at most <see cref="MaxWidth"/> wide, centered or left-aligned.</summary>
[Tool, GlobalClass]
public partial class Column : Container
{
    private float maxWidth = 1080;
    private bool centered = true;

    [Export]
    public float MaxWidth
    {
        get => maxWidth;
        set
        {
            maxWidth = value;
            QueueSort();
        }
    }

    [Export]
    public bool Centered
    {
        get => centered;
        set
        {
            centered = value;
            QueueSort();
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        var size = Vector2.Zero;
        foreach (var child in GetChildren())
            if (child is Control { Visible: true } control)
                size = size.Max(control.GetCombinedMinimumSize());
        return size;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
            return;
        var width = Mathf.Min(MaxWidth, Size.X);
        var x = Centered ? Mathf.Round((Size.X - width) / 2) : 0;
        foreach (var child in GetChildren())
            if (child is Control { Visible: true } control)
                FitChildInRect(control, new Rect2(x, 0, width, Size.Y));
    }
}
