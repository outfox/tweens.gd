using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using tweens.gd;

namespace tutorial;

/// <summary>
/// A code panel for the current language. It shows a lesson file, a method of one, or a snippet. Slots underline
/// values a page controls and flash when they change; marks number lines the notes beside the code explain.
/// </summary>
[Tool, GlobalClass]
public partial class CodeView : PanelContainer
{
    private const float Gutter = 34;

    private sealed class Slot(string name, Color color, Regex? csharp, Regex? gdscript)
    {
        public string Name { get; } = name;
        public Color Color { get; } = color;
        public Regex? Pattern => Languages.IsGDScript ? gdscript : csharp;
        public string? Value { get; set; }
        public float Flash { get; set; }
    }

    private sealed record Mark(int Number, Regex CSharpStart, Regex? CSharpEnd, Regex GDScriptStart, Regex? GDScriptEnd);

    private readonly List<Slot> slots = [];
    private readonly List<Mark> marks = [];
    private readonly Dictionary<int, float> flashes = [];
    private string csharpPath = "";
    private string gdscriptPath = "";
    private string? csharpCode;
    private string? gdscriptCode;
    private string? method;
    private string source = "";
    private string[] lines = [];
    private RichTextLabel code = null!;
    private Label file = null!;
    private Button copy = null!;

    /// <summary>The C# lesson file, below Lessons/.</summary>
    [Export]
    public string CSharpPath
    {
        get => csharpPath;
        set
        {
            csharpPath = value;
            Refresh();
        }
    }

    /// <summary>The GDScript lesson file, below Lessons/.</summary>
    [Export]
    public string GDScriptPath
    {
        get => gdscriptPath;
        set
        {
            gdscriptPath = value;
            Refresh();
        }
    }

    /// <summary>Replaces the file name in the bar, for snippets.</summary>
    [Export] public string Title { get; set; } = "";

    /// <summary>Shows the bar with the file name and the copy button.</summary>
    [Export] public bool ShowBar { get; set; } = true;

    /// <summary>A smaller code size, for two panels side by side.</summary>
    [Export] public bool Compact { get; set; }

    public string Source => source;

    /// <summary>The code as shown, with every slot's current value.</summary>
    public string Text => code.GetParsedText();

    public override void _Ready()
    {
        code = GetNode<RichTextLabel>("%Code");
        file = GetNode<Label>("%File");
        copy = GetNode<Button>("%Copy");
        if (Compact)
        {
            code.AddThemeFontSizeOverride("normal_font_size", 13);
            code.AddThemeConstantOverride("line_separation", 6);
        }
        // Marks follow the code's lines, so redraw whenever the layout moves them.
        code.Finished += QueueRedraw;
        code.Resized += QueueRedraw;
        GetNode<Container>("Layout").SortChildren += QueueRedraw;
        GetNode<Container>("%Body").SortChildren += QueueRedraw;
        if (!Engine.IsEditorHint())
            copy.Pressed += Copy;
        Refresh();
    }

    public override void _EnterTree()
    {
        if (!Engine.IsEditorHint())
            Languages.Changed += OnLanguageChanged;
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
            Languages.Changed -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Language language)
    {
        flashes.Clear();
        Refresh();
    }

    /// <summary>Shows one method's body of the lesson files, instead of the whole files.</summary>
    public void ShowMethod(string? csharpName)
    {
        method = csharpName;
        Refresh();
    }

    /// <summary>Shows a snippet instead of a file. «name:value» marks a slot's value.</summary>
    public void ShowCode(string csharp, string gdscript)
    {
        csharpCode = csharp;
        gdscriptCode = gdscript;
        Refresh();
    }

    /// <summary>Underlines the first group of a pattern on each line, or a «name:value» marker.</summary>
    public void AddSlot(string name, Color color, string? csharpPattern = null, string? gdscriptPattern = null)
    {
        slots.Add(new Slot(name, color, csharpPattern is null ? null : new Regex(csharpPattern),
            gdscriptPattern is null ? null : new Regex(gdscriptPattern)));
        Render();
    }

    /// <summary>Changes a slot's value, and flashes it unless <paramref name="quiet"/>.</summary>
    public void SetSlot(string name, string value, bool quiet = false)
    {
        var slot = slots.Single(s => s.Name == name);
        slot.Value = value;
        if (quiet)
            Render();
        else
            this.TweenFloat(0, 1, options =>
            {
                options.From = 1;
                options.Ease = Out.Expo;
                options.OnUpdate = (_, flash) =>
                {
                    slot.Flash = flash;
                    Render();
                };
            });
    }

    /// <summary>Numbers the lines from a start pattern through an end pattern, or the start line alone.</summary>
    public void AddMark(int number, string csharpStart, string? csharpEnd, string gdscriptStart, string? gdscriptEnd)
    {
        marks.Add(new Mark(number, new Regex(csharpStart), csharpEnd is null ? null : new Regex(csharpEnd),
            new Regex(gdscriptStart), gdscriptEnd is null ? null : new Regex(gdscriptEnd)));
        Render();
    }

