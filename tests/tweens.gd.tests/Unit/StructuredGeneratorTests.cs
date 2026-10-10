// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.Json;
using tweens.gd.Generators;

namespace tweens.gd.Tests.Unit;

public class StructuredGeneratorTests
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => Path.GetDirectoryName(path) == Path.GetDirectoryName(typeof(object).Assembly.Location))
        .Select(static MetadataReference (path) => MetadataReference.CreateFromFile(path))
        .ToArray();

    // Compile an independent library to exercise semantic discovery and generated
    // C# without relying on Godot or existing generated definitions.
    private const string Contracts = """
        #nullable enable
        using System;
        namespace Godot
        {
            public class SceneTree { }
            public class Node { public SceneTree GetTree() => new(); }
            public class Resource { public float Value { get; set; } }
        }
        namespace tweens.gd
        {
            public readonly record struct PlaybackOptions;
            public sealed partial class TweenScheduler
            {
                internal TweenInstance<TTarget, TValue> AddValue<TTarget, TValue, TDefinition>(TTarget target,
                    in TDefinition definition, Godot.Node? owner, Godot.SceneTree? tree, PlaybackOptions options)
                    where TTarget : class where TValue : struct where TDefinition : struct, ITweenDefinition<TTarget, TValue>
                    => new();
            }
            internal sealed class TweenRunner { internal TweenScheduler Scheduler { get; } = new(); }
            internal static class TweenRuntime
            {
                internal static TweenRunner GetRunner(Godot.Node target) => new();
                internal static TweenRunner GetRunner(Godot.SceneTree tree) => new();
                internal static void ValidateOwner(Godot.Node owner) { }
            }
            public enum EaseType { Linear }
            public readonly record struct Duration(double Seconds);
            public readonly record struct TweenOptions
            {
                public EaseType Ease { get; init; }
                public Duration Duration { get; init; }
                public Func<float, float>? EaseFunction { get; init; }
                internal void CopyTo(TweenOptionsBuilder target) { }
            }
            public class TweenOptionsBuilder
            {
                public EaseType Ease { get; set; }
                public Duration Duration { get; set; }
                public Func<float, float>? EaseFunction { get; set; }
                public static int Ignored { get; set; }
                public int ReadOnly => 0;
            }
            public interface ITweenDefinition<TTarget, TValue> where TTarget : class where TValue : struct
            {
                internal TweenDefinition<TTarget, TValue> CreatePlayback();
            }
            public class TweenInstance<TTarget, TValue> where TTarget : class where TValue : struct { }
            public abstract class TweenDefinition<TTarget, TValue> : TweenOptionsBuilder
                where TTarget : class where TValue : struct
            {
                public TValue? From { get; set; }
                public TValue? To { get; set; }
                public TValue? By { get; set; }
                public double FactorFrom { get; set; }
                public TValue? DeltaFrom { get; set; }
                public double FactorTo { get; set; }
                public TValue? DeltaTo { get; set; }
                public double FactorBy { get; set; }
                public TValue? DeltaBy { get; set; }
                public Action<TweenInstance<TTarget, TValue>>? OnAdd { get; set; }
                public Action<TweenInstance<TTarget, TValue>>? OnStart { get; set; }
                public Action<TweenInstance<TTarget, TValue>, TValue>? OnUpdate { get; set; }
                public Action<TweenInstance<TTarget, TValue>>? OnEnd { get; set; }
                public Action<TweenInstance<TTarget, TValue>>? OnCancel { get; set; }
                public Action<TweenInstance<TTarget, TValue>>? OnFinally { get; set; }
            }
            public class PropertyTween<TTarget, TValue> : TweenDefinition<TTarget, TValue>
                where TTarget : class where TValue : struct
            {
                public PropertyTween(Func<TTarget, TValue> getter, Action<TTarget, TValue> setter,
                    Func<TValue, TValue, float, TValue> interpolate) { }
            }
        }
        namespace Targets { public class Widget : Godot.Node { } }
        """;

    private const string Adapter = """
        using Base = tweens.gd.PropertyTween<Targets.Widget, float>;
        namespace tweens.gd
        {
            public sealed partial class OpacityTween : Base
            {
                public OpacityTween() : base(_ => 0, (_, _) => { }, (a, b, t) => a) { }
            }
            public sealed partial class OpacityTween { }
        }
        namespace Other
        {
            public sealed class IgnoredTween() : Base(_ => 0, (_, _) => { }, (a, b, t) => a);
        }
        """;

    [Fact]
    public void DiscoversAliasedBaseTypesAndPartialAdaptersAndEmitsCompilableDefinitions()
    {
        var (_, output, result) = Run(CreateCompilation(Contracts, Adapter));
        Assert.Equal(new[] { "Opacity.g.cs", "Property.g.cs", "TweenCatalog.g.cs", "TweenScheduler.g.cs", "TweenStarts.g.cs" },
            result.GeneratedSources.Select(source => source.HintName).Order().ToArray());
        var definition = output.GetTypeByMetadataName("tweens.gd.Tweens+Opacity")!;
        Assert.True(definition.IsReadOnly);
        var contract = Assert.Single(definition.Interfaces, type => type.Name == "ITweenDefinition");
        Assert.Equal("Targets.Widget", contract.TypeArguments[0].ToDisplayString());
        Assert.Equal(SpecialType.System_Single, contract.TypeArguments[1].SpecialType);
        Assert.True(Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(definition.GetMembers("Duration"))).SetMethod!.IsInitOnly);
        Assert.True(Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(definition.GetMembers("By"))).SetMethod!.IsInitOnly);
        Assert.True(Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(definition.GetMembers("FactorTo"))).SetMethod!.IsInitOnly);
        Assert.True(Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(definition.GetMembers("DeltaTo"))).SetMethod!.IsInitOnly);
        Assert.Empty(definition.GetMembers("Ignored"));
        Assert.Empty(definition.GetMembers("ReadOnly"));
        var ease = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(definition.GetMembers("EaseFunction")));
        Assert.Equal(NullableAnnotation.Annotated, ease.NullableAnnotation);
        // Constructor options follow their fixed order, not declaration order, and are optional. The endpoint is
        // required and never null; float endpoints take doubles.
        var constructor = Assert.Single(definition.InstanceConstructors, constructor => constructor.Parameters.Length > 0);
        Assert.Equal(["to", "duration", "ease"], constructor.Parameters.Select(parameter => parameter.Name));
        Assert.Equal(SpecialType.System_Double, constructor.Parameters[0].Type.SpecialType);
        Assert.False(constructor.Parameters[0].HasExplicitDefaultValue);
        Assert.All(constructor.Parameters.Skip(1), parameter => Assert.True(parameter.HasExplicitDefaultValue));
    }

    [Fact]
    public void EmitsCompilableStartsForNodeResourceAndCustomDefinitions()
    {
        const string starts = """
            namespace tweens.gd
            {
                public sealed class ResourceOpacityTween()
                    : PropertyTween<Godot.Resource, float>(n => n.Value, (n, value) => n.Value = value, (a, b, t) => a);
                internal static class Starts
                {
                    internal static void Use(TweenScheduler scheduler, Targets.Widget node, Godot.Resource resource,
                        Godot.SceneTree tree, Tweens.Opacity opacity, Tweens.ResourceOpacity resourceOpacity,
                        Tweens.Property<Targets.Widget, float> nodeProperty,
                        Tweens.Property<Godot.Resource, float> resourceProperty)
                    {
                        scheduler.Add(node, in opacity);
                        scheduler.Add(node, in opacity, node);
                        node.Tween(in opacity);
                        scheduler.Add(resource, in resourceOpacity);
                        resource.Tween(in resourceOpacity, tree);
                        resource.Tween(in resourceOpacity, node);
                        node.Tween(resource, in resourceOpacity);
                        node.Tween(in nodeProperty);
                        resource.Tween(in resourceProperty, tree);
                        resource.Tween(in resourceProperty, node);
                        node.Tween(resource, in resourceProperty);
                    }
                }
            }
            """;
        var (_, output, _) = Run(CreateCompilation(Contracts, Adapter, starts));
        var scheduler = output.GetTypeByMetadataName("tweens.gd.TweenScheduler")!;
        Assert.All(scheduler.GetMembers("Add").OfType<IMethodSymbol>(),
            method => Assert.Equal(RefKind.In, method.Parameters[1].RefKind));
    }

    [Fact]
    public void EmitsShaderAndCustomPropertyConstructorsWithGenericConstraints()
    {
        const string shaders = """
            namespace tweens.gd
            {
                public sealed class ShaderParameterTween<TValue>(string parameter)
                    : TweenDefinition<Targets.Widget, TValue> where TValue : struct;
                public abstract class InstanceShaderParameterTween<TNode, TValue>(string parameter)
                    : TweenDefinition<TNode, TValue> where TNode : class where TValue : struct;
                public sealed class CanvasItemInstanceShaderParameterTween<TValue>(string parameter)
                    : InstanceShaderParameterTween<Targets.Widget, TValue>(parameter) where TValue : struct;
                public sealed class GeometryInstanceShaderParameterTween<TValue>(string parameter)
                    : InstanceShaderParameterTween<Targets.Widget, TValue>(parameter) where TValue : struct;
            }
            """;
        var (_, output, result) = Run(CreateCompilation(Contracts, shaders));
        Assert.Equal(7, result.GeneratedSources.Length);
        foreach (var name in new[] { "ShaderParameter", "CanvasItemInstanceShaderParameter", "GeometryInstanceShaderParameter" })
        {
            var type = output.GetTypeByMetadataName("tweens.gd.Tweens+" + name + "`1")!;
            Assert.True(Assert.Single(type.TypeParameters).HasValueTypeConstraint);
            // The binding alone leaves the endpoints unset; with an endpoint, it is required.
            Assert.Contains(type.InstanceConstructors, constructor => constructor.Parameters.Length == 1
                && constructor.Parameters[0].Type.SpecialType == SpecialType.System_String);
            Assert.Contains(type.InstanceConstructors, constructor => constructor.Parameters.Length == 4
                && constructor.Parameters[0].Type.SpecialType == SpecialType.System_String
                && constructor.Parameters[1] is { Name: "to", HasExplicitDefaultValue: false }
                && constructor.Parameters.Skip(2).All(parameter => parameter.HasExplicitDefaultValue));
        }
        var property = output.GetTypeByMetadataName("tweens.gd.Tweens+Property`2")!;
        Assert.True(property.TypeParameters[0].HasReferenceTypeConstraint);
        Assert.True(property.TypeParameters[1].HasValueTypeConstraint);
        Assert.Contains(property.InstanceConstructors, constructor
            => constructor.Parameters.Select(parameter => parameter.Name).SequenceEqual(["getter", "setter", "interpolate"]));
        Assert.Contains(property.InstanceConstructors, constructor
            => constructor.Parameters.Select(parameter => parameter.Name).SequenceEqual(
                ["getter", "setter", "interpolate", "to", "duration", "ease"]));
    }

    [Fact]
    public void EmitsExtensionTwinsForShorterEndpointsAndEasing()
    {
        const string extensions = """
            #nullable enable
            namespace Godot
            {
                public struct Vector2 { public Vector2(float x, float y) { } }
                public struct Color
                {
                    public Color(float r, float g, float b) { }
                    public Color(float r, float g, float b, float a) { }
                    public Color(string code) { }
                }
            }
            namespace tweens.gd
            {
                internal static class EndpointComponents
                {
                    internal static Godot.Vector2 ToVector2(System.ReadOnlySpan<double> to) => default;
                    internal static Godot.Color ToColor(System.ReadOnlySpan<double> to) => default;
                }
                public enum Mode { A, B }
                public class Configurable { public EaseType Ease { get; set; } public Duration Delay { get; set; } }
                public static partial class TweenExtensions
                {
                    public static TweenInstance<Targets.Widget, Godot.Vector2> TweenScale(this Targets.Widget target,
                        Godot.Vector2 to, Duration duration, System.Action<Configurable>? configure = null, int count = 2,
                        bool flag = true, Mode mode = Mode.B, float factor = 1.5f, string? label = "x") => new();
                    public static TweenInstance<Targets.Widget, Godot.Color> TweenTint(this Targets.Widget target,
                        Godot.Color to, Duration duration, TweenOptions options) => new();
                    public static TweenInstance<Targets.Widget, float> TweenFade(this Targets.Widget target,
                        double to, Duration duration, System.Action<Configurable>? configure = null) => new();
                    public static TweenInstance<Targets.Widget, TValue> TweenAny<TValue>(this Targets.Widget target,
                        TValue to, Duration duration) where TValue : struct => new();
                }
            }
            """;
        var (_, output, result) = Run(CreateCompilation(Contracts, extensions));
        Assert.Equal(["Property.g.cs", "TweenCatalog.g.cs", "TweenExtensions.g.cs", "TweenScheduler.g.cs", "TweenStarts.g.cs"],
            result.GeneratedSources.Select(source => source.HintName).Order());
        var type = output.GetTypeByMetadataName("tweens.gd.TweenExtensions")!;
        string Forms(string name) => string.Join(" | ", type.GetMembers(name).OfType<IMethodSymbol>()
            .Select(method => method.Parameters[1].Type.ToDisplayString() + (method.Parameters.Any(p => p.Name == "ease") ? " eased" : ""))
            .Order(StringComparer.Ordinal));

        // Vectors take tuples and collections, scales one number; colors also take codes and names. Configure
        // overloads get an easing and a delay, in every form. Generic methods keep their signatures.
        Assert.Equal("(double X, double Y) | (double X, double Y) eased | Godot.Vector2 | Godot.Vector2 eased | "
            + "System.ReadOnlySpan<double> | System.ReadOnlySpan<double> eased | double | double eased", Forms("TweenScale"));
        Assert.Equal("(double R, double G, double B) | (double R, double G, double B, double A) | Godot.Color | "
            + "System.ReadOnlySpan<double> | string", Forms("TweenTint"));
        Assert.Equal("double | double eased", Forms("TweenFade"));
        Assert.Single(type.GetMembers("TweenAny"));

        // Twins keep the defaults after the endpoint.
        var eased = type.GetMembers("TweenScale").OfType<IMethodSymbol>()
            .Single(method => method.Parameters[1].Type.ToDisplayString() == "Godot.Vector2" && method.Parameters.Any(p => p.Name == "ease"));
        Assert.Equal(["target", "to", "duration", "ease", "delay", "count", "flag", "mode", "factor", "label"],
            eased.Parameters.Select(parameter => parameter.Name));
        Assert.Equal(new object?[] { null, 2, true, 1, 1.5f, "x" }, eased.Parameters.Skip(4).Select(parameter => parameter.ExplicitDefaultValue));
        Assert.Equal("tweens.gd.Duration", eased.Parameters[2].Type.ToDisplayString());
        Assert.Equal("tweens.gd.Duration", eased.Parameters[4].Type.ToDisplayString());
        Assert.False(eased.Parameters[3].HasExplicitDefaultValue);
    }

    [Fact]
    public void ValueScopedOptionsFollowTheirDeclaredTypeAndUpdateIncrementally()
    {
        var contracts = Contracts
            .Replace("public EaseType Ease { get; init; }", "public bool Precision { get; init; } public EaseType Ease { get; init; }")
            .Replace("public EaseType Ease { get; set; }", "[TweenValueOption(typeof(float))] public bool Precision { get; set; } public EaseType Ease { get; set; }");
        const string sources = """
            namespace tweens.gd
            {
                [System.AttributeUsage(System.AttributeTargets.Property)]
                internal sealed class TweenValueOptionAttribute : System.Attribute
                {
                    public TweenValueOptionAttribute(System.Type valueType) { }
                }
                public sealed class CountTween() : PropertyTween<Targets.Widget, int>(_ => 0, (_, _) => { }, (a, b, t) => a);
            }
            """;
        var compilation = CreateCompilation(contracts, Adapter, sources);
        var (driver, output, _) = Run(compilation);
        Assert.Single(output.GetTypeByMetadataName("tweens.gd.Tweens+Opacity")!.GetMembers("Precision"));
        Assert.Empty(output.GetTypeByMetadataName("tweens.gd.Tweens+Count")!.GetMembers("Precision"));
        Assert.Empty(output.GetTypeByMetadataName("tweens.gd.Tweens+Property`2")!.GetMembers("Precision"));
        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), Parse(contracts.Replace("typeof(float)", "typeof(int)")));
        (_, output, _) = Run(compilation, driver);
        Assert.Empty(output.GetTypeByMetadataName("tweens.gd.Tweens+Opacity")!.GetMembers("Precision"));
        Assert.Single(output.GetTypeByMetadataName("tweens.gd.Tweens+Count")!.GetMembers("Precision"));
    }

    [Fact]
    public void OnlyConcreteWholeColorDefinitionsExposeColorForwarders()
    {
        var definitions = typeof(Tweens).GetNestedTypes().Where(type => type.GetInterfaces()
            .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ITweenDefinition<,>))).ToArray();
        Assert.NotEmpty(definitions);
        foreach (var option in new[] { nameof(TweenOptions.ColorSpace), nameof(TweenOptions.AlphaMode), nameof(TweenOptions.ColorEncoding) })
        {
            var applicability = typeof(TweenOptionsBuilder).GetProperty(option)!.GetCustomAttribute<TweenValueOptionAttribute>()!;
            Assert.Equal(typeof(Godot.Color), applicability.ValueType);
            foreach (var definition in definitions)
            {
                var value = definition.GetInterfaces().Single(contract => contract.IsGenericType
                    && contract.GetGenericTypeDefinition() == typeof(ITweenDefinition<,>)).GetGenericArguments()[1];
                Assert.Equal(value == typeof(Godot.Color), definition.GetProperty(option) is not null);
                Assert.NotNull(definition.GetProperty(nameof(TweenOptions.Duration)));
                Assert.NotNull(definition.GetProperty("Options"));
            }
        }
    }

    [Fact]
    public void BindingManifestResolvesAliasesComponentsCompoundGettersAndPropertyOwners()
    {
        const string bindings = """
            using Base = tweens.gd.PropertyTween<Godot.Node2D, float>;
            namespace Godot
            {
                public struct Vector2 { public float X, Y; public Vector2(float x, float y) => (X, Y) = (x, y); }
                public class CanvasItem : Node { public float Opacity { get; set; } }
                public class Node2D : CanvasItem { public Vector2 Position { get; set; } }
                public class GpuParticles2D : Node2D { }
            }
            namespace tweens.gd
            {
                internal static class EndpointComponents { internal static Godot.Vector2 ToVector2(System.ReadOnlySpan<double> value) => default; }
                public sealed partial class X2DTween : Base
                {
                    public X2DTween() : base(target => { return target.Position.X; }, (_, _) => { }, (a, b, t) => a) { }
                }
                public sealed partial class X2DTween { }
                public sealed class InheritedOpacityTween() : PropertyTween<Godot.GpuParticles2D, float>(
                    target => target.Opacity, (_, _) => { }, (a, b, t) => a);
                public sealed class PairTween() : PropertyTween<Godot.Node2D, Godot.Vector2>(
                    target => new(target.Position.X, target.Position.Y), (_, _) => { }, (a, b, t) => a);
                public sealed class Vector2Tween() : PropertyTween<Godot.Node, Godot.Vector2>(
                    _ => new(0, 0), (_, _) => { }, (a, b, t) => a)
                {
                    protected override bool ReadsWrittenValue => false;
                }
            }
            """;
        var contracts = Contracts.Replace("public PropertyTween(Func<TTarget, TValue>",
            "protected virtual bool ReadsWrittenValue => true; public PropertyTween(Func<TTarget, TValue>");
        var (_, _, result) = Run(CreateCompilation(contracts, bindings));
        var source = result.GeneratedSources.Single(source => source.HintName == "TweenCatalog.g.cs").SourceText.ToString();
        using var manifest = JsonDocument.Parse(source["/* TWEENS_GD_CATALOG\n".Length..^3]);
        var entries = manifest.RootElement.EnumerateArray().ToDictionary(entry => entry.GetProperty("csharp").GetString()!);
        Assert.Equal(["position:x"], entries["X2D"].GetProperty("paths").EnumerateArray().Select(path => path.GetString()));
        Assert.Equal(["Node2D"], entries["X2D"].GetProperty("declaringTypes").EnumerateArray().Select(owner => owner.GetString()));
        Assert.Equal("GPUParticles2D", entries["InheritedOpacity"].GetProperty("target").GetString());
        Assert.Equal(["CanvasItem"], entries["InheritedOpacity"].GetProperty("declaringTypes").EnumerateArray().Select(owner => owner.GetString()));
        Assert.Equal("compound", entries["Pair"].GetProperty("kind").GetString());
        Assert.Equal(["position:x", "position:y"], entries["Pair"].GetProperty("paths").EnumerateArray().Select(path => path.GetString()));
        Assert.Equal("value", entries["Vector2"].GetProperty("kind").GetString());
        Assert.Equal("vector2_value", entries["Vector2"].GetProperty("name").GetString());
        Assert.Empty(entries["Vector2"].GetProperty("paths").EnumerateArray());
    }

    [Fact]
    public void TypedKeyframeChannelsFollowGodotInheritanceAndPreserveDegreeChannels()
    {
        const string engine = """
            namespace Godot
            {
                public struct Color { public float A; }
                public struct Vector2 { public float X, Y; }
                public struct Vector3 { public float X, Y, Z; }
                public struct Quaternion { }
                public class CanvasItem : Node { public Color Modulate { get; set; } public Color SelfModulate { get; set; } }
                public class Node2D : CanvasItem
                {
                    public Vector2 Position { get; set; } public Vector2 Scale { get; set; }
                    public float Rotation { get; set; } public float RotationDegrees { get; set; } public float Skew { get; set; }
                }
                public class Node3D : Node
                {
                    public Vector3 Position { get; set; } public Vector3 Scale { get; set; }
                    public Vector3 Rotation { get; set; } public Vector3 RotationDegrees { get; set; } public Quaternion Quaternion { get; set; }
                }
                public class GeometryInstance3D : Node3D { public float Transparency { get; set; } }
            }
            """;
        static Dictionary<string, string[]> Parameters(string engine)
        {
            var driver = CSharpGeneratorDriver.Create([new KeyframeApiGenerator().AsSourceGenerator()], parseOptions: ParseOptions)
                .RunGenerators(CreateCompilation(Contracts, engine));
            var result = Assert.Single(driver.GetRunResult().Results);
            Assert.Empty(result.Diagnostics);
            var source = result.GeneratedSources.Single(source => source.HintName == "KeyframeExtensions.g.cs").SourceText;
            return CSharpSyntaxTree.ParseText(source, ParseOptions).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText.StartsWith("For"))
                .ToDictionary(method => method.Identifier.ValueText, method => method.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText).ToArray());
        }
        var parameters = Parameters(engine);
        Assert.Equal(["modulate", "alpha", "selfModulate", "duration", "ease", "interpolation", "options"], parameters["ForCanvasItem"]);
        Assert.Equal(["x", "y", "position", "rotation", "rotationDegrees", "scale", "skew", "modulate", "alpha", "selfModulate",
            "duration", "ease", "interpolation", "options"], parameters["ForNode2D"]);
        Assert.DoesNotContain("modulate", parameters["ForNode3D"]);
        Assert.Contains("rotationDegrees", parameters["ForNode3D"]);
        Assert.Contains("position", parameters["ForGeometryInstance3D"]);
        Assert.Contains("transparency", parameters["ForGeometryInstance3D"]);
        // Changing only the engine hierarchy changes inherited channels, without generator target-name branches.
        parameters = Parameters(engine.Replace("GeometryInstance3D : Node3D", "GeometryInstance3D : CanvasItem"));
        Assert.DoesNotContain("position", parameters["ForGeometryInstance3D"]);
        Assert.Contains("modulate", parameters["ForGeometryInstance3D"]);
        Assert.Contains("transparency", parameters["ForGeometryInstance3D"]);
    }

    [Fact]
    public void OptionChangesAndAdapterRemovalUpdateGeneratedSources()
    {
        var compilation = CreateCompilation(Contracts, Adapter);
        var (driver, _, _) = Run(compilation);
        var changedContracts = Contracts.Replace("public Duration Duration", "public Duration Delay");
        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), Parse(changedContracts));
        var changed = Run(compilation, driver);
        var definition = changed.Output.GetTypeByMetadataName("tweens.gd.Tweens+Opacity")!;
        Assert.Single(definition.GetMembers("Delay"));
        Assert.Empty(definition.GetMembers("Duration"));
        Assert.Contains(definition.InstanceConstructors, constructor
            => constructor.Parameters.Select(parameter => parameter.Name).SequenceEqual(["to", "ease", "delay"]));

        compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last());
        var (_, removed, result) = Run(compilation, changed.Driver);
        Assert.Equal(["Property.g.cs", "TweenCatalog.g.cs", "TweenScheduler.g.cs", "TweenStarts.g.cs"],
            result.GeneratedSources.Select(source => source.HintName).Order());
        Assert.Null(removed.GetTypeByMetadataName("tweens.gd.Tweens+Opacity"));
    }

    [Fact]
    public void UnrelatedEditsKeepDefinitionModelsCached()
    {
        var unrelated = Parse("public class Unrelated { }");
        var compilation = CreateCompilation(Contracts, Adapter).AddSyntaxTrees(unrelated);
        var (driver, _, _) = Run(compilation);
        compilation = compilation.ReplaceSyntaxTree(unrelated, Parse("public class Unrelated { public int Value; }"));
        var (_, _, result) = Run(compilation, driver);
        Assert.All(result.TrackedSteps["Definitions"].SelectMany(step => step.Outputs),
            output => Assert.True(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
        Assert.All(result.TrackedSteps["Options"].SelectMany(step => step.Outputs),
            output => Assert.True(output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged));
    }

    private static SyntaxTree Parse(string source) => CSharpSyntaxTree.ParseText(source, ParseOptions);

    private static CSharpCompilation CreateCompilation(params string[] sources) => CSharpCompilation.Create(
        "GeneratorTests", sources.Select(Parse), References,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static (GeneratorDriver Driver, Compilation Output, GeneratorRunResult Result) Run(
        CSharpCompilation compilation, GeneratorDriver? driver = null)
    {
        driver ??= CSharpGeneratorDriver.Create(
            generators: [new StructuredDefinitionGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return (driver, output, Assert.Single(driver.GetRunResult().Results));
    }
}
