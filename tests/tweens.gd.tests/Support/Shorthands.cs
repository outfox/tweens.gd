// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Reflection;
using System.Reflection.Emit;
using Godot;

namespace tweens.gd.Tests.Support;

/// <summary>The shorter endpoint forms that generated definitions and extension twins accept.</summary>
public static class Shorthands
{
    /// <summary>
    /// Component tuples and collections for vectors and colors, one number for vector scales, and color codes or names.
    /// </summary>
    public static Type[] Forms(Type value, string name)
    {
        Type[] uniform = name.Contains("Scale") ? [typeof(double)] : [];
        return value == typeof(Vector2) ? [typeof(ValueTuple<double, double>), typeof(ReadOnlySpan<double>), .. uniform]
            : value == typeof(Vector3) ? [typeof(ValueTuple<double, double, double>), typeof(ReadOnlySpan<double>), .. uniform]
            : value == typeof(Vector4) ? [typeof(ValueTuple<double, double, double, double>), typeof(ReadOnlySpan<double>)]
            : value == typeof(Color) ? [typeof(ValueTuple<double, double, double>), typeof(ValueTuple<double, double, double, double>),
                typeof(ReadOnlySpan<double>), typeof(string)]
            : [];
    }

    /// <summary>
    /// An argument in the given form, built from a sample value, and the endpoint it stands for. Collections are
    /// arrays; <see cref="Invoke"/> passes them as spans.
    /// </summary>
    public static (object Argument, object Expected) Create(Type form, object sample)
    {
        if (form == typeof(string)) return ("tomato", Colors.Tomato);
        if (form == typeof(double)) return (0.5, sample is Vector2 ? new Vector2(0.5f, 0.5f) : new Vector3(0.5f, 0.5f, 0.5f));
        double[] components = sample switch
        {
            Vector2 v => [v.X, v.Y],
            Vector3 v => [v.X, v.Y, v.Z],
            Vector4 v => [v.X, v.Y, v.Z, v.W],
            Color c => [c.R, c.G, c.B, c.A],
            _ => throw new NotSupportedException(sample.GetType().Name),
        };
        if (form == typeof(ReadOnlySpan<double>)) return (components, sample);
        var arity = form.GetGenericArguments().Length;
        var argument = Activator.CreateInstance(form, components.Take(arity).Cast<object>().ToArray())!;
        // Three color components leave the alpha opaque.
        return (argument, sample is Color color && arity == 3 ? new Color(color.R, color.G, color.B) : sample);
    }

    /// <summary>
    /// Calls a static method or constructor. Reflection cannot box spans, so double[] arguments reach
    /// ReadOnlySpan&lt;double&gt; parameters through an emitted call.
    /// </summary>
    public static object? Invoke(MethodBase method, object?[] arguments)
    {
        var parameters = method.GetParameters();
        if (parameters.All(parameter => parameter.ParameterType != typeof(ReadOnlySpan<double>)))
            return method is ConstructorInfo constructor ? constructor.Invoke(arguments) : method.Invoke(null, arguments);
        var call = new DynamicMethod("Invoke", typeof(object), [typeof(object?[])], typeof(Shorthands).Module, skipVisibility: true);
        var il = call.GetILGenerator();
        for (var i = 0; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);
            if (type == typeof(ReadOnlySpan<double>))
            {
                il.Emit(OpCodes.Castclass, typeof(double[]));
                il.Emit(OpCodes.Newobj, typeof(ReadOnlySpan<double>).GetConstructor([typeof(double[])])!);
            }
            else il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        }
        var result = method is ConstructorInfo created ? created.DeclaringType! : ((MethodInfo)method).ReturnType;
        if (method is ConstructorInfo target) il.Emit(OpCodes.Newobj, target);
        else il.Emit(OpCodes.Call, (MethodInfo)method);
        if (result.IsValueType) il.Emit(OpCodes.Box, result);
        il.Emit(OpCodes.Ret);
        return call.Invoke(null, [arguments]);
    }
}
