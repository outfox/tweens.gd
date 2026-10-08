using Godot;

namespace tutorial.Tests;

/// <summary>The tutorial app and its pages, driven as a reader would, in both languages.</summary>
[Collection<TutorialCollection>]
public sealed class PageTests(TutorialFixture godot) : IDisposable
{
    private readonly Vector2I window = godot.Tree.Root.Size;

    public void Dispose()
    {
        Languages.Set(Language.CSharp);
        godot.Tree.Root.Size = window;
    }

    private Hosted<TutorialApp> App(Language language)
    {
        // The project's window size, so pages lay out as they do on screen and clicks land where they aim.
        godot.Tree.Root.Size = new Vector2I(1280, 800);
        Languages.Set(language);
        var app = new Hosted<TutorialApp>(godot, GD.Load<PackedScene>("res://main.tscn").Instantiate<TutorialApp>());
        godot.Frames(2);
        return app;
    }

    private Page Open(TutorialApp app, int step)
    {
        app.Open(step);
        godot.Seconds(0.6);
        return app.CurrentPage!;
    }

    private static string LessonLanguage(Node lesson)
        => lesson.FindChildren("*", owned: false).Prepend(lesson).Select(node => node.GetScript().Obj)
            .OfType<Script>().Single() is GDScript ? "gd" : "cs";

    [Fact]
    public void StageAcceptsRestoredWorldBeforeItsViewportReference()
    {
        using var app = App(Language.CSharp);
        var stage = Open(app.Node, 1).GetNode<Stage>("%Stage");
        var viewport = stage.Viewport;
        var property = typeof(Stage).GetProperty(nameof(Stage.Viewport))!;
        // Assembly reload keeps the native node ready while rebuilding its managed references.
        Assert.True(stage.IsNodeReady());
        property.SetValue(stage, null);
        try
        {
            stage.World = new Vector2I(800, 600);
        }
        finally
        {
            property.SetValue(stage, viewport);
        }
        stage.World = new Vector2I(640, 480);
        Assert.Equal(stage.World, viewport.Size2DOverride);
    }

