using System.Linq;
using Godot;

namespace tutorial;

/// <summary>
/// Two sides to compare, like the website's Compare: side by side when both fit their half, otherwise stacked.
/// </summary>
[Tool, GlobalClass]
public partial class Compare : Container
{
    [Export] public float Gap { get; set; } = 24;

    private Control[] Sides => GetChildren().OfType<Control>().Where(child => child.Visible).Take(2).ToArray();

    private bool SideBySide(Control[] sides)
        => sides.Length == 2 && sides.All(side => side.GetCombinedMinimumSize().X <= (Size.X - Gap) / 2);

    public override Vector2 _GetMinimumSize()
    {
        var sides = Sides;
        var heights = sides.Select(side => side.GetCombinedMinimumSize().Y).ToArray();
        if (heights.Length == 0)
            return Vector2.Zero;
        return new Vector2(0, SideBySide(sides) ? heights.Max() : heights.Sum() + Gap * (heights.Length - 1));
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
            return;
        var sides = Sides;
        if (SideBySide(sides))
        {
            var width = (Size.X - Gap) / 2;
            FitChildInRect(sides[0], new Rect2(0, 0, width, Size.Y));
            FitChildInRect(sides[1], new Rect2(width + Gap, 0, width, Size.Y));
        }
        else
        {
            var y = 0f;
            foreach (var side in sides)
            {
                var height = side.GetCombinedMinimumSize().Y;
                FitChildInRect(side, new Rect2(0, y, Size.X, height));
                y += height + Gap;
            }
        }
        UpdateMinimumSize();
    }
}
