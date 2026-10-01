// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class PlaybackPolicyTests(HeadlessFixture godot)
{
    [Fact]
    public async Task VariadicParallelStartAppliesThePolicyToEveryMember()
    {
        using var scope = new SceneScope(godot);
        var node = scope.Add(new Node2D());
        var group = node.Tween(new PlaybackOptions {
            ProcessMode = TweenProcessMode.Physics,
            PauseMode = TweenPauseMode.Always,
            UseUnscaledTime = true,
        }, new Tweens.Position2DX(10, 1), new Tweens.Position2DY(20, 1),
            new Tweens.ModulateAlpha(0, 1));
        var scheduler = TweenRuntime.GetRunner(node).Scheduler;
        scheduler.Update(0.5, 0.5);
        Assert.Equal(Vector2.Zero, node.Position);
        Assert.Equal(1, node.Modulate.A);
        scheduler.Update(0.1, 0.25, TweenProcessMode.Physics);
        Assert.Equal(new Vector2(2.5f, 5), node.Position);
        Assert.Equal(0.75f, node.Modulate.A);
        try
        {
            scope.Tree.Paused = true;
            scheduler.Update(0.1, 0.75, TweenProcessMode.Physics);
            Assert.Equal(new Vector2(10, 20), node.Position);
            Assert.Equal(0, node.Modulate.A);
            Assert.Equal(Reason.Completed, await group.End);
        }
        finally { scope.Tree.Paused = false; }
    }
}
