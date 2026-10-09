// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace tweens.gd.Tests.Rendering;

/// <summary>Uniforms start from the shader default or their override, and restore whichever one they found.</summary>
[Collection<RenderingCollection>]
public class ShaderDefaultTests(Fixture godot)
{
    private const string Uniforms = "uniform float scalar = 0.25; uniform int count = 2; "
        + "uniform vec2 pair = vec2(0.25); uniform vec3 triple = vec3(0.25); uniform vec4 quad = vec4(0.25); "
        + "uniform vec4 tint : source_color = vec4(0.25);";

    [Fact]
    public void MaterialUniformsStartFromTheirDefaultAndRemoveTheOverride()
    {
        using var shader = new Shader { Code = "shader_type canvas_item; uniform float amount = 0.25;" };
        using var material = new ShaderMaterial { Shader = shader };
        using var scheduler = new TweenScheduler();
        var tween = scheduler.Add(material, new Tweens.ShaderParameter<float>("amount") { To = 1, Duration = 1, Fill = FillMode.None });
        Assert.Equal(0, tween.Value);
        scheduler.Update(0);
        Assert.Equal(0.25f, tween.Value);
        scheduler.Update(0.5);
        Assert.Equal(0.625f, material.GetShaderParameter("amount").AsSingle());
        scheduler.Update(0.5);
        Assert.Equal(Reason.Completed, tween.CompletionReason);
        Assert.Equal(Variant.Type.Nil, material.GetShaderParameter("amount").VariantType);
    }

