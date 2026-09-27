// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Godot;
using tweens.gd.Tests.Support;

namespace tweens.gd.Tests.Unit;

/// <summary>The generated immutable definitions in the Tweens namespace, as a catalog and by behavior.</summary>
public class StructuredDefinitionTests
{
    private static readonly Func<float, float> Ease = static x => x;

    private static readonly Dictionary<string, object?> OptionValues = new()
    {
        ["Duration"] = 1.25, ["FactorDuration"] = 2.0, ["DeltaDuration"] = 0.25, ["Delay"] = 0.5, ["FactorDelay"] = 3.0, ["DeltaDelay"] = 0.125, ["PingPongInterval"] = 0.25, ["RepeatInterval"] = 0.75,
        ["Offset"] = 0.125, ["Repeats"] = 2, ["UsePingPong"] = true, ["UseUnscaledTime"] = true,
        ["Fill"] = FillMode.None, ["Ease"] = EaseType.QuadIn, ["Skew"] = 2.0, ["EaseFunction"] = Ease, ["Curve"] = null,
        ["ProcessMode"] = TweenProcessMode.Physics, ["PauseMode"] = TweenPauseMode.Always,
        ["SuppressCallbacksWhenTargetInvalid"] = true,
    };

    private static readonly string[] Callbacks = ["OnAdd", "OnStart", "OnUpdate", "OnEnd", "OnCancel", "OnFinally"];

    private static IEnumerable<Type> DefinitionTypes() => typeof(TweenScheduler).Assembly.GetExportedTypes()
        .Where(type => type.Namespace == "Tweens" && type.IsValueType)
        .OrderBy(type => type.Name);

    public static TheoryData<Type> Definitions() => new(DefinitionTypes());

