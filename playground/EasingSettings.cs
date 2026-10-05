// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using tweens.gd;

namespace playground;

public readonly record struct EaseFamily(string Name, EaseType In, EaseType Out);

/// <summary>The website composer's settings, evaluated by tweens.gd itself.</summary>
public sealed record EasingSettings
{
    // Recipes omit the library's defaults; the playground starts with a narrower blend.
    private const double LibraryDefaultBlend = 0.2;

    // Catalog positions are the stable IDs used by the curve pickers.
    public static IReadOnlyList<EaseFamily> Families { get; } = Array.AsReadOnly(new EaseFamily[]
    {
        new("None", In.None, Out.None),
        new("Linear", In.Linear, Out.Linear),
        new("Sine", In.Sine, Out.Sine),
        new("Quad", In.Quad, Out.Quad),
        new("Cubic", In.Cubic, Out.Cubic),
        new("Quart", In.Quart, Out.Quart),
        new("Quint", In.Quint, Out.Quint),
        new("Expo", In.Expo, Out.Expo),
        new("Circ", In.Circ, Out.Circ),
        new("Back", In.Back, Out.Back),
        new("Elastic", In.Elastic, Out.Elastic),
        new("Bounce", In.Bounce, Out.Bounce),
        new("SmoothStep", In.SmoothStep, Out.SmoothStep),
        new("SmootherStep", In.SmootherStep, Out.SmootherStep),

        new("Back10", In.Back10, Out.Back10),
        new("Back20", In.Back20, Out.Back20),
        new("Back30", In.Back30, Out.Back30),
        new("Back40", In.Back40, Out.Back40),
        new("Back50", In.Back50, Out.Back50),

        new("Elastic10", In.Elastic10, Out.Elastic10),
        new("Elastic20", In.Elastic20, Out.Elastic20),
        new("Elastic30", In.Elastic30, Out.Elastic30),
        new("Elastic40", In.Elastic40, Out.Elastic40),
        new("Elastic50", In.Elastic50, Out.Elastic50),

        new("Bounce10", In.Bounce10, Out.Bounce10),
        new("Bounce20", In.Bounce20, Out.Bounce20),
        new("Bounce30", In.Bounce30, Out.Bounce30),
        new("Bounce40", In.Bounce40, Out.Bounce40),
        new("Bounce50", In.Bounce50, Out.Bounce50),

        new("Jump", In.Jump, Out.Jump),
        new("Jump10", In.Jump10, Out.Jump10),
        new("Jump20", In.Jump20, Out.Jump20),
        new("Jump30", In.Jump30, Out.Jump30),
        new("Jump40", In.Jump40, Out.Jump40),
        new("Jump50", In.Jump50, Out.Jump50),
    });

    public EaseFamily Entry { get; init; } = Families.Single(family => family.Name == "Elastic");
    public EaseFamily Exit { get; init; } = Families.Single(family => family.Name == "Elastic");
    public double Duration { get; init; } = 1.5;
    public double Skew { get; init; } = 0.5;
    public double Blend { get; init; } = 0.1;
    public BlendType BlendType { get; init; } = BlendType.Makima;

    public EaseType Ease => Entry.In | Exit.Out;
    public bool HasBothLegs => Entry.In != In.None && Exit.Out != Out.None;
    public bool CanBlend => HasBothLegs && Entry.In != Exit.In && Skew > 0 && Skew < 1;

    public float Evaluate(float progress) => Easing.Evaluate(Ease, progress, BlendType, Blend, Skew);

    public float EvaluateEntry(float progress) => Easing.Evaluate(
        Exit.Out == Out.None ? Entry.In : Entry.In | Entry.Out, progress, skew: Skew);

    public float EvaluateExit(float progress) => Easing.Evaluate(
        Entry.In == In.None ? Exit.Out : Exit.In | Exit.Out, progress, skew: Skew);

    public string Recipe(bool gdscript = false) => gdscript ? GDScriptRecipe() : CSharpRecipe();

    private string EaseExpression(bool gdscript = false)
    {
        string Member(string type, string family) => $"{type}.{(gdscript ? GDScriptConstant(family) : family)}";

        if (Entry.In == Exit.In && Entry.In != In.None)
            return Member("InOut", Entry.Name);

        if (Entry.In == In.None)
            return Member("Out", Exit.Name);

        if (Exit.Out == Out.None)
            return Member("In", Entry.Name);

        return Member("In", Entry.Name) + " | " + Member("Out", Exit.Name);
    }

    private string CSharpRecipe()
    {
        var lines = new List<string>
        {
            "var move = new Tweens.Position2DX",
            "{",
            $"    Duration = {Number(Duration, "0.0")},",
            $"    Ease = {EaseExpression()},",
        };

        if (HasBothLegs && Skew != 0.5)
            lines.Add($"    Skew = {Number(Skew, "0.00")},");

        if (CanBlend && BlendType != BlendType.Makima)
            lines.Add($"    BlendType = BlendType.{BlendType},");

        if (CanBlend && Blend != LibraryDefaultBlend)
            lines.Add($"    Blend = {Number(Blend, "0.00#")},");

        lines.Add("};");
        lines.Add("sprite.Tween(move with { To = 300 });");
        return string.Join('\n', lines);
    }

    private string GDScriptRecipe()
    {
        var lines = new List<string>
        {
            $"var move = Tweens.position_2d_x(300.0, {Number(Duration, "0.0")}, {EaseExpression(gdscript: true)})",
        };

        if (HasBothLegs && Skew != 0.5)
            lines.Add($"move.skew = {Number(Skew, "0.00")}");

        if (CanBlend && BlendType != BlendType.Makima)
            lines.Add($"move.blend_type = BlendType.{GDScriptConstant(BlendType.ToString())}");

        if (CanBlend && Blend != LibraryDefaultBlend)
            lines.Add($"move.blend = {Number(Blend, "0.00#")}");

        lines.Add("Tweens.play(sprite, move)");
        return string.Join('\n', lines);
    }

    private static string GDScriptConstant(string name) => name switch
    {
        "SmoothStep" => "SMOOTH_STEP",
        "SmootherStep" => "SMOOTHER_STEP",
        _ => name.ToUpperInvariant(),
    };

    private static string Number(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
