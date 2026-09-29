// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

namespace tweens.gd;

/// <summary>In curves. Combine one In and one Out with |; a single curve runs on its own.</summary>
public static class In
{
    /// <summary>Omit this leg; the other curve runs over the entire duration.</summary>
    public const EaseType None = 0;
    public const EaseType Linear = (EaseType)(1L << 8);
    public const EaseType Sine = (EaseType)(1L << 9);
    public const EaseType Quad = (EaseType)(1L << 10);
    public const EaseType Cubic = (EaseType)(1L << 11);
    public const EaseType Quart = (EaseType)(1L << 12);
    public const EaseType Quint = (EaseType)(1L << 13);
    public const EaseType Expo = (EaseType)(1L << 14);
    public const EaseType Circ = (EaseType)(1L << 15);
    public const EaseType Back = (EaseType)(1L << 16);
    public const EaseType Elastic = (EaseType)(1L << 17);
    public const EaseType Bounce = (EaseType)(1L << 18);
    public const EaseType SmoothStep = (EaseType)(1L << 19);
    public const EaseType SmootherStep = (EaseType)(1L << 20);
    /// <summary>Alias for the default 10% Back curve.</summary>
    public const EaseType Back10 = Back;
    /// <summary>Alias for the default 10% Elastic curve.</summary>
    public const EaseType Elastic10 = Elastic;
    public const EaseType Back20 = (EaseType)(1L << 34);
    public const EaseType Back30 = (EaseType)(1L << 35);
    public const EaseType Back40 = (EaseType)(1L << 36);
    public const EaseType Back50 = (EaseType)(1L << 37);
    public const EaseType Elastic20 = (EaseType)(1L << 38);
    public const EaseType Elastic30 = (EaseType)(1L << 39);
    public const EaseType Elastic40 = (EaseType)(1L << 40);
    public const EaseType Elastic50 = (EaseType)(1L << 41);
    /// <summary>Alias for the default 10% first-rebound depth, relative to the full tween range.</summary>
    public const EaseType Bounce10 = Bounce;
    public const EaseType Bounce20 = (EaseType)(1L << 50);
    public const EaseType Bounce30 = (EaseType)(1L << 51);
    public const EaseType Bounce40 = (EaseType)(1L << 52);
    public const EaseType Bounce50 = (EaseType)(1L << 53);
    /// <summary>Mirrored Jump motion; the largest dip is 10% of the full tween range below the start.</summary>
    public const EaseType Jump = (EaseType)((1L << 58) | (1L << 59));
    public const EaseType Jump10 = Jump;
    public const EaseType Jump20 = (EaseType)((1L << 58) | (1L << 60));
    public const EaseType Jump30 = (EaseType)((1L << 59) | (1L << 60));
    public const EaseType Jump40 = (EaseType)((1L << 58) | (1L << 61));
    public const EaseType Jump50 = (EaseType)((1L << 59) | (1L << 61));
}

