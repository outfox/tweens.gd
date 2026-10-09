// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

/// <summary>
/// Every built-in adapter animates the Godot property and component named here. Expectations are read through Godot's
/// properties and computed by hand, never through the adapter or the library's interpolators.
/// </summary>
[Collection<HeadlessCollection>]
public class PropertyContractTests(HeadlessFixture godot)
{
    private sealed record Contract(Type Adapter, string Property, string Component, double Low, double High);

    private static Contract Row<TAdapter>(string property, string component = "", double low = 0.2, double high = 0.6)
        => new(typeof(TAdapter), property, component, low, high);

    private static readonly Contract[] NodeContracts =
    [
        Row<Position2DTween>("Position"),
        Row<Position2DXTween>("Position", "X"),
        Row<Position2DYTween>("Position", "Y"),
        Row<GlobalPosition2DTween>("GlobalPosition"),
        Row<GlobalPosition2DXTween>("GlobalPosition", "X"),
        Row<GlobalPosition2DYTween>("GlobalPosition", "Y"),
        Row<Scale2DTween>("Scale"),
        Row<Scale2DXTween>("Scale", "X"),
        Row<Scale2DYTween>("Scale", "Y"),
        Row<Position3DTween>("Position"),
        Row<Position3DXTween>("Position", "X"),
        Row<Position3DYTween>("Position", "Y"),
        Row<Position3DZTween>("Position", "Z"),
        Row<GlobalPosition3DTween>("GlobalPosition"),
        Row<GlobalPosition3DXTween>("GlobalPosition", "X"),
        Row<GlobalPosition3DYTween>("GlobalPosition", "Y"),
        Row<GlobalPosition3DZTween>("GlobalPosition", "Z"),
        Row<Scale3DTween>("Scale"),
        Row<Scale3DXTween>("Scale", "X"),
        Row<Scale3DYTween>("Scale", "Y"),
        Row<Scale3DZTween>("Scale", "Z"),
        Row<Rotation3DTween>("Rotation"),
        Row<Rotation3DXTween>("Rotation", "X"),
        Row<Rotation3DYTween>("Rotation", "Y"),
        Row<Rotation3DZTween>("Rotation", "Z"),
        Row<GlobalRotation3DTween>("GlobalRotation"),
        Row<GlobalRotation3DXTween>("GlobalRotation", "X"),
        Row<GlobalRotation3DYTween>("GlobalRotation", "Y"),
        Row<GlobalRotation3DZTween>("GlobalRotation", "Z"),
        Row<Rotation2DTween>("Rotation"),
        Row<GlobalRotation2DTween>("GlobalRotation"),
        Row<Quaternion3DTween>("Quaternion"),
        Row<ControlPositionTween>("Position"),
        Row<ControlPositionXTween>("Position", "X"),
        Row<ControlPositionYTween>("Position", "Y"),
        Row<ControlGlobalPositionTween>("GlobalPosition"),
        Row<ControlGlobalPositionXTween>("GlobalPosition", "X"),
        Row<ControlGlobalPositionYTween>("GlobalPosition", "Y"),
        Row<ControlSizeTween>("Size"),
        Row<ControlSizeXTween>("Size", "X"),
        Row<ControlSizeYTween>("Size", "Y"),
        Row<ControlScaleTween>("Scale"),
        Row<ControlScaleXTween>("Scale", "X"),
        Row<ControlScaleYTween>("Scale", "Y"),
        Row<ControlRotationTween>("Rotation"),
        Row<RangeValueTween>("Value"),
        Row<ModulateTween>("Modulate"),
        Row<ModulateAlphaTween>("Modulate", "A"),
        Row<SelfModulateTween>("SelfModulate"),
        Row<SelfModulateAlphaTween>("SelfModulate", "A"),
        Row<AudioVolumeDbTween>("VolumeDb"),
        Row<AudioVolumeLinearTween>("VolumeLinear"),
        Row<AudioPitchScaleTween>("PitchScale"),
        Row<AudioVolumeDb2DTween>("VolumeDb"),
        Row<AudioVolumeLinear2DTween>("VolumeLinear"),
        Row<AudioPitchScale2DTween>("PitchScale"),
        Row<AudioVolumeDb3DTween>("VolumeDb"),
        Row<AudioVolumeLinear3DTween>("VolumeLinear"),
        Row<AudioPitchScale3DTween>("PitchScale"),
        Row<LightColor2DTween>("Color"),
        Row<LightEnergy2DTween>("Energy"),
        Row<LightColor3DTween>("LightColor"),
        Row<LightEnergy3DTween>("LightEnergy"),
        Row<OmniRangeTween>("OmniRange"),
        Row<SpotRangeTween>("SpotRange"),
        Row<SpotAngleTween>("SpotAngle"),
        Row<Skew2DTween>("Skew"),
        Row<GlobalSkew2DTween>("GlobalSkew"),
        Row<GlobalScale2DTween>("GlobalScale"),
        Row<GlobalScale2DXTween>("GlobalScale", "X"),
        Row<GlobalScale2DYTween>("GlobalScale", "Y"),
        Row<ControlPivotOffsetTween>("PivotOffset"),
        Row<ControlPivotOffsetXTween>("PivotOffset", "X"),
        Row<ControlPivotOffsetYTween>("PivotOffset", "Y"),
        Row<ControlPivotOffsetRatioTween>("PivotOffsetRatio"),
        Row<ControlPivotOffsetRatioXTween>("PivotOffsetRatio", "X"),
        Row<ControlPivotOffsetRatioYTween>("PivotOffsetRatio", "Y"),
        Row<ControlCustomMinimumSizeTween>("CustomMinimumSize"),
        Row<ControlCustomMinimumSizeXTween>("CustomMinimumSize", "X"),
        Row<ControlCustomMinimumSizeYTween>("CustomMinimumSize", "Y"),
        Row<ControlCustomMaximumSizeTween>("CustomMaximumSize"),
        Row<ControlCustomMaximumSizeXTween>("CustomMaximumSize", "X"),
        Row<ControlCustomMaximumSizeYTween>("CustomMaximumSize", "Y"),
        Row<ControlOffsetTransformPositionTween>("OffsetTransformPosition"),
        Row<ControlOffsetTransformPositionXTween>("OffsetTransformPosition", "X"),
        Row<ControlOffsetTransformPositionYTween>("OffsetTransformPosition", "Y"),
        Row<ControlOffsetTransformPositionRatioTween>("OffsetTransformPositionRatio"),
        Row<ControlOffsetTransformPositionRatioXTween>("OffsetTransformPositionRatio", "X"),
        Row<ControlOffsetTransformPositionRatioYTween>("OffsetTransformPositionRatio", "Y"),
        Row<ControlOffsetTransformScaleTween>("OffsetTransformScale"),
        Row<ControlOffsetTransformScaleXTween>("OffsetTransformScale", "X"),
        Row<ControlOffsetTransformScaleYTween>("OffsetTransformScale", "Y"),
        Row<ControlOffsetTransformPivotTween>("OffsetTransformPivot"),
        Row<ControlOffsetTransformPivotXTween>("OffsetTransformPivot", "X"),
        Row<ControlOffsetTransformPivotYTween>("OffsetTransformPivot", "Y"),
        Row<ControlOffsetTransformPivotRatioTween>("OffsetTransformPivotRatio"),
        Row<ControlOffsetTransformPivotRatioXTween>("OffsetTransformPivotRatio", "X"),
        Row<ControlOffsetTransformPivotRatioYTween>("OffsetTransformPivotRatio", "Y"),
        Row<ControlSizeFlagsStretchRatioTween>("SizeFlagsStretchRatio"),
        Row<ControlOffsetTransformRotationTween>("OffsetTransformRotation"),
        Row<ControlAnchorLeftTween>("AnchorLeft"),
        Row<ControlOffsetLeftTween>("OffsetLeft"),
        Row<ControlAnchorTopTween>("AnchorTop"),
        Row<ControlOffsetTopTween>("OffsetTop"),
        Row<ControlAnchorRightTween>("AnchorRight"),
        Row<ControlOffsetRightTween>("OffsetRight"),
        Row<ControlAnchorBottomTween>("AnchorBottom"),
        Row<ControlOffsetBottomTween>("OffsetBottom"),
        Row<Camera2DZoomTween>("Zoom"),
        Row<Camera2DZoomXTween>("Zoom", "X"),
        Row<Camera2DZoomYTween>("Zoom", "Y"),
        Row<Camera2DOffsetTween>("Offset"),
        Row<Camera2DOffsetXTween>("Offset", "X"),
        Row<Camera2DOffsetYTween>("Offset", "Y"),
        Row<Camera3DFovTween>("Fov", low: 20, high: 60),
        Row<Camera3DSizeTween>("Size"),
        Row<Camera3DHOffsetTween>("HOffset"),
        Row<Camera3DVOffsetTween>("VOffset"),
        Row<Camera3DNearTween>("Near"),
        Row<Camera3DFarTween>("Far"),
        Row<Camera3DFrustumOffsetTween>("FrustumOffset"),
        Row<Camera3DFrustumOffsetXTween>("FrustumOffset", "X"),
        Row<Camera3DFrustumOffsetYTween>("FrustumOffset", "Y"),
        Row<SpriteBase3DModulateTween>("Modulate"),
        Row<SpriteBase3DModulateAlphaTween>("Modulate", "A"),
        Row<SpriteBase3DOffsetTween>("Offset"),
        Row<SpriteBase3DOffsetXTween>("Offset", "X"),
        Row<SpriteBase3DOffsetYTween>("Offset", "Y"),
        Row<SpriteBase3DPixelSizeTween>("PixelSize"),
        Row<Label3DModulateTween>("Modulate"),
        Row<Label3DModulateAlphaTween>("Modulate", "A"),
        Row<Label3DOutlineModulateTween>("OutlineModulate"),
        Row<Label3DOutlineModulateAlphaTween>("OutlineModulate", "A"),
        Row<Label3DOffsetTween>("Offset"),
        Row<Label3DOffsetXTween>("Offset", "X"),
        Row<Label3DOffsetYTween>("Offset", "Y"),
        Row<Label3DPixelSizeTween>("PixelSize"),
        Row<GeometryInstance3DTransparencyTween>("Transparency"),
        Row<PathFollow2DProgressTween>("Progress"),
        Row<PathFollow2DProgressRatioTween>("ProgressRatio"),
        Row<PathFollow2DHOffsetTween>("HOffset"),
        Row<PathFollow2DVOffsetTween>("VOffset"),
        Row<PathFollow3DProgressTween>("Progress"),
        Row<PathFollow3DProgressRatioTween>("ProgressRatio"),
        Row<PathFollow3DHOffsetTween>("HOffset"),
        Row<PathFollow3DVOffsetTween>("VOffset"),
        Row<ColorRectColorTween>("Color"),
        Row<ColorRectColorAlphaTween>("Color", "A"),
        Row<LabelVisibleRatioTween>("VisibleRatio"),
        Row<LabelVisibleCharactersTween>("VisibleCharacters", low: 2, high: 6),
        Row<RichTextLabelVisibleRatioTween>("VisibleRatio"),
        Row<RichTextLabelVisibleCharactersTween>("VisibleCharacters", low: 2, high: 6),
        Row<TextureProgressBarTintUnderTween>("TintUnder"),
        Row<TextureProgressBarTintUnderAlphaTween>("TintUnder", "A"),
        Row<TextureProgressBarTintOverTween>("TintOver"),
        Row<TextureProgressBarTintOverAlphaTween>("TintOver", "A"),
        Row<TextureProgressBarTintProgressTween>("TintProgress"),
        Row<TextureProgressBarTintProgressAlphaTween>("TintProgress", "A"),
        Row<TextureProgressBarRadialInitialAngleTween>("RadialInitialAngle", low: 20, high: 60),
        Row<TextureProgressBarRadialFillDegreesTween>("RadialFillDegrees", low: 20, high: 60),
        Row<TextureProgressBarRadialCenterOffsetTween>("RadialCenterOffset"),
        Row<TextureProgressBarRadialCenterOffsetXTween>("RadialCenterOffset", "X"),
        Row<TextureProgressBarRadialCenterOffsetYTween>("RadialCenterOffset", "Y"),
        Row<TextureProgressBarTextureProgressOffsetTween>("TextureProgressOffset"),
        Row<TextureProgressBarTextureProgressOffsetXTween>("TextureProgressOffset", "X"),
        Row<TextureProgressBarTextureProgressOffsetYTween>("TextureProgressOffset", "Y"),
        Row<CanvasLayerOffsetTween>("Offset"),
        Row<CanvasLayerOffsetXTween>("Offset", "X"),
        Row<CanvasLayerOffsetYTween>("Offset", "Y"),
        Row<CanvasLayerScaleTween>("Scale"),
        Row<CanvasLayerScaleXTween>("Scale", "X"),
        Row<CanvasLayerScaleYTween>("Scale", "Y"),
        Row<CanvasLayerRotationTween>("Rotation"),
        Row<CanvasModulateColorTween>("Color"),
        Row<CanvasModulateColorAlphaTween>("Color", "A"),
        Row<Parallax2DScrollOffsetTween>("ScrollOffset"),
        Row<Parallax2DScrollOffsetXTween>("ScrollOffset", "X"),
        Row<Parallax2DScrollOffsetYTween>("ScrollOffset", "Y"),
        Row<Parallax2DScrollScaleTween>("ScrollScale"),
        Row<Parallax2DScrollScaleXTween>("ScrollScale", "X"),
        Row<Parallax2DScrollScaleYTween>("ScrollScale", "Y"),
        Row<Parallax2DAutoscrollTween>("Autoscroll"),
        Row<Parallax2DAutoscrollXTween>("Autoscroll", "X"),
        Row<Parallax2DAutoscrollYTween>("Autoscroll", "Y"),
        Row<Sprite2DOffsetTween>("Offset"),
        Row<Sprite2DOffsetXTween>("Offset", "X"),
        Row<Sprite2DOffsetYTween>("Offset", "Y"),
        Row<Sprite2DRegionRectTween>("RegionRect"),
        Row<Line2DWidthTween>("Width"),
        Row<Line2DDefaultColorTween>("DefaultColor"),
        Row<Line2DDefaultColorAlphaTween>("DefaultColor", "A"),
        Row<Polygon2DColorTween>("Color"),
        Row<Polygon2DColorAlphaTween>("Color", "A"),
        Row<Polygon2DOffsetTween>("Offset"),
        Row<Polygon2DOffsetXTween>("Offset", "X"),
        Row<Polygon2DOffsetYTween>("Offset", "Y"),
        Row<Polygon2DTextureOffsetTween>("TextureOffset"),
        Row<Polygon2DTextureOffsetXTween>("TextureOffset", "X"),
        Row<Polygon2DTextureOffsetYTween>("TextureOffset", "Y"),
        Row<Polygon2DTextureScaleTween>("TextureScale"),
        Row<Polygon2DTextureScaleXTween>("TextureScale", "X"),
        Row<Polygon2DTextureScaleYTween>("TextureScale", "Y"),
        Row<Polygon2DTextureRotationTween>("TextureRotation"),
        Row<PointLight2DTextureScaleTween>("TextureScale"),
        Row<PointLight2DHeightTween>("Height"),
        Row<PointLight2DOffsetTween>("Offset"),
        Row<PointLight2DOffsetXTween>("Offset", "X"),
        Row<PointLight2DOffsetYTween>("Offset", "Y"),
        Row<Light2DShadowColorTween>("ShadowColor"),
        Row<Light2DShadowColorAlphaTween>("ShadowColor", "A"),
        Row<Light3DLightTemperatureTween>("LightTemperature", low: 2000, high: 6000),
        Row<Light3DLightIndirectEnergyTween>("LightIndirectEnergy"),
        Row<Light3DLightVolumetricFogEnergyTween>("LightVolumetricFogEnergy"),
        Row<Light3DShadowOpacityTween>("ShadowOpacity"),
        Row<OmniLight3DOmniAttenuationTween>("OmniAttenuation"),
        Row<SpotLight3DSpotAttenuationTween>("SpotAttenuation"),
        Row<SpotLight3DSpotAngleAttenuationTween>("SpotAngleAttenuation"),
        Row<AudioStreamPlayer2DPanningStrengthTween>("PanningStrength"),
        Row<AudioStreamPlayer2DMaxDistanceTween>("MaxDistance"),
        Row<AudioStreamPlayer3DPanningStrengthTween>("PanningStrength"),
        Row<AudioStreamPlayer3DMaxDistanceTween>("MaxDistance"),
        Row<AudioStreamPlayer2DAttenuationTween>("Attenuation"),
        Row<AudioStreamPlayer3DUnitSizeTween>("UnitSize"),
        Row<AudioStreamPlayer3DEmissionAngleDegreesTween>("EmissionAngleDegrees"),
        Row<AudioStreamPlayer3DEmissionAngleFilterAttenuationDbTween>("EmissionAngleFilterAttenuationDb"),
        Row<AudioStreamPlayer3DAttenuationFilterCutoffHzTween>("AttenuationFilterCutoffHz", low: 1000, high: 5000),
        Row<AudioStreamPlayer3DAttenuationFilterDbTween>("AttenuationFilterDb"),
        Row<AnimatedSprite2DSpeedScaleTween>("SpeedScale"),
        Row<AnimatedSprite3DSpeedScaleTween>("SpeedScale"),
        Row<AnimationPlayerSpeedScaleTween>("SpeedScale"),
        Row<GpuParticles2DSpeedScaleTween>("SpeedScale"),
        Row<GpuParticles2DLifetimeTween>("Lifetime"),
        Row<GpuParticles2DExplosivenessTween>("Explosiveness"),
        Row<GpuParticles2DRandomnessTween>("Randomness"),
        Row<GpuParticles2DAmountRatioTween>("AmountRatio"),
        Row<GpuParticles3DSpeedScaleTween>("SpeedScale"),
        Row<GpuParticles3DLifetimeTween>("Lifetime"),
        Row<GpuParticles3DExplosivenessTween>("Explosiveness"),
        Row<GpuParticles3DRandomnessTween>("Randomness"),
        Row<GpuParticles3DAmountRatioTween>("AmountRatio"),
        Row<CpuParticles2DSpeedScaleTween>("SpeedScale"),
        Row<CpuParticles2DLifetimeTween>("Lifetime"),
        Row<CpuParticles2DExplosivenessTween>("Explosiveness"),
        Row<CpuParticles2DRandomnessTween>("Randomness"),
        Row<CpuParticles2DDirectionTween>("Direction"),
        Row<CpuParticles2DDirectionXTween>("Direction", "X"),
        Row<CpuParticles2DDirectionYTween>("Direction", "Y"),
        Row<CpuParticles2DGravityTween>("Gravity"),
        Row<CpuParticles2DGravityXTween>("Gravity", "X"),
        Row<CpuParticles2DGravityYTween>("Gravity", "Y"),
        Row<CpuParticles2DSpreadTween>("Spread"),
        Row<CpuParticles2DEmissionSphereRadiusTween>("EmissionSphereRadius"),
        Row<CpuParticles2DEmissionRectExtentsTween>("EmissionRectExtents"),
        Row<CpuParticles2DEmissionRectExtentsXTween>("EmissionRectExtents", "X"),
        Row<CpuParticles2DEmissionRectExtentsYTween>("EmissionRectExtents", "Y"),
        Row<CpuParticles2DColorTween>("Color"),
        Row<CpuParticles2DColorAlphaTween>("Color", "A"),
        Row<CpuParticles3DSpeedScaleTween>("SpeedScale"),
        Row<CpuParticles3DLifetimeTween>("Lifetime"),
        Row<CpuParticles3DExplosivenessTween>("Explosiveness"),
        Row<CpuParticles3DRandomnessTween>("Randomness"),
        Row<CpuParticles3DDirectionTween>("Direction"),
        Row<CpuParticles3DDirectionXTween>("Direction", "X"),
        Row<CpuParticles3DDirectionYTween>("Direction", "Y"),
        Row<CpuParticles3DDirectionZTween>("Direction", "Z"),
        Row<CpuParticles3DGravityTween>("Gravity"),
        Row<CpuParticles3DGravityXTween>("Gravity", "X"),
        Row<CpuParticles3DGravityYTween>("Gravity", "Y"),
        Row<CpuParticles3DGravityZTween>("Gravity", "Z"),
        Row<CpuParticles3DSpreadTween>("Spread"),
        Row<CpuParticles3DEmissionSphereRadiusTween>("EmissionSphereRadius"),
        Row<CpuParticles3DEmissionBoxExtentsTween>("EmissionBoxExtents"),
        Row<CpuParticles3DEmissionBoxExtentsXTween>("EmissionBoxExtents", "X"),
        Row<CpuParticles3DEmissionBoxExtentsYTween>("EmissionBoxExtents", "Y"),
        Row<CpuParticles3DEmissionBoxExtentsZTween>("EmissionBoxExtents", "Z"),
        Row<CpuParticles3DColorTween>("Color"),
        Row<CpuParticles3DColorAlphaTween>("Color", "A"),
        Row<SpringArm3DSpringLengthTween>("SpringLength"),
        Row<DecalModulateTween>("Modulate"),
        Row<DecalModulateAlphaTween>("Modulate", "A"),
        Row<DecalSizeTween>("Size"),
        Row<DecalSizeXTween>("Size", "X"),
        Row<DecalSizeYTween>("Size", "Y"),
        Row<DecalSizeZTween>("Size", "Z"),
        Row<DecalEmissionEnergyTween>("EmissionEnergy"),
        Row<FogVolumeSizeTween>("Size"),
        Row<FogVolumeSizeXTween>("Size", "X"),
        Row<FogVolumeSizeYTween>("Size", "Y"),
        Row<FogVolumeSizeZTween>("Size", "Z"),
        Row<ScrollContainerScrollHorizontalTween>("ScrollHorizontal", low: 2, high: 6),
        Row<ScrollContainerScrollVerticalTween>("ScrollVertical", low: 2, high: 6),
        Row<Sprite2DFrameTween>("Frame", low: 2, high: 6),
        Row<AnimatedSprite2DFrameTween>("Frame", low: 2, high: 6),
        Row<AnimatedSprite3DFrameTween>("Frame", low: 2, high: 6),
    ];

