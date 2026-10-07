using Godot;
using twodog;
using twodog.Testing;

namespace tutorial.Tests;

/// <summary>Rendering at a fixed 60 frames per second; accepts a software GL driver without V-Sync control.</summary>
public sealed class RenderingFixture : FixtureBase
{
    public RenderingFixture() : base("--fixed-fps", "60")
    {
        var startup = Errors.Drain().Where(error => !error.Text.StartsWith("Could not set V-Sync mode")).ToArray();
        if (startup.Length > 0)
            throw new GodotErrorException("Godot reported errors during startup.", startup);
    }

    public void Frames(int count)
    {
        for (var i = 0; i < count; i++)
            Engine.Iteration();
    }
}

[CollectionDefinition(nameof(RenderingCollection), DisableParallelization = true)]
public sealed class RenderingCollection : ICollectionFixture<RenderingFixture>;

[Collection<RenderingCollection>]
public sealed class RenderingTests(RenderingFixture godot)
{
    // Optional visual QA artifacts; normal runs do not write screenshots.
    private static readonly string? Output = System.Environment.GetEnvironmentVariable("TUTORIAL_SCREENSHOT_DIR");

    private void Capture(string name)
    {
        if (string.IsNullOrEmpty(Output))
            return;
        Directory.CreateDirectory(Output);
        using var image = godot.Tree.Root.GetTexture().GetImage();
        Assert.Equal(Error.Ok, image.SavePng(Path.Combine(Output, $"{name}.png")));
    }

    [Fact]
    public void EveryPageFitsTheWindowInBothLanguages()
    {
        var window = godot.Tree.Root;
        var original = window.Size;
        var app = GD.Load<PackedScene>("res://main.tscn").Instantiate<TutorialApp>();
        window.AddChild(app);
        try
        {
            foreach (var size in new[] { new Vector2I(1280, 800), new Vector2I(960, 600) })
            foreach (var language in new[] { Language.CSharp, Language.GDScript })
            {
                window.Size = size;
                Languages.Set(language);
                for (var step = 0; step <= Steps.All.Length; step++)
                {
                    app.Open(step);
                    godot.Frames(75);
                    var page = app.CurrentPage!;
                    var scroll = page.GetNode<ScrollContainer>("Scroll");
                    Assert.Equal(0, scroll.GetHScrollBar().MaxValue - scroll.GetHScrollBar().Page, 1);
                    var name = $"{size.X}-{language.ToString().ToLowerInvariant()}-{step}";
                    var bottom = scroll.GetVScrollBar().MaxValue - scroll.GetVScrollBar().Page;
                    for (var part = 0; !string.IsNullOrEmpty(Output) && part * 560 < bottom + 560 && part < 6; part++)
                    {
                        scroll.ScrollVertical = part * 560;
                        godot.Frames(part == 0 ? 1 : 20);
                        Capture($"{name}-{part}");
                    }
                }
            }
        }
        finally
        {
            app.Free();
            window.Size = original;
            Languages.Set(Language.CSharp);
        }
    }
}
