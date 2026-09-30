// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace testbed.Tests.Integration;

[Collection<HeadlessCollection>]
public class GalleryLanguageTests
{
    private readonly HeadlessFixture godot;

    public GalleryLanguageTests(HeadlessFixture godot)
    {
        this.godot = godot;
        Pump();
        if (godot.Tree.CurrentScene is TweenDemo demo) demo.Free();
    }

    private void Pump() { for (var i = 0; i < 3; i++) godot.Engine.Iteration(); }

    public static IEnumerable<object[]> Examples => typeof(GalleryEffect).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(GalleryEffect)) && !t.IsAbstract && !t.IsNested && t.Namespace == "testbed")
        .Where(t => !new[] { "SharedUniform", "InstanceUniforms", "TypedUniforms", "VertexDisplacement" }.Contains(t.Name))
        .Select(t => new object[] { t.Name });

    [Theory]
    [MemberData(nameof(Examples))]
    public void SamplesMatchWithinDocumentedPrecisionAndBothLanguagesSettleOnSceneExit(string name)
    {
        var type = typeof(GalleryEffect).Assembly.GetType("testbed." + name)!;
        var csharp = (GalleryEffect)Activator.CreateInstance(type)!;
        var gdscript = (GalleryEffect)Activator.CreateInstance(type)!;
        var first = new Control { Size = new Vector2(600, 300) };
        var second = new Control { Size = new Vector2(600, 300) };
        godot.Tree.Root.AddChild(first);
        godot.Tree.Root.AddChild(second);
        csharp.Attach(first);
        gdscript.Attach(second, GalleryLanguage.GDScript);
        Pump();
        try
        {
            csharp.Start(1.0);
            gdscript.Start(1.0);
            var scheduler = TweenRuntime.GetRunner(first).Scheduler;
            if (name == nameof(JellyButton))
            {
                // This example waits for input: elapsed time alone must not award points.
                scheduler.Update(3);
                GDScriptScheduler()?.Call("update", 3.0);
                foreach (var effect in new[] { csharp, gdscript })
                {
                    var targets = effect.SceneTargets;
                    Assert.Equal("000", targets["score"].As<Label>().Text);
                    var button = targets["button"].As<Button>();
                    Assert.Equal("TAP ME!", button.Text);
                    button.EmitSignal(BaseButton.SignalName.Pressed);
                }
            }
            var gdScheduler = GDScriptScheduler();
            Assert.NotNull(gdScheduler);
            Assert.Equal(name == nameof(JellyButton), gdscript.Sequence!.IsCompleted);
            // Binary-exact deltas keep endpoint comparisons independent of accumulated decimal roundoff.
            for (var tick = 0; tick < 384; tick++)
            {
                scheduler.Update(1.0 / 64);
                gdScheduler!.Call("update", 1.0 / 64);
                Compare(csharp.SceneTargets, gdscript.SceneTargets, name, tick);
                Assert.Equal(csharp.Sequence!.IsCompleted, gdscript.Sequence.IsCompleted);
            }
            first.Free();
            second.Free();
            Assert.True(csharp.Sequence!.IsCompletedSuccessfully);
            Assert.True(gdscript.Sequence!.IsCompletedSuccessfully);
            Assert.Equal(0, scheduler.ActiveCount);
            Assert.Equal(0, gdScheduler!.Get("active_count").AsInt32());
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first)) first.Free();
            if (GodotObject.IsInstanceValid(second)) second.Free();
            csharp.ReleaseResources();
            gdscript.ReleaseResources();
        }
        Pump();
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public void GlobalSwitchChangesPlaybackAndSourceAndPreservesPageExampleAndDuration()
    {
        var demo = new TweenDemo();
        godot.Tree.Root.AddChild(demo);
        Pump();
        try
        {
            var picker = Descendants(demo).OfType<OptionButton>().Single(n => n.Name == "LanguageSwitch");
            var duration = Descendants(demo).OfType<HSlider>().Single();
            duration.Value = 0.8;
            for (var pageIndex = 0; pageIndex < TweenDemo.PageNames.Length; pageIndex++)
            {
                demo.SelectPage(pageIndex);
                Pump();
                demo.CurrentPage!.ShowSource(0);
                foreach (var language in new[] { GalleryLanguage.GDScript, GalleryLanguage.CSharp })
                {
                    var old = demo.CurrentPage!;
                    var sequence = old.SequenceTask;
                    picker.Select((int)language);
                    picker.EmitSignal(OptionButton.SignalName.ItemSelected, (int)language);
                    Pump();
                    Assert.False(GodotObject.IsInstanceValid(old));
                    if (sequence is not null) Assert.True(sequence.IsCompletedSuccessfully);
                    Assert.Equal(pageIndex, demo.SelectedPage);
                    Assert.Equal(language, demo.Language);
                    Assert.Equal(0, demo.CurrentPage!.SelectedEffect);
                    Assert.Equal(0.8, duration.Value);
                    Assert.All(demo.CurrentPage.Effects, effect => Assert.Equal(language, effect.Language));
                    var source = demo.CurrentPage.SourceView;
                    Assert.EndsWith(language == GalleryLanguage.CSharp ? ".cs" : ".gd", source.Source.Path);
                    Assert.Equal(Godot.FileAccess.GetFileAsString("res://" + source.Source.Path).Replace("\r\n", "\n"), source.Code.Text);
                    if (language == GalleryLanguage.GDScript)
                    {
                        Assert.Equal(0, TweenRuntime.GetRunner(demo).Scheduler.ActiveCount);
                        var files = Descendants(source).OfType<OptionButton>().Single();
                        Assert.Equal("Shared scene · C#", files.GetItemText(1));
                        files.EmitSignal(OptionButton.SignalName.ItemSelected, 2);
                        Assert.EndsWith("GalleryAnimation.gd", source.Source.Path);
                        files.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
                        Assert.EndsWith(".cs", source.Source.Path);
                    }
                    else if (GDScriptScheduler() is { } scheduler)
                        Assert.Equal(0, scheduler.Get("active_count").AsInt32());
                }
            }
            demo.SelectLanguage(GalleryLanguage.GDScript);
            demo.SelectPage(2);
            demo.SelectLanguage(GalleryLanguage.CSharp);
            demo.SelectLanguage(GalleryLanguage.GDScript);
            Pump();
            Assert.Equal(2, demo.SelectedPage);
            Assert.Equal(0, TweenRuntime.GetRunner(demo).Scheduler.ActiveCount);
            demo.RestartDemo();
            Pump();
            Assert.Equal(GalleryLanguage.GDScript, demo.CurrentPage!.Language);
            Assert.Throws<ArgumentOutOfRangeException>(() => demo.SelectLanguage((GalleryLanguage)99));
        }
        finally { demo.Free(); }
        Pump();
        Assert.Equal(0, GDScriptScheduler()!.Get("active_count").AsInt32());
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public void GDScriptButtonClicksUseOnlyTheGDScriptScheduler()
    {
        var demo = new TweenDemo();
        demo.SelectLanguage(GalleryLanguage.GDScript);
        godot.Tree.Root.AddChild(demo);
        Pump();
        try
        {
            var effect = Assert.IsType<JellyButton>(demo.CurrentPage!.Effects[2]);
            var targets = effect.SceneTargets;
            var button = targets["button"].As<Button>();
            var score = targets["score"].As<Label>();
            button.EmitSignal(BaseButton.SignalName.Pressed);
            button.EmitSignal(BaseButton.SignalName.Pressed);
            for (var i = 0; i < 24; i++) GDScriptScheduler()!.Call("update", 0.025);
            Assert.Equal("020", score.Text);
            Assert.Equal(0, TweenRuntime.GetRunner(demo).Scheduler.ActiveCount);
        }
        finally { demo.Free(); }
        Pump();
        Assert.Equal(0, GDScriptScheduler()!.Get("active_count").AsInt32());
        Assert.Empty(godot.Errors.Drain());
    }

    private GodotObject? GDScriptScheduler() => godot.Tree.HasMeta("_tweens_gd_runner")
        ? godot.Tree.GetMeta("_tweens_gd_runner").AsGodotObject().Get("scheduler").AsGodotObject() : null;

    [Fact]
    public void ComposerControlsChangeRealPlaybackAndSurviveDurationAndLanguageChanges()
    {
        var demo = new TweenDemo();
        godot.Tree.Root.AddChild(demo);
        Pump();
        try
        {
            demo.SelectPage(Array.IndexOf(TweenDemo.PageNames, "Easing"));
            Pump();
            foreach (var language in new[] { GalleryLanguage.CSharp, GalleryLanguage.GDScript })
            {
                demo.SelectLanguage(language);
                Pump();
                var targets = demo.CurrentPage!.Effects[0].SceneTargets;
                var entry = targets["entry"].As<OptionButton>();
                entry.Select(entry.GetItemIndex(3)); // Quad
                entry.EmitSignal(OptionButton.SignalName.ItemSelected, entry.Selected);
                var exit = targets["exit"].As<OptionButton>();
                exit.Select(exit.GetItemIndex(4)); // Cubic
                exit.EmitSignal(OptionButton.SignalName.ItemSelected, exit.Selected);
                targets["skew"].As<HSlider>().Value = 2;
                targets["blend"].As<OptionButton>().Select((int)BlendType.Linear);
                targets["blend"].As<OptionButton>().EmitSignal(OptionButton.SignalName.ItemSelected, (int)BlendType.Linear);
                targets["width"].As<HSlider>().Value = 0.8;
                var duration = Descendants(demo).OfType<HSlider>().Single(s => s.Name != "EasingSkew" && s.Name != "EasingWidth");
                duration.Value = duration.Value == 2 ? 1 : 2; // Rebuild while keeping the composition.
                Pump();
                targets = demo.CurrentPage!.Effects[0].SceneTargets;
                Assert.Equal(3, targets["entry"].As<OptionButton>().GetSelectedId());
                Assert.Equal(4, targets["exit"].As<OptionButton>().GetSelectedId());
                Assert.Equal(2, targets["skew"].As<HSlider>().Value);
                Assert.Equal((int)BlendType.Linear, targets["blend"].As<OptionButton>().Selected);
                Assert.Equal(0.8, targets["width"].As<HSlider>().Value);
                // Reset again to remove wall-clock progress from Pump, then sample exactly halfway.
                targets["entry"].As<OptionButton>().EmitSignal(OptionButton.SignalName.ItemSelected, targets["entry"].As<OptionButton>().Selected);
                if (language == GalleryLanguage.CSharp) TweenRuntime.GetRunner(demo).Scheduler.Update(duration.Value / 2);
                else GDScriptScheduler()!.Call("update", duration.Value / 2);
                Assert.InRange(Math.Abs(targets["ball"].As<Polygon2D>().Position.X - (-154.6875)), 0, 0.001);
                var points = targets["resultCurve"].As<Line2D>().Points;
                Assert.InRange(Math.Abs(points[120].Y - 51.046875), 0, 0.001);
            }
        }
        finally { demo.Free(); }
        Pump();
        Assert.Empty(godot.Errors.Drain());
    }

    [Theory]
    [InlineData(GalleryLanguage.CSharp)]
    [InlineData(GalleryLanguage.GDScript)]
    public void ComposerSupportsOvershootVariantsAndFitsTheirFullRange(GalleryLanguage language)
    {
        var stage = new Control { Size = new Vector2(600, 400) };
        godot.Tree.Root.AddChild(stage);
        var effect = new EasingComposer();
        effect.Attach(stage, language);
        Pump();
        try
        {
            effect.Start(1);
            var targets = effect.SceneTargets;
            var entry = targets["entry"].As<OptionButton>();
            var exit = targets["exit"].As<OptionButton>();
            foreach (var (a, b, ease) in new[] {
                ("Back50", "Back50", InOut.Back50),
                ("None", "Elastic50", Out.Elastic50),
                ("Elastic50", "Back20", In.Elastic50 | Out.Back20),
                ("Elastic10", "Elastic", InOut.Elastic),
                ("Back", "Back10", InOut.Back),
                ("Bounce", "Bounce10", InOut.Bounce),
                ("None", "Bounce50", Out.Bounce50),
                ("Bounce50", "Bounce50", InOut.Bounce50),
                ("Bounce20", "Bounce40", In.Bounce20 | Out.Bounce40),
                ("None", "Jump50", Out.Jump50),
                ("Jump", "Jump10", InOut.Jump),
                ("Jump50", "Jump50", InOut.Jump50),
                ("Jump20", "Jump40", In.Jump20 | Out.Jump40),
                ("Jump30", "Bounce20", In.Jump30 | Out.Bounce20),
            })
            {
                entry.Select(Enumerable.Range(0, entry.ItemCount).Single(i => entry.GetItemText(i) == a));
                exit.Select(Enumerable.Range(0, exit.ItemCount).Single(i => exit.GetItemText(i) == b));
                entry.EmitSignal(OptionButton.SignalName.ItemSelected, entry.Selected);
                if (language == GalleryLanguage.CSharp) TweenRuntime.GetRunner(stage).Scheduler.Update(0.25);
                else GDScriptScheduler()!.Call("update", 0.25);
                Assert.InRange(Math.Abs(targets["ball"].As<Polygon2D>().Position.X - (-200 + 400 * Easing.Evaluate(ease, 0.25f))), 0, 0.002);
                Assert.Equal(a is not ("Elastic50" or "Bounce20" or "Jump20" or "Jump30"), targets["blend"].As<OptionButton>().Disabled);
                var line = targets["resultCurve"].As<Line2D>();
                var view = line.GetViewport();
                var zoom = view.GetCamera2D().Zoom;
                foreach (var point in line.Points)
                {
                    Assert.True(Math.Abs(point.Y) * zoom.Y < view.GetVisibleRect().Size.Y / 2);
                    var ballX = -200 + 400 * ((66 - point.Y) / 132);
                    Assert.True(Math.Abs(ballX) * zoom.X < view.GetVisibleRect().Size.X / 2);
                }
            }
        }
        finally { stage.Free(); effect.ReleaseResources(); }
        Pump();
        Assert.Empty(godot.Errors.Drain());
    }

    private static readonly HashSet<string> Properties = [
        "position", "rotation", "scale", "skew", "modulate", "self_modulate", "visible", "text", "visible_ratio",
        "color", "default_color", "width", "offset", "zoom", "value", "scroll_vertical", "spread", "gravity",
        "texture_scale", "energy", "progress_ratio", "v_offset", "h_offset", "fov", "light_color", "light_energy", "spot_angle",
        "albedo_color", "roughness", "emission", "emission_energy_multiplier", "uv1_offset", "uv1_scale",
        "offset_transform_position", "offset_transform_rotation", "offset_transform_scale", "points"
    ];

    private static void Compare(Variant a, Variant b, string path, int tick)
    {
        if (a.VariantType == Variant.Type.Dictionary)
        {
            foreach (var pair in a.AsGodotDictionary())
            {
                // Confetti trajectories are intentionally random in both implementations.
                if (path == "JellyButton" && pair.Key.AsString() == "shards") continue;
                Compare(pair.Value, b.AsGodotDictionary()[pair.Key], path + "." + pair.Key.AsString(), tick);
            }
        }
        else if (a.VariantType == Variant.Type.Array)
        {
            var left = a.AsGodotArray(); var right = b.AsGodotArray();
            Assert.Equal(left.Count, right.Count);
            for (var i = 0; i < left.Count; i++) Compare(left[i], right[i], path + $"[{i}]", tick);
        }
        else if (a.VariantType == Variant.Type.Object)
        {
            var left = a.AsGodotObject(); var right = b.AsGodotObject();
            foreach (var property in left.GetPropertyList())
            {
                var name = property["name"].AsString();
                if (Properties.Contains(name)) Compare(left.Get(name), right.Get(name), path + "." + name, tick);
            }
        }
        else
        {
            var close = a.VariantType switch
            {
                // Callbacks belong to separate scene instances; their effects are checked through scene state.
                Variant.Type.Callable => !a.AsCallable().Equals(default(Callable)) && !b.AsCallable().Equals(default(Callable)),
                Variant.Type.String => a.AsString() == b.AsString(),
                Variant.Type.StringName => a.AsStringName() == b.AsStringName(),
                Variant.Type.Bool => a.AsBool() == b.AsBool(),
                Variant.Type.Int => a.AsInt64() == b.AsInt64(),
                Variant.Type.Float => Math.Abs(a.AsDouble() - b.AsDouble()) < 0.003,
                Variant.Type.Vector2 => a.AsVector2().DistanceTo(b.AsVector2()) < 0.003 || EasingEndpointDifference(a.AsVector2(), b.AsVector2(), path),
                Variant.Type.Vector3 => a.AsVector3().DistanceTo(b.AsVector3()) < 0.003,
                Variant.Type.Color => ColorClose(a.AsColor(), b.AsColor()),
                Variant.Type.PackedVector2Array => a.AsVector2Array().Zip(b.AsVector2Array()).All(p => p.First.DistanceTo(p.Second) < 0.003),
                _ => a.Equals(b),
            };
            Assert.True(close, $"{path}, tick {tick}: C# {a}, GDScript {b}");
        }
    }

    private static bool ColorClose(Color a, Color b) => Math.Abs(a.R - b.R) < 0.003 &&
        Math.Abs(a.G - b.G) < 0.003 && Math.Abs(a.B - b.B) < 0.003 && Math.Abs(a.A - b.A) < 0.003;

    private static bool EasingEndpointDifference(Vector2 csharp, Vector2 gdscript, string path) =>
        // Expo/Elastic have endpoint branches: float C# progress can round to 1 before double GDScript progress.
        // On the 240px race track the jump is 240 / 2048 = 0.1171875px. Keep this exception local.
        (path.StartsWith("EasingRace.racers[3][", StringComparison.Ordinal) || path.StartsWith("EasingRace.racers[5][", StringComparison.Ordinal)) &&
        path.EndsWith(".position", StringComparison.Ordinal) &&
        Math.Abs(Math.Abs(csharp.X) - 120) < 0.00001 && Math.Abs(csharp.Y - gdscript.Y) < 0.00001 &&
        Math.Abs(Math.Abs(csharp.X - gdscript.X) - 240.0 / 2048) < 0.0001;

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
