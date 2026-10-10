// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Text.Json;
using Godot;
using tweens.gd.Tests.Support;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Integration;

[Collection<HeadlessCollection>]
public class SampledTests(HeadlessFixture godot)
{
    private sealed class Box { public double Amount; public Color Tint; }

    [Fact]
    public void SameCurveUsesCustomAndShaderStorageWithOrdinaryTimingAndRestoration()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance", "keyframes.json")));
        var test = data.RootElement.GetProperty("binding_playback");
        var initial = test.GetProperty("initial").GetDouble();
        var box = new Box { Amount = initial };
        var material = scope.Track(new ShaderMaterial { Shader = scope.Track(new Shader { Code = "shader_type canvas_item; uniform float amount;" }) });
        material.SetShaderParameter("amount", initial);
        var keys = test.GetProperty("keys").EnumerateArray().Select(k => new CurveKey<double>(k[0].GetDouble(), k[1].GetDouble())).ToArray();
        var curve = new KeyframeCurve<double>(keys, Interpolation.Linear);
        var options = new TweenOptions { Duration = 1, Delay = 0.25, Fill = FillMode.None };
        var updates = 0;
        var property = new Tweens.Property<Box, double>(static b => b.Amount, static (b, v) => b.Amount = v,
            Interpolators.Double, 999) { Options = options, OnUpdate = (_, _) => updates++ };
        var custom = scheduler.Add(box, property.Through(curve));
        var shader = scheduler.Add(material, new Tweens.ShaderParameter<double>("amount", 999) { Options = options }.Through(curve));
        foreach (var step in test.GetProperty("steps").EnumerateArray())
        {
            scheduler.Update(step[0].GetDouble());
            Assert.Equal(step[1].GetDouble(), box.Amount, 6);
            Assert.Equal(step[1].GetDouble(), material.GetShaderParameter("amount").AsDouble(), 6);
        }
        Assert.Equal(Reason.Completed, custom.CompletionReason);
        Assert.Equal(Reason.Completed, shader.CompletionReason);
        Assert.Equal(6, updates);
    }

    [Fact]
    public void IndependentBindingsCaptureStartsAndFreezeConfiguration()
    {
        using var scheduler = new TweenScheduler();
        var events = new List<string>();
        var source = new Probe(events) { Field = "amount" };
        var curve = new KeyframeCurve<double>([new(100, 10)]);
        var sampled = new Tweens.Sampled<Box, double>(source, curve, new TweenTiming { Duration = 1, Fill = FillMode.None });
        source.Field = "other";
        var a = new Box { Amount = 2 }; var b = new Box { Amount = 6 };
        var first = scheduler.Add(a, sampled); var second = scheduler.Add(b, sampled);
        a.Amount = 4;
        scheduler.Update(0.5);
        Assert.Equal(7, a.Amount, 6); Assert.Equal(8, b.Amount, 6);
        first.Cancel(); scheduler.Update(0.5);
        Assert.Equal(7, a.Amount, 6); Assert.Equal(6, b.Amount, 6);
        Assert.Equal(2, events.Count(e => e == "prepare:amount"));
        Assert.Equal(2, events.Count(e => e == "release"));
        Assert.Equal(Reason.Completed, second.CompletionReason);
        Assert.Same(curve, sampled.Curve);
    }

    private sealed class Probe(List<string> events) : TweenBinding<Box, double>
    {
        public string Field = "amount";
        public bool Fail;
        protected override void Prepare(Box target)
        {
            events.Add("prepare:" + Field);
            if (Fail) throw new InvalidOperationException("binding preparation");
        }
        protected override double Read(Box target) => target.Amount;
        protected override void Write(Box target, double value) => target.Amount = value;
        protected override void Release() => events.Add("release");
    }

    [Fact]
    public void FailedBindingPreparationReleasesItsSnapshotOnce()
    {
        using var scheduler = new TweenScheduler();
        var events = new List<string>();
        var definition = new Tweens.Sampled<Box, double>(new Probe(events) { Fail = true },
            KeyframeCurve<double>.EvenlySpaced([0, 1]), new TweenTiming { Duration = 1 });
        var tween = scheduler.Add(new Box(), definition);
        scheduler.Update(0.5);
        Assert.Equal(TweenState.Faulted, tween.State);
        Assert.IsType<InvalidOperationException>(tween.Error);
        Assert.Equal(["prepare:amount", "release"], events);
        tween.Cancel(); Assert.Equal(2, events.Count);
    }

    [Fact]
    public void ColorSpecializationsSharePolicyWithCurvesAndKeepShortPlaybackSyntax()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var box = new Box { Tint = Colors.Red };
        var source = new Tweens.ColorProperty<Box>(static b => b.Tint, static (b, v) => b.Tint = v, Colors.Blue, 1)
        { ColorSpace = ColorSpace.Srgb, AlphaMode = AlphaMode.Straight };
        var sampled = source.Through([Colors.Red, Colors.Blue]);
        Assert.Equal(ColorSpace.Srgb, sampled.Curve.Policy.Space);
        scheduler.Add(box, sampled); scheduler.Update(0.5);
        Assert.Equal(new Color(0.5f, 0, 0.5f), box.Tint);
        scheduler.CancelAll();
        scheduler.Add(box, source); scheduler.Update(0.5);
        Assert.Equal(new Color(0.25f, 0, 0.75f), box.Tint);

        var material = scope.Track(new ShaderMaterial { Shader = scope.Track(new Shader { Code = "shader_type canvas_item; uniform vec4 tint : source_color;" }) });
        material.SetShaderParameter("tint", Colors.Red);
        var tint = new Tweens.ColorShaderParameter("tint", Colors.Blue, 1)
        { ColorSpace = ColorSpace.Srgb, AlphaMode = AlphaMode.Straight };
        var owner = scope.Add(new Node());
        var played = material.Tween(tint.Through([Colors.Red, Colors.Blue]), owner);
        scope.Advance(0.5);
        Assert.Equal(new Color(0.5f, 0, 0.5f), material.GetShaderParameter("tint").AsColor());
        played.Cancel();
        material.Tween(tint, owner).Cancel();
    }

    [Fact]
    public void ExplicitSamplerPolicyAndTimingAreIndependent()
    {
        using var scheduler = new TweenScheduler();
        var binding = new PropertyBinding<Box, Color>(static b => b.Tint, static (b, v) => b.Tint = v);
        var curve = KeyframeCurve<Color>.EvenlySpaced([Colors.Red, Colors.Blue], colorSpace: ColorSpace.Srgb, alphaMode: AlphaMode.Straight);
        var original = new Tweens.Sampled<Box, Color>(binding, curve, new TweenTiming { Duration = 1 });
        var slower = original with { Timing = original.Timing with { Duration = 2 } };
        var a = new Box(); var b = new Box();
        scheduler.Add(a, original); scheduler.Add(b, slower); scheduler.Update(0.5);
        Assert.Equal(new Color(0.5f, 0, 0.5f), a.Tint);
        Assert.Equal(new Color(0.75f, 0, 0.25f), b.Tint);
        Assert.Same(original.Curve, slower.Curve);
        Assert.Throws<ArgumentNullException>(() => new PropertyBinding<Box, double>(null!, static (_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => new PropertyBinding<Box, double>(static _ => 0, null!));
    }

    [Fact]
    public void OneBindingSupportsEndpointAndCurveSamplingIncludingRelativeRestoration()
    {
        using var scheduler = new TweenScheduler();
        var binding = new PropertyBinding<Box, double>(static b => b.Amount, static (b, v) => b.Amount = v);
        var box = new Box { Amount = 2 };
        var endpoints = new PropertyTween<Box, double>(binding, Interpolators.Double) { By = 4, Duration = 1, Fill = FillMode.None };
        scheduler.Add(box, endpoints); scheduler.Update(0.5);
        Assert.Equal(4, box.Amount);
        box.Amount = 10;
        scheduler.Update(0.5);
        Assert.Equal(8, box.Amount);
        var curve = KeyframeCurve<double>.EvenlySpaced([0, 4]);
        scheduler.Add(box, new Tweens.Sampled<Box, double>(binding, curve, new TweenTiming { Duration = 1, Fill = FillMode.None }));
        scheduler.Update(0.5); Assert.Equal(2, box.Amount);
        scheduler.Update(0.5); Assert.Equal(8, box.Amount);
        Assert.Throws<ArgumentNullException>(() => new PropertyTween<Box, double>((ITweenBinding<Box, double>)null!, Interpolators.Double));
    }

    [Fact]
    public void SampledStartsKeepNodeResourceAndOwnerConventions()
    {
        using var scope = new SceneScope(godot);
        using var scheduler = new TweenScheduler();
        var owner = scope.Add(new Node2D());
        var motion = new Tweens.Position2DX(10, 1).Through([0f, 10f]);
        var a = owner.Tween(motion);
        scope.Advance(0.5); Assert.Equal(5, owner.Position.X); a.Cancel();
        scheduler.Add(owner, motion, owner).Cancel();
        Assert.Throws<ArgumentNullException>(() => scheduler.Add(owner, motion, (Node)null!));
        var material = scope.Track(new ShaderMaterial { Shader = scope.Track(new Shader { Code = "shader_type canvas_item; uniform float amount;" }) });
        material.SetShaderParameter("amount", 0f);
        var sampled = new Tweens.ShaderParameter<float>("amount", 1, 1).Through([0f, 1f]);
        var b = material.Tween(sampled, godot.Tree);
        scope.Advance(0.5); Assert.Equal(0.5f, material.GetShaderParameter("amount").AsSingle()); b.Cancel();
        owner.Tween(material, sampled).Cancel();
    }

    [Fact]
    public void PreparedBindingAndCurvePlaybackSamplesWithoutManagedAllocations()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        var source = new Tweens.Property<Box, double>(static b => b.Amount, static (b, v) => b.Amount = v,
            Interpolators.Double, 1, 1).Through([0d, 1d, 0d]);
        scheduler.Add(box, source);
        for (var i = 0; i < 128; i++) scheduler.Update(0.00001);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) scheduler.Update(0.00001);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void ThroughRejectsRelativeEndpointsAndAdjustments()
    {
        var curve = KeyframeCurve<double>.EvenlySpaced([0, 1]);
        foreach (var definition in new[] {
            new DoubleTween { By = 1 }, new DoubleTween { FactorFrom = 2 }, new DoubleTween { FactorTo = 2 },
            new DoubleTween { FactorBy = 2 }, new DoubleTween { DeltaFrom = 1 }, new DoubleTween { DeltaTo = 1 }, new DoubleTween { DeltaBy = 1 },
        }) Assert.Throws<ArgumentException>(() => definition.Through(curve));
    }
}
