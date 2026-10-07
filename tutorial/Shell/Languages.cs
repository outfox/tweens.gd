using System;
using Godot;

namespace tutorial;

public enum Language
{
    CSharp,
    GDScript,
}

/// <summary>The tutorial's language. Lessons, code, prose, and accents follow it.</summary>
public static class Languages
{
    public static Language Current { get; private set; }
    public static bool IsGDScript => Current == Language.GDScript;

    /// <summary>Raised after <see cref="Current"/> changes.</summary>
    public static event Action<Language>? Changed;

    public static void Set(Language language)
    {
        if (language == Current)
            return;
        Current = language;
        Changed?.Invoke(language);
    }

    public static T Pick<T>(T csharp, T gdscript) => IsGDScript ? gdscript : csharp;

    /// <summary>A lesson member's name: PascalCase in C#, snake_case in GDScript.</summary>
    public static StringName Member(string csharpName) => IsGDScript ? csharpName.ToSnakeCase() : csharpName;
}
