// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using Godot;

namespace playground;

/// <summary>Draws the moving ball and sampled ghost positions, also visible in the editor.</summary>
[Tool]
public partial class MotionPreview : Control
{
    public EasingSettings Settings { get; private set; } = new();
    public float Reach { get; private set; } = 0.5f;
    public float Weight { get; private set; }

    public override void _Ready()
    {
        Resized += QueueRedraw;

        // Runtime bounds come from the graph; the editor also needs a fitted initial preview.
        if (Engine.IsEditorHint())
        {
            for (var i = 0; i <= 1000; i++)
                Reach = Math.Max(Reach, Math.Abs(Settings.Evaluate(i / 1000f) - 0.5f));
        }
    }

    public void Refresh(EasingSettings settings, float reach)
    {
        Settings = settings;
        Reach = reach;
        QueueRedraw();
    }

    public void SetWeight(float weight)
    {
        Weight = weight;
        QueueRedraw();
    }

    private float LaneX(float value) => 12 + (Size.X - 24) * (0.5f + (value - 0.5f) * 0.5f / Reach);

    public override void _Draw()
    {
        var y = Size.Y / 2;
        var gridColor = GetThemeColor("grid", "EasingGraph");
        var markerColor = GetThemeColor("labels", "EasingGraph");
        var ballColor = GetThemeColor("result", "EasingGraph");

        DrawDashedLine(new(LaneX(0), y), new(LaneX(1), y), gridColor, 2, 3);
        foreach (var endpoint in new[] { 0f, 1f })
            DrawLine(new(LaneX(endpoint), y - 6), new(LaneX(endpoint), y + 6), markerColor, 1);

        for (var i = 0; i <= 10; i++)
        {
            var ghostPosition = new Vector2(LaneX(Settings.Evaluate(i / 10f)), y);
            DrawArc(ghostPosition, 4, 0, Mathf.Tau, 24, ballColor with { A = 0.5f }, 1, antialiased: true);
        }

        DrawCircle(new(LaneX(Weight), y), 9, ballColor);
    }
}