/// <summary>Out curves. Combine one In and one Out with |; a single curve runs on its own.</summary>
public static class Out
{
    /// <summary>Omit this leg; the other curve runs over the entire duration.</summary>
    public const EaseType None = 0;
    public const EaseType Linear = (EaseType)(1L << 21);
    public const EaseType Sine = (EaseType)(1L << 22);
    public const EaseType Quad = (EaseType)(1L << 23);
    public const EaseType Cubic = (EaseType)(1L << 24);
    public const EaseType Quart = (EaseType)(1L << 25);
    public const EaseType Quint = (EaseType)(1L << 26);
    public const EaseType Expo = (EaseType)(1L << 27);
    public const EaseType Circ = (EaseType)(1L << 28);
    public const EaseType Back = (EaseType)(1L << 29);
    public const EaseType Elastic = (EaseType)(1L << 30);
    public const EaseType Bounce = (EaseType)(1L << 31);
    public const EaseType SmoothStep = (EaseType)(1L << 32);
    public const EaseType SmootherStep = (EaseType)(1L << 33);
    /// <summary>Alias for the default 10% Back curve.</summary>
    public const EaseType Back10 = Back;
    /// <summary>Alias for the default 10% Elastic curve.</summary>
    public const EaseType Elastic10 = Elastic;
    public const EaseType Back20 = (EaseType)(1L << 42);
    public const EaseType Back30 = (EaseType)(1L << 43);
    public const EaseType Back40 = (EaseType)(1L << 44);
    public const EaseType Back50 = (EaseType)(1L << 45);
    public const EaseType Elastic20 = (EaseType)(1L << 46);
    public const EaseType Elastic30 = (EaseType)(1L << 47);
    public const EaseType Elastic40 = (EaseType)(1L << 48);
    public const EaseType Elastic50 = (EaseType)(1L << 49);
    /// <summary>Alias for the default 10% first-rebound depth, relative to the full tween range.</summary>
    public const EaseType Bounce10 = Bounce;
    public const EaseType Bounce20 = (EaseType)(1L << 54);
    public const EaseType Bounce30 = (EaseType)(1L << 55);
    public const EaseType Bounce40 = (EaseType)(1L << 56);
    public const EaseType Bounce50 = (EaseType)(1L << 57);
    /// <summary>Three peaks above the target; the first is 10% of the full tween range.</summary>
    public const EaseType Jump = (EaseType)((1L << 62) | (1L << 63));
    public const EaseType Jump10 = Jump;
    public const EaseType Jump20 = (EaseType)((1L << 62) | (1L << 6));
    public const EaseType Jump30 = (EaseType)((1L << 63) | (1L << 6));
    public const EaseType Jump40 = (EaseType)((1L << 62) | (1L << 7));
    public const EaseType Jump50 = (EaseType)((1L << 63) | (1L << 7));
}

/// <summary>Matching half-duration legs. Back/Elastic/Jump overshoot and Bounce first-rebound depth are percentages of the full tween range.</summary>
public static class InOut
{
    public const EaseType Linear = In.Linear | Out.Linear;
    public const EaseType Sine = In.Sine | Out.Sine;
    public const EaseType Quad = In.Quad | Out.Quad;
    public const EaseType Cubic = In.Cubic | Out.Cubic;
    public const EaseType Quart = In.Quart | Out.Quart;
    public const EaseType Quint = In.Quint | Out.Quint;
    public const EaseType Expo = In.Expo | Out.Expo;
    public const EaseType Circ = In.Circ | Out.Circ;
    public const EaseType Back = In.Back | Out.Back;
    public const EaseType Elastic = In.Elastic | Out.Elastic;
    public const EaseType Bounce = In.Bounce | Out.Bounce;
    public const EaseType SmoothStep = In.SmoothStep | Out.SmoothStep;
    public const EaseType SmootherStep = In.SmootherStep | Out.SmootherStep;
    public const EaseType Back10 = Back;
    public const EaseType Elastic10 = Elastic;
    public const EaseType Back20 = In.Back20 | Out.Back20;
    public const EaseType Back30 = In.Back30 | Out.Back30;
    public const EaseType Back40 = In.Back40 | Out.Back40;
    public const EaseType Back50 = In.Back50 | Out.Back50;
    public const EaseType Elastic20 = In.Elastic20 | Out.Elastic20;
    public const EaseType Elastic30 = In.Elastic30 | Out.Elastic30;
    public const EaseType Elastic40 = In.Elastic40 | Out.Elastic40;
    public const EaseType Elastic50 = In.Elastic50 | Out.Elastic50;
    /// <summary>Alias for the default 10% first-rebound depth, relative to the full tween range.</summary>
    public const EaseType Bounce10 = Bounce;
    public const EaseType Bounce20 = In.Bounce20 | Out.Bounce20;
    public const EaseType Bounce30 = In.Bounce30 | Out.Bounce30;
    public const EaseType Bounce40 = In.Bounce40 | Out.Bounce40;
    public const EaseType Bounce50 = In.Bounce50 | Out.Bounce50;
    /// <summary>Matching Jump legs with 10% peak overshoot relative to the full tween range.</summary>
    public const EaseType Jump = In.Jump | Out.Jump;
    public const EaseType Jump10 = Jump;
    public const EaseType Jump20 = In.Jump20 | Out.Jump20;
    public const EaseType Jump30 = In.Jump30 | Out.Jump30;
    public const EaseType Jump40 = In.Jump40 | Out.Jump40;
    public const EaseType Jump50 = In.Jump50 | Out.Jump50;
}

