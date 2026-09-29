// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd;
using twodog.Testing;
using twodog.Testing.Xunit;
namespace testbed.Tests;

[Collection<RenderingCollection>]
[Trait("Category", "Rendering")]
public class GalleryRenderingTests(Fixture godot)
{
    [Theory]
    [InlineData(GalleryLanguage.CSharp)] [InlineData(GalleryLanguage.GDScript)]
    public void SourceLayoutUsesExtraSpaceAndCanShrinkBackWithoutClipping(GalleryLanguage language)
    {
        var window = godot.Tree.Root;
        var originalSize = window.Size;
        var demo = new TweenDemo();
        demo.SelectLanguage(language);
        godot.Tree.Root.AddChild(demo);
        demo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        try
        {
            demo.CurrentPage!.ShowSource(0);
            Vector2 startingCodeSize = default;
            foreach (var size in new[] { new Vector2I(1440, 720), new Vector2I(1920, 1080), new Vector2I(1200, 640), new Vector2I(1440, 720) })
            {
                window.Size = size;
                for (var i = 0; i < 6; i++) godot.Engine.Iteration();
                var code = demo.CurrentPage.SourceView.Code;
                var stage = Descendants(demo).OfType<Control>().Single(c => c.Name == "PreviewStage" && c.IsVisibleInTree());
                foreach (var control in new Control[] { code, stage })
                {
                    var rect = control.GetGlobalRect();
                    Assert.True(rect.Position.X >= 0 && rect.Position.Y >= 0);
                    Assert.True(rect.End.X <= size.X + 1 && rect.End.Y <= size.Y + 1);
                }
                Assert.InRange(stage.Size.X / stage.Size.Y, 1.99f, 2.01f);
                if (startingCodeSize == default) startingCodeSize = code.Size;
                if (size.X == 1920)
                {
                    Assert.True(code.Size.X > startingCodeSize.X);
                    Assert.True(code.Size.Y > startingCodeSize.Y);
                }
            }
            demo.CurrentPage.ShowGallery();
            for (var i = 0; i < 3; i++) godot.Engine.Iteration();
            Assert.Equal(4, Descendants(demo).OfType<Control>().Count(c => c.Name == "PreviewStage" && c.IsVisibleInTree()));
        }
        finally { demo.Free(); window.Size = originalSize; }
    }

