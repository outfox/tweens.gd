using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>The window around the pages: brand, step dots, and the language switch.</summary>
public partial class TutorialApp : Control
{
    [Export] public Theme GDScriptAccents { get; set; } = null!;

    private Theme csharpTheme = null!;
    private Theme gdscriptTheme = null!;
    private Control host = null!;
    private StepNav nav = null!;
    private Button csharp = null!;
    private Button gdscript = null!;

    public Page? CurrentPage { get; private set; }
    public int CurrentStep { get; private set; } = -1;

    public override void _Ready()
    {
        // As in the playground: GDScript merges a small accent theme into a copy of the authored one.
        csharpTheme = Theme;
        gdscriptTheme = (Theme)csharpTheme.Duplicate();
        gdscriptTheme.MergeWith(GDScriptAccents);

        host = GetNode<Control>("%PageHost");
        nav = GetNode<StepNav>("%StepNav");
        csharp = GetNode<Button>("%CSharpMode");
        gdscript = GetNode<Button>("%GDScriptMode");
        csharp.Pressed += () => Languages.Set(Language.CSharp);
        gdscript.Pressed += () => Languages.Set(Language.GDScript);
        GetNode<Button>("%Home").Pressed += () => Open(0);
        nav.Selected += Open;

        OnLanguageChanged(Languages.Current);
        Open(0);
    }

    public override void _EnterTree() => Languages.Changed += OnLanguageChanged;
    public override void _ExitTree() => Languages.Changed -= OnLanguageChanged;

    private void OnLanguageChanged(Language language)
    {
        var gd = language == Language.GDScript;
        Theme = gd ? gdscriptTheme : csharpTheme;
        csharp.SetPressedNoSignal(!gd);
        gdscript.SetPressedNoSignal(gd);
        GetNode<CanvasItem>("%Glow").TweenModulate(Theme.GetColor("accent", "Tutorial") with { A = 0.16f }, 0.6, Out.Cubic);
    }

    /// <summary>Opens a step from 1, or the overview for zero.</summary>
    public void Open(int step)
    {
        if (step == CurrentStep)
            return;
        var forward = step > CurrentStep;
        CurrentPage?.QueueFree();
        CurrentPage = GD.Load<PackedScene>(step == 0 ? Steps.Hub : Steps.All[step - 1].Scene).Instantiate<Page>();
        CurrentPage.Navigate = Open;
        host.AddChild(CurrentPage);
        CurrentStep = step;
        nav.Select(step);

        // The new page slides in from the reading direction.
        CurrentPage.OffsetTransformEnabled = true;
        CurrentPage.OffsetTransformPosition = new Vector2(forward ? 40 : -40, 0);
        CurrentPage.Modulate = Colors.Transparent;
        CurrentPage.TweenOffsetTransformPosition(Vector2.Zero, 0.5, Out.Cubic);
        CurrentPage.TweenModulateAlpha(1, 0.3, Out.Quad);
    }
}
