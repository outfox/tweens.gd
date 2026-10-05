// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Globalization;
using Godot;

namespace playground;

/// <summary>Draws the sampled curves inside the scene's graph panel, including an editor preview.</summary>
[Tool]
public partial class EasingGraph : Control
{
    private const int CurveSegments = 240;
    private const int BoundsSamples = 1000;
    private const double GridIntervalSeconds = 0.25;

    public EasingSettings Settings { get; private set; } = new();
    public float Progress { get; private set; }
    public float Weight { get; private set; }
    public float Extent { get; private set; } = 0.7f;
    public float Reach { get; private set; } = 0.5f;

    private readonly Vector2[] resultSamples = new Vector2[CurveSegments + 1];
    private readonly Vector2[] entrySamples = new Vector2[CurveSegments + 1];
    private readonly Vector2[] exitSamples = new Vector2[CurveSegments + 1];

    public override void _Ready()
    {
        Resized += QueueRedraw;
        Refresh(Settings);
    }

    public void Refresh(EasingSettings settings)
    {
        Settings = settings;
        UpdateBounds();
        SampleCurves();
        QueueRedraw();
    }

    public void SetPreview(float progress, float weight)
    {
        Progress = progress;
        Weight = weight;
        QueueRedraw();
    }

    private void UpdateBounds()
    {
        float extent = 0.5f;
        float reach = 0.5f;

        for (var i = 0; i <= BoundsSamples; i++)
        {
            var progress = (float)i / BoundsSamples;
            reach = Math.Max(reach, Math.Abs(Settings.Evaluate(progress) - 0.5f));

            if (Settings.Entry.In != 0 && (Settings.Exit.Out == 0 || progress <= Settings.Skew))
                extent = Math.Max(extent, Math.Abs(Settings.EvaluateEntry(progress) - 0.5f));

            if (Settings.Exit.Out != 0 && (Settings.Entry.In == 0 || progress >= Settings.Skew))
                extent = Math.Max(extent, Math.Abs(Settings.EvaluateExit(progress) - 0.5f));
        }

        Reach = reach;
        Extent = Math.Max(0.7f, Math.Max(extent, reach) * 1.08f);
    }

    private void SampleCurves()
    {
        var entryEnd = Settings.Exit.Out == 0 ? 1 : (float)Settings.Skew;
        var exitStart = Settings.Entry.In == 0 ? 0 : (float)Settings.Skew;

        for (var i = 0; i <= CurveSegments; i++)
        {
            var progress = (float)i / CurveSegments;
            var entryProgress = progress * entryEnd;
            var exitProgress = exitStart + progress * (1 - exitStart);

            resultSamples[i] = new(progress, Settings.Evaluate(progress));
            entrySamples[i] = new(entryProgress, Settings.EvaluateEntry(entryProgress));
            exitSamples[i] = new(exitProgress, Settings.EvaluateExit(exitProgress));
        }
    }

    public override void _Draw()
    {
        var plot = new Rect2(42, 20, Math.Max(1, Size.X - 62), Math.Max(1, Size.Y - 56));
        DrawBlendWindow(plot);
        DrawGrid(plot);
        DrawCurves(plot);
        DrawTracer(plot);
        DrawTimeAxis(plot);
    }

    private Vector2 PlotPoint(Vector2 sample, Rect2 plot)
    {
        return new Vector2(
            plot.Position.X + plot.Size.X * sample.X,
            plot.GetCenter().Y - (sample.Y - 0.5f) / Extent * plot.Size.Y / 2);
    }

    private Color GraphColor(string name) => GetThemeColor(name, "EasingGraph");

    private void DrawBlendWindow(Rect2 plot)
    {
        if (!Settings.CanBlend)
            return;

        var halfWidth = (float)(Settings.Blend * Math.Min(Settings.Skew, 1 - Settings.Skew));
        var window = new Rect2(
            plot.Position.X + plot.Size.X * ((float)Settings.Skew - halfWidth),
            plot.Position.Y,
            plot.Size.X * 2 * halfWidth,
            plot.Size.Y);
        DrawRect(window, GraphColor("result") with { A = 0.08f });
    }

    private void DrawGrid(Rect2 plot)
    {
        var font = GetThemeDefaultFont();
        var gridColor = GraphColor("grid");
        var labelColor = GraphColor("labels");

        foreach (var value in new[] { 0f, 0.5f, 1f })
        {
            var y = PlotPoint(new(0, value), plot).Y;
            DrawDashedLine(new(plot.Position.X, y), new(plot.End.X, y), gridColor, 1, 4);
            DrawString(font, new(8, y + 5), value.ToString("0.#", CultureInfo.InvariantCulture),
                fontSize: 13, modulate: labelColor);
        }

        // Space ticks by elapsed time, so changing duration also changes the grid density.
        var lastTick = (int)Math.Floor(Settings.Duration / GridIntervalSeconds);
        for (var tick = 0; tick <= lastTick; tick++)
        {
            var progress = (float)(tick * GridIntervalSeconds / Settings.Duration);
            var x = PlotPoint(new(progress, 0), plot).X;
            DrawDashedLine(new(x, plot.Position.Y), new(x, plot.End.Y), gridColor, 1, 4);
        }
    }

    private void DrawCurves(Rect2 plot)
    {
        DrawCurve(resultSamples, plot, GraphColor("result").Darkened(0.5f), 14);

        if (Settings.Entry.In != 0)
            DrawCurve(entrySamples, plot, GraphColor("entry"), 2.5f, dashSegments: 4);

        if (Settings.Exit.Out != 0)
            DrawCurve(exitSamples, plot, GraphColor("exit"), 2.5f, dashSegments: 2);
    }

    private void DrawCurve(Vector2[] samples, Rect2 plot, Color color, float width, int dashSegments = 0)
    {
        var points = Array.ConvertAll(samples, sample => PlotPoint(sample, plot));
        if (dashSegments == 0)
        {
            DrawPolyline(points, color, width, antialiased: true);
            return;
        }

        for (var i = 1; i < points.Length; i++)
        {
            if ((i / dashSegments) % 2 == 0)
                DrawLine(points[i - 1], points[i], color, width, antialiased: true);
        }
    }

    private void DrawTracer(Rect2 plot)
    {
        var position = PlotPoint(new(Progress, Weight), plot);
        DrawDashedLine(new(position.X, plot.Position.Y), new(position.X, plot.End.Y),
            GraphColor("labels") with { A = 0.55f }, 1, 4);
        DrawCircle(position, 6, GraphColor("background"));
        DrawArc(position, 6, 0, Mathf.Tau, 32, GraphColor("result"), 2, antialiased: true);
    }

    private void DrawTimeAxis(Rect2 plot)
    {
        var font = GetThemeDefaultFont();
        var color = GraphColor("labels");
        var duration = Settings.Duration.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        var durationWidth = font.GetStringSize(duration, fontSize: 13).X;

        DrawString(font, new(plot.Position.X, Size.Y - 10), "0 s", fontSize: 13, modulate: color);
        DrawString(font, new(plot.End.X - durationWidth, Size.Y - 10), duration, fontSize: 13, modulate: color);
    }
}
