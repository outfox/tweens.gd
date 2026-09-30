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
    /// <summary>Starts a ModulateTween and returns its playback handle.</summary>
    /// <remarks>Configure runs before snapshotting and can override any definition option, including duration.</remarks>
    public static TweenInstance<CanvasItem, Color> TweenModulate(this CanvasItem target,
        Color to, Duration duration, Action<ModulateTween>? configure = null)
        => target.Tween(ConfigureDefinition(new ModulateTween { To = to, Duration = duration }, configure));

    /// <summary>Starts a ModulateTween and returns its playback handle.</summary>
    /// <remarks>Copies the options; the explicit duration takes precedence.</remarks>
    public static TweenInstance<CanvasItem, Color> TweenModulate(this CanvasItem target,
        Color to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new ModulateTween { To = to, Duration = duration }, options));

    /// <summary>Starts a ModulateAlphaTween and returns its playback handle.</summary>
    /// <remarks>Configure runs before snapshotting and can override any definition option, including duration.</remarks>
    public static TweenInstance<CanvasItem, float> TweenModulateAlpha(this CanvasItem target,
        double to, Duration duration, Action<ModulateAlphaTween>? configure = null)
        => target.Tween(ConfigureDefinition(new ModulateAlphaTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a ModulateAlphaTween and returns its playback handle.</summary>
    /// <remarks>Copies the options; the explicit duration takes precedence.</remarks>
    public static TweenInstance<CanvasItem, float> TweenModulateAlpha(this CanvasItem target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new ModulateAlphaTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a SelfModulateTween and returns its playback handle.</summary>
    /// <remarks>Configure runs before snapshotting and can override any definition option, including duration.</remarks>
    public static TweenInstance<CanvasItem, Color> TweenSelfModulate(this CanvasItem target,
        Color to, Duration duration, Action<SelfModulateTween>? configure = null)
        => target.Tween(ConfigureDefinition(new SelfModulateTween { To = to, Duration = duration }, configure));

    /// <summary>Starts a SelfModulateTween and returns its playback handle.</summary>
    /// <remarks>Copies the options; the explicit duration takes precedence.</remarks>
    public static TweenInstance<CanvasItem, Color> TweenSelfModulate(this CanvasItem target,
        Color to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new SelfModulateTween { To = to, Duration = duration }, options));

    /// <summary>Starts a SelfModulateAlphaTween and returns its playback handle.</summary>
    /// <remarks>Configure runs before snapshotting and can override any definition option, including duration.</remarks>
    public static TweenInstance<CanvasItem, float> TweenSelfModulateAlpha(this CanvasItem target,
        double to, Duration duration, Action<SelfModulateAlphaTween>? configure = null)
        => target.Tween(ConfigureDefinition(new SelfModulateAlphaTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a SelfModulateAlphaTween and returns its playback handle.</summary>
    /// <remarks>Copies the options; the explicit duration takes precedence.</remarks>
    public static TweenInstance<CanvasItem, float> TweenSelfModulateAlpha(this CanvasItem target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new SelfModulateAlphaTween { To = (float)to, Duration = duration }, options));
}
