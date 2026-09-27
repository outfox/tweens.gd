// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Collections.Generic;
using Godot;
namespace testbed;

/// <summary>Read-only source for the selected running implementation and its shared scene.</summary>
public partial class GallerySourceView : VBoxContainer
{
    private readonly List<Resource> resources = [];
    private GallerySource example = null!, setup = null!;
    private OptionButton files = null!;
    private Label path = null!;
    private Button entry = null!, copy = null!;
    private GalleryLanguage language;
    private CodeHighlighter csharpHighlighter = null!, gdscriptHighlighter = null!;
    public CodeEdit Code { get; private set; } = null!;
    public GallerySource Source { get; private set; } = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(480, 320);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsStretchRatio = 1.35f;
        AddThemeConstantOverride("separation", 8);

        files = this.Add(new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        files.AddThemeFontSizeOverride("font_size", 12);
        foreach (var name in new[] { "Animation", "Scene & playback", "Playback helpers", "Scene helpers", "Palette", "Node helpers", "Shader panels" }) files.AddItem(name);
        files.ItemSelected += index => ShowFile((int)index);
        var toolbar = this.Add(new HBoxContainer());
        copy = toolbar.Add(new Button { Text = "Copy file", TooltipText = "Copy the complete source file" });
        copy.Pressed += () => { DisplayServer.ClipboardSet(Code.Text); copy.Text = "Copied!"; };
        entry = toolbar.Add(new Button { Text = "Tween entry", TooltipText = "Jump to the animation code or its playback entry point" });
        entry.Pressed += JumpToTween;
        var top = toolbar.Add(new Button { Text = "File start" });
        top.Pressed += () => Jump(0);
        var wrap = toolbar.Add(new CheckBox { Text = "Wrap", ButtonPressed = true });
        wrap.Toggled += enabled => Code.WrapMode = enabled ? TextEdit.LineWrappingMode.Boundary : TextEdit.LineWrappingMode.None;
        foreach (var control in toolbar.GetChildren())
            if (control is Control item) item.AddThemeFontSizeOverride("font_size", 12);

        path = this.Add(GalleryTheme.Label("", 12, Palette.Muted));
        path.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        Code = this.Add(new CodeEdit
        {
            Name = "SourceCode", Editable = false, GuttersDrawLineNumbers = true,
            GuttersLineNumbersMinDigits = 2, HighlightCurrentLine = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollPastEndOfFile = true, ContextMenuEnabled = true,
            WrapMode = TextEdit.LineWrappingMode.Boundary,
        });
        var font = Own(GD.Load<FontFile>("res://Fonts/JetBrainsMono-Regular.ttf"));
        Code.AddThemeFontOverride("font", font);
        Code.AddThemeFontSizeOverride("font_size", 14);
        Code.AddThemeColorOverride("font_color", Palette.Soft);
        Code.AddThemeColorOverride("font_readonly_color", Palette.Soft);
        Code.AddThemeColorOverride("line_number_color", Palette.Muted);
        Code.AddThemeColorOverride("background_color", Palette.Stage);
        Code.AddThemeColorOverride("current_line_color", Palette.Raised);
        Code.AddThemeColorOverride("selection_color", new Color("355276"));
        var box = Own(GalleryTheme.Box(Palette.Stage, 10, 1, null, 10, 12));
        Code.AddThemeStyleboxOverride("normal", box);
        Code.AddThemeStyleboxOverride("read_only", box);
        var focus = Own(GalleryTheme.Box(Colors.Transparent, 10, 1, Palette.Mint));
        focus.DrawCenter = false;
        Code.AddThemeStyleboxOverride("focus", focus);
        csharpHighlighter = Own(CreateHighlighter());
        gdscriptHighlighter = Own(CreateHighlighter(gdscript: true));
        Code.SyntaxHighlighter = csharpHighlighter;
    }

