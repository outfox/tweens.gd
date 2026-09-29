// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
namespace tweens.gd;

/// <summary>How different In/Out families meet inside the centered transition window.</summary>
public enum BlendType
{
    /// <summary>Two cubic Hermite segments preserve the midpoint and join the legs with continuous velocity.</summary>
    Hermite,
    /// <summary>Crossfade the families' conventional InOut curves with a smooth weight.</summary>
    SmoothStep,
    /// <summary>Crossfade with a linear weight; velocity may jump at the window edges.</summary>
    Linear,
}
