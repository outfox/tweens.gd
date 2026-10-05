// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Globalization;
using Godot;
using tweens.gd;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace playground.Tests;

[Collection<HeadlessCollection>]
public class EasingPlaygroundTests(HeadlessFixture godot)
{
    private EasingPlayground Load()
    {
        var page = GD.Load<PackedScene>("res://main.tscn").Instantiate<EasingPlayground>();
        godot.Tree.Root.AddChild(page);
        return page;
    }

    private static EaseFamily Family(string name) => EasingSettings.Families.Single(f => f.Name == name);
    private static T Find<T>(Node root, string name) where T : Node => (T)root.FindChild(name, true, false);

    [Fact]
    public void AuthoredSceneIsCompleteBeforeReadyAndPreservesManualDecorations()
    {
        var page = GD.Load<PackedScene>("res://main.tscn").Instantiate<EasingPlayground>();
        try
        {
            // These nodes and resources must exist before the controller's _Ready runs.
            var authoredNodes = Descendants(page).ToArray();
            Assert.All(authoredNodes, node => Assert.Equal(page, node.Owner));
            Assert.IsType<EasingGraph>(page.GetNode<Control>("%CurveGraph"));
            Assert.IsType<MotionPreview>(page.GetNode<Control>("%MotionPreview"));
            Assert.Equal("res://Themes/playground.tres", page.Theme.ResourcePath);
            Assert.Equal(new EasingSettings().Recipe(), page.GetNode<TextEdit>("%Recipe").Text);
            Assert.NotNull(page.GetNode<TextEdit>("%Recipe").SyntaxHighlighter);
            Assert.Equal(4, page.GetNode<OptionButton>("%BlendType").ItemCount);
            Assert.True(page.GetNode<Button>("%CSharpMode").ButtonPressed);
            Assert.Same(page.GetNode<Button>("%CSharpMode").ButtonGroup,
                page.GetNode<Button>("%GDScriptMode").ButtonGroup);

            var decorations = page.GetNode<Control>("%GraphDecorations");
            Assert.Equal(1, decorations.AnchorRight);
            Assert.Equal(1, decorations.AnchorBottom);
            var artwork = new ColorRect { Name = "ManualArtwork", Color = Colors.Coral };
            decorations.AddChild(artwork);
            artwork.Owner = page;

            godot.Tree.Root.AddChild(page);
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Cubic") });
            page.Reset();

            Assert.Equal(authoredNodes.Length + 1, Descendants(page).Count());
            Assert.Equal(decorations, artwork.GetParent());
            Assert.Equal(Colors.Coral, artwork.Color);
            Assert.All(authoredNodes, node => Assert.True(GodotObject.IsInstanceValid(node)));
        }
        finally { page.Free(); }
    }

