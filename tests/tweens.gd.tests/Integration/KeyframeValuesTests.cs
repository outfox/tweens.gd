// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class KeyframeValuesTests(HeadlessFixture godot)
{
    [Fact]
    public void EverySupportedValueSurvivesVariantBindingAndNonfiniteComponentsAreRejected()
    {
        Check(1.25); Check(7); Check(5_000_000_000L); Check(new Vector2(1, 2)); Check(new Vector3(1, 2, 3)); Check(new Vector4(1, 2, 3, 4));
        Check(new Rect2(1, 2, 3, 4)); Check(new Color(0.1f, 0.2f, 0.3f, 0.4f)); Check(Quaternion.Identity);
        static void Check<T>(T value) where T : struct
        {
            using var variant = AnimationValues.Write(value);
            AnimationValues.Validate(variant);
            Assert.Equal(value, AnimationValues.Read<T>(variant));
        }
        Variant[] invalid = ["string", default, double.PositiveInfinity, new Vector2(float.NaN, 0), new Vector3(0, float.NaN, 0),
            new Vector4(0, 0, 0, float.NaN), new Rect2(float.NaN, 0, 0, 0), new Rect2(0, 0, float.NaN, 0),
            new Quaternion(0, 0, 0, 0), new Quaternion(float.NaN, 0, 0, 1), new Quaternion(float.MaxValue, 0, 0, 1),
            new Quaternion(float.Epsilon, 0, 0, 0), new Color(float.NaN, 0, 0, 1),
            new Color(0, float.NaN, 0, 1), new Color(0, 0, float.NaN, 1), new Color(0, 0, 0, float.NaN)];
        foreach (var value in invalid) Assert.Throws<ArgumentException>(() => AnimationValues.Validate(value));
        Assert.Throws<ArgumentException>(() => AnimationValues.Read<double>((Variant)1));
        using var coerced = AnimationValues.Coerce((Variant)2, Variant.Type.Float, false); Assert.Equal(2.0, coerced.AsDouble());
        using var rounded = AnimationValues.Coerce((Variant)1.5, Variant.Type.Int, false); Assert.Equal(2, rounded.AsInt32());
        using var scale2 = AnimationValues.Coerce((Variant)2, Variant.Type.Vector2, true); Assert.Equal(Vector2.One * 2, scale2.AsVector2());
        using var scale3 = AnimationValues.Coerce((Variant)2, Variant.Type.Vector3, true); Assert.Equal(Vector3.One * 2, scale3.AsVector3());
        Assert.Throws<ArgumentException>(() => AnimationValues.Coerce((Variant)2, Variant.Type.Vector2, false));
        Assert.Throws<ArgumentException>(() => AnimationValues.Coerce((Variant)2, Variant.Type.Color, true));
    }

    [Fact]
    public void ComponentPathsResolveAllSupportedValueShapesAndRejectInvalidComponents()
    {
        using var scope = new SceneScope(godot);
        var two = scope.Add(new Node2D()); var three = scope.Add(new Node3D()); var sprite = scope.Add(new Sprite2D());
        foreach (var path in new[] { "position:x", "position:y", "modulate:r", "modulate:g", "modulate:b", "modulate:a" }) AnimationValues.ValidatePath(two, path);
        foreach (var path in new[] { "position:x", "position:y", "position:z", "quaternion:x", "quaternion:y", "quaternion:z", "quaternion:w" }) AnimationValues.ValidatePath(three, path);
        foreach (var path in new[] { "region_rect:position:x", "region_rect:size:y", "region_rect:end:x" }) AnimationValues.ValidatePath(sprite, path);
        foreach (var path in new[] { "position:r", "quaternion:q" }) Assert.Throws<ArgumentException>(() => AnimationValues.ValidatePath(three, path));
        Assert.Throws<ArgumentException>(() => AnimationValues.ValidatePath(two, "modulate:x"));
    }

    [Fact]
    public void SparseChannelsSupportIntegersRectsVectorsQuaternionsAndScalarScale()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var sprite = scope.Add(new Sprite2D());
        var rectangle = new Tweens.Keyframes([Tweens.Keyframe.At(0, ("region_rect", (Variant)new Rect2(0, 0, 0, 0)), ("z_index", (Variant)0)),
            Tweens.Keyframe.At(100, ("region_rect", (Variant)new Rect2(2, 4, 6, 8)), ("z_index", (Variant)4))]);
        rectangle.Play(scheduler, sprite); scheduler.Update(0.5);
        Assert.Equal(new Rect2(1, 2, 3, 4), sprite.RegionRect); Assert.Equal(2, sprite.ZIndex);
        scheduler.Update(0.5);
        var three = scope.Add(new Node3D());
        var rotation = new Tweens.Keyframes([Tweens.Keyframe.At(0, scale: 1, quaternion: Quaternion.Identity),
            Tweens.Keyframe.At(100, scale: 2, quaternion: new Quaternion(Vector3.Up, 1))]);
        rotation.Play(scheduler, three); scheduler.Update(0.5);
        Assert.Equal(Vector3.One * 1.5f, three.Scale); Assert.True(three.Quaternion.IsEqualApprox(new Quaternion(Vector3.Up, 0.5f)));
        scheduler.Update(0.5);
        var two = scope.Add(new Node2D());
        new Tweens.Keyframes([Tweens.Keyframe.At(0, position: new Vector2(0, 0), rotation: 0f, scale: 1f, skew: 0, selfModulate: Colors.Red),
            Tweens.Keyframe.At(Percent.Of(100), position: (10f, 20f), rotation: 1, scale: 2, skew: 0.1, selfModulate: Colors.Blue, interpolation: In.Quad)]).Play(scheduler, two);
        scheduler.Update(0.5);
        Assert.Equal(new Vector2(2.5f, 5), two.Position); Assert.Equal(1.25f, two.Scale.X);
        scheduler.Update(0.5);
        new Tweens.Keyframes([Tweens.Keyframe.At(0, position: (0f, 0f, 0f)), Tweens.Keyframe.At(100, position: new Vector3(2, 4, 6))]).Play(scheduler, three);
        scheduler.Update(0.5); Assert.Equal(new Vector3(1, 2, 3), three.Position);
        var scalar = (Real)1f; Assert.Equal(1, scalar.Value);
        Assert.Equal(Variant.Type.Float, default(Vec).ToVariant().VariantType);
    }

    [Fact]
    public void InvalidEasesBindingsAndEmptyDefinitionsFailBeforeEnrollment()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var target = scope.Add(new Node2D());
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes(ReadOnlySpan<Tweens.Keyframe>.Empty));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(0, x: 0, interpolation: Interpolation.Linear)]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(100, ("", (Variant)1.0))]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(100, ("name", (Variant)1.0))]).Play(scheduler, target));
        Assert.Throws<NotImplementedException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(0, x: 0), Tweens.Keyframe.At(100, x: 1, interpolation: (EaseType)123456)]));
        Assert.Throws<NotImplementedException>(() => new Tweens.Keyframes(x: [0, 1], ease: (EaseType)123456));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(101, x: 1)]));
        var definition = new Tweens.Keyframes(x: [0, 1]);
        target.QueueFree(); Assert.Throws<ArgumentException>(() => definition.Play(scheduler, target));
        Assert.Equal(0, scheduler.ActiveCount);
    }
}
