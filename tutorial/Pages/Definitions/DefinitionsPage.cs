using System.Globalization;
using System.Linq;
using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// "Reusable definitions": one hop definition started on five icons, each start a copy with its own delay. Height edits
/// the definition and stagger the copies; each wave runs the lesson scene afresh, so it snapshots both when it starts.
/// </summary>
public partial class DefinitionsPage : Page
{
    // The lesson's hop: a 0.25-second leg, played there and back. Waves rest a little in between.
    private const double Leg = 0.25;
    private const double Rest = 0.8;

    [Export] public PackedScene CSharpScene { get; set; } = null!;
    [Export] public PackedScene GDScriptScene { get; set; } = null!;

    private Stage stage = null!;
    private CodeView widget = null!;
    private CodeView script = null!;
    private HSlider height = null!;
    private HSlider stagger = null!;
    private HopFloor floor = null!;
    private TweenInstance? wave;

    public override void _Ready()
    {
        stage = GetNode<Stage>("%Stage");
        widget = GetNode<CodeView>("%Widget");
        script = GetNode<CodeView>("%Script");
        height = GetNode<HSlider>("%Height");
        stagger = GetNode<HSlider>("%Stagger");
        stage.Underlay.AddChild(floor = new HopFloor { Name = "Floor" });

        var amber = GetThemeColor("amber", "Tutorial");
        var blue = GetThemeColor("blue", "Tutorial");
        widget.AddSlot("height", amber);
        widget.AddSlot("stagger", blue);
        script.AddSlot("height", amber, @"Height \{ get; set; \} = ([\d.]+);", @"var height := ([\d.]+)");
        script.AddSlot("stagger", blue, @"Stagger \{ get; set; \} = ([\d.]+);", @"var stagger := ([\d.]+)");
        script.AddMark(1, @"var hop = new", @"^\s*\};", @"var hop :=", @"hop\.ping_pong");
        script.AddMark(2, @"var icons =", null, @"var icons :=", null);
        script.AddMark(3, @"for \(var i", @"icons\[i\]\.Tween", @"for i in", @"Tweens\.play\(icons");
        script.AddMark(4, @"\[Export\] public float Height", @"Stagger \{", @"@export var height", @"@export var stagger");

        height.ValueChanged += _ => Changed("height");
        stagger.ValueChanged += _ => Changed("stagger");
        base._Ready();
    }

    protected override void LanguageChanged(Language language)
    {
        widget.ShowCode(
            """
            var hop = new Tweens.Position2DY
            {
                By = «height:-80»,
                Duration = 0.25,
                Ease = Out.Quad,
                PingPong = true,
            };

            for (var i = 0; i < icons.Length; i++)
                icons[i].Tween(hop with { Delay = i * «stagger:0.2» });
            """,
            """
            var hop := Tweens.position_2d_y()
            hop.by_value = «height:-80.0»
            hop.duration = 0.25
            hop.ease = Out.QUAD
            hop.ping_pong = true

            for i in icons.size():
            	Tweens.play(icons[i], hop.with_delay(i * «stagger:0.2»))
            """);
        ShowValues(flash: null);
        Waves();
    }

    private void Changed(string slot)
    {
        ShowValues(flash: slot);
        GetNode<Label>("%HeightValue").Text = $"{height.Value:0} px";
        GetNode<Label>("%StaggerValue").Text = Format(stagger.Value, "0.00") + " s";
    }

    private void ShowValues(string? flash)
    {
        // GDScript writes the height as a float, as its by_value expects.
        var pixels = Format(height.Value, Languages.Pick("0", "0.0"));
        var delay = Format(stagger.Value, "0.0#");
        widget.SetSlot("height", "-" + pixels, quiet: flash != "height");
        script.SetSlot("height", pixels, quiet: flash != "height");
        widget.SetSlot("stagger", delay, quiet: flash != "stagger");
        script.SetSlot("stagger", delay, quiet: flash != "stagger");
        floor.Stagger = stagger.Value;
    }

    private static string Format(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Runs a wave, rests, and runs the next with the settings of that moment.</summary>
    private async void Waves()
    {
        wave?.Cancel();
        while (true)
        {
            var lesson = stage.Load(Languages.Pick(CSharpScene, GDScriptScene), lesson =>
            {
                lesson.Set(Languages.Member("Height"), height.Value);
                lesson.Set(Languages.Member("Stagger"), stagger.Value);
            });
            floor.Icons = lesson.GetChildren().OfType<Node2D>().ToArray();
            var length = (floor.Icons.Length - 1) * stagger.Value + 2 * Leg + Rest;
            var current = wave = this.TweenFloat(0, length);
            if (await current.End != Reason.Completed)
                return;
        }
    }
}