    [Fact]
    public void EveryBuiltInAdapterHasAStructuredDefinition()
    {
        var adapters = typeof(TweenScheduler).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "tweens.gd" && type.IsSealed && type.Name.EndsWith("Tween"))
            .Where(type => type.IsGenericType || type.BaseType is { IsGenericType: true } parent
                && parent.GetGenericTypeDefinition() == typeof(PropertyTween<,>))
            .Select(type => type.Name.Split('`')[0][..^"Tween".Length]);
        var definitions = DefinitionTypes().Select(type => type.Name.Split('`')[0]).ToHashSet();
        Assert.All(adapters, name => Assert.Contains(name, definitions));
        Assert.Contains("Property", definitions);
        Assert.True(definitions.Count > 300, $"Only {definitions.Count} definitions were generated.");
    }

    [Theory]
    [MemberData(nameof(Definitions))]
    public void EveryPropertyRoundTripsIntoTheIndependentPlayback(Type definition) => Run(nameof(Verify), definition);

    [Theory]
    [MemberData(nameof(Definitions))]
    public void EveryConstructorTakesTheEndpointAndTiming(Type definition) => Run(nameof(VerifyConstructor), definition);

    private static void Run(string check, Type definition)
    {
        try
        {
            typeof(StructuredDefinitionTests).GetMethod(check, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(Close(definition), Contract(Close(definition)).GetGenericArguments()[0],
                    Contract(Close(definition)).GetGenericArguments()[1])
                .Invoke(null, []);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }

    // Shader and custom definitions lead with their binding; omitted arguments take their defaults.
    private static TDefinition Create<TDefinition>(params object?[] arguments)
    {
        var constructor = typeof(TDefinition).GetConstructors().Single();
        object?[] binding = constructor.GetParameters().Length switch
        {
            5 => ["amount"],
            7 => [(Func<Box, float>)(b => b.Value), (Action<Box, float>)((b, v) => b.Value = v),
                (Func<float, float, float, float>)Interpolators.Float],
            _ => [],
        };
        return (TDefinition)constructor.Invoke([.. binding, .. arguments, .. Enumerable.Repeat(Type.Missing, 4 - arguments.Length)]);
    }

    private static object? Get<TDefinition>(TDefinition definition, string property)
        => typeof(TDefinition).GetProperty(property)!.GetValue(definition);

    private static void VerifyConstructor<TDefinition, TTarget, TValue>()
        where TDefinition : struct, ITweenDefinition<TTarget, TValue> where TTarget : class where TValue : struct
    {
        var to = Sample<TValue>(0.75f);
        var given = Create<TDefinition>(to, 1.25, EaseType.QuadIn, 0.5);
        Assert.Equal(to, Get(given, "To"));
        Assert.Null(Get(given, "From"));
        Assert.Equal(new TweenOptions { Duration = 1.25, Ease = EaseType.QuadIn, Delay = 0.5 }, Get(given, "Options"));

        var omitted = Create<TDefinition>();
        Assert.Null(Get(omitted, "To"));
        Assert.Equal(new TweenOptions(), Get(omitted, "Options"));
        if (typeof(TDefinition).GetConstructors().Single().GetParameters().Length == 4)
            Assert.Equal(new TDefinition(), omitted);
    }

    private static Type Close(Type definition) => definition.IsGenericTypeDefinition
        ? definition.MakeGenericType(definition.GetGenericArguments().Length == 2 ? [typeof(Box), typeof(float)] : [typeof(float)])
        : definition;

    private static Type Contract(Type definition) => definition.GetInterfaces()
        .Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ITweenDefinition<,>));

    private static void Verify<TDefinition, TTarget, TValue>()
        where TDefinition : struct, ITweenDefinition<TTarget, TValue> where TTarget : class where TValue : struct
    {
        object boxed = Create<TDefinition>();

        // Assign through the init accessors, then read everything back.
        var expected = new Dictionary<string, object?>();
        foreach (var property in typeof(TDefinition).GetProperties().Where(p => p.CanWrite))
        {
            var value = property.Name switch
            {
                "Options" => null,
                "From" => Sample<TValue>(0.25f),
                "To" => Sample<TValue>(0.75f),
                "By" => Sample<TValue>(0.5f),
                "DeltaFrom" or "DeltaTo" or "DeltaBy" => Sample<TValue>(0.125f),
                "FactorFrom" => 0.5, "FactorTo" => 1.5, "FactorBy" => -2.0,
                "Parameter" => "uniform",
                _ when OptionValues.TryGetValue(property.Name, out var option) => option,
                _ when Callbacks.Contains(property.Name) => Expression.Lambda(property.PropertyType,
                    Expression.Empty(), property.PropertyType.GetMethod("Invoke")!.GetParameters()
                        .Select(parameter => Expression.Parameter(parameter.ParameterType))).Compile(),
                _ => property.GetValue(boxed),
            };
            if (property.Name == "Options") continue;
            property.SetValue(boxed, value);
            expected[property.Name] = value;
        }
        foreach (var (name, value) in expected)
            Assert.Equal(value, typeof(TDefinition).GetProperty(name)!.GetValue(boxed));

        var structured = (TDefinition)boxed;
        var options = (TweenOptions)typeof(TDefinition).GetProperty("Options")!.GetValue(structured)!;
        Assert.Equal(1.25, options.Duration);
        Assert.Equal(FillMode.None, options.Fill);

        var playback = structured.CreatePlayback();
        Assert.Equal(typeof(TDefinition).Name.Split('`')[0] + "Tween", playback.GetType().Name.Split('`')[0]);
        Assert.Equal(options, playback.ToOptions());
        Assert.Equal(expected["From"], playback.From);
        Assert.Equal(expected["To"], playback.To);
        Assert.Equal(expected["By"], playback.By);
        foreach (var endpoint in new[] { "From", "To", "By" })
        {
            Assert.Equal(expected["Factor" + endpoint], playback.GetType().GetProperty("Factor" + endpoint)!.GetValue(playback));
            Assert.Equal(expected["Delta" + endpoint], playback.GetType().GetProperty("Delta" + endpoint)!.GetValue(playback));
        }
        foreach (var callback in Callbacks)
            Assert.Same(expected[callback], playback.GetType().GetProperty(callback)!.GetValue(playback));
        if (expected.TryGetValue("Parameter", out var parameter))
            Assert.Equal(parameter, playback.GetType().GetProperty("Parameter")!.GetValue(playback));

        // Every start snapshots a new playback.
        Assert.NotSame(playback, structured.CreatePlayback());
    }

    private static TValue Sample<TValue>(float amount) where TValue : struct
    {
        object value = typeof(TValue) == typeof(float) ? amount
            : typeof(TValue) == typeof(double) ? (double)amount
            : typeof(TValue) == typeof(int) ? (int)(amount * 8)
            : typeof(TValue) == typeof(Vector2) ? new Vector2(amount, -amount)
            : typeof(TValue) == typeof(Vector3) ? new Vector3(amount, -amount, 2 * amount)
            : typeof(TValue) == typeof(Vector4) ? new Vector4(amount, -amount, 2 * amount, 3 * amount)
            : typeof(TValue) == typeof(Color) ? new Color(amount, amount / 2, amount / 4, amount)
            : typeof(TValue) == typeof(Quaternion) ? new Quaternion(Vector3.Up, amount)
            : typeof(TValue) == typeof(Rect2) ? new Rect2(amount, -amount, 2 * amount, 3 * amount)
            : throw new NotSupportedException(typeof(TValue).Name);
        return (TValue)value;
    }

    private readonly Tweens.Property<Box, float> movement = new(
        static target => target.Value, static (target, value) => target.Value = value, Interpolators.Float, 10, 1);

    [Fact]
    public void ConstructorArgumentsMatchTheirInitializers()
    {
        Assert.Equal(new Tweens.Position2D { To = new Vector2(4, 2), Duration = 0.5, Ease = EaseType.CubicOut, Delay = 0.25 },
            new Tweens.Position2D(new Vector2(4, 2), 0.5, EaseType.CubicOut, 0.25));
        Assert.Equal(new Tweens.ModulateAlpha { Duration = 0.3 }, new Tweens.ModulateAlpha(duration: 0.3));
        Assert.Equal(new Tweens.ShaderParameter<float>("amount") { To = 1, Delay = 0.1 },
            new Tweens.ShaderParameter<float>("amount", 1, delay: 0.1));
        Assert.Equal(10, movement.To);
        Assert.Equal(1, movement.Duration);
        Assert.Equal(FillMode.RetainFinalValue, new Tweens.Scale2D(Vector2.One, 0.2).Fill);
    }

    [Fact]
    public void ReusableDefinitionsVaryWithWithExpressions()
    {
        using var scheduler = new TweenScheduler();
        var first = new Box { Value = 2 };
        var second = new Box { Value = 4 };
        var updates = new List<float>();
        var a = scheduler.Add(first, movement);
        var b = scheduler.Add(second, movement with { To = 20, Delay = 0.5, OnUpdate = (_, v) => updates.Add(v) });
        scheduler.Update(0.5);
        Assert.Equal(6, first.Value);
        Assert.Equal(4, second.Value);
        scheduler.Update(0.5);
        Assert.True(a.IsTerminal);
        Assert.Equal(12, second.Value);
        scheduler.Update(0.5);
        Assert.True(b.IsTerminal);
        Assert.Equal([4f, 12f, 20f], updates);
        Assert.Equal(10, movement.To);
        Assert.Null(movement.OnUpdate);
    }

    [Fact]
    public void DefinitionsAreValuesAndDefaultToRetainingTheFinalValue()
    {
        var copy = movement with { };
        Assert.Equal(movement, copy);
        Assert.Equal(movement.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(movement, movement with { Delay = 1 });
        Assert.Equal(FillMode.RetainFinalValue, new Tweens.Modulate().Fill);
        Assert.Equal(FillMode.RetainFinalValue, ((ITweenDefinition<CanvasItem, Color>)new Tweens.Modulate()).CreatePlayback().Fill);
        Assert.Equal(new Tweens.Modulate { Options = new TweenOptions { Duration = 2 } }, new Tweens.Modulate { Duration = 2 });
    }

    [Fact]
    public void UntypedDefinitionsStartThroughTheirTypedContract()
    {
        using var scheduler = new TweenScheduler();
        var box = new Box();
        ITweenDefinition<Box> untyped = movement;
        var tween = untyped.AddTo(scheduler, box);
        scheduler.Update(0.5);
        Assert.Equal(5, box.Value);
        Assert.IsType<TweenInstance<Box, float>>(tween);
    }
}
