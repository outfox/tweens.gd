using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace tutorial;

/// <summary>Colors code as BBCode: Rider's Islands Dark for C#, Godot's script editor for GDScript.</summary>
public static partial class Syntax
{
    private sealed record Palette(string Text, string Keyword, string Control, string Type, string Function,
        string Member, string Number, string String, string Comment, string Annotation, string Punctuation);

    private static readonly Palette CSharp = new("#bcbec4", "#6c95eb", "#6c95eb", "#c191ff", "#39cc9b", "#66c3cc",
        "#ed94c0", "#c9a26d", "#85c46c", "#c191ff", "#bcbec4");

    private static readonly Palette GDScript = new("#cdcfd2", "#ff7085", "#ff8ccc", "#8fffdb", "#57b3ff", "#bce0ff",
        "#a1ffe0", "#ffeda1", "#8a9196", "#ffb373", "#abc9ff");

    private static readonly HashSet<string> CSharpKeywords =
    [
        "using", "public", "private", "protected", "internal", "partial", "class", "struct", "record", "override",
        "void", "if", "else", "is", "or", "and", "not", "var", "new", "for", "foreach", "in", "with", "this", "true",
        "false", "null", "return", "await", "async", "static", "readonly", "get", "set", "double", "float", "int",
        "bool", "string", "long", "delegate", "namespace",
    ];

    private static readonly HashSet<string> GDScriptKeywords =
    [
        "extends", "func", "var", "const", "signal", "class_name", "static", "and", "or", "not", "is", "as", "in",
        "true", "false", "null", "self", "void",
    ];

    private static readonly HashSet<string> GDScriptControl =
        ["if", "elif", "else", "for", "while", "match", "return", "await", "break", "continue", "pass"];

    [GeneratedRegex("""(?<comment>//.*)|(?<string>\$?"(?:[^"\\]|\\.)*")|(?<number>\b\d+(?:\.\d+)?\b)|(?<annotation>@\w+)|(?<ident>[A-Za-z_]\w*)|(?<space>\s+)|(?<punct>.)""")]
    private static partial Regex CSharpTokens();

    [GeneratedRegex("""(?<comment>#.*)|(?<string>&?"(?:[^"\\]|\\.)*")|(?<number>\b\d+(?:\.\d+)?\b)|(?<annotation>@\w+)|(?<ident>[A-Za-z_]\w*)|(?<space>\s+)|(?<punct>.)""")]
    private static partial Regex GDScriptTokens();

    private sealed record Token(string Kind, string Text);

    /// <summary>Highlights one line, or part of one, as BBCode.</summary>
    public static string Highlight(string code, Language language)
    {
        var gd = language == Language.GDScript;
        var palette = gd ? GDScript : CSharp;
        var tokens = new List<Token>();
        foreach (Match match in (gd ? GDScriptTokens() : CSharpTokens()).Matches(code))
        {
            foreach (var kind in new[] { "comment", "string", "number", "annotation", "ident", "space", "punct" })
            {
                if (!match.Groups[kind].Success)
                    continue;
                tokens.Add(new(kind, match.Value));
                break;
            }
        }

        var bbcode = new StringBuilder();
        var attribute = false;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind == "punct" && token.Text == "[" && !gd)
                attribute = IsAttribute(tokens, i);
            else if (token.Kind == "punct" && token.Text == "]")
                attribute = false;
            var color = token.Kind switch
            {
                "comment" => palette.Comment,
                "string" => palette.String,
                "number" => palette.Number,
                "annotation" => palette.Annotation,
                "punct" => palette.Punctuation,
                "ident" => gd ? GDScriptColor(tokens, i, palette) : CSharpColor(tokens, i, palette, attribute),
                _ => null,
            };
            var text = Escape(token.Text);
            bbcode.Append(color is null || color == palette.Text ? text : $"[color={color}]{text}[/color]");
        }
        return bbcode.ToString();
    }

    public static string Escape(string text) => text.Replace("[", "\u0001").Replace("]", "[rb]").Replace("\u0001", "[lb]");

    private static string CSharpColor(List<Token> tokens, int i, Palette palette, bool attribute)
    {
        var text = tokens[i].Text;
        if (CSharpKeywords.Contains(text))
            return palette.Keyword;
        var previous = Previous(tokens, i);
        var next = Next(tokens, i);
        if (previous == ".")
        {
            // Nested definition types, as in new Tweens.Position2D(...).
            if (Previous(tokens, i, 2) == "Tweens" && char.IsUpper(text[0]))
                return palette.Type;
            return next == "(" ? palette.Function : palette.Member;
        }
        if (!char.IsUpper(text[0]))
            return next == "(" ? palette.Function : palette.Text;
        if (attribute || previous is "new" or ":" or "class" or "<" or "is" or "typeof")
            return palette.Type;
        if (next == "(")
            return palette.Function;
        if (Previous(tokens, i, 1) == "using" || FirstWord(tokens) == "using")
            return palette.Text;
        // Static access (Out.Back), generic arguments, and declarations (Sprite2D sprite) name types.
        if (next is "." or ">" or "?" or "<" || NextIsIdentifier(tokens, i))
            return palette.Type;
        return palette.Member;
    }

    private static string GDScriptColor(List<Token> tokens, int i, Palette palette)
    {
        var text = tokens[i].Text;
        if (GDScriptControl.Contains(text))
            return palette.Control;
        if (GDScriptKeywords.Contains(text))
            return palette.Keyword;
        var previous = Previous(tokens, i);
        var next = Next(tokens, i);
        if (previous == ".")
            return next == "(" ? palette.Function : palette.Member;
        if (char.IsUpper(text[0]))
            return text.Length > 1 && text == text.ToUpperInvariant() ? palette.Member : palette.Type;
        return next == "(" && previous != "func" ? palette.Function : palette.Text;
    }

    private static bool IsAttribute(List<Token> tokens, int i)
    {
        for (var j = i - 1; j >= 0; j--)
            if (tokens[j].Kind != "space")
                return false;
        return true;
    }

    private static string? FirstWord(List<Token> tokens)
    {
        foreach (var token in tokens)
            if (token.Kind != "space")
                return token.Text;
        return null;
    }

    private static string? Previous(List<Token> tokens, int i, int count = 1)
    {
        for (var j = i - 1; j >= 0; j--)
        {
            if (tokens[j].Kind == "space")
                continue;
            if (--count == 0)
                return tokens[j].Text;
        }
        return null;
    }

    private static string? Next(List<Token> tokens, int i)
    {
        for (var j = i + 1; j < tokens.Count; j++)
            if (tokens[j].Kind != "space")
                return tokens[j].Text;
        return null;
    }

    private static bool NextIsIdentifier(List<Token> tokens, int i)
    {
        for (var j = i + 1; j < tokens.Count; j++)
            if (tokens[j].Kind != "space")
                return tokens[j].Kind == "ident" || tokens[j].Text == "@";
        return false;
    }
}