    private static readonly Contract[] MaterialContracts =
    [
        Row<MaterialAlbedoColorTween>("AlbedoColor"),
        Row<MaterialAlbedoAlphaTween>("AlbedoColor", "A"),
        Row<MaterialMetallicTween>("Metallic"),
        Row<MaterialMetallicSpecularTween>("MetallicSpecular"),
        Row<MaterialRoughnessTween>("Roughness"),
        Row<MaterialEmissionEnergyMultiplierTween>("EmissionEnergyMultiplier"),
        Row<MaterialEmissionIntensityTween>("EmissionIntensity"),
        Row<MaterialNormalScaleTween>("NormalScale"),
        Row<MaterialEmissionTween>("Emission"),
        Row<MaterialUv1OffsetTween>("Uv1Offset"),
        Row<MaterialUv1OffsetXTween>("Uv1Offset", "X"),
        Row<MaterialUv1OffsetYTween>("Uv1Offset", "Y"),
        Row<MaterialUv1OffsetZTween>("Uv1Offset", "Z"),
        Row<MaterialUv1ScaleTween>("Uv1Scale"),
        Row<MaterialUv1ScaleXTween>("Uv1Scale", "X"),
        Row<MaterialUv1ScaleYTween>("Uv1Scale", "Y"),
        Row<MaterialUv1ScaleZTween>("Uv1Scale", "Z"),
        Row<MaterialUV2OffsetTween>("UV2Offset"),
        Row<MaterialUV2OffsetXTween>("UV2Offset", "X"),
        Row<MaterialUV2OffsetYTween>("UV2Offset", "Y"),
        Row<MaterialUV2OffsetZTween>("UV2Offset", "Z"),
        Row<MaterialUV2ScaleTween>("UV2Scale"),
        Row<MaterialUV2ScaleXTween>("UV2Scale", "X"),
        Row<MaterialUV2ScaleYTween>("UV2Scale", "Y"),
        Row<MaterialUV2ScaleZTween>("UV2Scale", "Z"),
    ];

