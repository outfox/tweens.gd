// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

/// <summary>What adapters do on the Godot side: under transformed parents, on paths, in layouts and together.</summary>
[Collection<HeadlessCollection>]
public class PropertyBehaviorTests(HeadlessFixture godot)
{
    private static void AssertClose(object expected, object actual)
        => Assert.True(Values.Close(expected, actual, 1e-5f), $"Expected {expected}, got {actual}.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldQuaternionPreservesGlobalOriginAndScaleUnderTransformedParents(bool nonuniformParent)
    {
        using var scope = new SceneScope(godot);
        var parent = scope.Add(new Node3D
        {
            Position = new Vector3(10, 20, 30), Rotation = new Vector3(0.2f, 0.4f, -0.1f),
            Scale = nonuniformParent ? new Vector3(2, 3, 4) : Vector3.One * 2,
        });
        var child = new Node3D
        {
            Position = new Vector3(3, 4, 5), Scale = new Vector3(0.7f, 1.2f, 1.8f),
            RotationOrder = EulerOrder.Zxy, Rotation = new Vector3(0.1f, 0.3f, 0.2f),
        };
        parent.AddChild(child);
        var start = Quaternion.FromEuler(child.GlobalRotation);
        var end = Quaternion.FromEuler(new Vector3(0.8f, -0.6f, 0.2f));
        var localScale = child.Scale;
        var origin = child.GlobalPosition;
        var scale = child.GlobalBasis.Scale;

        // The negated endpoint is the same rotation.
        var tween = child.TweenGlobalQuaternion(-end, 1);
        scope.Advance(0.5);
        Assert.True(Math.Abs(Quaternion.FromEuler(child.GlobalRotation).Dot(start.Slerp(end, 0.5f))) > 0.99999f);
        AssertClose(origin, child.GlobalPosition);
        AssertClose(scale, child.GlobalBasis.Scale);
        if (!nonuniformParent) AssertClose(localScale, child.Scale);
        scope.Advance(0.5);
        Assert.True(Math.Abs(Quaternion.FromEuler(child.GlobalRotation).Dot(end)) > 0.99999f);
        AssertClose(origin, child.GlobalPosition);
        AssertClose(scale, child.GlobalBasis.Scale);
        Assert.Equal(TweenState.Completed, tween.State);
    }

    [Fact]
    public void GlobalScaleAndSkewRespectA2DParent()
    {
        using var scope = new SceneScope(godot);
        var parent = scope.Add(new Node2D { Rotation = 0.3f, Scale = new Vector2(2, 3), Position = new Vector2(10, 20) });
        var child = new Node2D { Position = new Vector2(4, 5) };
        parent.AddChild(child);
        var origin = child.GlobalPosition;
        var rotation = child.GlobalRotation;
        var scale = child.GlobalScale;
        child.TweenGlobalScale(new Vector2(4, 6), 1);
        scope.Advance(0.5);
        AssertClose((scale + new Vector2(4, 6)) / 2, child.GlobalScale);
        child.CancelTweens();

        // Godot decomposes skew relative to its parent; compare the native setter contract.
        var expected = new Node2D { Transform = child.Transform };
        parent.AddChild(expected);
        expected.GlobalSkew = 0.2f;
        child.TweenGlobalSkew(0.4f, 1);
        scope.Advance(0.5);
        AssertClose(expected.GlobalScale, child.GlobalScale);
        Assert.InRange(Math.Abs(child.GlobalSkew - expected.GlobalSkew), 0, 0.0001f);
        AssertClose(origin, child.GlobalPosition);
        Assert.InRange(Math.Abs(child.GlobalRotation - rotation), 0, 0.0001f);
    }

    [Fact]
    public void GlobalTweensFollowTheParentAndLocalRotationsKeepTheScale()
    {
        using var scope = new SceneScope(godot);
        var parent = scope.Add(new Node3D { Position = new Vector3(10, 0, 0), Scale = Vector3.One * 2 });
        var child = new Node3D { Scale = new Vector3(2, 3, 4) };
        parent.AddChild(child);
        child.TweenGlobalPosition(new Vector3(30, 0, 0), 1);
        child.TweenQuaternion(new Quaternion(Vector3.Up, Mathf.Pi / 2), 1);
        scope.Advance(0.5);
        Values.AssertClose(new Vector3(20, 0, 0), child.GlobalPosition);
        Values.AssertClose(new Vector3(5, 0, 0), child.Position);
        Values.AssertClose(new Quaternion(Vector3.Up, Mathf.Pi / 4), child.Quaternion);
        Values.AssertClose(new Vector3(2, 3, 4), child.Scale);
    }

    [Fact]
    public void ComponentTweensKeepConcurrentChangesToOtherComponents()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D { Position = new Vector2(2, 3), Modulate = new Color(1, 0, 0) });
        var camera = scope.Add(new Camera2D { Zoom = new Vector2(1, 2) });
        var sprite = scope.Add(new Sprite3D { Modulate = new Color(0.2f, 0.3f, 0.4f, 0.8f) });
        var material = scope.Track(new StandardMaterial3D { AlbedoColor = Colors.White });
        node.TweenPositionX(12, 1);
        node.TweenPositionY(23, 1);
        node.TweenModulateAlpha(0, 1);
        camera.TweenZoomX(3, 1);
        camera.TweenZoomY(6, 1);
        sprite.TweenModulateAlpha(0, 1);
        material.TweenAlbedoAlpha(0, 1, godot.Tree);
        material.TweenUv1OffsetX(2, 1, godot.Tree);
        material.TweenUv1OffsetY(4, 1, godot.Tree);
        // Change the other components after every tween captured its start.
        scope.Advance(0);
        node.Modulate = new Color(0, 1, 0);
        sprite.Modulate = new Color(0.7f, 0.1f, 0.6f, 0.8f);
        material.AlbedoColor = Colors.Red;
        material.Uv1Offset = new Vector3(0, 0, 7);
        scope.Advance(0.5);
        Assert.Equal(new Vector2(7, 13), node.Position);
        Assert.Equal(new Color(0, 1, 0, 0.5f), node.Modulate);
        AssertClose(new Vector2(2, 4), camera.Zoom);
        AssertClose(new Color(0.7f, 0.1f, 0.6f, 0.4f), sprite.Modulate);
        Assert.Equal(new Color(1, 0, 0, 0.5f), material.AlbedoColor);
        Assert.Equal(new Vector3(1, 2, 7), material.Uv1Offset);
        // Alpha is written as is; enabling transparency stays the material author's choice.
        Assert.Equal(BaseMaterial3D.TransparencyEnum.Disabled, material.Transparency);
    }

    [Fact]
    public void PathProgressMovesFollowersInSceneUnits()
    {
        using var scope = new SceneScope(godot);
        var follower2 = (PathFollow2D)Targets.Create(scope, typeof(PathFollow2D));
        var follower3 = (PathFollow3D)Targets.Create(scope, typeof(PathFollow3D));
        follower2.TweenProgressRatio(1, 1);
        follower3.TweenProgress(100, 1);
        scope.Advance(0.5);
        AssertClose(new Vector2(50, 0), follower2.Position);
        AssertClose(new Vector3(50, 0, 0), follower3.Position);
        Assert.InRange(Math.Abs(follower3.ProgressRatio - 0.5f), 0, 0.0001f);
        scope.Advance(0.5);
        AssertClose(new Vector2(100, 0), follower2.Position);
        AssertClose(new Vector3(100, 0, 0), follower3.Position);
    }

    [Fact]
    public void OffsetTransformsLeaveTheContainerLayoutAlone()
    {
        using var scope = new SceneScope(godot);
        var container = scope.Add(new HBoxContainer { Size = new Vector2(300, 100) });
        var control = new Control { CustomMinimumSize = new Vector2(100, 40), OffsetTransformEnabled = true };
        container.AddChild(control);
        scope.Frames();
        var position = control.Position;
        var size = control.Size;
        control.TweenOffsetTransformPosition(new Vector2(20, 30), 1);
        control.TweenOffsetTransformScale(new Vector2(2, 3), 1);
        scope.Advance(0.5);
        AssertClose(new Vector2(10, 15), control.OffsetTransformPosition);
        AssertClose(new Vector2(1.5f, 2), control.OffsetTransformScale);
        AssertClose(position, control.Position);
        AssertClose(size, control.Size);
    }

    [Fact]
    public void AnchorEdgesPushTheirOpposites()
    {
        using var scope = new SceneScope(godot);
        var node = (Control)Targets.Create(scope, typeof(Control));
        node.AnchorRight = 0.2f;
        node.TweenAnchorLeft(0.8f, 1);
        scope.Advance(1);
        Assert.Equal(0.8f, node.AnchorLeft);
        Assert.Equal(0.8f, node.AnchorRight);
        node.CancelTweens();
        node.TweenAnchorMin(new Vector2(0.1f, 0.2f), 0);
        node.TweenAnchorMax(new Vector2(0.8f, 0.9f), 0);
        node.TweenOffsets(new Vector4(10, 20, 30, 40), 0);
        scope.Advance(0);
        AssertClose(0.1f, node.AnchorLeft);
        AssertClose(0.2f, node.AnchorTop);
        AssertClose(0.8f, node.AnchorRight);
        AssertClose(0.9f, node.AnchorBottom);
        Assert.Equal(10, node.OffsetLeft);
        Assert.Equal(20, node.OffsetTop);
        Assert.Equal(30, node.OffsetRight);
        Assert.Equal(40, node.OffsetBottom);
    }

    [Fact]
    public void ConfigurationFailureDoesNotRegisterPlayback()
    {
        using var scope = new SceneScope(godot);
        var camera = scope.Add(new Camera2D());
        var before = TweenRuntime.GetActiveCount(camera);
        Assert.Throws<InvalidOperationException>(() => camera.TweenZoom(Vector2.One, 1,
            _ => throw new InvalidOperationException("configuration")));
        Assert.Equal(before, TweenRuntime.GetActiveCount(camera));
    }
}
