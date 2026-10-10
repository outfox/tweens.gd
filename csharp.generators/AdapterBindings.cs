// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace tweens.gd.Generators;

// Symbols stay inside compilation transforms. Both language catalogs and typed keyframes use
// these bindings, including the Godot class that actually declares each property.
internal sealed class AdapterBinding
{
    internal string Name { get; }
    internal INamedTypeSymbol Target { get; }
    internal ITypeSymbol Value { get; }
    internal string Kind { get; }
    internal (string Path, string Owner)[] Properties { get; }

    internal AdapterBinding(string name, INamedTypeSymbol target, ITypeSymbol value, string kind,
        (string Path, string Owner)[] properties)
        => (Name, Target, Value, Kind, Properties) = (name, target, value, kind, properties);
}

internal static class AdapterBindings
{
    internal static (ITypeSymbol Value, string Path, string Owner)? ResolveProperty(
        INamedTypeSymbol target, string name, string component)
    {
        for (var current = target; current is not null; current = current.BaseType)
        {
            var property = current.GetMembers(name).OfType<IPropertySymbol>().SingleOrDefault();
            if (property is null) continue;
            if (property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                || property.SetMethod?.DeclaredAccessibility != Accessibility.Public) return null;
            var value = property.Type;
            if (component.Length != 0)
            {
                var member = value.GetMembers(component).SingleOrDefault();
                value = member switch { IFieldSymbol field => field.Type, IPropertySymbol part => part.Type, _ => null };
                if (value is null) return null;
            }
            return (value, Snake(name) + (component.Length == 0 ? "" : ":" + Snake(component)), NativeName(property.ContainingType));
        }
        return null;
    }

    internal static IEnumerable<AdapterBinding> Read(Compilation compilation, CancellationToken token)
    {
        var propertyTween = compilation.GetTypeByMetadataName("tweens.gd.PropertyTween`2");
        if (propertyTween is null) yield break;
        foreach (var symbol in propertyTween.ContainingNamespace.GetTypeMembers().OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            if (!symbol.IsSealed || symbol.Arity != 0 || symbol.DeclaredAccessibility != Accessibility.Public
                || !symbol.Name.EndsWith("Tween", StringComparison.Ordinal)
                || symbol.BaseType is not { Arity: 2 } parent
                || !SymbolEqualityComparer.Default.Equals(parent.OriginalDefinition, propertyTween)
                || parent.TypeArguments[0] is not INamedTypeSymbol target
                || target.ContainingNamespace.ToDisplayString() != "Godot") continue;

            var getter = Getter(symbol, compilation, token);
            var name = symbol.Name.Substring(0, symbol.Name.Length - "Tween".Length);
            var value = parent.TypeArguments[1];
            var operation = Unwrap(getter);
            string kind;
            (string Path, string Owner)[] properties;
            if (target.ToDisplayString() == "Godot.Node" && symbol.GetMembers("ReadsWrittenValue")
                .OfType<IPropertySymbol>().Any(property => property.IsOverride && property.DeclaringSyntaxReferences.Any(reference =>
                    reference.GetSyntax(token) is PropertyDeclarationSyntax { ExpressionBody.Expression: { } expression }
                    && compilation.GetSemanticModel(expression.SyntaxTree).GetConstantValue(expression, token).Value is false)))
            {
                kind = "value";
                properties = Array.Empty<(string, string)>();
            }
            else if (operation is IObjectCreationOperation creation && value.Name.StartsWith("Vector", StringComparison.Ordinal))
            {
                kind = "compound";
                properties = creation.Arguments.Select(argument => Property(argument.Value)).ToArray();
            }
            else if (operation is IInvocationOperation invocation && invocation.TargetMethod.Name == "FromEuler"
                && invocation.TargetMethod.ContainingType.ToDisplayString() == "Godot.Quaternion")
            {
                kind = "global_quaternion";
                properties = new[] { Property(invocation.Arguments.Single().Value) };
            }
            else
            {
                kind = "property";
                properties = new[] { Property(operation) };
            }
            yield return new AdapterBinding(name, target, value, kind, properties);
        }
    }

