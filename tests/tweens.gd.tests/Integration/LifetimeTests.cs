// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

/// <summary>Playback follows the lifetime and pause state of its target, owner node and scene tree.</summary>
[Collection<HeadlessCollection>]
public class LifetimeTests(HeadlessFixture godot)
{
    [Fact]
    public async Task QueueFreedTargetsSettleAsTargetFreed()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var cancelled = false;
        var tween = node.TweenPositionX(10, 1, d => d.OnCancel = _ => cancelled = true);
        node.QueueFree();
        scope.Frames();
        Assert.Equal(Reason.TargetFreed, await tween.End);
        Assert.False(cancelled); // An unactivated slot has no playback callbacks.
    }

    [Fact]
    public void TargetsQueuedForDeletionStopAtTheNextUpdate()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var tween = node.TweenPositionX(10, 1);
        node.QueueFree();
        scope.Advance(0.5);
        Assert.Equal(Reason.TargetFreed, tween.CompletionReason);
        Assert.Equal(0, node.Position.X);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovingTheTargetFromTheTreeSettlesAsOwnerExited(bool treePaused)
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var end = node.TweenPositionX(10, 1).End;
        godot.Tree.Paused = treePaused;
        try { scope.Root.RemoveChild(node); }
        finally { godot.Tree.Paused = false; }
        Assert.True(end.IsCompleted);
        Assert.Equal(Reason.OwnerExited, await end);
        node.Free();
    }

    [Fact]
    public void FinishedTweensNoLongerObserveTheirOwner()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var tween = node.TweenPositionX(10, 1);
        scope.Advance(1);
        Assert.Equal(Reason.Completed, tween.CompletionReason);
        scope.Root.RemoveChild(node);
        Assert.Equal(Reason.Completed, tween.CompletionReason);
        node.Free();
    }

    [Fact]
    public void SuppressedCallbacksSkipInvalidTargets()
    {
        using var scope = new SceneScope(godot);
        var calls = 0;
        var freed = scope.Add(new Node2D()).TweenPositionX(10, 1, d =>
        {
            d.SuppressCallbacksWhenTargetInvalid = true;
            d.OnFinally = _ => calls++;
        });
        var queued = scope.Add(new Node2D());
        var cancelled = queued.TweenPositionX(10, 1, d =>
        {
            d.SuppressCallbacksWhenTargetInvalid = true;
            d.OnFinally = _ => calls++;
        });
        ((Node2D)freed.Target).QueueFree();
        queued.QueueFree();
        cancelled.Cancel();
        scope.Frames();
        Assert.Equal(Reason.TargetFreed, freed.CompletionReason);
        Assert.Equal(Reason.Cancelled, cancelled.CompletionReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void SuppressedCallbacksSkipOwnersLeavingTheTree()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var calls = 0;
        var tween = node.TweenPositionX(10, 1, d =>
        {
            d.SuppressCallbacksWhenTargetInvalid = true;
            d.OnFinally = _ => calls++;
        });
        scope.Root.RemoveChild(node);
        Assert.Equal(Reason.OwnerExited, tween.CompletionReason);
        Assert.Equal(0, calls);
        node.Free();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResourceTweensFollowTheirOwnerNode(bool paused)
    {
        using var scope = new SceneScope(godot);
        var material = scope.Track(new StandardMaterial3D());
        var removed = scope.Add(new Node());
        var queued = scope.Add(new Node());
        TweenInstance[] byRemoval =
        [
            material.TweenMetallic(1, 1, removed),
            material.Tween(new Tweens.MaterialMetallicSpecular(1, 1), removed),
            removed.Tween(material, new Tweens.MaterialAlbedoAlpha(0, 1)),
        ];
        var byQueue = material.TweenRoughness(0, 1, queued);
        var unowned = material.Tween(new Tweens.MaterialNormalScale(2, 1), godot.Tree);
        if (paused)
        {
            foreach (var tween in byRemoval) tween.Pause();
            byQueue.Pause();
        }
        scope.Root.RemoveChild(removed);
        Assert.All(byRemoval, tween => Assert.Equal(Reason.OwnerExited, tween.CompletionReason));
        queued.QueueFree();
        scope.Advance(0.25);
        Assert.Equal(Reason.OwnerExited, byQueue.CompletionReason);
        scope.Advance(0.75);
        Assert.Equal(Reason.Completed, unowned.CompletionReason);
        removed.Free();
    }

    [Fact]
    public void DisposedResourceTargetsSettleAsTargetFreed()
    {
        using var scope = new SceneScope(godot);
        var material = new StandardMaterial3D();
        var tween = material.TweenMetallic(1, 1, godot.Tree);
        material.Dispose();
        scope.Advance(0.25);
        Assert.Equal(Reason.TargetFreed, tween.CompletionReason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedTweensStillSettleWhenTheirTargetIsFreed(bool suppress)
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var material = new StandardMaterial3D();
        var calls = 0;
        var queued = node.TweenPositionX(10, 1, d =>
        {
            d.SuppressCallbacksWhenTargetInvalid = suppress;
            d.OnFinally = _ => calls++;
        });
        var disposed = material.TweenMetallic(1, 1, godot.Tree, d =>
        {
            d.SuppressCallbacksWhenTargetInvalid = suppress;
            d.OnFinally = _ => calls++;
        });
        // Activated playback owes its terminal callbacks unless suppressed.
        scope.Advance(0);
        queued.Pause();
        disposed.Pause();
        node.QueueFree();
        material.Dispose();
        scope.Frames();
        Assert.Equal(Reason.TargetFreed, await queued.End);
        Assert.Equal(Reason.TargetFreed, await disposed.End);
        Assert.Equal(suppress ? 0 : 2, calls);
    }

    // Each phase invalidates the target from a later hook: OnAdd, OnStart, the easing function or OnUpdate.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task TargetsInvalidatedFromCallbacksAreNotWrittenAgain(int phase)
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var material = new StandardMaterial3D();
        var freed = node.TweenPositionX(10, 1, d => InvalidateFrom(d, phase, node.Free));
        var disposed = material.TweenRoughness(0, 1, godot.Tree, d => InvalidateFrom(d, phase, material.Dispose));
        scope.Advance(1);
        // Free() removes a node that owns its tween from the tree before deleting it, which reports OwnerExited.
        Assert.Equal(Reason.OwnerExited, await freed.End);
        Assert.Equal(Reason.TargetFreed, await disposed.End);
    }

    private static void InvalidateFrom<TTarget, TValue>(TweenDefinition<TTarget, TValue> definition, int phase,
        Action invalidate) where TTarget : class where TValue : struct
    {
        // Restoring the start value would be one more write.
        definition.Fill = FillMode.None;
        switch (phase)
        {
            case 0: definition.OnAdd = _ => invalidate(); break;
            case 1: definition.OnStart = _ => invalidate(); break;
            case 2: definition.EaseFunction = t => { invalidate(); return t; }; break;
            default: definition.OnUpdate = (_, _) => invalidate(); break;
        }
    }

    [Fact]
    public void TargetsInvalidatedWhilePreparingAreRejected()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var material = new StandardMaterial3D();
        var released = 0;
        var a = node.Tween(new PreparingTween<Node2D>(n => n.QueueFree(), () => released++));
        scope.Advance(0);
        Assert.Equal(Reason.TargetFreed, a.CompletionReason);
        var b = material.Tween(new PreparingTween<StandardMaterial3D>(m => m.Dispose(), () => released++), godot.Tree);
        scope.Advance(0);
        Assert.Equal(Reason.TargetFreed, b.CompletionReason);
        Assert.Equal(2, released);
    }

    [Fact]
    public void SharedResourcesAreAnimatedInPlaceAndOutliveTheirUsers()
    {
        using var scope = new SceneScope(godot);
        var material = scope.Track(new StandardMaterial3D { Roughness = 0 });
        var first = scope.Add(new MeshInstance3D { MaterialOverride = material });
        var second = scope.Add(new MeshInstance3D { MaterialOverride = material });
        var tween = material.TweenRoughness(1, 1, godot.Tree);
        scope.Advance(0.5);
        Assert.Same(material, first.MaterialOverride);
        Assert.Same(material, second.MaterialOverride);
        Assert.Equal(0.5f, material.Roughness);
        first.MaterialOverride = null;
        first.Free();
        scope.Advance(0.5);
        Assert.Equal(Reason.Completed, tween.CompletionReason);
        Assert.Equal(1, material.Roughness);
        Assert.Same(material, second.MaterialOverride);
    }

    [Fact]
    public void OwnersInvalidatedWhilePreparingAreRejected()
    {
        using var scope = new SceneScope(godot);
        var material = scope.Track(new StandardMaterial3D());
        var freed = scope.Add(new Node());
        var removed = scope.Add(new Node());
        var a = material.Tween(new PreparingTween<StandardMaterial3D>(_ => freed.Free()), freed);
        scope.Advance(0);
        Assert.Equal(Reason.OwnerExited, a.CompletionReason);
        var b = material.Tween(new PreparingTween<StandardMaterial3D>(_ => scope.Root.RemoveChild(removed)), removed);
        scope.Advance(0);
        Assert.Equal(Reason.OwnerExited, b.CompletionReason);
        removed.Free();
    }

    [Fact]
    public void TargetsFreedDuringTheInitialReadSettleImmediately()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var tween = node.Tween(new PropertyTween<Node2D, float>(n =>
        {
            n.QueueFree();
            return 0;
        }, (_, _) => { }, Interpolators.Float) { Duration = 1 });
        scope.Advance(0);
        Assert.Equal(Reason.TargetFreed, tween.CompletionReason);
    }

    [Fact]
    public void OwnersLeavingDuringTheInitialReadSettleImmediately()
    {
        using var scope = new SceneScope(godot);
        var owner = scope.Add(new Node());
        var material = scope.Track(new StandardMaterial3D());
        var tween = material.Tween(new PropertyTween<StandardMaterial3D, float>(m =>
        {
            scope.Root.RemoveChild(owner);
            return m.Metallic;
        }, (m, v) => m.Metallic = v, Interpolators.Float) { To = 1, Duration = 1 }, owner);
        scope.Advance(0);
        Assert.Equal(Reason.OwnerExited, tween.CompletionReason);
        owner.Free();
    }

    [Fact]
    public void BoundTweensFollowTheirNodesProcessMode()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D { ProcessMode = Node.ProcessModeEnum.Disabled });
        var bound = node.TweenPositionX(10, 1);
        var always = node.TweenRotation(1, 1, playback: new PlaybackOptions { PauseMode = TweenPauseMode.Always });
        var treeBound = node.TweenScale(Vector2.One * 2, 1, playback: new PlaybackOptions { PauseMode = TweenPauseMode.SceneTree });
        scope.Advance(0.5);
        Assert.Equal(0, bound.Progress);
        Assert.Equal(0.5f, always.Progress);
        Assert.Equal(0.5f, treeBound.Progress);
    }

    [Fact]
    public void PausedTreesHoldTreeFollowingTweens()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var material = scope.Track(new StandardMaterial3D());
        var bound = node.TweenPositionX(10, 1);
        var treeBound = node.TweenRotation(1, 1, playback: new PlaybackOptions { PauseMode = TweenPauseMode.SceneTree });
        var always = node.TweenScale(Vector2.One * 2, 1, playback: new PlaybackOptions { PauseMode = TweenPauseMode.Always });
        var resource = material.TweenMetallic(1, 1, godot.Tree);
        var ownedResource = material.TweenRoughness(0, 1, godot.Tree, owner: node);
        // Bound tweens follow whether their owner processes, so an always-processing owner keeps them running.
        var processing = scope.Add(new Node2D { ProcessMode = Node.ProcessModeEnum.Always });
        var processingBound = processing.TweenPositionX(10, 1);
        var processingResource = material.TweenMetallicSpecular(1, 1, godot.Tree, owner: processing);
        var held = processing.TweenRotation(1, 1, playback: new PlaybackOptions { PauseMode = TweenPauseMode.Always });
        held.Pause();
        godot.Tree.Paused = true;
        try
        {
            scope.Advance(0.5);
            Assert.Equal(0, bound.Progress);
            Assert.Equal(0, treeBound.Progress);
            Assert.Equal(0.5f, always.Progress);
            Assert.Equal(0, resource.Progress);
            Assert.Equal(0, ownedResource.Progress);
            Assert.Equal(0.5f, processingBound.Progress);
            Assert.Equal(0.5f, processingResource.Progress);
            Assert.Equal(0, held.Progress);
        }
        finally { godot.Tree.Paused = false; }
        scope.Advance(0.5);
        Assert.Equal(0.5f, resource.Progress);
    }

    [Fact]
    public void LanesAdvanceIndependently()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var process = node.TweenPositionX(10, 1);
        var physics = node.TweenRotation(1, 1, playback: new PlaybackOptions { ProcessMode = TweenProcessMode.Physics });
        scope.Advance(0.5);
        scope.Advance(0.25, TweenProcessMode.Physics);
        Assert.Equal(0.5f, process.Progress);
        Assert.Equal(0.25f, physics.Progress);
    }

    [Fact]
    public void ScheduledNodeOwnersBindPlainTargets()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var owner = scope.Add(new Node { ProcessMode = Node.ProcessModeEnum.Disabled });
        var box = new Box();
        var tween = scheduler.Add(box, new PlainTween { To = 1, Duration = 1 }, owner);
        scheduler.Update(0.5);
        Assert.Equal(0, box.Value);
        owner.ProcessMode = Node.ProcessModeEnum.Inherit;
        scheduler.Update(0.5);
        Assert.Equal(0.5f, box.Value);
        owner.QueueFree();
        scheduler.Update(0.5);
        Assert.Equal(Reason.OwnerExited, tween.CompletionReason);
    }

    [Fact]
    public async Task FailuresOnTheAutomaticRunnerAreReportedToGodot()
    {
        using var scope = new SceneScope(godot);
        var tween = scope.Add(new Node2D()).TweenPositionX(10, 1, d => d.OnStart = _ => throw new FormatException("broken start"));
        // The error log still records the report; only its console output is silenced.
        Engine.PrintErrorMessages = false;
        try { scope.Advance(0.5); }
        finally { Engine.PrintErrorMessages = true; }
        await Assert.ThrowsAsync<FormatException>(() => tween.End);
        godot.Errors.Expect("broken start");
    }

    [Theory]
    [InlineData(1, 0.5f)]
    [InlineData(0, 0.5f)]
    [InlineData(0.5, 0.5f)]
    public void CurveEasingSamplesAPrivateCopy(double skew, float sampleTime)
    {
        using var scope = new SceneScope(godot);
        var curve = scope.Track(new Curve());
        curve.AddPoint(new Vector2(0, 0));
        curve.AddPoint(new Vector2(0.5f, 0.8f));
        curve.AddPoint(new Vector2(1, 1));
        var node = scope.Add(new Node2D());
        var expected = 10 * curve.Sample(sampleTime);
        var tween = node.TweenPositionX(10, 1, d =>
        {
            d.Curve = curve;
            d.Ease = EaseType.ExpoIn;
            d.Skew = skew;
            d.Weks = skew;
            d.PingPong = true;
        });
        curve.ClearPoints();
        scope.Advance(0.5);
        Assert.Equal(expected, node.Position.X, 3);
        scope.Advance(1);
        Assert.Equal(expected, node.Position.X, 3);
        scope.Advance(0.5);
        Assert.Equal(Reason.Completed, tween.CompletionReason);
        Assert.Throws<ArgumentException>(() => node.TweenPositionX(10, 1, d =>
        {
            d.Curve = curve;
            d.EaseFunction = static x => x;
        }));
    }

    // Preparation always invalidates the target or owner here, so playback must never read or write.
    private sealed class PreparingTween<TTarget>(Action<TTarget> prepare, Action? release = null)
        : TweenDefinition<TTarget, float> where TTarget : GodotObject
    {
        protected override void Prepare(TTarget target) => prepare(target);
        protected override float Read(TTarget target) => throw new InvalidOperationException(nameof(Read));
        protected override void Write(TTarget target, float value) => throw new InvalidOperationException(nameof(Write));
        protected override float Interpolate(float from, float to, float weight) => to;
        protected override void Release() => release?.Invoke();
    }
}