    [Theory]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("int")]
    [InlineData("Vector2")]
    [InlineData("Vector3")]
    [InlineData("Vector4")]
    [InlineData("Color")]
    public void MaterialUniformsOfEveryTypeStartFromTheirDefaultAndRestoreAnOverride(string type)
    {
        using var shader = new Shader { Code = "shader_type canvas_item; " + Uniforms };
        using var material = new ShaderMaterial { Shader = shader };
        switch (type)
        {
            case "float": Check(material, godot.Tree, "scalar", 0.25f, 0.75f, 0.5f); break;
            case "double": Check(material, godot.Tree, "scalar", 0.25d, 0.75d, 0.5d); break;
            case "int": Check(material, godot.Tree, "count", 2, 5, 4); break;
            case "Vector2": Check(material, godot.Tree, "pair", Vector2.One * 0.25f, Vector2.One * 0.75f, Vector2.One * 0.5f); break;
            case "Vector3": Check(material, godot.Tree, "triple", Vector3.One * 0.25f, Vector3.One * 0.75f, Vector3.One * 0.5f); break;
            case "Vector4": Check(material, godot.Tree, "quad", Vector4.One * 0.25f, Vector4.One * 0.75f, Vector4.One * 0.5f); break;
            case "Color":
                Check(material, godot.Tree, "tint", new Color(0.25f, 0.25f, 0.25f, 0.25f), new Color(0.75f, 0.75f, 0.75f, 0.75f),
                    new Color(0.616543f, 0.616543f, 0.616543f, 0.5f));
                break;
        }

        static void Check<[MustBeVariant] T>(ShaderMaterial material, SceneTree tree, string name, T initial, T to, T middle)
            where T : struct
        {
            var scheduler = TweenRuntime.GetRunner(tree).Scheduler;
            var tween = material.TweenShaderParameter(name, to, 1, tree, d => d.Fill = FillMode.None);
            scheduler.Update(0);
            Assert.Equal(initial, tween.Value);
            scheduler.Update(0.5);
            Assert.Null(tween.Error);
            var sampled = material.GetShaderParameter(name).As<T>();
            if (middle is Color color) Assert.True(color.IsEqualApprox((Color)(object)sampled));
            else Assert.Equal(middle, sampled);
            scheduler.Update(0.5);
            Assert.Equal(TweenState.Completed, tween.State);
            Assert.Equal(Variant.Type.Nil, material.GetShaderParameter(name).VariantType);

            // An override equal to the default is still restored as an override.
            material.SetShaderParameter(name, Variant.From(initial));
            var overridden = material.TweenShaderParameter(name, to, 1, tree, d => d.Fill = FillMode.None);
            scheduler.Update(1);
            Assert.Equal(TweenState.Completed, overridden.State);
            Assert.Equal(initial, material.GetShaderParameter(name).As<T>());
        }
    }

    [Fact]
    public void DefinitionReuseCapturesParameterAndOverrideIndependently()
    {
        using var shader = new Shader { Code = "shader_type canvas_item; uniform float amount = 0.25; uniform float other = 1.0;" };
        using var first = new ShaderMaterial { Shader = shader };
        using var second = new ShaderMaterial { Shader = shader };
        second.SetShaderParameter("amount", 0.5f);
        using var scheduler = new TweenScheduler();
        var definition = new Tweens.ShaderParameter<float>("amount") { To = 1, Duration = 1, Fill = FillMode.None };
        var a = scheduler.Add(first, definition);
        var b = scheduler.Add(second, definition);
        definition = definition with { Parameter = "other", To = 0 };
        scheduler.Update(0.5);
        Assert.Equal(0.625f, first.GetShaderParameter("amount").AsSingle());
        Assert.Equal(0.75f, second.GetShaderParameter("amount").AsSingle());
        scheduler.Update(0.5);
        Assert.Equal(TweenState.Completed, a.State);
        Assert.Equal(TweenState.Completed, b.State);
        Assert.Equal(Variant.Type.Nil, first.GetShaderParameter("amount").VariantType);
        Assert.Equal(0.5f, second.GetShaderParameter("amount").AsSingle());
        Assert.Equal(Variant.Type.Nil, first.GetShaderParameter("other").VariantType);
    }

    [Fact]
    public void OwnerOverloadUsesSharedResourceAndCancellationRetainsTheSample()
    {
        using var shader = new Shader { Code = "shader_type canvas_item; uniform float amount = 0.25;" };
        using var material = new ShaderMaterial { Shader = shader };
        var first = new ColorRect { Material = material };
        var second = new ColorRect { Material = material };
        godot.Tree.Root.AddChild(first);
        godot.Tree.Root.AddChild(second);
        try
        {
            var tween = material.TweenShaderParameter("amount", 0.75f, 1, first);
            TweenRuntime.GetRunner(first).Scheduler.Update(0.5);
            Assert.Equal(0.5f, ((ShaderMaterial)second.Material).GetShaderParameter("amount").AsSingle());
            first.CancelTweens();
            Assert.Equal(Reason.Cancelled, tween.CompletionReason);
            Assert.Equal(0.5f, material.GetShaderParameter("amount").AsSingle());
            Assert.Same(material, first.Material);
            Assert.Same(material, second.Material);
        }
        finally
        {
            first.Free();
            second.Free();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void InstanceUniformsRestoreTheirOverrideOrItsAbsence(bool spatial, bool explicitOverride)
    {
        using var shader = new Shader
        {
            Code = $"shader_type {(spatial ? "spatial" : "canvas_item")}; instance uniform float amount = 0.25;",
        };
        using var material = new ShaderMaterial { Shader = shader };
        using var mesh = new BoxMesh();
        var node = Instance(spatial, material, mesh);
        var other = Instance(spatial, material, mesh);
        godot.Tree.Root.AddChild(node);
        godot.Tree.Root.AddChild(other);
        try
        {
            if (explicitOverride) Set(node, "amount", 0.25f);
            using var scheduler = new TweenScheduler();
            TweenInstance tween = node is CanvasItem canvas
                ? scheduler.Add(canvas, new Tweens.CanvasItemInstanceShaderParameter<float>("amount") { To = 1, Duration = 1, Fill = FillMode.None })
                : scheduler.Add((GeometryInstance3D)node, new Tweens.GeometryInstanceShaderParameter<float>("amount") { To = 1, Duration = 1, Fill = FillMode.None });
            Assert.Equal(explicitOverride, HasOverride(node, "amount"));
            scheduler.Update(0.5);
            Assert.Null(tween.Error);
            Assert.Equal(0.625f, Get(node, "amount").AsSingle());
            Assert.Equal(0.25f, Get(other, "amount").AsSingle());
            Assert.True(HasOverride(node, "amount"));
            scheduler.Update(0.5);
            Assert.Equal(Reason.Completed, tween.CompletionReason);
            Assert.Equal(0.25f, Get(node, "amount").AsSingle());
            Assert.Equal(explicitOverride, HasOverride(node, "amount"));
        }
        finally
        {
            node.Free();
            other.Free();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InstanceUniformsOfEveryTypeStartFromTheirDefault(bool spatial)
    {
        using var shader = new Shader
        {
            Code = $"shader_type {(spatial ? "spatial" : "canvas_item")}; " + Uniforms.Replace("uniform ", "instance uniform "),
        };
        using var material = new ShaderMaterial { Shader = shader };
        using var mesh = new QuadMesh();
        var node = Instance(spatial, material, mesh);
        godot.Tree.Root.AddChild(node);
        try
        {
            Check("scalar", 0.75f, 0.5f);
            Check("scalar", 0.75d, 0.5d);
            Check("count", 5, 4);
            Check("pair", Vector2.One * 0.75f, Vector2.One * 0.5f);
            Check("triple", Vector3.One * 0.75f, Vector3.One * 0.5f);
            Check("quad", Vector4.One * 0.75f, Vector4.One * 0.5f);
            Check("tint", new Color(0.75f, 0.75f, 0.75f, 0.75f), new Color(0.616543f, 0.616543f, 0.616543f, 0.5f));
        }
        finally { node.Free(); }

        void Check<[MustBeVariant] T>(string name, T to, T middle) where T : struct
        {
            TweenInstance tween = spatial
                ? ((GeometryInstance3D)node).TweenInstanceShaderParameter(name, to, 1, d => d.Fill = FillMode.None)
                : ((CanvasItem)node).TweenInstanceShaderParameter(name, to, 1, d => d.Fill = FillMode.None);
            var scheduler = TweenRuntime.GetRunner(node).Scheduler;
            scheduler.Update(0.5);
            Assert.Null(tween.Error);
            var sampled = Get(node, name).As<T>();
            if (middle is Color color) Assert.True(color.IsEqualApprox((Color)(object)sampled));
            else Assert.Equal(middle, sampled);
            scheduler.Update(0.5);
            Assert.Equal(TweenState.Completed, tween.State);
            Assert.False(HasOverride(node, name));
        }
    }

    // Instance uniforms may come from a parent's canvas material, the mesh's own surface material or a next pass.
    [Theory]
    [InlineData("parent")]
    [InlineData("mesh")]
    [InlineData("next-pass")]
    public void InstanceUniformsStartFromTheDefaultOfAnIndirectMaterial(string binding)
    {
        using var shader = new Shader
        {
            Code = $"shader_type {(binding == "parent" ? "canvas_item" : "spatial")}; instance uniform float amount = 0.25;",
        };
        using var material = new ShaderMaterial { Shader = shader };
        using var passes = new StandardMaterial3D { NextPass = material };
        using var mesh = new QuadMesh { Material = binding == "next-pass" ? passes : material };
        Node node = binding == "parent" ? new ColorRect { UseParentMaterial = true } : new MeshInstance3D { Mesh = mesh };
        var root = node;
        if (binding == "parent")
        {
            root = new Node2D { Material = material };
            root.AddChild(node);
        }
        godot.Tree.Root.AddChild(root);
        try
        {
            TweenInstance tween = node is CanvasItem canvas
                ? canvas.TweenInstanceShaderParameter("amount", 1f, 1, d => d.Fill = FillMode.None)
                : ((GeometryInstance3D)node).TweenInstanceShaderParameter("amount", 1f, 1, d => d.Fill = FillMode.None);
            var scheduler = TweenRuntime.GetRunner(node).Scheduler;
            scheduler.Update(0.5);
            Assert.Null(tween.Error);
            Assert.Equal(0.625f, Get(node, "amount").AsSingle());
            Assert.True(HasOverride(node, "amount"));
            scheduler.Update(0.5);
            Assert.Equal(Reason.Completed, tween.CompletionReason);
            Assert.Equal(0.25f, Get(node, "amount").AsSingle());
            Assert.False(HasOverride(node, "amount"));
        }
        finally { root.Free(); }
    }

    private static Node Instance(bool spatial, ShaderMaterial material, Mesh mesh)
        => spatial ? new MeshInstance3D { Mesh = mesh, MaterialOverride = material } : new ColorRect { Material = material };

    private static Variant Get(Node node, string name) => node is CanvasItem canvas
        ? canvas.GetInstanceShaderParameter(name)
        : ((GeometryInstance3D)node).GetInstanceShaderParameter(name);

    private static void Set(Node node, string name, Variant value)
    {
        if (node is CanvasItem canvas) canvas.SetInstanceShaderParameter(name, value);
        else ((GeometryInstance3D)node).SetInstanceShaderParameter(name, value);
    }

    // Without an override the getter reports the default; only the property's storage flag tells them apart.
    private static bool HasOverride(Node node, string name)
    {
        foreach (var property in node.GetPropertyList())
            using (property)
                if (property["name"].AsString() == "instance_shader_parameters/" + name)
                    return ((PropertyUsageFlags)property["usage"].AsInt64() & PropertyUsageFlags.Storage) != 0;
        throw new InvalidOperationException("The instance uniform is not listed.");
    }
}
