using Godot;

namespace tutorial;

/// <summary>Opens a page of tweens.gd in the browser. "{lang}" in the path picks the reader's language.</summary>
[Tool, GlobalClass]
public partial class LinkCard : CardButton
{
    private const string Site = "https://tweens.gd";
    private string title = "";
    private string description = "";

    [Export]
    public string Title
    {
        get => title;
        set
        {
            title = value;
            Fill();
        }
    }

    [Export(PropertyHint.MultilineText)]
    public string Description
    {
        get => description;
        set
        {
            description = value;
            Fill();
        }
    }

    [Export] public string Path { get; set; } = "/";

    public string Url => Site + Path.Replace("{lang}", Languages.Pick("csharp", "gdscript"));

    public override void _Ready()
    {
        Fill();
        base._Ready();
        if (Engine.IsEditorHint())
            return;
        Pressed += () => OS.ShellOpen(Url);
        MouseEntered += () => TooltipText = Url;
    }

    private void Fill()
    {
        if (GetNodeOrNull<Label>("%Title") is not { } label)
            return;
        label.Text = title;
        GetNode<Label>("%Description").Text = description;
    }
}