    public void ShowEffect(GalleryEffect effect, GalleryLanguage selectedLanguage = GalleryLanguage.CSharp)
    {
        language = selectedLanguage;
        example = GallerySource.ForEffect(effect, language);
        setup = GallerySource.ForSetup(effect);
        files.SetItemText(0, language == GalleryLanguage.CSharp ? "Animation" : "Animation · GDScript");
        files.SetItemText(1, language == GalleryLanguage.CSharp ? "Scene & playback" : "Shared scene · C#");
        files.SetItemText(2, language == GalleryLanguage.CSharp ? "Playback helpers" : "Playback helpers · GDScript");
        files.SetItemText(3, language == GalleryLanguage.CSharp ? "Scene helpers" : "Shared scene helpers · C#");
        files.SetItemText(4, language == GalleryLanguage.CSharp ? "Palette" : "Shared palette · C#");
        files.SetItemText(5, language == GalleryLanguage.CSharp ? "Node helpers" : "Shared node helpers · C#");
        files.SetItemText(6, language == GalleryLanguage.CSharp ? "Shader panels" : "Shared shader panels · C#");
        files.Select(0);
        ShowFile(0);
    }

    private void ShowFile(int index)
    {
        Source = index switch
        {
            1 => setup,
            2 => GallerySource.Load(language == GalleryLanguage.CSharp ? "GalleryEffect.cs" : "GalleryAnimation.gd"),
            3 => GallerySource.Load("GalleryEffect.Stage.cs"),
            4 => GallerySource.Load("Palette.cs"),
            5 => GallerySource.Load("NodeExtensions.cs"),
            6 => GallerySource.Load("SplitPanel.cs"),
            _ => example,
        };
        path.Text = Source.Path;
        path.TooltipText = Source.Path;
        Code.Text = Source.Text;
        Code.SyntaxHighlighter = Source.Path.EndsWith(".gd", System.StringComparison.Ordinal) ? gdscriptHighlighter : csharpHighlighter;
        Code.Deselect();
        entry.Disabled = Source.TweenLine < 0;
        copy.Text = "Copy file";
        Jump(Source.TweenLine < 0 ? 0 : Source.TweenLine);
    }

    private void JumpToTween() => Jump(Source.TweenLine < 0 ? 0 : Source.TweenLine);

    private void Jump(int line)
    {
        Code.SetCaretLine(line);
        Code.SetCaretColumn(0);
        Code.SetLineAsFirstVisible(line);
        Code.ScrollHorizontal = 0;
    }

    private static CodeHighlighter CreateHighlighter(bool gdscript = false)
    {
        var highlighter = new CodeHighlighter
        {
            NumberColor = Palette.Mint, SymbolColor = Palette.Muted,
            FunctionColor = Palette.Blue, MemberVariableColor = Palette.Soft,
        };
        foreach (var word in ("extends class_name func signal pass elif and or self preload enum match break continue " +
            "using namespace public private protected internal sealed abstract partial static readonly const " +
            "override virtual async await return if else for foreach while in is not null true false new var void bool byte " +
            "int long float double string object out ref params get set init record struct switch case default try catch throw typeof with yield").Split(' '))
            highlighter.AddKeywordColor(word, new Color("d5a6ef"));
        foreach (var word in ("Vector2 Vector3 Vector4 Vector2I Color Colors Math MathF Mathf Task Tweens TweenOptions TweenOptionsBuilder TweenInstance " +
            "Group EaseType TweenState Reason Node Node2D Node3D Control Stage Palette GalleryEffect " +
            "Polygon2D Line2D ShaderMaterial StandardMaterial3D Camera2D Camera3D IEnumerable").Split(' '))
            highlighter.AddKeywordColor(word, Palette.Mint);
        highlighter.AddColorRegion(gdscript ? "#" : "//", "", Palette.Muted, true);
        if (!gdscript) highlighter.AddColorRegion("/*", "*/", Palette.Muted);
        highlighter.AddColorRegion("\"\"\"", "\"\"\"", Palette.Amber);
        highlighter.AddColorRegion("\"", "\"", Palette.Amber);
        highlighter.AddColorRegion("'", "'", Palette.Amber);
        return highlighter;
    }

    private T Own<T>(T resource) where T : Resource { resources.Add(resource); return resource; }

    /// <summary>Called after the page's controls have been freed.</summary>
    public void ReleaseResources()
    {
        foreach (var resource in resources) resource.Dispose();
        resources.Clear();
    }
}
