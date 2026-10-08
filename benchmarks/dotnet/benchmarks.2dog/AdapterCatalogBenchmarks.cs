// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Godot;
using tweens.gd;

namespace TweensBenchmarks;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[ShortRunJob]
[IterationTime(100)]
public class AdapterCatalogBenchmarks : EngineBenchmark
{
    [Params(100, 1000)] public int Count { get; set; }
    [ParamsSource(nameof(Definitions))] public string Definition { get; set; } = "Position2D";
    public static IEnumerable<string> Definitions => Catalog.Keys;
    internal static readonly IReadOnlyDictionary<string, Type> Catalog = BuildCatalog();
    private CatalogTargets targets = null!;
    private ICatalogWorkload workload = null!;

    private static IReadOnlyDictionary<string, Type> BuildCatalog()
    {
        var result = new SortedDictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in typeof(Tweens).GetNestedTypes(BindingFlags.Public).Where(t => t.IsValueType))
        {
            if (!type.IsGenericTypeDefinition) { result.Add(type.Name, type); continue; }
            var family = type.Name.Split('`')[0];
            if (family == "Property")
                foreach (var value in CatalogValues.Types)
                    result.Add($"{family}<{value.Name}>", type.MakeGenericType(typeof(CatalogValue<>).MakeGenericType(value), value));
            else if (family is "ShaderParameter" or "CanvasItemInstanceShaderParameter" or "GeometryInstanceShaderParameter")
                foreach (var value in CatalogValues.ShaderTypes)
                    result.Add($"{family}<{value.Name}>", type.MakeGenericType(value));
            else throw new NotSupportedException($"Add fixtures for new generic definition {type}.");
        }
        return result;
    }

    [GlobalSetup]
    public void Setup()
    {
        StartEngine();
        targets = new(Owner); targets.Initialize();
        workload = CreateWorkload(Definition, Count, targets);
        workload.Validate();
        CheckEngineErrors();
    }

    internal static ICatalogWorkload CreateWorkload(string name, int count, CatalogTargets targets)
    {
        var definition = Catalog[name];
        var contract = definition.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITweenDefinition<,>));
        var type = typeof(CatalogWorkload<,>).MakeGenericType(contract.GetGenericArguments());
        return (ICatalogWorkload)Activator.CreateInstance(type, definition, count, targets)!;
    }

    // Same adapter setter/interpolator and targets, without scheduler/lifetime/timeline work.
    // Delegates are bound once during setup; no reflection occurs in timed loops.
    [Benchmark(Baseline = true)] public void DirectWrite() => workload.DirectWrite();
    [Benchmark] public void TweenUpdate() => workload.TweenUpdate();

    [GlobalCleanup]
    public void Cleanup()
    {
        try { workload.Validate(); }
        finally { workload.Dispose(); targets.Dispose(); StopEngine(); }
    }

    // One engine for a fast inventory check; benchmarks themselves still use a
    // fresh BDN child/engine per method and parameter combination.
    internal void VerifyCatalog()
    {
        StartEngine();
        try
        {
            foreach (var name in Definitions)
            {
                using var scope = new CatalogTargets(Owner); scope.Initialize();
                using var item = CreateWorkload(name, 2, scope);
                item.Validate();
                for (var i = 0; i < 128; i++) { item.DirectWrite(); item.TweenUpdate(); }
                item.Validate();
                CheckEngineErrors();
                Console.WriteLine($"PASS {name}");
            }
        }
        finally { StopEngine(); }
    }
}

internal interface ICatalogWorkload : IDisposable
{
    void DirectWrite();
    void TweenUpdate();
    void Validate();
}

internal sealed class CatalogWorkload<TTarget, TValue> : ICatalogWorkload where TTarget : class where TValue : struct
{
    private readonly TweenScheduler scheduler = new();
    private readonly TTarget[] targets;
    private readonly TweenInstance<TTarget, TValue>[] handles;
    private readonly Func<TTarget, TValue> getter;
    private readonly Action<TTarget, TValue> setter;
    private readonly Func<TValue, TValue, float, TValue> interpolate;
    private readonly TValue from, to;
    private readonly StringName? parameter;
    private double elapsed;
    private TValue sink;
    private const double Delta = 1.0 / 60;

