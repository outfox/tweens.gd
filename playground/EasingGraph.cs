// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using Godot;

namespace playground;

/// <summary>Samples the composed and source curves; rescales for their full overshoot.</summary>
public partial class EasingGraph : Control
{
    public EasingSettings Settings { get; private set; } = new();
    public float Progress { get; set; }
    public float Weight { get; set; }
    public float Extent { get; private set; } = 0.7f;
    public float Reach { get; private set; } = 0.5f;
    private readonly Vector2[] result = new Vector2[241], entry = new Vector2[241], exit = new Vector2[241];

    public EasingGraph()
    {
        CustomMinimumSize = new(300, 280);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public void Refresh(EasingSettings settings)
    {
        Settings = settings;
        float extent = 0.5f, reach = 0.5f;
        for (var i = 0; i <= 1000; i++)
        {
            var t = i / 1000f;
            reach = Math.Max(reach, Math.Abs(settings.Evaluate(t) - 0.5f));
            if (settings.Entry.In != 0 && (settings.Exit.Out == 0 || t <= settings.Skew))
                extent = Math.Max(extent, Math.Abs(settings.EvaluateEntry(t) - 0.5f));
            if (settings.Exit.Out != 0 && (settings.Entry.In == 0 || t >= settings.Skew))
                extent = Math.Max(extent, Math.Abs(settings.EvaluateExit(t) - 0.5f));
        }
        Reach = reach;
        Extent = Math.Max(0.7f, Math.Max(extent, reach) * 1.08f);
        for (var i = 0; i < result.Length; i++)
        {
            var t = i / 240f;
            result[i] = new(t, settings.Evaluate(t));
            var a = t * (settings.Exit.Out == 0 ? 1 : (float)settings.Skew);
            var b = settings.Entry.In == 0 ? t : (float)settings.Skew + t * (1 - (float)settings.Skew);
            entry[i] = new(a, settings.EvaluateEntry(a));
            exit[i] = new(b, settings.EvaluateExit(b));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawStyleBox(PlaygroundTheme.Box(PlaygroundTheme.Stage, 0, false), new Rect2(Vector2.Zero, Size));
        var plot = new Rect2(42, 20, Math.Max(1, Size.X - 62), Math.Max(1, Size.Y - 56));
        Vector2 Point(Vector2 value) => new(plot.Position.X + plot.Size.X * value.X,
            plot.GetCenter().Y - (value.Y - 0.5f) / Extent * plot.Size.Y / 2);
        if (Settings.CanBlend)
        {
            var half = (float)(Settings.Blend * Math.Min(Settings.Skew, 1 - Settings.Skew));
            DrawRect(new Rect2(plot.Position.X + plot.Size.X * ((float)Settings.Skew - half), plot.Position.Y,
                plot.Size.X * 2 * half, plot.Size.Y), PlaygroundTheme.Mint with { A = 0.08f });
        }
        var font = GetThemeDefaultFont();
        foreach (var value in new[] { 0f, 0.5f, 1f })
        {
            var y = Point(new(0, value)).Y;
            DrawDashedLine(new(plot.Position.X, y), new(plot.End.X, y), PlaygroundTheme.Border, 1, 4);
            DrawString(font, new(8, y + 5), value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), fontSize: 13, modulate: PlaygroundTheme.Muted);
        }
        foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            var x = Point(new(t, 0)).X;
            DrawDashedLine(new(x, plot.Position.Y), new(x, plot.End.Y), PlaygroundTheme.Border, 1, 4);
        }
        void Curve(Vector2[] samples, Color color, float width, int dash = 0)
        {
            var points = Array.ConvertAll(samples, Point);
            if (dash == 0) DrawPolyline(points, color, width, true);
            else for (var i = 1; i < points.Length; i++)
                if ((i / dash) % 2 == 0) DrawLine(points[i - 1], points[i], color, width, true);
        }
        Curve(result, PlaygroundTheme.Mint.Darkened(0.5f), 7);
        if (Settings.Entry.In != 0) Curve(entry, PlaygroundTheme.Amber, 2.5f, 4);
        if (Settings.Exit.Out != 0) Curve(exit, PlaygroundTheme.Blue, 2.5f, 2);
        var now = Point(new(Progress, Weight));
        DrawDashedLine(new(now.X, plot.Position.Y), new(now.X, plot.End.Y), PlaygroundTheme.Muted with { A = 0.55f }, 1, 4);
        DrawCircle(now, 6, PlaygroundTheme.Stage);
        DrawArc(now, 6, 0, Mathf.Tau, 32, PlaygroundTheme.Mint, 2, true);
        DrawString(font, new(plot.Position.X, Size.Y - 10), "0 s", fontSize: 13, modulate: PlaygroundTheme.Muted);
        var duration = Settings.Duration.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s";
        DrawString(font, new(plot.End.X - font.GetStringSize(duration, fontSize: 13).X, Size.Y - 10), duration, fontSize: 13, modulate: PlaygroundTheme.Muted);
    }
}

public partial class MotionPreview : Control
{
    public EasingSettings Settings { get; set; } = new();
    public float Reach { get; set; } = 0.5f;
    public float Weight { get; set; }

    public MotionPreview()
    {
        CustomMinimumSize = new(140, 38);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        float X(float value) => 12 + (Size.X - 24) * (0.5f + (value - 0.5f) * 0.5f / Reach);
        var y = Size.Y / 2;
        DrawDashedLine(new(X(0), y), new(X(1), y), PlaygroundTheme.Border, 2, 3);
        foreach (var value in new[] { 0f, 1f }) DrawLine(new(X(value), y - 6), new(X(value), y + 6), PlaygroundTheme.Muted, 1);
        for (var i = 0; i <= 10; i++)
            DrawArc(new(X(Settings.Evaluate(i / 10f)), y), 4, 0, Mathf.Tau, 24, PlaygroundTheme.Mint with { A = 0.5f }, 1, true);
        DrawCircle(new(X(Weight), y), 9, PlaygroundTheme.Mint);
    }
}
