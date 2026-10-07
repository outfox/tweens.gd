using System.Linq;
using Godot;

namespace tutorial;

/// <summary>
/// Cards in equal columns, like the website's card grids. It halves <see cref="Columns"/> until each column is at
/// least <see cref="MinColumnWidth"/> wide and fits every card, so four cards sit in one row, two by two, or stacked,
/// never three and one.
/// </summary>
[Tool, GlobalClass]
public partial class CardGrid : Container
{
    [Export] public int Columns { get; set; } = 2;
    [Export] public float MinColumnWidth { get; set; } = 220;
    [Export] public float Gap { get; set; } = 16;

    private Control[] Cards => GetChildren().OfType<Control>().Where(child => child.Visible).ToArray();

    private int Fit(Control[] cards)
    {
        var need = cards.Select(card => card.GetCombinedMinimumSize().X).Append(MinColumnWidth).Max();
        var columns = Mathf.Max(1, Columns);
        while (columns > 1 && (Size.X - Gap * (columns - 1)) / columns < need)
            columns /= 2;
        return columns;
    }

    private float[] RowHeights(Control[] cards, int columns)
        => cards.Chunk(columns).Select(row => row.Max(card => card.GetCombinedMinimumSize().Y)).ToArray();

    public override Vector2 _GetMinimumSize()
    {
        var cards = Cards;
        if (cards.Length == 0)
            return Vector2.Zero;
        var heights = RowHeights(cards, Fit(cards));
        return new Vector2(0, heights.Sum() + Gap * (heights.Length - 1));
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
            return;
        var cards = Cards;
        var columns = Fit(cards);
        var width = (Size.X - Gap * (columns - 1)) / columns;
        var heights = RowHeights(cards, columns);
        var y = 0f;
        for (var i = 0; i < cards.Length; i++)
        {
            var row = i / columns;
            FitChildInRect(cards[i], new Rect2((i % columns) * (width + Gap), y, width, heights[row]));
            if (i % columns == columns - 1 || i == cards.Length - 1)
                y += heights[row] + Gap;
        }
        UpdateMinimumSize();
    }
}
