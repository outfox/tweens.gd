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
}

/// <summary>Matching half-duration legs. Reproduces the conventional InOut curve exactly.</summary>
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
}

