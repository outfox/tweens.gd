using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace tutorial;

/// <summary>Body copy written like the website's Markdown: `code`, **bold**, [links](/path/), and lists.</summary>
[Tool, GlobalClass]
public partial class Prose : RichTextLabel
{
    private const string Site = "https://tweens.gd";
    private string markdown = "";

    [Export(PropertyHint.MultilineText)]
    public string Markdown
    {
        get => markdown;
        set
        {
            markdown = value;
            Render();
        }
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
            MetaClicked += meta => OS.ShellOpen(meta.AsString());
        Render();
    }

    public override void _Notification(int what)
    {
        // Code and link colors follow the language accent.
        if (what == NotificationThemeChanged)
            Render();
    }

    private void Render()
    {
        if (!IsInsideTree())
            return;
        BbcodeEnabled = true;
        Text = ToBBCode(markdown, GetThemeColor("code_bg", "Tutorial"), GetThemeColor("accent_ink", "Tutorial"));
    }

    public static string ToBBCode(string markdown, Color code, Color link)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var bbcode = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            var list = ListOf(lines[i]);
            if (i > 0)
                bbcode.Append('\n');
            if (list is not null && (i == 0 || ListOf(lines[i - 1]) != list))
                bbcode.Append(list == "ul" ? "[ul]" : "[ol type=1]");
            bbcode.Append(Inline(list is null ? lines[i] : ItemMarker().Replace(lines[i], "", 1), code, link));
            if (list is not null && (i + 1 == lines.Length || ListOf(lines[i + 1]) != list))
                bbcode.Append($"[/{list}]");
        }
        return bbcode.ToString();
    }

    [GeneratedRegex(@"^(- |\d+\. )")]
    private static partial Regex ItemMarker();

    private static string? ListOf(string line)
        => ItemMarker().Match(line) is { Success: true } match ? match.Value.StartsWith('-') ? "ul" : "ol" : null;

    [GeneratedRegex(@"`([^`]+)`|\*\*([^*]+)\*\*|\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex InlineMarkup();

    private static string Inline(string text, Color code, Color link)
    {
        var bbcode = new StringBuilder();
        var at = 0;
        foreach (Match match in InlineMarkup().Matches(text))
        {
            bbcode.Append(Syntax.Escape(text[at..match.Index]));
            at = match.Index + match.Length;
            if (match.Groups[1].Success)
                bbcode.Append($"[bgcolor={code.ToHtml()}] [code]{Syntax.Escape(match.Groups[1].Value)}[/code] [/bgcolor]");
            else if (match.Groups[2].Success)
                bbcode.Append($"[b]{Syntax.Escape(match.Groups[2].Value)}[/b]");
            else
            {
                var url = match.Groups[4].Value;
                if (url.StartsWith('/'))
                    url = Site + url;
                bbcode.Append($"[url={url}][color={link.ToHtml()}]{Syntax.Escape(match.Groups[3].Value)}[/color][/url]");
            }
        }
        return bbcode.Append(Syntax.Escape(text[at..])).ToString();
    }
}
