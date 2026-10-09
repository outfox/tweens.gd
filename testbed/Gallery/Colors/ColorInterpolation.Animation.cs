// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System;
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in ColorInterpolation.cs. The comparison uses the library color policies; OKLab + premultiplied is the default.
public sealed partial class ColorInterpolation
{
    private TweenInstance<Node, float> clock = null!;

    private async Task AnimateAsync()
    {
        clock = Stage.TweenFloat(1, Seconds, options =>
        {
            options.From = 0;
            options.Ease = EaseType.Linear;
            options.PingPong = true;
            options.Repeats = TweenOptions.Infinite;
            options.PingPongInterval = options.RepeatInterval = 0.35;
            options.OnUpdate = (_, value) => Sample(value);
        });
        progress.ValueChanged += value => Scrub((float)value);
        midpoint.Pressed += () => Scrub(0.5f);
        automatic.Toggled += enabled => clock.IsPaused = !enabled;
        Sample(0);
        await clock.End;
    }

    private void Scrub(float value)
    {
        automatic.SetPressedNoSignal(false);
        clock.Pause();
        Sample(value);
    }

    private void Sample(float value)
    {
        progress.SetValueNoSignal(value);
        for (var row = 0; row < 6; row++)
            for (var column = 0; column < Starts.Length; column++)
            {
                var color = MixColor(Starts[column], Ends[column], value, row / 2, (row & 1) != 0);
                // Independent RGB tint and opacity: the color endpoints themselves are both opaque.
                if (column == 3) color.A = 1 - value;
                var swatch = swatches[row * Starts.Length + column];
                swatch.Modulate = color;
                swatch.TooltipText = $"R {color.R:F3} · G {color.G:F3} · B {color.B:F3} · A {color.A:F3}";
            }
    }

    internal static Color MixColor(Color from, Color to, float weight, int space, bool premultiply)
        => Interpolators.Color(from, to, weight,
            space == 0 ? ColorSpace.Srgb : space == 1 ? ColorSpace.LinearRgb : ColorSpace.Oklab,
            premultiply ? AlphaMode.Premultiplied : AlphaMode.Straight);
}