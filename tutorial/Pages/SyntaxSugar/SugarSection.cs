using System.Linq;
using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// One comparison: each side shows a method of the lesson script and runs it on its own lane of a shared stage, over
/// and over, so the two ways of writing a motion can be watched side by side. A side can offer variants to pick.
/// </summary>
public partial class SugarSection : VBoxContainer
{
    private const double Loop = 2.4;

    [Export] public PackedScene CSharpScene { get; set; } = null!;
    [Export] public PackedScene GDScriptScene { get; set; } = null!;

    /// <summary>C# method names per side; a ":csharp" suffix marks a variant GDScript doesn't have.</summary>
    [Export] public string[] LeftMethods { get; set; } = [];
    [Export] public string[] LeftLabels { get; set; } = [];
    [Export] public string[] RightMethods { get; set; } = [];
    [Export] public string[] RightLabels { get; set; } = [];

    /// <summary>Marks each lane's endpoint, for sections whose motions move.</summary>
    [Export] public bool ShowTargets { get; set; } = true;

    private readonly string[] chosen = ["", ""];
    private Stage stage = null!;
    private TweenInstance? loop;

    public override void _Ready()
    {
        stage = GetNode<Stage>("Stage");
        var outline = GetThemeColor("outline", "Tutorial");
        stage.Underlay.AddChild(new Line2D { Points = [new(576, 0), new(576, 324)], Width = 3, DefaultColor = outline });
        if (ShowTargets)
            foreach (var x in new[] { 400, 976 })
                stage.Underlay.AddChild(new Marker { Position = new Vector2(x, 180) });
        Refresh();
    }

    public override void _EnterTree() => Languages.Changed += OnLanguageChanged;
    public override void _ExitTree() => Languages.Changed -= OnLanguageChanged;

    private void OnLanguageChanged(Language language) => Refresh();

    private (string Method, string Label)[] Variants(int side)
    {
        var methods = side == 0 ? LeftMethods : RightMethods;
        var labels = side == 0 ? LeftLabels : RightLabels;
        return methods.Zip(labels)
            .Where(variant => !Languages.IsGDScript || !variant.First.EndsWith(":csharp"))
            .Select(variant => (variant.First.Split(':')[0], variant.Second))
            .ToArray();
    }

    private void Refresh()
    {
        for (var side = 0; side < 2; side++)
        {
            var variants = Variants(side);
            if (!variants.Any(variant => variant.Method == chosen[side]))
                chosen[side] = variants[0].Method;
            var tabs = GetNode<Container>($"Compare/{(side == 0 ? "Left" : "Right")}/Variants");
            tabs.Visible = variants.Length > 1;
            foreach (var tab in tabs.GetNode("Choices").GetChildren())
                tab.Free();
            var group = new ButtonGroup();
            foreach (var (method, label) in variants)
            {
                var tab = new Button
                {
                    Text = label,
                    ToggleMode = true,
                    ButtonGroup = group,
                    ButtonPressed = method == chosen[side],
                    FocusMode = FocusModeEnum.None,
                    ThemeTypeVariation = "LanguageButton",
                };
                var index = side;
                tab.Pressed += () => Choose(index, method);
                tabs.GetNode("Choices").AddChild(tab);
            }
            ShowCode(side);
        }
        Run();
    }

    private void Choose(int side, string method)
    {
        chosen[side] = method;
        ShowCode(side);
        Run();
    }

    private void ShowCode(int side)
    {
        var code = GetNode<CodeView>($"Compare/{(side == 0 ? "Left" : "Right")}/Code");
        code.Title = Variants(side).First(variant => variant.Method == chosen[side]).Label;
        code.ShowMethod(chosen[side]);
    }

    /// <summary>Runs both sides on a fresh copy of the lesson scene, again and again.</summary>
    private async void Run()
    {
        loop?.Cancel();
        while (true)
        {
            var lesson = stage.Load(Languages.Pick(CSharpScene, GDScriptScene));
            lesson.Call(Languages.Member(chosen[0]), lesson.GetNode("Left/Sprite"));
            lesson.Call(Languages.Member(chosen[1]), lesson.GetNode("Right/Sprite"));
            var current = loop = this.TweenFloat(0, Loop);
            if (await current.End != Reason.Completed)
                return;
        }
    }
}