    // Adapters without one named property. Their contracts are AnchorPairsAndOffsetsUseGodotLayoutUnits,
    // ValueTweensDeliverTypedUpdatesWithTheirDefaults and
    // PropertyBehaviorTests.WorldQuaternionPreservesGlobalOriginAndScaleUnderTransformedParents.
    private static readonly Type[] SpecialNodeAdapters =
    [
        typeof(ControlAnchorMinTween), typeof(ControlAnchorMaxTween), typeof(ControlOffsetsTween),
        typeof(FloatTween), typeof(DoubleTween), typeof(Vector2Tween), typeof(Vector3Tween), typeof(Vector4Tween),
        typeof(ColorTween), typeof(QuaternionTween), typeof(Rect2Tween),
        typeof(GlobalQuaternion3DTween),
    ];

    public static TheoryData<Type, string, string, double, double> NodeCases()
        => new(NodeContracts.Select(row => (row.Adapter, row.Property, row.Component, row.Low, row.High)));

    public static TheoryData<Type, string, string> MaterialCases()
        => new(MaterialContracts.Select(row => (row.Adapter, row.Property, row.Component)));

    [Fact]
    public void EveryNodeAdapterHasAPropertyContractAndAConvenienceExtension()
    {
        var adapters = Adapters<Node>();
        Assert.Equal(adapters.Select(type => type.Name).Order(),
            NodeContracts.Select(row => row.Adapter).Concat(SpecialNodeAdapters).Select(type => type.Name).Order());
        foreach (var adapter in adapters)
            Assert.Single(typeof(TweenExtensions).GetMethods(), method => method.GetParameters() is { Length: 5 } parameters
                && parameters[3].ParameterType == typeof(Action<>).MakeGenericType(adapter)
                && parameters[1].ParameterType == Endpoint(adapter));
    }

