using Godot;

namespace tutorial;

/// <summary>
/// A tutorial page. Nodes in the "csharp" or "gdscript" group show only for that language, like the website's
/// language blocks. Pages reload their lesson when the language changes.
/// </summary>
[GlobalClass]
public partial class Page : Control
{
    /// <summary>The step this page teaches, from 1; zero for the overview.</summary>
    [Export] public int Step { get; set; }

    /// <summary>Opens a step, or the overview for zero. The app sets this; a page on its own ignores it.</summary>
    public System.Action<int>? Navigate { get; set; }

    public override void _EnterTree() => Languages.Changed += OnLanguageChanged;
    public override void _ExitTree() => Languages.Changed -= OnLanguageChanged;

    public override void _Ready() => OnLanguageChanged(Languages.Current);

    private void OnLanguageChanged(Language language)
    {
        var gd = language == Language.GDScript;
        foreach (var node in FindChildren("*", owned: false))
        {
            if (node is not CanvasItem item)
                continue;
            if (item.IsInGroup("csharp"))
                item.Visible = !gd;
            else if (item.IsInGroup("gdscript"))
                item.Visible = gd;
        }
        LanguageChanged(language);
    }

    /// <summary>Called on ready and after each language change, once the language blocks have switched.</summary>
    protected virtual void LanguageChanged(Language language)
    {
    }
}
