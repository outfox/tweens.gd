// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class ChainPolicyTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedNodeTargetStopsActiveAndPendingEntriesEvenWhilePausedOnAnotherLane(bool suppress)
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var node = scope.Add(new Node2D());
        var callbacks = new List<string>();
        var pendingHooks = 0;
        var connections = node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count;
        var chain = scheduler.AddChain(node, new ITweenDefinition<Node2D>[] {
            new Tweens.Position2DX(10, 2) {
                SuppressCallbacksWhenTargetInvalid = suppress,
                OnCancel = _ => callbacks.Add("x cancel"), OnFinally = _ => callbacks.Add("x finally"),
            },
            new Tweens.Position2DY(20, 2) {
                Delay = -2, SuppressCallbacksWhenTargetInvalid = suppress,
                OnCancel = _ => callbacks.Add("y cancel"), OnFinally = _ => callbacks.Add("y finally"),
            },
            new Tweens.Rotation2D(1, 1) { OnAdd = _ => pendingHooks++, OnFinally = _ => pendingHooks++ },
        }, options: new PlaybackOptions { ProcessMode = TweenProcessMode.Physics });
        Assert.Equal(connections + 1, node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
        scheduler.Update(0.5, mode: TweenProcessMode.Physics);
        Assert.Equal(new Vector2(2.5f, 5), node.Position);
        Assert.Equal(2, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        chain.Pause();
        node.QueueFree();
        scheduler.Update(10); // Lifetime checks still run on the process lane.

        Assert.Equal(Reason.TargetFreed, await chain.End);
        Assert.True(chain.IsSettled);
        Assert.Null(chain.Error);
        Assert.Equal(new Vector2(2.5f, 5), node.Position);
        Assert.Equal(0, node.Rotation);
        if (suppress) Assert.Empty(callbacks);
        else Assert.Equal(new[] { "x cancel", "x finally", "y cancel", "y finally" }, callbacks);
        Assert.Equal(0, pendingHooks);
        Assert.Equal(3, chain.EntryCount);
        Assert.Equal(0, chain.ActiveCount);
        Assert.Equal(0, chain.PendingCount);
        Assert.Equal(0, scheduler.ActiveCount);
        Assert.Equal(connections, node.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
    }

    [Theory]
    [InlineData("resource", "target-disposed", false)]
    [InlineData("resource", "target-disposed", true)]
    [InlineData("resource", "owner-queued", false)]
    [InlineData("resource", "owner-queued", true)]
    [InlineData("resource", "owner-removed", false)]
    [InlineData("resource", "owner-removed", true)]
    [InlineData("resource", "cancel", true)]
    [InlineData("managed", "owner-queued", true)]
    [InlineData("managed", "owner-removed", true)]
    [InlineData("managed", "owner-freed", false)]
    [InlineData("managed", "owner-freed", true)]
    [InlineData("managed", "cancel", true)]
    public async Task OwnedChainsSuppressInvalidLifetimeCallbacksButKeepHealthyCancellationCallbacks(
        string targetKind, string invalidation, bool suppress)
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var owner = scope.Add(new Node());
        var resource = scope.Track(new StandardMaterial3D { Metallic = 0 });
        var box = new Box();
        object target = targetKind == "managed" ? box : resource;
        var callbacks = new List<string>();
        var leaves = new List<TweenInstance>();
        var pendingHooks = 0;
        float Read(object value) => value is Box b ? b.Value : ((StandardMaterial3D)value).Metallic;
        void Write(object value, float sample)
        {
            if (value is Box b) b.Value = sample;
            else ((StandardMaterial3D)value).Metallic = sample;
        }
        PropertyTween<object, float> Definition(string name, double delay) => new(Read, Write, Interpolators.Float) {
            From = 0, To = 1, Duration = 2, Delay = delay,
            SuppressCallbacksWhenTargetInvalid = suppress,
            OnAdd = h => leaves.Add(h),
            OnCancel = _ => callbacks.Add(name + " cancel"), OnFinally = _ => callbacks.Add(name + " finally"),
        };
        var connections = owner.GetSignalConnectionList(Node.SignalName.TreeExiting).Count;
        var chain = scheduler.AddChain(target, new ITweenDefinition<object>[] {
            Definition("first", 0), Definition("second", -2),
            new PropertyTween<object, float>(value => { pendingHooks++; return Read(value); }, Write, Interpolators.Float) {
                To = 1, Duration = 1, OnAdd = _ => pendingHooks++,
                OnCancel = _ => pendingHooks++, OnFinally = _ => pendingHooks++,
            },
        }, owner, new PlaybackOptions { ProcessMode = TweenProcessMode.Physics });
        Assert.Equal(connections + 1, owner.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
        scheduler.Update(0.5, mode: TweenProcessMode.Physics);
        Assert.Equal(0.25f, Read(target));
        Assert.Equal(2, leaves.Count);
        Assert.Equal(2, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        Assert.Equal(1, scheduler.ActiveCount);
        chain.Pause();
        try
        {
            switch (invalidation)
            {
                case "target-disposed": resource.Dispose(); break;
                case "owner-queued": owner.QueueFree(); break;
                case "owner-removed": scope.Root.RemoveChild(owner); break;
                case "owner-freed": owner.Free(); break;
                case "cancel": chain.Cancel(); break;
            }
            scheduler.Update(10);

            var reason = invalidation == "target-disposed" ? Reason.TargetFreed
                : invalidation == "cancel" ? Reason.Cancelled : Reason.OwnerExited;
            Assert.Equal(reason, await chain.End);
            Assert.True(chain.IsSettled);
            Assert.Null(chain.Error);
            foreach (var leaf in leaves) Assert.Equal(reason, await leaf.End);
            if (suppress && invalidation != "cancel") Assert.Empty(callbacks);
            else Assert.Equal(new[] { "first cancel", "first finally", "second cancel", "second finally" }, callbacks);
            Assert.Equal(0, pendingHooks);
            Assert.Equal(3, chain.EntryCount);
            Assert.Equal(0, chain.ActiveCount);
            Assert.Equal(0, chain.PendingCount);
            Assert.Equal(0, scheduler.ActiveCount);
            if (invalidation != "target-disposed") Assert.Equal(0.25f, Read(target));
            Assert.Equal(invalidation != "target-disposed", GodotObject.IsInstanceValid(resource));
            if (GodotObject.IsInstanceValid(owner))
                Assert.Equal(connections, owner.GetSignalConnectionList(Node.SignalName.TreeExiting).Count);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(owner) && !owner.IsInsideTree()) owner.Free();
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("end")]
    public async Task BoundPolicyChangesAtLeafBoundariesDeferWritesAndSuccessorCapture(string hook)
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var node = scope.Add(new Node2D());
        var firstAdds = 0;
        var firstEnds = 0;
        var nextAdds = 0;
        var chain = scheduler.AddChain(node, new ITweenDefinition<Node2D>[] {
            new Tweens.Position2DX(10, 1) {
                OnAdd = _ => { firstAdds++; if (hook == "add") node.ProcessMode = Node.ProcessModeEnum.Disabled; },
                OnEnd = _ => { firstEnds++; if (hook == "end") node.ProcessMode = Node.ProcessModeEnum.Disabled; },
            },
            new Tweens.Position2DY(20, 1) { OnAdd = _ => nextAdds++ },
        });
        scheduler.Update(10);
        Assert.False(chain.IsPaused); // Owner policy holds playback without setting the explicit pause flag.
        Assert.False(chain.IsTerminal);
        Assert.Equal(hook == "add" ? 0 : 1, chain.Elapsed);
        Assert.Equal(hook == "add" ? 0 : 10, node.Position.X);
        Assert.Equal(0, node.Position.Y);
        Assert.Equal(hook == "add" ? 1 : 0, chain.ActiveCount);
        Assert.Equal(1, chain.PendingCount);
        Assert.Equal(1, firstAdds);
        Assert.Equal(0, nextAdds);
        scheduler.Update(10);
        Assert.Equal(0, nextAdds);
        node.ProcessMode = Node.ProcessModeEnum.Inherit;
        scheduler.Update(0.5);
        Assert.Equal(hook == "add" ? new Vector2(5, 0) : new Vector2(10, 10), node.Position);
        Assert.Equal(hook == "add" ? 0 : 1, nextAdds);
        Assert.Equal(1, firstAdds);
        scheduler.Update(10);

        Assert.Equal(Reason.Completed, await chain.End);
        Assert.Equal(new Vector2(10, 20), node.Position);
        Assert.Equal(1, firstAdds);
        Assert.Equal(1, firstEnds);
        Assert.Equal(1, nextAdds);
        Assert.Equal(0, scheduler.ActiveCount);
    }
}
