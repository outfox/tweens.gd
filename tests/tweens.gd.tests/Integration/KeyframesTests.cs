// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class KeyframesTests(HeadlessFixture godot)
{
    [Fact]
    public void SparseDefinitionCapturesAtActivationAndReusesAcrossTargets()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var a = scope.Add(new Node2D()); var b = scope.Add(new Node2D());
        var animation = new Tweens.Keyframes([Tweens.Keyframe.At(50, x: 10), Tweens.Keyframe.At(100, x: 20)], interpolation: Interpolation.Linear);
        var first = animation.Play(scheduler, a); var second = animation.Play(scheduler, b);
        a.Position = new(2, 3); b.Position = new(6, 7);
        scheduler.Update(0.25);
        Assert.Equal(new Vector2(6, 3), a.Position); Assert.Equal(new Vector2(8, 7), b.Position);
        first.Pause(); scheduler.Update(0.25);
        Assert.Equal(6, a.Position.X); Assert.Equal(10, b.Position.X);
        first.Resume(); scheduler.Update(0.75);
        Assert.Equal(20, a.Position.X); Assert.Equal(Reason.Completed, first.CompletionReason); Assert.Equal(Reason.Completed, second.CompletionReason);
        a.Position = Vector2.Zero;
        animation.Play(scheduler, a); scheduler.Update(0.25); Assert.Equal(5, a.Position.X);
    }

    [Fact]
    public void TypedArraysSparseHoldsAndWholeColorDefault()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var target = scope.Add(new Node2D());
        var animation = Tweens.Keyframes.ForNode2D(position: [(0, 0), (10, 20)], scale: [1, (2, 4)],
            rotationDegrees: [0, 90], modulate: [Colors.Red, new Color(0, 0, 0, 0)], duration: 1);
        Assert.Equal(4, animation.Count);
        animation.Play(scheduler, target); scheduler.Update(0.5);
        Assert.Equal(new Vector2(5, 10), target.Position); Assert.Equal(new Vector2(1.5f, 2.5f), target.Scale);
        Assert.InRange(target.RotationDegrees, 44.999f, 45.001f);
        Assert.True(target.Modulate.IsEqualApprox(new Color(1, 0, 0, 0.5f)));
        scheduler.Update(0.5);
        var sparse = new Tweens.Keyframes([Tweens.Keyframe.At(0, x: 0, alpha: 1), Tweens.Keyframe.At(25, x: 10), Tweens.Keyframe.At(100, alpha: 0)], interpolation: Interpolation.Linear);
        sparse.Play(scheduler, target); scheduler.Update(0.75);
        Assert.Equal(10, target.Position.X); Assert.Equal(0.25f, target.Modulate.A);
    }

    [Fact]
    public void DelayPingPongAndCancellationUseOrdinaryPlayback()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var target = scope.Add(new Node3D());
        var animation = Tweens.Keyframes.ForNode3D(position: [(0, 0, 0), (10, 20, 30)], scale: [1, 2],
            options: new TweenOptions { Duration = 1, Delay = 1, PingPong = true });
        var group = animation.Play(scheduler, target);
        scheduler.Update(0.5); Assert.Equal(Vector3.Zero, target.Position);
        scheduler.Update(1); Assert.Equal(new Vector3(5, 10, 15), target.Position);
        scheduler.Update(1); Assert.Equal(new Vector3(5, 10, 15), target.Position);
        scheduler.Update(0.5); Assert.Equal(Vector3.Zero, target.Position); Assert.Equal(Reason.Completed, group.CompletionReason);
        var again = animation.Play(scheduler, target); again.Members[0].Cancel();
        Assert.Equal(Reason.Cancelled, again.CompletionReason); Assert.True(again.Members.All(m => m.IsTerminal));
    }

    [Fact]
    public void InvalidBatchStartsNothingAndDoesNotLogGodotErrors()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var target = scope.Add(new Node2D());
        foreach (var path in new[] { "missing", "position:z", "material:resource_name", "position:" })
        {
            var invalid = new Tweens.Keyframes([Tweens.Keyframe.At(100, x: 10), Tweens.Keyframe.At(100, (path, (Variant)1.0))]);
            Assert.Throws<ArgumentException>(() => invalid.Play(scheduler, target));
            Assert.Equal(0, scheduler.ActiveCount); Assert.Equal(Vector2.Zero, target.Position);
        }
        var mismatch = new Tweens.Keyframes(position: [(0, 0, 0), (1, 1, 1)]);
        Assert.Throws<ArgumentException>(() => mismatch.Play(scheduler, target));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes(x: [1]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes(x: [0, 1], position: [(0, 0), (1, 1)]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes(rotation: [0, 1], rotationDegrees: [0, 90]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes([Tweens.Keyframe.At(50, x: 1), Tweens.Keyframe.At(50, x: 2)]));
        Assert.Throws<ArgumentException>(() => new Tweens.Keyframes(x: [0, double.NaN]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Percent.Of(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tweens.Keyframes(x: [0, 1], options: new TweenOptions { ColorSpace = (ColorSpace)99 }));
    }

    [Fact]
    public void NodeExtensionsAndExplicitPathsWork()
    {
        using var scope = new SceneScope(godot);
        var target = scope.Add(new Node2D());
        var animation = new Tweens.Keyframes([Tweens.Keyframe.At(0, ("position:x", (Variant)0.0)), Tweens.Keyframe.At(100, ("position:x", (Variant)10.0))]);
        var first = target.Animate(animation); scope.Advance(0.5); Assert.Equal(5, target.Position.X); first.Cancel();
        target.Tween(animation).Cancel();
        target.Animate(x: [0, 20], duration: 1).Cancel();
        target.Animate([Tweens.Keyframe.At(0, x: 0), Tweens.Keyframe.At(100, x: 4)]).Cancel();
    }
}
