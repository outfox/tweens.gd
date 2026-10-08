// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;

namespace TweensBenchmarks;

// Mirrors the valid target preconditions in the adapter conformance tests.
// Owned nodes are freed before their backing resources.
internal sealed class CatalogTargets(Node root) : IDisposable
{
    private readonly Node owner = new();
    private readonly List<GodotObject> resources = [];
    private Shader? shader;
    internal void Initialize() => root.AddChild(owner);
    private T Track<T>(T resource) where T : GodotObject { resources.Add(resource); return resource; }
    private void Add(Node node) => owner.AddChild(node);

    internal object Create(Type target, string family, string shaderType)
    {
        if (!typeof(GodotObject).IsAssignableFrom(target)) return Activator.CreateInstance(target)!;
        if (family.Contains("ShaderParameter", StringComparison.Ordinal))
        {
            var spatial = target == typeof(GeometryInstance3D);
            var instance = family.Contains("Instance", StringComparison.Ordinal);
            var declaration = shaderType == "vec4 : source_color" ? "vec4 amount : source_color" : shaderType + " amount";
            shader ??= Track(new Shader { Code = $"shader_type {(spatial ? "spatial" : "canvas_item")}; {(instance ? "instance " : "")}uniform {declaration};" });
            var material = Track(new ShaderMaterial { Shader = shader });
            if (!instance) return material;
            Node node = spatial
                ? new MeshInstance3D { Mesh = Track(new BoxMesh()), MaterialOverride = material }
                : new ColorRect { Material = material };
            Add(node);
            return node;
        }
        var concrete = target == typeof(CanvasItem) ? typeof(Node2D)
            : target == typeof(Godot.Range) ? typeof(ProgressBar)
            : target == typeof(SpriteBase3D) ? typeof(Sprite3D)
            : target == typeof(GeometryInstance3D) ? typeof(MeshInstance3D)
            : target == typeof(Light2D) ? typeof(PointLight2D)
            : target == typeof(Light3D) ? typeof(OmniLight3D)
            : target == typeof(BaseMaterial3D) ? typeof(StandardMaterial3D) : target;
        var result = (GodotObject)Activator.CreateInstance(concrete)!;
        if (result is Resource resource) return Track(resource);
        var n = (Node)result;
        if (n is Control control) { control.Size = new Vector2(100, 200); control.OffsetTransformEnabled = true; }
        switch (n)
        {
            case Godot.Range range: range.Step = 0; break;
            case Label label: label.Text = new string('a', 100); break;
            case RichTextLabel rich: rich.Text = new string('a', 100); break;
            case Sprite2D sprite: sprite.Hframes = 10; sprite.RegionEnabled = true; break;
            case AnimatedSprite2D sprite: sprite.SpriteFrames = Frames(); break;
            case AnimatedSprite3D sprite: sprite.SpriteFrames = Frames(); break;
            case GpuParticles2D particles: particles.Emitting = false; break;
            case GpuParticles3D particles: particles.Emitting = false; break;
            case CpuParticles2D particles: particles.Emitting = false; break;
            case CpuParticles3D particles: particles.Emitting = false; break;
        }
        switch (n)
        {
            case PathFollow2D follow:
                var curve2 = Track(new Curve2D()); curve2.AddPoint(Vector2.Zero); curve2.AddPoint(new Vector2(100, 0));
                var path2 = new Path2D { Curve = curve2 }; Add(path2); path2.AddChild(follow); follow.Loop = false;
                break;
            case PathFollow3D follow:
                var curve3 = Track(new Curve3D()); curve3.AddPoint(Vector3.Zero); curve3.AddPoint(new Vector3(100, 0, 0));
                var path3 = new Path3D { Curve = curve3 }; Add(path3); path3.AddChild(follow); follow.Loop = false;
                break;
            default: Add(n); break;
        }
        if (n is ScrollContainer scroll)
        {
            scroll.GetHScrollBar().MaxValue = scroll.GetVScrollBar().MaxValue = 1000;
            scroll.GetHScrollBar().Page = scroll.GetVScrollBar().Page = 100;
        }
        return n;
    }

    private SpriteFrames Frames()
    {
        var frames = Track(new SpriteFrames());
        for (var i = 0; i < 10; i++) frames.AddFrame("default", null!);
        return frames;
    }

    public void Dispose()
    {
        owner.Free();
        for (var i = resources.Count - 1; i >= 0; i--) resources[i].Dispose();
    }
}

internal sealed class CatalogValue<T> where T : struct { public T Value; }

internal static class CatalogValues
{
    internal static readonly Type[] Types = [typeof(float), typeof(double), typeof(int), typeof(Vector2), typeof(Vector3),
        typeof(Vector4), typeof(Color), typeof(Quaternion), typeof(Rect2)];
    internal static readonly Type[] ShaderTypes = Types[..7];
    internal static string ShaderType(Type type) => type == typeof(int) ? "int" : type == typeof(Vector2) ? "vec2"
        : type == typeof(Vector3) ? "vec3" : type == typeof(Vector4) ? "vec4" : type == typeof(Color) ? "vec4 : source_color" : "float";
    internal static object Nudge(object value) => value switch
    {
        float x => Float(x), double x => (double)Float((float)x), int x => x + 3,
        Vector2 x => new Vector2(Float(x.X), Float(x.Y)),
        Vector3 x => new Vector3(Float(x.X), Float(x.Y), Float(x.Z)),
        Vector4 x => new Vector4(Float(x.X), Float(x.Y), Float(x.Z), Float(x.W)),
        Color x => new Color(Float(x.R), Float(x.G), Float(x.B), Float(x.A)),
        Quaternion x => ((x.LengthSquared() == 0 ? Quaternion.Identity : x) * new Quaternion(Vector3.Up, 0.5f)).Normalized(),
        Rect2 x => new Rect2(x.Position + new Vector2(0.5f, 0.5f), x.Size + new Vector2(0.5f, 0.5f)),
        _ => throw new NotSupportedException(value.GetType().Name),
    };
    private static float Float(float x) => x <= 0 ? 0.5f : x * 0.75f;
    internal static bool Close(object expected, object actual) => expected switch
    {
        float x => Mathf.IsEqualApprox(x, (float)actual),
        double x => Math.Abs(x - (double)actual) <= 0.00001 * Math.Max(1, Math.Abs(x)),
        Vector2 x => x.IsEqualApprox((Vector2)actual),
        Vector3 x => x.IsEqualApprox((Vector3)actual),
        Vector4 x => x.IsEqualApprox((Vector4)actual),
        Color x => x.IsEqualApprox((Color)actual),
        Quaternion x => x.IsEqualApprox((Quaternion)actual),
        Rect2 x => x.IsEqualApprox((Rect2)actual),
        _ => expected.Equals(actual),
    };
}
