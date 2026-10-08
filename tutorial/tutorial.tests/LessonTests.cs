using Godot;
using tweens.gd;

namespace tutorial.Tests;

/// <summary>Each lesson behaves the same in C# and GDScript, as the tutorial describes it.</summary>
[Collection<TutorialCollection>]
public sealed class LessonTests(TutorialFixture godot) : IDisposable
{
    public void Dispose() => Languages.Set(Language.CSharp);

    private Hosted<Stage> HostStage(Language language, int width, Vector2I world)
    {
        Languages.Set(language);
        var stage = GD.Load<PackedScene>("res://Shell/Stage.tscn").Instantiate<Stage>();
        stage.World = world;
        var hosted = new Hosted<Stage>(godot, stage);
        stage.Size = new Vector2(width, width * world.Y / world.X);
        godot.Frames(1);
        return hosted;
    }

    private static PackedScene Scene(Language language, string csharp, string gdscript)
        => GD.Load<PackedScene>(language == Language.GDScript ? gdscript : csharp);

    private static void Near(Vector2 expected, Vector2 actual, float tolerance = 0.5f)
        => Assert.True(expected.DistanceTo(actual) <= tolerance, $"Expected {expected}, got {actual}.");

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void HelloTweensPopsTheIconIn(Language language)
    {
        using var stage = HostStage(language, 320, new Vector2I(640, 360));
        var lesson = stage.Node.Load(Scene(language, "res://Lessons/Install/HelloTweens.tscn",
            "res://Lessons/Install/hello_tweens.tscn"));
        var icon = lesson.GetNode<Sprite2D>("Icon");
        godot.Frames(1);
        Assert.True(icon.Scale.X < 0.2f);
        godot.Seconds(0.3);
        Assert.True(icon.Scale.X > 1, "Out.Back overshoots before it settles.");
        godot.Seconds(0.4);
        Near(Vector2.One, icon.Scale, 1e-4f);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void ClickToMoveSendsTheIconToEachClick(Language language)
    {
        using var stage = HostStage(language, 768, new Vector2I(1152, 648));
        var lesson = stage.Node.Load(Scene(language, "res://Lessons/Quickstart/ClickToMove.tscn",
            "res://Lessons/Quickstart/click_to_move.tscn"));
        var icon = lesson.GetNode<Sprite2D>("Icon");
        Near(new Vector2(288, 324), icon.Position);

        stage.Node.Click(new Vector2(864, 324));
        godot.Seconds(0.6);
        Assert.True(icon.Position.X > 800, $"Out.Back covers most of the way early, got {icon.Position}.");
        godot.Seconds(0.7);
        Near(new Vector2(864, 324), icon.Position);

        // The page's controls set the script's properties, named as each language names them.
        icon.Set(Languages.Member("Seconds"), 0.5);
        icon.Set(Languages.Member("Spin"), true);
        icon.Set(Languages.Pick("Ease", "easing"), (long)Out.Bounce);
        stage.Node.Click(new Vector2(200, 100));
        godot.Seconds(0.25);
        Assert.InRange(icon.Rotation, 1, Mathf.Tau - 1);
        godot.Seconds(0.3);
        Near(new Vector2(200, 100), icon.Position);
        Assert.Equal(Mathf.Tau, icon.Rotation, 1e-3);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void HopWaveStartsOneCopyPerIconWithItsOwnDelay(Language language)
    {
        using var stage = HostStage(language, 640, new Vector2I(960, 360));
        var lesson = stage.Node.Load(Scene(language, "res://Lessons/Definitions/HopWave.tscn",
            "res://Lessons/Definitions/hop_wave.tscn"), lesson =>
        {
            lesson.Set(Languages.Member("Height"), 120.0);
            lesson.Set(Languages.Member("Stagger"), 0.25);
        });
        var icons = lesson.GetChildren().OfType<Sprite2D>().ToArray();
        Assert.Equal(5, icons.Length);

        // At its peak, each icon has risen the full height; the next one is only starting.
        godot.Seconds(0.25);
        Assert.Equal(128, icons[0].Position.Y, 1);
        Assert.Equal(248, icons[1].Position.Y, 1);
        godot.Seconds(0.25);
        Assert.Equal(248, icons[0].Position.Y, 1);
        Assert.Equal(128, icons[1].Position.Y, 1);
        godot.Seconds(0.75);
        Assert.Equal(128, icons[4].Position.Y, 1);
        godot.Seconds(0.3);
        Assert.All(icons, icon => Assert.Equal(248, icon.Position.Y, 1e-3));
    }

    public static TheoryData<Language, string, bool> SyntaxSugarMethods => new()
    {
        { Language.CSharp, "Structured", true }, { Language.CSharp, "FullyStructured", true },
        { Language.CSharp, "Convenient", true }, { Language.CSharp, "SafeVector", true },
        { Language.CSharp, "SafeTuple", true }, { Language.CSharp, "SweetArray", true },
        { Language.CSharp, "Clean", true }, { Language.CSharp, "Quick", true }, { Language.CSharp, "QuickCall", true },
        { Language.CSharp, "StructuredOptions", false }, { Language.CSharp, "ConvenientOptions", false },
        { Language.GDScript, "Structured", true }, { Language.GDScript, "FullyStructured", true },
        { Language.GDScript, "Convenient", true }, { Language.GDScript, "SafeVector", true },
        { Language.GDScript, "SweetArray", true }, { Language.GDScript, "Clean", true },
        { Language.GDScript, "Quick", true }, { Language.GDScript, "QuickCall", true },
        { Language.GDScript, "StructuredOptions", false }, { Language.GDScript, "ConvenientOptions", false },
    };

    [Theory]
    [MemberData(nameof(SyntaxSugarMethods))]
    public void EverySyntaxReachesTheSameEndpoint(Language language, string method, bool moves)
    {
        using var stage = HostStage(language, 768, new Vector2I(1152, 324));
        var lesson = stage.Node.Load(Scene(language, "res://Lessons/SyntaxSugar/SyntaxSugar.tscn",
            "res://Lessons/SyntaxSugar/syntax_sugar.tscn"));
        var sprite = lesson.GetNode<Sprite2D>("Right/Sprite");
        lesson.Call(Languages.Member(method), sprite);
        godot.Seconds(0.2);
        if (!moves)
        {
            Assert.True(sprite.Scale.X > 1.15f, $"{method} pulses the sprite, got {sprite.Scale}.");
            for (var repeat = 1; repeat <= 2; repeat++)
            {
                godot.Seconds(0.4);
                Assert.True(sprite.Scale.X > 1.15f, $"{method} repeat {repeat} pulses the sprite, got {sprite.Scale}.");
            }
            godot.Seconds(0.2);
        }
        else
            godot.Seconds(1.0);
        Near(moves ? new Vector2(400, 180) : new Vector2(112, 180), sprite.Position);
        Near(Vector2.One, sprite.Scale, 1e-4f);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.GDScript)]
    public void RemotePausesResumesCancelsAndReportsTheEnd(Language language)
    {
        using var stage = HostStage(language, 768, new Vector2I(1152, 256));
        var scene = Scene(language, "res://Lessons/Playback/PlaybackRemote.tscn", "res://Lessons/Playback/playback_remote.tscn");
        var ends = new List<Reason>();
        var lesson = stage.Node.Load(scene);
        lesson.Connect(Languages.Member("Ended"), Callable.From((int reason) => ends.Add((Reason)reason)));
        var sprite = lesson.GetNode<Sprite2D>("Sprite");

        lesson.Call(Languages.Member("Start"));
        godot.Seconds(1.2);
        Assert.Equal(576, sprite.Position.X, 8);
        lesson.Call(Languages.Member("Pause"));
        var paused = sprite.Position;
        godot.Seconds(0.5);
        Assert.Equal(paused, sprite.Position);
        lesson.Call(Languages.Member("Resume"));
        godot.Seconds(0.3);
        Assert.True(sprite.Position.X > paused.X);
        lesson.Call(Languages.Member("Cancel"));
        var cancelled = sprite.Position;
        godot.Frames(3);
        Assert.Equal([Reason.Cancelled], ends);
        Assert.Equal(cancelled, sprite.Position);

        lesson.Call(Languages.Member("Start"));
        godot.Seconds(2.5);
        Assert.Equal([Reason.Cancelled, Reason.Completed], ends);
        Near(new Vector2(1024, 128), sprite.Position);

        // Closing the scene mid-flight ends playback, and the await resumes while the scene is still valid.
        lesson.Call(Languages.Member("Start"));
        godot.Frames(5);
        stage.Node.Unload();
        godot.Frames(5);
        Assert.Equal([Reason.Cancelled, Reason.Completed, Reason.OwnerExited], ends);
    }
}
