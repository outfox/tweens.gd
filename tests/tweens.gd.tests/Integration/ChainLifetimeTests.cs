// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class ChainLifetimeTests(HeadlessFixture godot)
{
    [Fact]
    public async Task TreePauseFromStartDefersTheWriteUntilTheNextEligibleUpdate()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D { Position = new Vector2(4, 0) });
        var chain = node.Chain(new ITweenDefinition<Node2D>[] {
            new Tweens.Position2DX(10, 1) { From = 0, Offset = 0.5,
                OnStart = _ => scope.Tree.Paused = true },
        });
        try
        {
            scope.Advance(10);
            Assert.Equal(4, node.Position.X);
            Assert.Equal(0, chain.Elapsed);
            scope.Tree.Paused = false;
            scope.Advance(0.25);
            Assert.Equal(7.5f, node.Position.X);
            scope.Advance(0.25);
            Assert.Equal(Reason.Completed, await chain.End);
        }
        finally { scope.Tree.Paused = false; }
    }

    [Fact]
    public async Task DifferentValueTypesAndBaseTargetsKeepTypedCallbacks()
    {
        using var scope = new SceneScope(godot);
        var sprite = scope.Add(new Sprite2D());
        var values = new List<float>();
        var chain = sprite.Chain(new ITweenDefinition<Sprite2D>[] {
            new Tweens.Position2D((10, 20), 1),
            new Tweens.ModulateAlpha(0, 1) { OnUpdate = (_, value) => values.Add(value) },
        });
        scope.Advance(1.5);
        Assert.Equal(new Vector2(10, 20), sprite.Position);
        Assert.Equal(0.5f, sprite.Modulate.A);
        Assert.Equal(new[] { 1f, 0.5f }, values);
        scope.Advance(0.5);
        Assert.Equal(Reason.Completed, await chain.End);
    }

    [Fact]
    public async Task PausedPendingChainHasOneSubscriptionAndNoLeafHooksOnOwnerExit()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var hooks = 0;
        var before = node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count;
        var chain = node.Chain(new ITweenDefinition<Node2D>[] {
            new Tweens.Position2DX(10, 1) { OnAdd = _ => hooks++, OnFinally = _ => hooks++ },
            new Tweens.Position2DY(20, 1) { OnAdd = _ => hooks++, OnFinally = _ => hooks++ },
        });
        Assert.Equal(before + 1, node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
        chain.Pause();
        scope.Root.RemoveChild(node);
        Assert.Equal(Reason.OwnerExited, await chain.End);
        Assert.Equal(0, hooks);
        Assert.Equal(before, node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
        node.Free();
    }

    [Fact]
    public async Task ResourceChainSnapshotsItsRootClockAndCancelsWhenOwnerLeaves()
    {
        using var scope = new SceneScope(godot);
        var owner = scope.Add(new Node());
        var material = scope.Track(new StandardMaterial3D { Roughness = 0 });
        var chain = material.Chain(new ITweenDefinition<StandardMaterial3D>[] {
            new Tweens.MaterialRoughness(1, 1),
            new Tweens.MaterialMetallic(1, 1),
        }, owner, new PlaybackOptions { ProcessMode = TweenProcessMode.Physics, UseUnscaledTime = true });
        var scheduler = TweenRuntime.GetRunner(owner).Scheduler;
        scheduler.Update(0.5);
        Assert.Equal(0, material.Roughness);
        scheduler.Update(0.1, 0.5, TweenProcessMode.Physics);
        Assert.Equal(0.5f, material.Roughness);
        chain.Pause();
        scope.Root.RemoveChild(owner);
        Assert.Equal(Reason.OwnerExited, await chain.End);
        Assert.True(GodotObject.IsInstanceValid(material));
        owner.Free();
    }
}
