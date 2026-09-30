// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

#nullable enable
using System;

namespace tweens.gd;

/// <summary>A duration in seconds, accepting numeric seconds or a <see cref="TimeSpan"/>.</summary>
/// <remarks>Values are validated when playback starts, allowing signed timing adjustments.</remarks>
public readonly record struct Duration(double Seconds)
{
    public static implicit operator Duration(TimeSpan value) => new(value.TotalSeconds);
    public static implicit operator Duration(float seconds) => new(seconds);
    public static implicit operator Duration(double seconds) => new(seconds);
    public static implicit operator double(Duration duration) => duration.Seconds;
}
