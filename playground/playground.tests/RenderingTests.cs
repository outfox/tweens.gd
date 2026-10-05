// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace playground.Tests;

[Collection<RenderingCollection>]
public class RenderingTests(Fixture godot)
{
    [Fact]
    public void DesktopAndNarrowLayoutsKeepControlsInsideViewport()
    {
        var window = godot.Tree.Root;
        var original = window.Size;
        var page = GD.Load<PackedScene>("res://main.tscn").Instantiate<EasingPlayground>();
        window.AddChild(page);
        try
        {
            foreach (var gdscript in new[] { false, true })
            foreach (var size in new[] { new Vector2I(1280, 1024), new Vector2I(480, 900), new Vector2I(1280, 1024) })
            {
                page.SetLanguage(gdscript);
                window.Size = size;
                for (var i = 0; i < 8; i++) godot.Engine.Iteration();
                var composer = (GridContainer)page.FindChild("Composer", true, false);
                Assert.Equal(size.X < 860 ? 1 : 2, composer.Columns);
                foreach (var name in new[] { "CurveGraph", "CurveControls", "Recipe", "Progress", "Reset" })
                {
                    var rect = ((Control)page.FindChild(name, true, false)).GetGlobalRect();
                    Assert.True(rect.Position.X >= 0, name);
                    Assert.True(rect.End.X <= size.X, $"{name}: {rect.End.X} exceeds {size.X}");
                }
                Assert.True(((ScrollContainer)page.FindChild("PageScroll", true, false)).GetVScrollBar().MaxValue > 0);
                var code = (TextEdit)page.FindChild("Recipe", true, false);
                Assert.True(code.ScrollFitContentHeight);
                Assert.True(code.Size.Y >= code.GetLineCount() * code.GetLineHeight());
                // Optional visual QA artifact; normal runs do not write screenshots.
                var output = System.Environment.GetEnvironmentVariable("PLAYGROUND_SCREENSHOT_DIR");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    using var image = window.GetTexture().GetImage();
                    var language = gdscript ? "gdscript" : "csharp";
                    Assert.Equal(Error.Ok, image.SavePng(Path.Combine(output, $"playground-{language}-{size.X}.png")));
                }
            }
        }
        finally { page.Free(); window.Size = original; }
    }
}