    private static IEnumerable<Node> Descendants(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    [Fact]
    public void LanguageSwitchChangesRecipeAndAccentsWithoutInterruptingPlayback()
    {
        var page = Load();
        var otherPage = Load();
        try
        {
            Assert.Equal(1280, (int)ProjectSettings.GetSetting("display/window/size/viewport_width"));
            Assert.Equal(1024, (int)ProjectSettings.GetSetting("display/window/size/viewport_height"));
            Assert.False((bool)ProjectSettings.GetSetting("display/window/size/resizable"));

            var csharpTheme = page.Theme;
            var blue = Color.FromHtml("#67a2dd");
            var green = Color.FromHtml("#79deb4");
            Assert.True(blue.IsEqualApprox(page.Theme.GetColor("accent", "LanguageMode")));
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Cubic"), Duration = 2.5 });
            page.TogglePlayback();
            var playback = page.Playback;
            var settings = page.Settings;

            page.GetNode<Button>("%GDScriptMode").EmitSignal(Button.SignalName.Pressed);
            Assert.True(page.UsesGDScript);
            Assert.True(page.GetNode<Button>("%GDScriptMode").ButtonPressed);
            Assert.False(page.GetNode<Button>("%CSharpMode").ButtonPressed);
            Assert.Equal(settings.Recipe(true), page.GetNode<TextEdit>("%Recipe").Text);
            Assert.True(green.IsEqualApprox(page.Theme.GetColor("accent", "LanguageMode")));
            Assert.True(green.IsEqualApprox(page.GetNode<Button>("%Play").GetThemeColor("font_color")));
            Assert.Same(playback, page.Playback);
            Assert.True(page.IsPlaying);
            Assert.Equal(settings, page.Settings);
            Assert.True(blue.IsEqualApprox(otherPage.Theme.GetColor("accent", "LanguageMode")));

            page.Reset();
            Assert.True(page.UsesGDScript);
            Assert.Equal(new EasingSettings().Recipe(true), page.Recipe);
            page.GetNode<Button>("%CSharpMode").EmitSignal(Button.SignalName.Pressed);
            Assert.False(page.UsesGDScript);
            Assert.Same(csharpTheme, page.Theme);
            Assert.Equal(new EasingSettings().Recipe(), page.Recipe);
        }
        finally { page.Free(); otherPage.Free(); }
    }

    [Fact]
    public void LogarithmicBlendSliderSnapsToEveryWholePercentageIncludingTen()
    {
        var page = Load();
        try
        {
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Cubic") });
            var slider = page.GetNode<HSlider>("%BlendWidth");
            Assert.True(slider.ExpEdit);
            Assert.Equal(0.1, page.Settings.Blend);
            Assert.InRange(slider.Ratio, 0.51, 0.53);
            Assert.Contains("Blend = 0.10", page.Recipe);
            slider.Ratio = 0;
            Assert.Equal(0, page.Settings.Blend);
            slider.Ratio = 1;
            Assert.Equal(1, page.Settings.Blend);
            slider.Ratio = 0.5;
            Assert.InRange(page.Settings.Blend, 0.085, 0.095);

            for (var percent = 0; percent <= 100; percent++)
            {
                slider.Ratio = Math.Log(1 + percent) / Math.Log(101);
                Assert.Equal(percent / 100.0, page.Settings.Blend);
                Assert.Equal($"{percent}%", page.GetNode<Label>("%BlendValue").Text);
            }

            slider.Ratio = Math.Log(1 + 10.49) / Math.Log(101);
            Assert.Equal(0.1, page.Settings.Blend);
            Assert.Equal(11, slider.Value);
            page.SetLanguage(gdscript: true);
            Assert.Contains("move.blend = 0.10", page.Recipe);
            page.Reset();
            Assert.Equal(0.1, page.Settings.Blend);
            Assert.Equal("10%", page.GetNode<Label>("%BlendValue").Text);
        }
        finally { page.Free(); }
    }

    [Fact]
    public void DefaultsAndResetMatchWebsite()
    {
        var page = Load();
        try
        {
            Assert.Equal(new EasingSettings(), page.Settings);
            Assert.Equal(InOut.Elastic, page.Settings.Ease);
            Assert.True(Find<OptionButton>(page, "BlendType").Disabled);
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Jump50"), Duration = 3, Skew = 0.3, Blend = 0.6 });
            page.Scrub(0.7f);
            page.TogglePlayback();
            var handle = page.Playback!;
            page.Reset();
            Assert.Equal(Reason.Cancelled, handle.CompletionReason);
            Assert.Equal(new EasingSettings(), page.Settings);
            Assert.Equal(0, page.Progress);
            Assert.Equal(0, page.Weight);
            Assert.False(page.IsPlaying);
        }
        finally { page.Free(); }
    }

    [Fact]
    public void ScrubbingPreservesOvershootAndCancelsPlayback()
    {
        var page = Load();
        try
        {
            page.ApplySettings(new() { Entry = Family("None"), Exit = Family("Back50") });
            page.TogglePlayback();
            var handle = page.Playback!;
            page.Scrub(0.7f);
            Assert.Equal(Reason.Cancelled, handle.CompletionReason);
            Assert.Equal(page.Settings.Evaluate(0.7f), page.Weight);
            Assert.True(page.Weight > 1);
            Assert.False(Find<HSlider>(page, "Skew").Editable);
            Assert.False(Find<HSlider>(page, "BlendWidth").Editable);
            page.Scrub(2);
            Assert.Equal(1, page.Progress);
            Assert.Equal(1, page.Weight);
        }
        finally { page.Free(); }
    }

    [Fact]
    public void BlendAvailabilityRecognizesAliasesAndEndpointSkew()
    {
        var page = Load();
        try
        {
            foreach (var settings in new[]
            {
                new EasingSettings { Entry = Family("Elastic"), Exit = Family("Elastic30") },
                new EasingSettings { Entry = Family("Sine"), Exit = Family("Cubic"), Skew = 0 },
                new EasingSettings { Entry = Family("Sine"), Exit = Family("Cubic"), Skew = 1 },
                new EasingSettings { Entry = Family("None"), Exit = Family("None") },
            })
            {
                page.ApplySettings(settings);
                Assert.True(Find<OptionButton>(page, "BlendType").Disabled);
                Assert.False(Find<HSlider>(page, "BlendWidth").Editable);
            }
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Cubic") });
            Assert.False(Find<OptionButton>(page, "BlendType").Disabled);
            Assert.True(Find<HSlider>(page, "Skew").Editable);
            Assert.True(Find<HSlider>(page, "BlendWidth").Editable);
            // Exercise the actual UI signals as well as the settings API.
            Find<HSlider>(page, "Skew").Value = 0.25;
            Assert.Equal(0.25, page.Settings.Skew);
            var picker = Find<OptionButton>(page, "InCurve");
            var index = picker.GetItemIndex(EasingSettings.Families.ToList().IndexOf(Family("Quad")));
            picker.Select(index);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            Assert.Equal(Family("Quad"), page.Settings.Entry);
        }
        finally { page.Free(); }
    }

    [Fact]
    public void RealTweenPlaybackTracksCurveCompletesAndRestarts()
    {
        var page = Load();
        var originalScale = Engine.TimeScale;
        try
        {
            page.ApplySettings(new() { Entry = Family("Sine"), Exit = Family("Cubic"), Duration = 0.2, Skew = 0.3, BlendType = BlendType.Hermite, Blend = 0.6 });
            page.Scrub(0.9f);
            page.TogglePlayback();
            Assert.Equal(0, page.Progress);
            var handle = page.Playback!;
            Assert.IsType<TweenInstance<Node, float>>(handle);
            // Accelerate engine time without a wall-clock delay or scheduler internals.
            Engine.TimeScale = 5;
            for (var i = 0; i < 300 && !handle.IsTerminal; i++)
            {
                godot.Engine.Iteration();
                Assert.InRange(Math.Abs(page.Settings.Evaluate(page.Progress) - page.Weight), 0, 0.00001f);
            }
            Assert.Equal(Reason.Completed, handle.CompletionReason);
            Assert.Equal(1, page.Progress);
            Assert.Equal(1, page.Weight);
            Assert.False(page.IsPlaying);
            Assert.Equal("▶ Play", Find<Button>(page, "Play").Text);
            page.TogglePlayback();
            Assert.True(page.IsPlaying);
            Assert.Equal(0, page.Progress);
            var restarted = page.Playback!;
            page.ApplySettings(new());
            Assert.Equal(Reason.Cancelled, restarted.CompletionReason);
            page.TogglePlayback();
            var last = page.Playback!;
            page.Free();
            Assert.True(last.IsTerminal);
        }
        finally
        {
            Engine.TimeScale = originalScale;
            if (GodotObject.IsInstanceValid(page)) page.Free();
        }
    }

    [Fact]
    public void AllFamiliesAndBlendMethodsHaveFiniteCurvesAndVisibleOvershoot()
    {
        var graph = new EasingGraph();
        try
        {
            Assert.Equal(35, EasingSettings.Families.Count);
            Assert.Equal(30, EasingSettings.Families.Where(f => f.In != 0).Select(f => f.In).Distinct().Count());
            foreach (var family in EasingSettings.Families)
            foreach (var method in Enum.GetValues<BlendType>())
            foreach (var split in new[] { 0.0, 0.01, 0.5, 0.99, 1.0 })
            {
                var settings = new EasingSettings { Entry = family, Exit = Family("Jump50"), BlendType = method, Skew = split, Blend = 1 };
                graph.Refresh(settings);
                Assert.Equal(0, settings.Evaluate(0));
                Assert.Equal(1, settings.Evaluate(1));
                for (var i = 0; i <= 1000; i++)
                {
                    var value = settings.Evaluate(i / 1000f);
                    Assert.True(float.IsFinite(value));
                    Assert.True(Math.Abs(value - 0.5f) <= graph.Reach + 0.00001f);
                    Assert.True(Math.Abs(value - 0.5f) < graph.Extent);
                }
            }
        }
        finally { graph.Free(); }
    }

    [Fact]
    public void RecipesUseInvariantNumbersAndOnlyRelevantOptions()
    {
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var defaults = new EasingSettings();
            Assert.Contains("Duration = 1.5", defaults.Recipe());
            Assert.Contains("Ease = InOut.Elastic", defaults.Recipe());
            Assert.DoesNotContain("Skew =", defaults.Recipe());
            Assert.DoesNotContain("Blend =", defaults.Recipe());
            var settings = defaults with { Entry = Family("SmoothStep"), Exit = Family("SmootherStep"), Skew = 0.3, Blend = 0.6, BlendType = BlendType.SmoothStep };
            Assert.Contains("In.SmoothStep | Out.SmootherStep", settings.Recipe());
            Assert.Contains("Skew = 0.30", settings.Recipe());
            Assert.Contains("Blend = 0.60", settings.Recipe());
            Assert.Contains("In.SMOOTH_STEP | Out.SMOOTHER_STEP", settings.Recipe(true));
            Assert.Contains("BlendType.SMOOTH_STEP", settings.Recipe(true));
            Assert.Contains("Out.None", (defaults with { Entry = Family("None"), Exit = Family("None") }).Recipe());
        }
        finally { CultureInfo.CurrentCulture = oldCulture; }
    }
}