    private static IOperation Getter(INamedTypeSymbol symbol, Compilation compilation, CancellationToken token)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(token);
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var arguments in syntax.DescendantNodes().OfType<ArgumentListSyntax>())
            {
                if (arguments.Parent is not (PrimaryConstructorBaseTypeSyntax or ConstructorInitializerSyntax)
                    || arguments.Arguments.FirstOrDefault()?.Expression is not LambdaExpressionSyntax lambda) continue;
                var body = lambda.Body is ExpressionSyntax expression ? expression
                    : ((BlockSyntax)lambda.Body).Statements.OfType<ReturnStatementSyntax>().SingleOrDefault()?.Expression;
                if (body is not null && model.GetOperation(body, token) is { } operation) return operation;
            }
        }
        throw new InvalidOperationException("No property getter found for " + symbol.Name + ".");
    }

    private static IOperation Unwrap(IOperation operation) => operation switch
    {
        IConversionOperation conversion => Unwrap(conversion.Operand),
        IParenthesizedOperation parentheses => Unwrap(parentheses.Operand),
        _ => operation,
    };

    private static (string Path, string Owner) Property(IOperation operation)
    {
        operation = Unwrap(operation);
        var (member, instance) = operation switch
        {
            IPropertyReferenceOperation property => ((ISymbol)property.Property, property.Instance),
            IFieldReferenceOperation field => ((ISymbol)field.Field, field.Instance),
            _ => throw new InvalidOperationException("Unsupported tween getter: " + operation.Syntax),
        };
        if (instance is not null && Unwrap(instance) is IParameterReferenceOperation)
            return (Snake(member.Name), NativeName(member.ContainingType));
        if (instance is not null)
        {
            var parent = Property(instance);
            return (parent.Path + ":" + Snake(member.Name), parent.Owner);
        }
        throw new InvalidOperationException("Tween getter must read a target property: " + operation.Syntax);
    }

    internal static string NativeName(INamedTypeSymbol type)
        => type.Name.StartsWith("Gpu", StringComparison.Ordinal) ? "GPU" + type.Name.Substring(3)
            : type.Name.StartsWith("Cpu", StringComparison.Ordinal) ? "CPU" + type.Name.Substring(3) : type.Name;

    private static string Snake(string name)
    {
        name = Regex.Replace(name, "([23])D([XYZ])", "$1d_$2");
        name = Regex.Replace(name, "([23])D", "$1d");
        name = Regex.Replace(name, "([a-zA-Z])([23]d)", "$1_$2");
        name = Regex.Replace(name, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        return Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }

    internal static string Manifest(Compilation compilation, CancellationToken token)
    {
        var source = new StringBuilder("[\n");
        var separator = "";
        foreach (var binding in Read(compilation, token).OrderBy(binding => Snake(binding.Name), StringComparer.Ordinal))
        {
            var name = Snake(binding.Name) + (binding.Kind == "value" ? "_value" : "");
            var type = binding.Value.SpecialType switch
            {
                SpecialType.System_Single => "float", SpecialType.System_Double => "double", SpecialType.System_Int32 => "int",
                _ => binding.Value.Name,
            };
            // Names and paths come from identifiers, so JSON quoting needs no text escaping.
            string Quoted(string value) => "\"" + value + "\"";
            source.Append(separator).Append("  {\n    \"csharp\": ").Append(Quoted(binding.Name))
                .Append(",\n    \"name\": ").Append(Quoted(name))
                .Append(",\n    \"target\": ").Append(Quoted(NativeName(binding.Target)))
                .Append(",\n    \"type\": ").Append(Quoted(type))
                .Append(",\n    \"kind\": ").Append(Quoted(binding.Kind))
                .Append(",\n    \"paths\": [").Append(binding.Properties.Length == 0 ? "" : "\n      "
                    + string.Join(",\n      ", binding.Properties.Select(property => Quoted(property.Path))) + "\n    ")
                .Append("],\n    \"declaringTypes\": [").Append(string.Join(", ", binding.Properties.Select(property => Quoted(property.Owner))))
                .Append("]\n  }");
            separator = ",\n";
        }
        return source.Append(source.Length > 2 ? "\n]\n" : "]\n").ToString();
    }
}
