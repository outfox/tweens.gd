using System.Linq;
using Godot;

namespace tutorial;

/// <summary>
/// Code with numbered notes, like the website's Moves: side by side when the code leaves the notes enough room,
/// otherwise the notes go below. The first visible child is the code; the next is the notes.
/// </summary>
[Tool, GlobalClass]
public partial class Moves : Container
{
    [Export] public float Gap { get; set; } = 32;
    [Export] public float NotesWidth { get; set; } = 300;

    private (Control Code, Control Notes)? Parts()
    {
        var visible = GetChildren().OfType<Control>().Where(child => child.Visible).ToArray();
        return visible.Length >= 2 ? (visible[0], visible[1]) : null;
    }

    private bool SideBySide(Control code) => code.GetCombinedMinimumSize().X + Gap + NotesWidth <= Size.X;

    public override Vector2 _GetMinimumSize()
    {
        if (Parts() is not var (code, notes))
            return Vector2.Zero;
        var codeSize = code.GetCombinedMinimumSize();
        var notesSize = notes.GetCombinedMinimumSize();
        return SideBySide(code)
            ? new Vector2(0, Mathf.Max(codeSize.Y, notesSize.Y))
            : new Vector2(0, codeSize.Y + Gap + notesSize.Y);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren || Parts() is not var (code, notes))
            return;
        var codeSize = code.GetCombinedMinimumSize();
        if (SideBySide(code))
        {
            FitChildInRect(code, new Rect2(0, 0, codeSize.X, codeSize.Y));
            var x = codeSize.X + Gap;
            FitChildInRect(notes, new Rect2(x, 0, Size.X - x, notes.GetCombinedMinimumSize().Y));
        }
        else
        {
            FitChildInRect(code, new Rect2(0, 0, Size.X, codeSize.Y));
            FitChildInRect(notes, new Rect2(0, codeSize.Y + Gap, Size.X, notes.GetCombinedMinimumSize().Y));
        }
        UpdateMinimumSize();
    }
}
