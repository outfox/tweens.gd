// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in EasingComposer.cs.
public sealed partial class EasingComposer
{
    private static readonly (string Name, EaseType In, EaseType Out)[] Families =
    [
        ("None", In.None, Out.None), ("Linear", In.Linear, Out.Linear),
        ("Sine", In.Sine, Out.Sine), ("Quad", In.Quad, Out.Quad), ("Cubic", In.Cubic, Out.Cubic),
        ("Quart", In.Quart, Out.Quart), ("Quint", In.Quint, Out.Quint), ("Expo", In.Expo, Out.Expo),
        ("Circ", In.Circ, Out.Circ), ("Back", In.Back, Out.Back), ("Elastic", In.Elastic, Out.Elastic),
        ("Bounce", In.Bounce, Out.Bounce), ("SmoothStep", In.SmoothStep, Out.SmoothStep),
        ("SmootherStep", In.SmootherStep, Out.SmootherStep),
        ("Back10", In.Back10, Out.Back10),
        ("Back20", In.Back20, Out.Back20),
        ("Back30", In.Back30, Out.Back30),
        ("Back40", In.Back40, Out.Back40),
        ("Back50", In.Back50, Out.Back50),
        ("Elastic10", In.Elastic10, Out.Elastic10),
        ("Elastic20", In.Elastic20, Out.Elastic20),
        ("Elastic30", In.Elastic30, Out.Elastic30),
        ("Elastic40", In.Elastic40, Out.Elastic40),
        ("Elastic50", In.Elastic50, Out.Elastic50),
    ];
    private TweenInstance? preview;

    private async Task AnimateAsync()
    {
        entry.ItemSelected += _ => Refresh();
        exit.ItemSelected += _ => Refresh();
        skew.ValueChanged += _ => Refresh();
        blend.ItemSelected += _ => Refresh();
        width.ValueChanged += _ => Refresh();
        Refresh();
        // A separate lifetime wait survives preview restarts and also settles on engine shutdown.
        await Stage.TweenFloat(1, Seconds, options => options.Repeats = TweenOptions.Infinite).End;
        preview = null;
    }

    private void Refresh()
    {
        preview?.Cancel();
        var a = Families[entry.Selected];
        var b = Families[exit.Selected];
        var ease = a.In | b.Out;
        var exponent = skew.Value;
        var method = (BlendType)blend.Selected;
        var joinWidth = width.Value;
        var single = a.In == 0 || b.Out == 0 || a.In == b.In;
        blend.Disabled = single;
        width.Editable = !single;
        entryCurve.Points = a.In == 0 ? [] : Sample(b.Out == 0 ? a.In : a.In | a.Out, exponent, end: b.Out == 0 ? 1 : Math.Pow(0.5, 1/exponent));
        exitCurve.Points = b.Out == 0 ? [] : Sample(a.In == 0 ? b.Out : b.In | b.Out, exponent, start: a.In == 0 ? 0 : Math.Pow(0.5, 1/exponent));
        resultCurve.Points = Sample(ease, exponent, method, joinWidth);
        FitPreview();
        var left = -200 + 400 * (float)Math.Pow((1-joinWidth)/2, 1 / exponent);
        var right = -200 + 400 * (float)Math.Pow((1+joinWidth)/2, 1 / exponent);
        region.Polygon = [new(left, -110), new(right, -110), new(right, 85), new(left, 85)];
        region.Visible = !single;
        recipe.Text = $"{a.Name} | {b.Name}    ·    Skew {exponent:0.00}    ·    {method} {joinWidth:P0}";
        tracer.Position = new(-200, 66);
        preview = ball.TweenPositionX(200, Seconds, options =>
        {
            options.From = -200;
            options.Ease = ease; // For example: In.Sine | Out.Cubic, or InOut.Sine.
            options.Skew = exponent;
            options.BlendType = method;
            options.Blend = joinWidth;
            options.Repeats = TweenOptions.Infinite;
            options.RepeatInterval = 0.35;
            options.OnUpdate = (handle, x) => tracer.Position = new(-200 + 400 * handle.Progress, 66 - 132 * ((x + 200) / 400));
        });
    }

    private static Vector2[] Sample(EaseType ease, double skew, BlendType blendType = BlendType.Hermite, double blend = 0.4, double start = 0, double end = 1)
    {
        var points = new Vector2[241];
        for (var i = 0; i < points.Length; i++)
        {
            var t = (float)(start + (end-start)*i/240);
            points[i] = new(-200 + 400 * t, 66 - 132 * Easing.Evaluate(ease, (float)Math.Pow(t, skew), blendType, blend));
        }
        return points;
    }
}