    [Theory]
    [InlineData(0, GalleryLanguage.CSharp)] [InlineData(1, GalleryLanguage.CSharp)]
    [InlineData(2, GalleryLanguage.CSharp)] [InlineData(3, GalleryLanguage.CSharp)]
    [InlineData(4, GalleryLanguage.CSharp)] [InlineData(5, GalleryLanguage.CSharp)]
    [InlineData(6, GalleryLanguage.CSharp)] [InlineData(7, GalleryLanguage.CSharp)]
    [InlineData(0, GalleryLanguage.GDScript)] [InlineData(1, GalleryLanguage.GDScript)]
    [InlineData(2, GalleryLanguage.GDScript)] [InlineData(3, GalleryLanguage.GDScript)]
    [InlineData(4, GalleryLanguage.GDScript)] [InlineData(5, GalleryLanguage.GDScript)]
    [InlineData(6, GalleryLanguage.GDScript)] [InlineData(7, GalleryLanguage.GDScript)]
    [InlineData(8, GalleryLanguage.CSharp)] [InlineData(8, GalleryLanguage.GDScript)]
    public void EachPagePlaysAndReleasesItsNativeScene(int index, GalleryLanguage language)
    {
        var demo = new testbed.TweenDemo(); godot.Tree.Root.AddChild(demo);
        try
        {
            demo.SelectLanguage(language);
            demo.SelectPage(index);
            for (var i = 0; i < 4; i++) godot.Engine.Iteration();
            var page = demo.CurrentPage!;
            var animation = Assert.IsAssignableFrom<Task>(page.SequenceTask);
            var scheduler = TweenRuntime.GetRunner(demo).Scheduler;
            if (language == GalleryLanguage.CSharp) scheduler.Update(1.9);
            else godot.Tree.GetMeta("_tweens_gd_runner").AsGodotObject().Get("scheduler").AsGodotObject().Call("update", 1.9);
            Assert.False(animation.IsCompleted);
            Assert.True(Descendants(page).OfType<SubViewport>().All(v => v.Size.X > 0 && v.Size.Y > 0));
            var sourceButtons = Descendants(page).OfType<Button>().Where(b => b.Text == (language == GalleryLanguage.CSharp ? "View C#" : "View GDScript")).ToArray();
            Assert.Equal(page.Effects.Count, sourceButtons.Length);
            for (var effect = 0; effect < sourceButtons.Length; effect++)
            {
                sourceButtons[effect].EmitSignal(BaseButton.SignalName.Pressed);
                godot.Engine.Iteration();
                var view = page.SourceView;
                Assert.Equal(effect, page.SelectedEffect);
                Assert.EndsWith(language == GalleryLanguage.CSharp ? ".Animation.cs" : ".gd", view.Source.Path);
                Assert.Equal(Godot.FileAccess.GetFileAsString("res://" + view.Source.Path).Replace("\r\n", "\n"), view.Code.Text);
                Assert.True(view.Source.TweenLine > 0);
                Assert.True(view.Code.Size.X > 0 && view.Code.Size.Y > 0);
            }
            demo.RestartDemo();
            Assert.True(animation.IsCompletedSuccessfully);
            Assert.False(GodotObject.IsInstanceValid(page));
            for (var i = 0; i < 3; i++) godot.Engine.Iteration();
            Assert.False(Assert.IsAssignableFrom<Task>(demo.CurrentPage!.SequenceTask).IsCompleted);
        }
        finally { demo.Free(); }
        Assert.Empty(godot.Errors.Drain());
    }
    [Theory]
    [InlineData(nameof(SharedUniform))] [InlineData(nameof(InstanceUniforms))]
    [InlineData(nameof(TypedUniforms))] [InlineData(nameof(VertexDisplacement))]
    public void ShaderSamplesMatchBetweenLanguages(string name)
    {
        var type = typeof(GalleryEffect).Assembly.GetType("testbed." + name)!;
        var csharp = (GalleryEffect)Activator.CreateInstance(type)!;
        var gdscript = (GalleryEffect)Activator.CreateInstance(type)!;
        var first = new Control { Size = new Vector2(600, 300) };
        var second = new Control { Size = new Vector2(600, 300) };
        godot.Tree.Root.AddChild(first); godot.Tree.Root.AddChild(second);
        csharp.Attach(first); gdscript.Attach(second, GalleryLanguage.GDScript);
        for (var i = 0; i < 3; i++) godot.Engine.Iteration();
        try
        {
            csharp.Start(1); gdscript.Start(1);
            var scheduler = TweenRuntime.GetRunner(first).Scheduler;
            var gdScheduler = godot.Tree.GetMeta("_tweens_gd_runner").AsGodotObject().Get("scheduler").AsGodotObject();
            var initial = ShaderSamples(csharp);
            for (var tick = 0; tick < 128; tick++)
            {
                scheduler.Update(1.0 / 32);
                gdScheduler.Call("update", 1.0 / 32);
                var a = ShaderSamples(csharp); var b = ShaderSamples(gdscript);
                Assert.Equal(a.Length, b.Length);
                for (var i = 0; i < a.Length; i++) Assert.InRange(Math.Abs(a[i] - b[i]), 0, 0.001);
                if (tick == 15) Assert.Contains(a.Zip(initial), p => Math.Abs(p.First - p.Second) > 0.01);
            }
            first.Free(); second.Free();
            Assert.True(csharp.Sequence!.IsCompletedSuccessfully);
            Assert.True(gdscript.Sequence!.IsCompletedSuccessfully);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first)) first.Free();
            if (GodotObject.IsInstanceValid(second)) second.Free();
            csharp.ReleaseResources(); gdscript.ReleaseResources();
        }
        Assert.Empty(godot.Errors.Drain());
    }

    private static double[] ShaderSamples(GalleryEffect effect)
    {
        var targets = effect.SceneTargets;
        if (effect is InstanceUniforms)
            return [targets["first"].As<CanvasItem>().GetInstanceShaderParameter("amount").AsDouble(),
                targets["second"].As<CanvasItem>().GetInstanceShaderParameter("amount").AsDouble()];
        if (effect is VertexDisplacement)
            return [targets["deformed"].As<GeometryInstance3D>().GetInstanceShaderParameter("amplitude").AsDouble()];
        var material = targets["material"].As<ShaderMaterial>();
        if (effect is SharedUniform) return [material.GetShaderParameter("amount").AsDouble()];
        var color = material.GetShaderParameter("tint").AsColor();
        var offset = material.GetShaderParameter("offset").AsVector2();
        return [color.R, color.G, color.B, color.A, offset.X, offset.Y];
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren()) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