    [Fact]
    public void EveryMaterialAdapterHasAPropertyContractAndTreeAndOwnerExtensions()
    {
        var adapters = Adapters<Material>();
        Assert.Equal(adapters.Select(type => type.Name).Order(), MaterialContracts.Select(row => row.Adapter.Name).Order());
        foreach (var adapter in adapters)
        {
            var configured = typeof(TweenExtensions).GetMethods().Where(method =>
                method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Action<>).MakeGenericType(adapter))
                && method.GetParameters()[1].ParameterType == Endpoint(adapter)).ToArray();
            Assert.Equal(2, configured.Length);
            Assert.Contains(configured, method => method.GetParameters()[3].ParameterType == typeof(SceneTree));
            Assert.Contains(configured, method => method.GetParameters()[3].ParameterType == typeof(Node));
        }
    }

    private static Type[] Adapters<TTarget>() => typeof(TweenScheduler).Assembly.GetExportedTypes()
        .Where(type => type is { IsAbstract: false, ContainsGenericParameters: false, BaseType.IsGenericType: true }
            && type.BaseType.GetGenericTypeDefinition() == typeof(PropertyTween<,>)
            && typeof(TTarget).IsAssignableFrom(type.BaseType.GetGenericArguments()[0]))
        .ToArray();

    // Built-in float endpoints take doubles.
    private static Type Endpoint(Type adapter)
    {
        var value = adapter.BaseType!.GetGenericArguments()[1];
        return value == typeof(float) ? typeof(double) : value;
    }

    [Theory]
    [MemberData(nameof(NodeCases))]
    public void NodeAdapterAnimatesItsNamedProperty(Type adapter, string property, string component, double low, double high)
    {
        using var scope = new SceneScope(godot);
        var node = Targets.Create(scope, adapter.BaseType!.GetGenericArguments()[0]);
        Verify(adapter, node, node.GetType().GetProperty(property)!, component, low, high);
    }

    [Theory]
    [MemberData(nameof(MaterialCases))]
    public void MaterialAdapterAnimatesItsNamedProperty(Type adapter, string property, string component)
    {
        using var standard = new StandardMaterial3D();
        using var orm = new OrmMaterial3D();
        foreach (var material in new BaseMaterial3D[] { standard, orm })
            Verify(adapter, material, typeof(BaseMaterial3D).GetProperty(property)!, component, 0.2, 0.6);
    }

    private static void Verify(Type adapter, GodotObject target, PropertyInfo property, string component, double low, double high)
    {
        try
        {
            typeof(PropertyContractTests).GetMethod(nameof(Animate), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(adapter.BaseType!.GetGenericArguments())
                .Invoke(null, [adapter, target, property, component, low, high]);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }

    private static void Animate<TTarget, TValue>(Type adapter, TTarget target, PropertyInfo property, string component,
        double low, double high) where TTarget : class where TValue : struct
    {
        using var scheduler = new TweenScheduler();
        var initial = Sample(property.PropertyType, low);
        var to = (TValue)Sample(typeof(TValue), high);
        property.SetValue(target, initial);
        AssertClose(initial, property.GetValue(target)!, "after Godot's own write");
        var definition = (TweenDefinition<TTarget, TValue>)Activator.CreateInstance(adapter)!;
        definition.Duration = 1;
        definition.To = to;
        var tween = scheduler.Add(target, definition);
        scheduler.Update(0.5);
        var midpoint = Expected(initial, to, component, 0.5);
        AssertClose(midpoint, property.GetValue(target)!, "at the midpoint");
        scheduler.Update(0.5);
        AssertClose(Expected(initial, to, component, 1), property.GetValue(target)!, "at the end");
        Assert.Equal(TweenState.Completed, tween.State);
        Assert.Equal(Reason.Completed, tween.CompletionReason);

        // Cancellation keeps the last sample, even without a retaining fill.
        property.SetValue(target, initial);
        definition.Fill = FillMode.None;
        var cancelled = scheduler.Add(target, definition);
        scheduler.Update(0.5);
        cancelled.Cancel();
        scheduler.Update(1);
        AssertClose(midpoint, property.GetValue(target)!, "after cancelling");
        Assert.Equal(Reason.Cancelled, cancelled.CompletionReason);
    }

    // Distinct components, so a swapped axis or channel shows.
    private static object Sample(Type type, double v)
    {
        if (type == typeof(float)) return (float)v;
        if (type == typeof(double)) return v;
        if (type == typeof(int)) return (int)v;
        if (type == typeof(Vector2)) return new Vector2((float)v, (float)v + 0.07f);
        if (type == typeof(Vector3)) return new Vector3((float)v, (float)v + 0.07f, (float)v + 0.13f);
        if (type == typeof(Color)) return new Color((float)v, (float)v + 0.03f, (float)v + 0.07f, (float)v + 0.11f);
        if (type == typeof(Quaternion)) return new Quaternion(Vector3.Up, (float)v);
        if (type == typeof(Rect2)) return new Rect2((float)v, (float)v + 0.1f, (float)v + 0.2f, (float)v + 0.3f);
        throw new NotSupportedException(type.Name);
    }

    // A component adapter moves only its field; the others keep their initial values.
    private static object Expected(object initial, object target, string component, double weight)
    {
        if (component.Length > 0)
        {
            var field = initial.GetType().GetField(component)!;
            var result = RuntimeHelpers.GetObjectValue(initial);
            field.SetValue(result, (float)Mix(Convert.ToDouble(field.GetValue(initial)), Convert.ToDouble(target), weight));
            return result;
        }
        return initial switch
        {
            float x => (float)Mix(x, (float)target, weight),
            double x => Mix(x, (double)target, weight),
            int x => (int)Math.Round(Mix(x, (int)target, weight), MidpointRounding.AwayFromZero),
            Vector2 x => x * (float)(1 - weight) + (Vector2)target * (float)weight,
            Vector3 x => x * (float)(1 - weight) + (Vector3)target * (float)weight,
            Color x => x * (float)(1 - weight) + (Color)target * (float)weight,
            Quaternion x => new Quaternion(Vector3.Up, (float)Mix(x.GetAngle(), ((Quaternion)target).GetAngle(), weight)),
            Rect2 x => new Rect2((Vector2)Expected(x.Position, ((Rect2)target).Position, "", weight),
                (Vector2)Expected(x.Size, ((Rect2)target).Size, "", weight)),
            _ => throw new NotSupportedException(initial.GetType().Name),
        };
    }

    private static double Mix(double a, double b, double t) => a * (1 - t) + b * t;

    private static void AssertClose(object expected, object actual, string because)
        => Assert.True(Values.Close(expected, actual, 1e-5f), $"Expected {expected}, got {actual} {because}.");

    [Fact]
    public void AnchorPairsAndOffsetsUseGodotLayoutUnits()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var node = new Control { AnchorRight = 1, AnchorBottom = 1 };
        scope.Add(new Control { Size = new Vector2(1000, 1000) }).AddChild(node);
        scheduler.Add(node, new ControlAnchorMinTween { From = Vector2.Zero, To = new Vector2(0.2f, 0.4f), Duration = 1 });
        scheduler.Add(node, new ControlAnchorMaxTween { From = Vector2.One, To = new Vector2(0.6f, 0.8f), Duration = 1 });
        scheduler.Add(node, new ControlOffsetsTween { From = Vector4.Zero, To = new Vector4(10, 20, 30, 40), Duration = 1 });
        scheduler.Update(0.5);
        Assert.Equal(0.1f, node.AnchorLeft);
        Assert.Equal(0.2f, node.AnchorTop);
        Assert.Equal(0.8f, node.AnchorRight);
        Assert.Equal(0.9f, node.AnchorBottom);
        Assert.Equal(5, node.OffsetLeft);
        Assert.Equal(10, node.OffsetTop);
        Assert.Equal(15, node.OffsetRight);
        Assert.Equal(20, node.OffsetBottom);
    }

    [Fact]
    public void ValueTweensDeliverTypedUpdatesWithTheirDefaults()
    {
        using var scope = new SceneScope(godot);
        T Midpoint<T>(TweenDefinition<Node, T> definition, T to) where T : struct
        {
            using var scheduler = new TweenScheduler();
            T reported = default;
            definition.To = to;
            definition.Duration = 1;
            definition.OnUpdate = (_, value) => reported = value;
            scheduler.Add(scope.Root, definition);
            scheduler.Update(0.5);
            return reported;
        }

        // Each starts from zero, transparent black or the identity rotation.
        Assert.Equal(5f, Midpoint(new FloatTween(), 10f));
        Assert.Equal(5d, Midpoint(new DoubleTween(), 10d));
        Assert.Equal(new Vector2(1, 2), Midpoint(new Vector2Tween(), new Vector2(2, 4)));
        Assert.Equal(new Vector3(1, 2, 3), Midpoint(new Vector3Tween(), new Vector3(2, 4, 6)));
        Assert.Equal(new Vector4(1, 2, 3, 4), Midpoint(new Vector4Tween(), new Vector4(2, 4, 6, 8)));
        Assert.Equal(new Color(0.5f, 0, 0, 0.5f), Midpoint(new ColorTween(), new Color(1, 0, 0)));
        Assert.Equal(new Rect2(1, 2, 3, 4), Midpoint(new Rect2Tween(), new Rect2(2, 4, 6, 8)));
        Assert.True(Midpoint(new QuaternionTween(), new Quaternion(Vector3.Up, 0.6f)).IsEqualApprox(new Quaternion(Vector3.Up, 0.3f)));
    }
}