    [Fact]
    public void CodeViewAcceptsRestoredPathsBeforeItsLabelReferences()
    {
        using var app = App(Language.CSharp);
        var view = Open(app.Node, 1).GetNode<CodeView>("%Command");
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var fields = new[] { "code", "file" }.Select(name => typeof(CodeView).GetField(name, flags)!).ToArray();
        var labels = fields.Select(field => field.GetValue(view)).ToArray();
        Assert.True(view.IsNodeReady());
        foreach (var field in fields)
            field.SetValue(view, null);
        try
        {
            view.CSharpPath = "Quickstart/ClickToMove.cs";
            view.GDScriptPath = "Quickstart/click_to_move.gd";
            view.AddSlot("reload", Colors.White);
        }
        finally
        {
            for (var i = 0; i < fields.Length; i++)
                fields[i].SetValue(view, labels[i]);
        }
        view.ShowCode("restored", "restored");
        Assert.Equal("restored", view.Text);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void EveryStepRunsItsLessonInTheChosenLanguageAndSwitches(Language language)
    {
        using var app = App(language);
        for (var step = 1; step <= Steps.All.Length; step++)
        {
            var page = Open(app.Node, step);
            Assert.Equal(step, page.Step);
            foreach (var stage in page.FindChildren("*", owned: false).OfType<Stage>())
            {
                Assert.Equal(language == Language.GDScript ? "gd" : "cs", LessonLanguage(stage.Lesson!));
                Assert.True(stage.Size.X >= 300 && stage.Size.Y >= 100, $"Stage of step {step} is {stage.Size}.");
            }

            // Switching language reloads every lesson as its twin, in place.
            var other = language == Language.GDScript ? Language.CSharp : Language.GDScript;
            Languages.Set(other);
            godot.Seconds(0.5);
            foreach (var stage in page.FindChildren("*", owned: false).OfType<Stage>())
                Assert.Equal(other == Language.GDScript ? "gd" : "cs", LessonLanguage(stage.Lesson!));
            Languages.Set(language);
            godot.Seconds(0.2);
        }
        Open(app.Node, 0);
        Assert.Equal(0, app.Node.CurrentStep);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void ClickingTheStageSendsTheIconToThatPixel(Language language)
    {
        using var app = App(language);
        var page = Open(app.Node, 2);
        var stage = page.GetNode<Stage>("%Stage");
        godot.Seconds(1.5);

        // A real click through the window, so the stage's scaling maps it to the lesson's pixels.
        var target = new Vector2(200, 500);
        var screen = stage.GetGlobalRect().Position + target / stage.Zoom;
        foreach (var pressed in new[] { true, false })
            godot.Tree.Root.PushInput(new InputEventMouseButton
                { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = screen, GlobalPosition = screen });
        godot.Seconds(1.4);
        var icon = stage.Lesson!.GetNode<Node2D>("Icon");
        Assert.True(icon.Position.DistanceTo(target) < 1.5f, $"Expected {target}, got {icon.Position}.");
        Assert.Contains(language == Language.GDScript ? "[200, 500]" : "(200, 500)", page.GetNode<CodeView>("%Call").Text);

        // An ease chip sets the script's property and replays the move from its start.
        page.GetNode<Button>("%Chips/Bounce").ButtonPressed = true;
        page.GetNode<Button>("%Chips/Bounce").EmitSignal(BaseButton.SignalName.Pressed);
        godot.Frames(1);
        icon = stage.Lesson!.GetNode<Node2D>("Icon");
        Assert.Equal(1L << 55, icon.Get(Languages.Pick("Ease", "easing")).AsInt64());
        godot.Seconds(1.4);
        Assert.True(icon.Position.DistanceTo(target) < 1.5f);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void EachWaveSnapshotsTheSettingsItStartsWith(Language language)
    {
        using var app = App(language);
        var page = Open(app.Node, 3);
        var stage = page.GetNode<Stage>("%Stage");
        var first = stage.Lesson;
        page.GetNode<HSlider>("%Height").Value = 160;
        Assert.Same(first, stage.Lesson);
        Assert.Equal(80, first!.Get(Languages.Member("Height")).AsDouble());
        Assert.Contains(Languages.Pick("By = -160", "by_value = -160.00"), page.GetNode<CodeView>("%Widget").Text);

        // The wave lasts 4 × 0.2 + 2 × 0.25 s and rests 0.8 s; the next one starts with the new height.
        godot.Seconds(2.2);
        Assert.NotSame(first, stage.Lesson);
        Assert.Equal(160, stage.Lesson!.Get(Languages.Member("Height")).AsDouble());
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void TheDefinitionCodeKeepsItsWidthWhileSlidersMove(Language language)
    {
        using var app = App(language);
        var page = Open(app.Node, 3);
        var widget = page.GetNode<CodeView>("%Widget");
        var stage = page.GetNode<Stage>("%Stage");
        var sizes = new HashSet<(Vector2, Vector2)>();
        foreach (var (slider, values) in new[] { ("%Stagger", new[] { 0, 0.2, 0.22, 0.5 }), ("%Height", [40, 100, 160]) })
            foreach (var value in values)
            {
                page.GetNode<HSlider>(slider).Value = value;
                godot.Frames(2);
                sizes.Add((widget.Size, stage.Size));
            }
        Assert.Single(sizes);
        Assert.Contains(Languages.Pick("i * 0.50 }", "i * 0.50))"), widget.Text);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void TheRemoteControlsOneHandleAndShowsHowItEnded(Language language)
    {
        using var app = App(language);
        var page = Open(app.Node, 5);
        var state = page.GetNode<Label>("%State/Text");
        void Press(string button) => page.GetNode<Button>($"%{button}").EmitSignal(BaseButton.SignalName.Pressed);

        Assert.Equal("Ready", state.Text);
        Press("Start");
        godot.Seconds(0.5);
        Assert.Equal("Playing", state.Text);
        Press("Pause");
        var sprite = page.GetNode<Stage>("%Stage").Lesson!.GetNode<Node2D>("Sprite");
        var paused = sprite.Position;
        godot.Seconds(0.5);
        Assert.Equal(("Paused", paused), (state.Text, sprite.Position));
        Press("Resume");
        Press("Cancel");
        godot.Frames(2);
        Assert.Equal("Cancelled", state.Text);
        Assert.Contains(Languages.Pick("// Reason.Cancelled", "# Tweens.Reason.CANCELLED"),
            page.GetNode<CodeView>("%Remote").Text);

        Press("Start");
        godot.Seconds(2.6);
        Assert.Equal("Completed", state.Text);
        Assert.Equal("1.00", page.GetNode<Label>("%Progress").Text);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void InstallCheckPassesForThisProject(Language language)
    {
        using var app = App(language);
        var page = Open(app.Node, 1);
        godot.Seconds(1);
        var checks = page.GetNode<Container>("%Checks").GetChildren();
        Assert.Equal(language == Language.GDScript ? 3 : 2, checks.Count);
        Assert.All(checks, row => Assert.IsType<Dot>(row.GetChild(0)));
    }

    [Fact]
    public void TheLanguageSwitchFillsItsTrackWithEqualButtons()
    {
        using var app = App(Language.CSharp);
        var csharp = app.Node.GetNode<Button>("%CSharpMode");
        var gdscript = app.Node.GetNode<Button>("%GDScriptMode");
        var track = csharp.GetParent().GetParent<PanelContainer>();
        Assert.Equal(csharp.Size, gdscript.Size);

        var style = track.GetThemeStylebox("panel");
        var inner = track.GetGlobalRect().GrowIndividual(-style.GetMargin(Side.Left), -style.GetMargin(Side.Top),
            -style.GetMargin(Side.Right), -style.GetMargin(Side.Bottom));
        var buttons = csharp.GetGlobalRect().Merge(gdscript.GetGlobalRect());
        Assert.True(inner.Position.IsEqualApprox(buttons.Position) && inner.Size.IsEqualApprox(buttons.Size),
            $"Buttons span {buttons}, the track's inside is {inner}.");
    }

    [Fact]
    public void TheOverviewDoorsChooseTheLanguageAndTheStepsOpen()
    {
        using var app = App(Language.CSharp);
        var hub = app.Node.CurrentPage!;
        hub.GetNode<CardButton>("%GDScriptDoor").EmitSignal(CardButton.SignalName.Pressed);
        godot.Seconds(1);
        Assert.Equal(Language.GDScript, Languages.Current);
        Assert.True(hub.GetNode<Control>("%Path").GetNode<Control>("GDScriptTrack").Visible);
        Assert.False(hub.GetNode<Control>("%Path").GetNode<Control>("CSharpTrack").Visible);

        hub.GetNode<Control>("%Path").GetNode<StepRow>("GDScriptTrack/Step3").EmitSignal(CardButton.SignalName.Pressed);
        godot.Seconds(0.6);
        Assert.Equal(3, app.Node.CurrentStep);
    }

    [Fact]
    public void CodePanelsShowTheLessonFilesThatRun()
    {
        Assert.Contains("this.TweenPosition(click.Position, Seconds, Ease);", Sources.Read("Quickstart/ClickToMove.cs"));
        Assert.Contains("hop.with_delay(i * stagger)", Sources.Read("Definitions/hop_wave.gd"));
        var structured = Sources.Method(Sources.Read("SyntaxSugar/SyntaxSugar.cs"), "Structured", Language.CSharp);
        Assert.Equal("var arrive = new Tweens.Position2D((400, 180), 0.6, Out.Cubic);\n\nsprite.Tween(arrive with { Delay = 0.1 });",
            structured);
        var convenient = Sources.Method(Sources.Read("SyntaxSugar/syntax_sugar.gd"), "convenient_options", Language.GDScript);
        Assert.Equal("Tweens.play(sprite, Tweens.scale_2d([1.2, 1.2], 0.2).with_ping_pong())", convenient);
    }
}
