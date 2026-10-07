using System;
using System.IO;
using System.Linq;

namespace tutorial;

/// <summary>Reads lesson files embedded in the build, so code panels show the exact code that runs.</summary>
public static class Sources
{
    /// <summary>Reads a lesson file by its path below Lessons/, such as "Quickstart/ClickToMove.cs".</summary>
    public static string Read(string path)
    {
        // MSBuild names the resources with the build machine's directory separator.
        var assembly = typeof(Sources).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(name => name.Replace('\\', '/') == $"Lessons/{path}")
            ?? throw new FileNotFoundException($"No embedded lesson source '{path}'.");
        using var stream = assembly.GetManifestResourceStream(name)!;
        return new StreamReader(stream).ReadToEnd().Replace("\r\n", "\n").TrimEnd('\n');
    }

    /// <summary>The body of a method, dedented: braces delimit it in C#, indentation in GDScript.</summary>
    public static string Method(string source, string name, Language language)
    {
        var lines = source.Split('\n');
        var header = language == Language.GDScript ? $"func {name}(" : $" {name}(";
        var start = Array.FindIndex(lines, line => line.Contains(header));
        if (start < 0)
            throw new ArgumentException($"No method '{name}' in the lesson source.");

        var body = language == Language.GDScript ? GDScriptBody(lines, start) : CSharpBody(lines, start);
        var indent = body.Where(line => line.Trim().Length > 0).Min(line => line.Length - line.TrimStart().Length);
        return string.Join('\n', body.Select(line => line.Length >= indent ? line[indent..] : "")).Trim('\n');
    }

    private static string[] CSharpBody(string[] lines, int start)
    {
        var open = Array.FindIndex(lines, start, line => line.Contains('{'));
        var depth = 0;
        for (var end = open; end < lines.Length; end++)
        {
            depth += lines[end].Count(c => c == '{') - lines[end].Count(c => c == '}');
            if (depth == 0)
                return lines[(open + 1)..end];
        }
        throw new ArgumentException("Unbalanced braces in the lesson source.");
    }

    private static string[] GDScriptBody(string[] lines, int start)
    {
        var end = start + 1;
        while (end < lines.Length && (lines[end].Length == 0 || char.IsWhiteSpace(lines[end][0])))
            end++;
        return lines[(start + 1)..end];
    }
}
