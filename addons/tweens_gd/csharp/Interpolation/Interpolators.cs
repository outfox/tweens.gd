// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Godot;

namespace tweens.gd;

/// <summary>Value interpolation preserving easing overshoot; integer results saturate at Int32 limits.</summary>
public static class Interpolators
{
    /// <summary>Rounds to nearest with ties away from zero; overshoot saturates at Int32 limits.</summary>
    public static int Int(int from, int to, float weight)
    {
        if (!float.IsFinite(weight)) throw new ArgumentOutOfRangeException(nameof(weight));
        var value = from + ((double)to - from) * weight;
        return (int)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), int.MinValue, int.MaxValue);
    }

    /// <summary>Linear Float interpolation. Weight may overshoot [0, 1].</summary>
    public static float Float(float from, float to, float weight) => from + (to - from) * weight;
    /// <summary>Linear Double interpolation. Weight may overshoot [0, 1].</summary>
    public static double Double(double from, double to, float weight) => from + (to - from) * weight;
    /// <summary>Linear Vector2 interpolation. Weight may overshoot [0, 1].</summary>
    public static Vector2 Vector2(Vector2 from, Vector2 to, float weight) => from.Lerp(to, weight);
    /// <summary>Linear Vector3 interpolation. Weight may overshoot [0, 1].</summary>
    public static Vector3 Vector3(Vector3 from, Vector3 to, float weight) => from.Lerp(to, weight);
    /// <summary>Linear Vector4 interpolation. Weight may overshoot [0, 1].</summary>
    public static Vector4 Vector4(Vector4 from, Vector4 to, float weight) => from.Lerp(to, weight);
    /// <summary>Linear Color interpolation. Weight may overshoot [0, 1].</summary>
    public static Color Color(Color from, Color to, float weight) => from.Lerp(to, weight);
    /// <summary>Linear Rect2 interpolation. Weight may overshoot [0, 1].</summary>
    public static Rect2 Rect2(Rect2 from, Rect2 to, float weight)
        => new(from.Position.Lerp(to.Position, weight), from.Size.Lerp(to.Size, weight));
    /// <summary>Spherical quaternion interpolation. Weight may overshoot [0, 1].</summary>
    public static Quaternion Quaternion(Quaternion from, Quaternion to, float weight)
    {
        if (from.LengthSquared() == 0 || to.LengthSquared() == 0)
            throw new ArgumentException("Quaternion endpoints must be nonzero.");
        return from.Normalized().Slerp(to.Normalized(), weight).Normalized();
    }
}
