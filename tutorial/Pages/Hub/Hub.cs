using System.Linq;
using Godot;

namespace tutorial;

/// <summary>The tutorial overview: choose a language, then start its first step, as on the website.</summary>
public partial class Hub : Page
{
    private TutorialPath path = null!;
    private Ferret ferret = null!;
    private CardButton csharpDoor = null!;
    private CardButton gdscriptDoor = null!;
    private bool shown;

    public override void _Ready()
    {
        path = GetNode<TutorialPath>("%Path");
        ferret = GetNode<Ferret>("%Ferret");
        csharpDoor = GetNode<CardButton>("%CSharpDoor");
        gdscriptDoor = GetNode<CardButton>("%GDScriptDoor");
        csharpDoor.Pressed += () => Choose(Language.CSharp);
        gdscriptDoor.Pressed += () => Choose(Language.GDScript);

        foreach (var row in path.CSharpTrack.GetChildren().Concat(path.GDScriptTrack.GetChildren()).OfType<StepRow>())
            row.Pressed += () => Navigate?.Invoke(row.Number);

        base._Ready();
        ferret.Enter();
    }

    private void Choose(Language language)
    {
        // Choosing the language already chosen still gets a reaction.
        if (language == Languages.Current)
            ferret.Bounce();
        Languages.Set(language);
    }

    protected override void LanguageChanged(Language language)
    {
        foreach (var (door, choice) in new[] { (csharpDoor, Language.CSharp), (gdscriptDoor, Language.GDScript) })
        {
            var selected = choice == language;
            door.Selected = selected;
            door.GetNode<Dot>("Row/Check").Filled = selected;
            door.GetNode<Label>("Row/Copy/Name").ThemeTypeVariation = selected ? "DoorTitleSelected" : "DoorTitle";
        }
        path.Show(language, animate: shown);
        shown = true;
    }
}
