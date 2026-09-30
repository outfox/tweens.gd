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
    /// <summary>Starts a SpringArm3DSpringLengthTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<SpringArm3D, float> TweenSpringLength(this SpringArm3D target,
        double to, Duration duration, Action<SpringArm3DSpringLengthTween>? configure = null)
        => target.Tween(ConfigureDefinition(new SpringArm3DSpringLengthTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a SpringArm3DSpringLengthTween.
    /// Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<SpringArm3D, float> TweenSpringLength(this SpringArm3D target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new SpringArm3DSpringLengthTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a DecalModulateTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, Color> TweenModulate(this Decal target,
        Color to, Duration duration, Action<DecalModulateTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalModulateTween { To = to, Duration = duration }, configure));

    /// <summary>Starts a DecalModulateTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, Color> TweenModulate(this Decal target,
        Color to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalModulateTween { To = to, Duration = duration }, options));

    /// <summary>Starts a DecalModulateAlphaTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, float> TweenModulateAlpha(this Decal target,
        double to, Duration duration, Action<DecalModulateAlphaTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalModulateAlphaTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a DecalModulateAlphaTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, float> TweenModulateAlpha(this Decal target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalModulateAlphaTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a DecalSizeTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, Vector3> TweenSize(this Decal target,
        Vector3 to, Duration duration, Action<DecalSizeTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalSizeTween { To = to, Duration = duration }, configure));

    /// <summary>Starts a DecalSizeTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, Vector3> TweenSize(this Decal target,
        Vector3 to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalSizeTween { To = to, Duration = duration }, options));

    /// <summary>Starts a DecalSizeXTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, float> TweenSizeX(this Decal target,
        double to, Duration duration, Action<DecalSizeXTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalSizeXTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a DecalSizeXTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, float> TweenSizeX(this Decal target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalSizeXTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a DecalSizeYTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, float> TweenSizeY(this Decal target,
        double to, Duration duration, Action<DecalSizeYTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalSizeYTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a DecalSizeYTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, float> TweenSizeY(this Decal target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalSizeYTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a DecalSizeZTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, float> TweenSizeZ(this Decal target,
        double to, Duration duration, Action<DecalSizeZTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalSizeZTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a DecalSizeZTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, float> TweenSizeZ(this Decal target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalSizeZTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a DecalEmissionEnergyTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<Decal, float> TweenEmissionEnergy(this Decal target,
        double to, Duration duration, Action<DecalEmissionEnergyTween>? configure = null)
        => target.Tween(ConfigureDefinition(new DecalEmissionEnergyTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a DecalEmissionEnergyTween.
    /// Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<Decal, float> TweenEmissionEnergy(this Decal target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new DecalEmissionEnergyTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a FogVolumeSizeTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<FogVolume, Vector3> TweenSize(this FogVolume target,
        Vector3 to, Duration duration, Action<FogVolumeSizeTween>? configure = null)
        => target.Tween(ConfigureDefinition(new FogVolumeSizeTween { To = to, Duration = duration }, configure));

    /// <summary>Starts a FogVolumeSizeTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<FogVolume, Vector3> TweenSize(this FogVolume target,
        Vector3 to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new FogVolumeSizeTween { To = to, Duration = duration }, options));

    /// <summary>Starts a FogVolumeSizeXTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeX(this FogVolume target,
        double to, Duration duration, Action<FogVolumeSizeXTween>? configure = null)
        => target.Tween(ConfigureDefinition(new FogVolumeSizeXTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a FogVolumeSizeXTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeX(this FogVolume target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new FogVolumeSizeXTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a FogVolumeSizeYTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeY(this FogVolume target,
        double to, Duration duration, Action<FogVolumeSizeYTween>? configure = null)
        => target.Tween(ConfigureDefinition(new FogVolumeSizeYTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a FogVolumeSizeYTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeY(this FogVolume target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new FogVolumeSizeYTween { To = (float)to, Duration = duration }, options));

    /// <summary>Starts a FogVolumeSizeZTween. Configure runs before snapshotting and can override any definition option.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeZ(this FogVolume target,
        double to, Duration duration, Action<FogVolumeSizeZTween>? configure = null)
        => target.Tween(ConfigureDefinition(new FogVolumeSizeZTween { To = (float)to, Duration = duration }, configure));

    /// <summary>Starts a FogVolumeSizeZTween. Copies the options; the explicit duration takes precedence.</summary>
    public static TweenInstance<FogVolume, float> TweenSizeZ(this FogVolume target,
        double to, Duration duration, TweenOptions options)
        => target.Tween(ApplyOptions(new FogVolumeSizeZTween { To = (float)to, Duration = duration }, options));
}