    public CatalogWorkload(Type definitionType, int count, CatalogTargets scope)
    {
        var definition = Activator.CreateInstance(definitionType)!;
        var family = definitionType.Name.Split('`')[0];
        var custom = family == "Property";
        var shader = family.Contains("ShaderParameter", StringComparison.Ordinal);
        var callback = typeof(TTarget) == typeof(Node);
        if (shader)
        {
            parameter = new StringName("amount");
            Set(definition, "Parameter", "amount");
            getter = ReadShader;
            setter = WriteShader;
            interpolate = Interpolator();
        }
        else if (custom)
        {
            getter = static t => ((CatalogValue<TValue>)(object)t).Value;
            setter = static (t, v) => ((CatalogValue<TValue>)(object)t).Value = v;
            interpolate = Interpolator();
            Set(definition, "Getter", getter); Set(definition, "Setter", setter); Set(definition, "Interpolate", interpolate);
        }
        else
        {
            // Extract the actual strongly typed adapter operations, not PropertyInfo.SetValue
            // or GodotObject.Set. Fail loudly if the adapter contract changes.
            var adapterType = typeof(TweenScheduler).Assembly.GetType("tweens.gd." + family + "Tween", throwOnError: true)!;
            var adapter = Activator.CreateInstance(adapterType)!;
            var property = typeof(PropertyTween<TTarget, TValue>);
            getter = (Func<TTarget, TValue>)property.GetField("getter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adapter)!;
            setter = (Action<TTarget, TValue>)property.GetField("setter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adapter)!;
            interpolate = (Func<TValue, TValue, float, TValue>)property.GetField("interpolate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(adapter)!;
        }
        if (callback)
        {
            setter = (_, v) => sink = v;
            Set(definition, "OnUpdate", (Action<TweenInstance<TTarget, TValue>, TValue>)((_, v) => sink = v));
        }
        targets = new TTarget[count]; handles = new TweenInstance<TTarget, TValue>[count];
        for (var i = 0; i < count; i++) targets[i] = (TTarget)scope.Create(typeof(TTarget), family, CatalogValues.ShaderType(typeof(TValue)));
        // Initialize uniforms explicitly: headless rendering provides no shader defaults.
        if (shader) setter(targets[0], (TValue)CatalogValues.Nudge(default(TValue)));
        from = (TValue)CatalogValues.Nudge(getter(targets[0]));
        setter(targets[0], from);
        if (!callback) from = getter(targets[0]); // Respect clamping/normalization/sentinels.
        to = (TValue)CatalogValues.Nudge(from);
        Set(definition, "From", from); Set(definition, "To", to);
        Set(definition, "Duration", (Duration)1); Set(definition, "Repeats", -1);
        for (var i = 0; i < count; i++)
        {
            setter(targets[i], from);
            handles[i] = scheduler.Add(targets[i], (ITweenDefinition<TTarget, TValue>)definition);
        }
        scheduler.Update(0);
        // Compare direct and tween writes at the first sample, outside timing.
        DirectWrite();
        var expected = callback ? sink : getter(targets[0]);
        TweenUpdate();
        var actual = callback ? sink : getter(targets[0]);
        if (!CatalogValues.Close(expected, actual))
            throw new InvalidOperationException($"{definitionType.Name}: direct/tween mismatch {expected} / {actual}");
        Validate();
    }

    private static void Set(object definition, string property, object value) => definition.GetType().GetProperty(property)!.SetValue(definition, value);
    private static Func<TValue, TValue, float, TValue> Interpolator()
    {
        var name = typeof(TValue) == typeof(float) ? "Float" : typeof(TValue) == typeof(double) ? "Double" : typeof(TValue) == typeof(int) ? "Int" : typeof(TValue).Name;
        return typeof(Interpolators).GetMethod(name)!.CreateDelegate<Func<TValue, TValue, float, TValue>>();
    }
    private TValue ReadShader(TTarget target)
    {
        using var value = target switch
        {
            ShaderMaterial material => material.GetShaderParameter(parameter!),
            CanvasItem canvas => canvas.GetInstanceShaderParameter(parameter!),
            GeometryInstance3D geometry => geometry.GetInstanceShaderParameter(parameter!),
            _ => throw new NotSupportedException(),
        };
        return value.As<TValue>();
    }
    private void WriteShader(TTarget target, TValue value)
    {
        using var variant = Variant.From(value);
        switch (target)
        {
            case ShaderMaterial material: material.SetShaderParameter(parameter!, variant); break;
            case CanvasItem canvas: canvas.SetInstanceShaderParameter(parameter!, variant); break;
            case GeometryInstance3D geometry: geometry.SetInstanceShaderParameter(parameter!, variant); break;
            default: throw new NotSupportedException();
        }
    }
    public void DirectWrite()
    {
        elapsed += Delta;
        var weight = (float)(elapsed % 1);
        for (var i = 0; i < targets.Length; i++) setter(targets[i], interpolate(from, to, weight));
    }
    public void TweenUpdate() => scheduler.Update(Delta);
    public void Validate()
    {
        if (handles.Any(h => h.IsTerminal || h.Error is not null))
            throw new InvalidOperationException("Catalog workload terminated or faulted: " + handles.FirstOrDefault(h => h.Error is not null)?.Error);
    }
    public void Dispose() { scheduler.Dispose(); parameter?.Dispose(); }
}