    /// <summary>Flashes the first line that matches the current language's pattern.</summary>
    public void FlashLine(string csharpPattern, string gdscriptPattern)
    {
        var pattern = new Regex(Languages.Pick(csharpPattern, gdscriptPattern));
        var line = System.Array.FindIndex(lines, pattern.IsMatch);
        if (line < 0)
            return;
        this.TweenFloat(0, 1.2, options =>
        {
            options.From = 1;
            options.Ease = Out.Expo;
            options.OnUpdate = (_, flash) =>
            {
                flashes[line] = flash;
                QueueRedraw();
            };
        });
    }

    private void Refresh()
    {
        // During editor assembly reload, the native node is ready before C# references are restored.
        if (!IsNodeReady() || code is null || file is null)
            return;
        var gd = Languages.IsGDScript && !Engine.IsEditorHint();
        var path = gd ? gdscriptPath : csharpPath;
        if (csharpCode is not null)
            source = gd ? gdscriptCode! : csharpCode;
        else if (path.Length > 0)
        {
            source = Sources.Read(path);
            if (method is not null)
                source = Sources.Method(source, gd ? method.ToSnakeCase() : method, Languages.Current);
        }
        file.Text = Title.Length > 0 ? Title : path.Split('/')[^1];
        GetNode<Control>("%Bar").Visible = ShowBar;
        // Tabs become four spaces, as in Godot's script editor.
        lines = source.Replace("\t", "    ").Split('\n');
        Render();
    }

    [GeneratedRegex("«(\\w+):([^»]*)»")]
    private static partial Regex Marker();

    private void Render()
    {
        if (!IsNodeReady() || code is null)
            return;
        var language = Engine.IsEditorHint() ? Language.CSharp : Languages.Current;
        var bbcode = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                bbcode.Append('\n');
            var line = lines[i];
            foreach (var slot in slots)
                if (slot.Pattern?.Match(line) is { Success: true } match)
                {
                    var group = match.Groups[1];
                    line = $"{line[..group.Index]}«{slot.Name}:{group.Value}»{line[(group.Index + group.Length)..]}";
                }
            var at = 0;
            foreach (Match marker in Marker().Matches(line))
            {
                bbcode.Append(Syntax.Highlight(line[at..marker.Index], language));
                at = marker.Index + marker.Length;
                var slot = slots.FirstOrDefault(s => s.Name == marker.Groups[1].Value);
                var value = Syntax.Highlight(slot?.Value ?? marker.Groups[2].Value, language);
                if (slot is null)
                {
                    bbcode.Append(value);
                    continue;
                }
                var flash = slot.Color with { A = 0.35f * slot.Flash };
                bbcode.Append($"[bgcolor={flash.ToHtml()}][u color={slot.Color.ToHtml()}]{value}[/u][/bgcolor]");
            }
            bbcode.Append(Syntax.Highlight(line[at..], language));
        }
        code.Text = bbcode.ToString();
        GetNode<MarginContainer>("%Body").AddThemeConstantOverride("margin_left", marks.Count > 0 ? (int)Gutter : 0);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (code is null || lines.Length == 0 || code.GetLineCount() < lines.Length)
            return;
        var origin = code.GetGlobalRect().Position - GetGlobalRect().Position;
        var accent = GetThemeColor("accent", "Tutorial");
        var right = Size.X - GetThemeStylebox("panel").GetMargin(Side.Right) / 2;
        var left = GetThemeStylebox("panel").GetMargin(Side.Left) / 2;
        Rect2 Row(int first, int last) => new(left, origin.Y + code.GetLineOffset(first) - 2, right - left,
            code.GetLineOffset(last) - code.GetLineOffset(first) + code.GetLineHeight(last) + 2);

        foreach (var mark in marks)
        {
            var first = System.Array.FindIndex(lines, Languages.Pick(mark.CSharpStart, mark.GDScriptStart).IsMatch);
            if (first < 0)
                continue;
            var end = Languages.Pick(mark.CSharpEnd, mark.GDScriptEnd);
            var last = end is null ? first : System.Array.FindIndex(lines, first, end.IsMatch);
            var rect = Row(first, last < 0 ? first : last);
            DrawRect(rect, accent with { A = 0.1f });
            DrawRect(new Rect2(rect.Position, new Vector2(3, rect.Size.Y)), accent);
            var center = new Vector2(left + Gutter / 2 + 4, origin.Y + code.GetLineOffset(first) + code.GetLineHeight(first) / 2 - 1);
            DrawCircle(center, 10, accent, antialiased: true);
            var font = GetThemeFont("font", "ValueLabel");
            var number = mark.Number.ToString();
            var size = font.GetStringSize(number, HorizontalAlignment.Left, -1, 13);
            DrawString(font, center + new Vector2(-size.X / 2, size.Y / 2 - 3), number, HorizontalAlignment.Left, -1, 13,
                GetThemeColor("on_accent", "Tutorial"));
        }
        foreach (var (line, flash) in flashes)
            if (line < lines.Length && flash > 0)
                DrawRect(Row(line, line), GetThemeColor("mint", "Tutorial") with { A = 0.3f * flash });
    }

    private void Copy()
    {
        DisplayServer.ClipboardSet(source);
        copy.Text = "Copied";
        this.TweenFloat(0, 1.5, options => options.OnEnd = _ => copy.Text = "Copy");
    }
}
