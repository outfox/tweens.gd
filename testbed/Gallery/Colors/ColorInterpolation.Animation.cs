// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System;
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in ColorInterpolation.cs. These are comparison policies, not library defaults.
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
    {
        if (weight == 0) return from;
        if (weight == 1) return to;
        var a = Coordinates(from, space);
        var b = Coordinates(to, space);
        var alpha = from.A + (to.A - from.A) * weight;
        if (premultiply) { a *= from.A; b *= to.A; }
        var value = a.Lerp(b, weight);
        if (premultiply) value = alpha == 0 ? Vector3.Zero : value / alpha;
        if (space == 2) value = FromOklab(value);
        var result = new Color(value.X, value.Y, value.Z, alpha);
        return space == 0 ? result : result.LinearToSrgb();
    }

    private static Vector3 Coordinates(Color color, int space)
    {
        if (space != 0) color = color.SrgbToLinear();
        var value = new Vector3(color.R, color.G, color.B);
        return space == 2 ? ToOklab(value) : value;
    }

    // Björn Ottosson's linear-sRGB/OKLab matrices: https://bottosson.github.io/posts/oklab/
    private static Vector3 ToOklab(Vector3 rgb)
    {
        var l = MathF.Cbrt(0.4122214708f * rgb.X + 0.5363325363f * rgb.Y + 0.0514459929f * rgb.Z);
        var m = MathF.Cbrt(0.2119034982f * rgb.X + 0.6806995451f * rgb.Y + 0.1073969566f * rgb.Z);
        var s = MathF.Cbrt(0.0883024619f * rgb.X + 0.2817188376f * rgb.Y + 0.6299787005f * rgb.Z);
        return new(0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
    }

    private static Vector3 FromOklab(Vector3 lab)
    {
        var l = lab.X + 0.3963377774f * lab.Y + 0.2158037573f * lab.Z;
        var m = lab.X - 0.1055613458f * lab.Y - 0.0638541728f * lab.Z;
        var s = lab.X - 0.0894841775f * lab.Y - 1.2914855480f * lab.Z;
        l *= l * l; m *= m * m; s *= s * s;
        return new(4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
            -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
            -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
    }
}
