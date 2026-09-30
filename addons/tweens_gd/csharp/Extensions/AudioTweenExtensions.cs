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

public static partial class TweenExtensions
{
    /// <summary>Starts a AudioVolumeDbTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenVolumeDb(this AudioStreamPlayer target,
        double to, Duration duration, Action<AudioVolumeDbTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeDbTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeDbTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenVolumeDb(this AudioStreamPlayer target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeDbTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioVolumeLinearTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenVolumeLinear(this AudioStreamPlayer target,
        double to, Duration duration, Action<AudioVolumeLinearTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeLinearTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeLinearTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenVolumeLinear(this AudioStreamPlayer target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeLinearTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioPitchScaleTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenPitchScale(this AudioStreamPlayer target,
        double to, Duration duration, Action<AudioPitchScaleTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioPitchScaleTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioPitchScaleTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer, float> TweenPitchScale(this AudioStreamPlayer target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioPitchScaleTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioVolumeDb2DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenVolumeDb(this AudioStreamPlayer2D target,
        double to, Duration duration, Action<AudioVolumeDb2DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeDb2DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeDb2DTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenVolumeDb(this AudioStreamPlayer2D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeDb2DTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioVolumeLinear2DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenVolumeLinear(this AudioStreamPlayer2D target,
        double to, Duration duration, Action<AudioVolumeLinear2DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeLinear2DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeLinear2DTween.
    /// Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenVolumeLinear(this AudioStreamPlayer2D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeLinear2DTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioPitchScale2DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenPitchScale(this AudioStreamPlayer2D target,
        double to, Duration duration, Action<AudioPitchScale2DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioPitchScale2DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioPitchScale2DTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer2D, float> TweenPitchScale(this AudioStreamPlayer2D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioPitchScale2DTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioVolumeDb3DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenVolumeDb(this AudioStreamPlayer3D target,
        double to, Duration duration, Action<AudioVolumeDb3DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeDb3DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeDb3DTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenVolumeDb(this AudioStreamPlayer3D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeDb3DTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioVolumeLinear3DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenVolumeLinear(this AudioStreamPlayer3D target,
        double to, Duration duration, Action<AudioVolumeLinear3DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioVolumeLinear3DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioVolumeLinear3DTween.
    /// Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenVolumeLinear(this AudioStreamPlayer3D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioVolumeLinear3DTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a AudioPitchScale3DTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenPitchScale(this AudioStreamPlayer3D target,
        double to, Duration duration, Action<AudioPitchScale3DTween>? configure = null)
        => target.Tween(ConfigureDefinition(new AudioPitchScale3DTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a AudioPitchScale3DTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<AudioStreamPlayer3D, float> TweenPitchScale(this AudioStreamPlayer3D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new AudioPitchScale3DTween { To = (float)to, Duration = duration }, options));
}
