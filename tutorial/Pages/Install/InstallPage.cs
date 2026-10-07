using System.Reflection;
using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// "Install": where to get tweens.gd, and a live check of this project's install. The check runs the smallest lesson
/// in the chosen language: if its icon pops in, tweens work.
/// </summary>
public partial class InstallPage : Page
{
    private const string Addon = "res://addons/tweens_gd/tweens.gd";

    [Export] public PackedScene CSharpScene { get; set; } = null!;
    [Export] public PackedScene GDScriptScene { get; set; } = null!;

    private Stage stage = null!;
    private VBoxContainer checks = null!;
    private TweenInstance? wait;

    public override void _Ready()
    {
        stage = GetNode<Stage>("%Stage");
        checks = GetNode<VBoxContainer>("%Checks");
        GetNode<Button>("%Again").Pressed += Check;
        GetNode<CodeView>("%Command").ShowCode("dotnet add package tweens.gd", "dotnet add package tweens.gd");
        foreach (var banner in new[] { "%StoreBanner", "%NuGetBanner", "%GitHubBanner" })
        {
            var card = GetNode<CardButton>(banner);
            card.Pressed += () => OS.ShellOpen(card.GetMeta("url").AsString());
        }
        base._Ready();
    }

    protected override void LanguageChanged(Language language) => Check();

    /// <summary>Lists what this project has installed, then runs the lesson to see its icon pop in.</summary>
    private async void Check()
    {
        wait?.Cancel();
        foreach (var row in checks.GetChildren())
            row.Free();

        if (Languages.IsGDScript)
        {
            Row("Addon", Addon, ResourceLoader.Exists(Addon));
            Row("Native extension", "TweensGdRunner", ClassDB.ClassExists("TweensGdRunner"));
        }
        else
        {
            var library = typeof(TweenInstance).Assembly;
            var version = library.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Row("C# library", $"{library.GetName().Name} {version?.Split('+')[0]}", true);
        }

        var icon = stage.Load(Languages.Pick(CSharpScene, GDScriptScene)).GetNode<Node2D>("Icon");
        var current = wait = this.TweenFloat(0, 0.8);
        if (await current.End != Reason.Completed)
            return;
        Row("Lesson scene", "The icon popped in", icon.Scale.IsEqualApprox(Vector2.One));
    }

    private void Row(string title, string detail, bool ok)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        if (ok)
            row.AddChild(new Dot { Filled = true, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        else
            row.AddChild(new Label { Text = "✕", ThemeTypeVariation = "AccentLabel", Modulate = GetThemeColor("rose", "Tutorial") });
        var copy = new VBoxContainer();
        copy.AddThemeConstantOverride("separation", 0);
        copy.AddChild(new Label { Text = title, ThemeTypeVariation = "FieldLabel" });
        copy.AddChild(new Label { Text = detail, ThemeTypeVariation = "ValueLabel" });
        row.AddChild(copy);
        checks.AddChild(row);

        // Each result pops in, as the lesson's icon does.
        row.OffsetTransformEnabled = true;
        row.OffsetTransformPivotRatio = new Vector2(0, 0.5f);
        row.OffsetTransformScale = new Vector2(0.6f, 0.6f);
        row.Modulate = Colors.Transparent;
        row.TweenOffsetTransformScale(Vector2.One, 0.5, Out.Back);
        row.TweenModulateAlpha(1, 0.25, Out.Quad);
    }
}
