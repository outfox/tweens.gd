// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd;
using twodog.Testing;
using twodog.Testing.Xunit;
namespace testbed.Tests.Integration;

[Collection<HeadlessCollection>]
public class ColorInterpolationTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(GalleryLanguage.CSharp)]
    [InlineData(GalleryLanguage.GDScript)]
    public void MidpointShowsAlphaAndEncodingDifferencesAndControlsPauseRealPlayback(GalleryLanguage language)
    {
        for (var i = 0; i < 3; i++) godot.Engine.Iteration();
        if (godot.Tree.CurrentScene is Gallery gallery) gallery.Free();
        var stage = new Control { Size = new Vector2(800, 400) };
        godot.Tree.Root.AddChild(stage);
        var effect = new ColorInterpolation();
        effect.Attach(stage, language);
        try
        {
            effect.Start(1);
            var targets = effect.SceneTargets;
            var swatches = targets["swatches"].AsGodotArray();
            Color At(int row, int column) => swatches[row * 4 + column].As<ColorRect>().Modulate;
            void Advance(double seconds)
            {
                if (language == GalleryLanguage.CSharp) TweenRuntime.GetRunner(stage).Scheduler.Update(seconds);
                else godot.Tree.GetMeta("_tweens_gd_runner").AsGodotObject().Get("scheduler").AsGodotObject().Call("update", seconds);
            }
            Advance(0.25);
            targets["midpoint"].As<Button>().EmitSignal(BaseButton.SignalName.Pressed);
            Assert.False(targets["automatic"].As<CheckButton>().ButtonPressed);
            Assert.Equal(0.5, targets["progress"].As<HSlider>().Value);
            // Fading to transparent black must keep red under premultiplied interpolation in every space.
            foreach (var row in new[] { 1, 3, 5 })
            {
                var color = At(row, 1);
                Assert.InRange(color.R, 0.9999f, 1.0001f);
                Assert.InRange(Math.Abs(color.G) + Math.Abs(color.B), 0, 0.0001f);
                Assert.Equal(0.5f, color.A);
            }
            Assert.Equal(0.5f, At(0, 1).R); // Raw sRGB components.
            Assert.InRange(At(2, 1).R, 0.734f, 0.737f); // Half the linear-light energy, encoded back to sRGB.
            Assert.InRange(At(4, 1).R, 0.387f, 0.390f); // Half the OKLab lightness, with straight alpha.
            for (var row = 0; row < 6; row += 2)
            {
                Assert.Equal(0.5f, At(row, 3).A);
                Assert.InRange(At(row, 3).R - At(row + 1, 3).R, -0.00001f, 0.00001f);
                Assert.InRange(At(row, 3).G - At(row + 1, 3).G, -0.00001f, 0.00001f);
                Assert.InRange(At(row, 3).B - At(row + 1, 3).B, -0.00001f, 0.00001f);
            }
            Advance(0.125);
            Assert.Equal(0.5f, At(5, 1).A);
            var automatic = targets["automatic"].As<CheckButton>();
            automatic.ButtonPressed = true;
            Advance(0.125);
            Assert.Equal(0.625f, At(5, 1).A);
            stage.Free();
            Assert.True(effect.Sequence!.IsCompletedSuccessfully);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(stage)) stage.Free();
            effect.ReleaseResources();
        }
        for (var i = 0; i < 3; i++) godot.Engine.Iteration();
        Assert.Empty(godot.Errors.Drain());
    }
}
