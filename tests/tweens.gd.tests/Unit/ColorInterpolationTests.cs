// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;

namespace tweens.gd.Tests.Unit;

public class ColorInterpolationTests
{
    [Fact]
    public void DefaultsPreserveTintWhenFadingToTransparentBlack()
    {
        var value = Interpolators.Color(Colors.Red, new Color(0, 0, 0, 0), 0.5f);
        Assert.True(value.IsEqualApprox(new Color(1, 0, 0, 0.5f)));
        Assert.Equal(new Color(1, 0, 0, 0), Interpolators.Color(new Color(1, 0, 0, 0), Colors.Blue, 0));
        Assert.Equal(Colors.Blue, Interpolators.Color(Colors.Red, Colors.Blue, 1));
        Assert.Equal(new Color(0, 0, 0, 0), Interpolators.Color(new Color(1, 0, 0, 0), new Color(0, 0, 1, 0), 0.5f));
    }

    [Fact]
    public void OklabMidpointMatchesPublishedReference()
    {
        var value = Interpolators.Color(Colors.Red, Colors.Blue, 0.5f);
        Assert.InRange(value.R, 0.5503f, 0.5506f);
        Assert.InRange(value.G, 0.3255f, 0.3258f);
        Assert.InRange(value.B, 0.6364f, 0.6367f);
    }

    [Theory]
    [InlineData(ColorSpace.Oklab)] [InlineData(ColorSpace.Srgb)] [InlineData(ColorSpace.LinearRgb)]
    public void SpacesAndEncodingsRoundTripExtendedGodotColors(ColorSpace space)
    {
        foreach (var alpha in Enum.GetValues<AlphaMode>())
            foreach (var encoding in Enum.GetValues<ColorEncoding>())
                foreach (var color in new[] { new Color(0.2f, 0.6f, 0.8f, 0.3f), new Color(1.2f, -0.1f, 0.4f, 1) })
                {
                    var decoded = gd.ColorInterpolation.Decode(gd.ColorInterpolation.Encode(color, space, alpha, encoding), space, alpha, encoding);
                    Assert.InRange(Math.Abs(decoded.R - color.R) + Math.Abs(decoded.G - color.G) + Math.Abs(decoded.B - color.B), 0, 0.00002f);
                    Assert.Equal(color.A, decoded.A);
                }
    }

    [Fact]
    public void InvalidPoliciesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Interpolators.Color(Colors.Red, Colors.Blue, 0.5f, (ColorSpace)3, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => Interpolators.Color(Colors.Red, Colors.Blue, 0.5f, default, (AlphaMode)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Interpolators.Color(Colors.Red, Colors.Blue, 0.5f, default, default, (ColorEncoding)2));
    }

    private sealed class Box { public Color Value { get; set; } }

    [Fact]
    public void ExplicitPolicyIsSnapshottedAndCustomInterpolatorsKeepTheirMath()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = Colors.Red };
        var definition = new PropertyTween<Box, Color>(b => b.Value, (b, c) => b.Value = c, Interpolators.Color)
        { To = new Color(0, 0, 0, 0), Duration = 1, ColorSpace = ColorSpace.Srgb, AlphaMode = AlphaMode.Straight };
        scheduler.Add(box, definition);
        definition.ColorSpace = ColorSpace.Oklab; definition.AlphaMode = AlphaMode.Premultiplied;
        scheduler.Update(0.5);
        Assert.Equal(new Color(0.5f, 0, 0, 0.5f), box.Value);
        scheduler.Update(0.5);
        box.Value = Colors.Red;
        var custom = new PropertyTween<Box, Color>(b => b.Value, (b, c) => b.Value = c, static (a, b, w) => a.Lerp(b, w))
        { To = new Color(0, 0, 0, 0), Duration = 1 };
        scheduler.Add(box, custom); scheduler.Update(0.5);
        Assert.Equal(new Color(0.5f, 0, 0, 0.5f), box.Value);
    }

    [Fact]
    public void AbsolutePlaybackUsesPolicyWhileFactorsAndOffsetsUseComponents()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box { Value = Colors.Red };
        var definition = new PropertyTween<Box, Color>(b => b.Value, (b, c) => b.Value = c, Interpolators.Color)
        { To = new Color(0, 0, 0, 0), Duration = 1 };
        scheduler.Add(box, definition);
        scheduler.Update(0.5);
        Assert.True(box.Value.IsEqualApprox(new Color(1, 0, 0, 0.5f)));
        scheduler.Update(0.5);
        box.Value = new Color(0.2f, 0.4f, 0.6f, 1);
        definition.To = null;
        definition.FactorTo = 2;
        scheduler.Add(box, definition);
        scheduler.Update(1);
        Assert.Equal(new Color(0.4f, 0.8f, 1.2f, 2), box.Value);
        definition.FactorTo = 1;
        definition.By = new Color(0.2f, 0.4f, 0.6f, 0);
        scheduler.Add(box, definition);
        scheduler.Update(0.5);
        Assert.True(box.Value.IsEqualApprox(new Color(0.5f, 1, 1.5f, 2)));
    }
}
