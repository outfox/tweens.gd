// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Globalization;
using System.Linq;
using Godot;
using tweens.gd;

namespace playground;

public partial class EasingPlayground : Control
{
    public EasingSettings Settings { get; private set; } = new();
    public TweenInstance? Playback { get; private set; }
    public float Progress { get; private set; }
    public float Weight { get; private set; }
    public bool IsPlaying => Playback is { IsTerminal: false };
    public string Recipe => Settings.Recipe(language.Selected == 1);
    private OptionButton entry = null!, exit = null!, blendType = null!, language = null!;
    private HSlider duration = null!, skew = null!, blend = null!, scrub = null!;
    private Label durationOut = null!, skewOut = null!, blendOut = null!, valueOut = null!, timeOut = null!, copyStatus = null!;
    private Button play = null!;
    private TextEdit code = null!;
    private EasingGraph graph = null!;
    private MotionPreview preview = null!;
    private GridContainer composer = null!;
    private bool syncing;

    public override void _Ready()
    {
        Theme = PlaygroundTheme.Create();
        var background = this.Add(new ColorRect { Color = PlaygroundTheme.Background, MouseFilter = MouseFilterEnum.Ignore });
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var scroll = this.Add(new ScrollContainer { Name = "PageScroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled });
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margins = scroll.Add(new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margins.AddThemeConstantOverride("margin_" + edge, 24);
        var page = margins.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        page.AddThemeConstantOverride("separation", 20);
        var heading = page.Add(new HBoxContainer());
        var title = heading.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        title.Add(PlaygroundTheme.Label("tweens.gd", 16, PlaygroundTheme.Mint));
        title.Add(PlaygroundTheme.Label("Easing playground", 30));
        var reset = heading.Add(new Button { Name = "Reset", Text = "Reset", SizeFlagsVertical = SizeFlags.ShrinkCenter });
        reset.Pressed += Reset;
        page.Add(PlaygroundTheme.Label("Compose a curve. Watch it move. Take the code.", 15, PlaygroundTheme.Muted));

        composer = page.Add(new GridContainer { Name = "Composer", Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        composer.AddThemeConstantOverride("h_separation", 20);
        composer.AddThemeConstantOverride("v_separation", 20);
        var visual = composer.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        visual.AddThemeConstantOverride("separation", 10);
        graph = visual.Add(new EasingGraph { Name = "CurveGraph" });
        var legend = visual.Add(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center });
        legend.AddThemeConstantOverride("separation", 16);
        legend.Add(PlaygroundTheme.Label("In", 13, PlaygroundTheme.Amber));
        legend.Add(PlaygroundTheme.Label("Out", 13, PlaygroundTheme.Blue));
        legend.Add(PlaygroundTheme.Label("Result", 13, PlaygroundTheme.Mint));
        legend.Add(PlaygroundTheme.Label("Blend window", 13, PlaygroundTheme.Muted));
        var transport = visual.Add(new HBoxContainer());
        transport.AddThemeConstantOverride("separation", 12);
        play = transport.Add(new Button { Name = "Play", Text = "▶ Play", CustomMinimumSize = new(96, 64) });
        play.AddThemeColorOverride("font_color", PlaygroundTheme.Mint);
        play.Pressed += TogglePlayback;
        var motion = transport.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        preview = motion.Add(new MotionPreview { Name = "MotionPreview" });
        scrub = motion.Add(new HSlider { Name = "Progress", MinValue = 0, MaxValue = 1, Step = 0.001,
            CustomMinimumSize = new(140, 24), SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = "Scrub preview progress" });
        scrub.ValueChanged += value => { if (!syncing) Scrub((float)value); };
        var readouts = transport.Add(new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new(66, 0) });
        valueOut = readouts.Add(PlaygroundTheme.Label("0.000", 14, PlaygroundTheme.Mint));
        timeOut = readouts.Add(PlaygroundTheme.Label("0.00 s", 13, PlaygroundTheme.Muted));
        valueOut.HorizontalAlignment = timeOut.HorizontalAlignment = HorizontalAlignment.Right;

        var panel = composer.Add(new PanelContainer { Name = "CurveControls", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var controls = panel.Add(new VBoxContainer());
        controls.AddThemeConstantOverride("separation", 14);
        var choices = controls.Add(new HBoxContainer());
        choices.AddThemeConstantOverride("separation", 12);
        entry = Picker(choices, "In curve", "InCurve", PlaygroundTheme.Amber);
        exit = Picker(choices, "Out curve", "OutCurve", PlaygroundTheme.Blue);
        AddFamilies(entry, true);
        AddFamilies(exit, false);
        entry.ItemSelected += _ => RefreshFromControls();
        exit.ItemSelected += _ => RefreshFromControls();
        duration = Slider(controls, "Duration", "Duration", 0.2, 4, 0.1, out durationOut);
        var pacing = controls.Add(new HBoxContainer());
        pacing.AddThemeConstantOverride("separation", 16);
        skew = Slider(pacing, "Skew", "Skew", 0, 1, 0.01, out skewOut);
        blend = Slider(pacing, "Blend", "BlendWidth", 0, 1, 0.02, out blendOut);
        blendType = Picker(controls, "Blend type", "BlendType", PlaygroundTheme.Text);
        foreach (var method in new[] { "Makima · match velocity", "Hermite · match acceleration", "SmoothStep · crossfade", "Linear · crossfade" })
            blendType.AddItem(method);
        blendType.ItemSelected += _ => RefreshFromControls();
        var help = controls.Add(PlaygroundTheme.Label("Skew moves the join. Blend reshapes the window where two different curves meet.", 13, PlaygroundTheme.Muted));
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var recipePanel = page.Add(new PanelContainer());
        var recipe = recipePanel.Add(new VBoxContainer());
        recipe.AddThemeConstantOverride("separation", 10);
        var recipeBar = recipe.Add(new HBoxContainer());
        var caption = recipeBar.Add(PlaygroundTheme.Label("Source code recipe", 14, PlaygroundTheme.Muted));
        caption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        language = recipeBar.Add(new OptionButton { Name = "RecipeLanguage" });
        language.AddItem("C#"); language.AddItem("GDScript");
        language.ItemSelected += _ => UpdateRecipe();
        var copy = recipeBar.Add(new Button { Name = "Copy", Text = "Copy" });
        copy.Pressed += () => { DisplayServer.ClipboardSet(Recipe); copyStatus.Text = "Copied to clipboard"; };
        code = recipe.Add(new TextEdit { Name = "Recipe", Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary,
            ScrollFitContentHeight = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, HighlightCurrentLine = false });
        code.AddThemeFontSizeOverride("font_size", 14);
        code.AddThemeFontOverride("font", GD.Load<FontFile>("res://Fonts/JetBrainsMono-Regular.ttf"));
        var syntax = new CodeHighlighter { NumberColor = PlaygroundTheme.Amber, SymbolColor = PlaygroundTheme.Muted,
            FunctionColor = PlaygroundTheme.Mint, MemberVariableColor = PlaygroundTheme.Text };
        foreach (var keyword in new[] { "var", "new", "with" }) syntax.AddKeywordColor(keyword, PlaygroundTheme.Blue);
        foreach (var type in new[] { "Tweens", "In", "Out", "InOut", "BlendType", "Position2DX" }) syntax.AddKeywordColor(type, PlaygroundTheme.Mint);
        code.SyntaxHighlighter = syntax;
        copyStatus = recipe.Add(PlaygroundTheme.Label("", 12, PlaygroundTheme.Muted));
        page.Add(PlaygroundTheme.Label("30 × 30 curves · Powered by tweens.gd + Godot / 2dog", 13, PlaygroundTheme.Muted));
        Resized += FitLayout;
        FitLayout();
        ApplySettings(new());
    }

    private static OptionButton Picker(Node parent, string caption, string name, Color color)
    {
        var field = parent.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        field.Add(PlaygroundTheme.Label(caption, 14, color));
        var picker = field.Add(new OptionButton { Name = name, FitToLongestItem = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new(128, 42) });
        picker.AddThemeColorOverride("font_color", color);
        return picker;
    }

    private HSlider Slider(Node parent, string caption, string name, double min, double max, double step, out Label output)
    {
        var field = parent.Add(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var row = field.Add(new HBoxContainer());
        var label = row.Add(PlaygroundTheme.Label(caption, 14));
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        output = row.Add(PlaygroundTheme.Label("", 14, PlaygroundTheme.Muted));
        var slider = field.Add(new HSlider { Name = name, MinValue = min, MaxValue = max, Step = step,
            CustomMinimumSize = new(110, 24), SizeFlagsHorizontal = SizeFlags.ExpandFill });
        slider.ValueChanged += _ => RefreshFromControls();
        return slider;
    }

    private static void AddFamilies(OptionButton picker, bool isEntry)
    {
        picker.AddItem(isEntry ? "None (Out only)" : "None (In only)", 0);
        foreach (var variants in new[] { false, true })
        {
            picker.AddSeparator();
            foreach (var item in EasingSettings.Families.Select((family, id) => (family, id))
                .Where(item => item.id != 0 && char.IsDigit(item.family.Name[^1]) == variants)
                .OrderBy(item => item.family.Name, StringComparer.Ordinal))
                picker.AddItem(item.family.Name, item.id);
        }
    }

    private void FitLayout() => composer.Columns = Size.X < 860 ? 1 : 2;

    public void ApplySettings(EasingSettings settings)
    {
        Stop();
        syncing = true;
        entry.Select(entry.GetItemIndex(EasingSettings.Families.ToList().IndexOf(settings.Entry)));
        exit.Select(exit.GetItemIndex(EasingSettings.Families.ToList().IndexOf(settings.Exit)));
        duration.Value = settings.Duration; skew.Value = settings.Skew; blend.Value = settings.Blend;
        blendType.Select((int)settings.BlendType);
        syncing = false;
        RefreshFromControls();
    }

    private void RefreshFromControls()
    {
        if (syncing) return;
        Stop();
        Settings = new EasingSettings
        {
            Entry = EasingSettings.Families[entry.GetSelectedId()], Exit = EasingSettings.Families[exit.GetSelectedId()],
            Duration = duration.Value, Skew = skew.Value, Blend = blend.Value, BlendType = (BlendType)blendType.Selected,
        };
        durationOut.Text = Format(Settings.Duration, "0.0") + " s";
        skewOut.Text = Format(Settings.Skew, "0.00");
        blendOut.Text = Format(Settings.Blend * 100, "0") + "%";
        skew.Editable = Settings.HasBothLegs;
        blend.Editable = Settings.CanBlend;
        blendType.Disabled = !Settings.CanBlend;
        skew.TooltipText = Settings.HasBothLegs ? "In/Out split along the timeline" : "Skew needs both an In and an Out curve";
        blend.TooltipText = blendType.TooltipText = Settings.CanBlend ? "Blend two different curves around the split" : "Blend needs different In/Out curves and a skew between 0 and 1";
        foreach (var field in new Control[] { skew, blend, blendType })
            field.GetParent<Control>().Modulate = Colors.White with { A = (field == skew ? Settings.HasBothLegs : Settings.CanBlend) ? 1 : 0.45f };
        graph.Refresh(Settings);
        preview.Settings = Settings; preview.Reach = graph.Reach;
        UpdateRecipe();
        DrawProgress(Progress, Settings.Evaluate(Progress));
    }

    public void Scrub(float progress)
    {
        Stop();
        progress = Math.Clamp(progress, 0, 1);
        DrawProgress(progress, Settings.Evaluate(progress));
    }

    public void TogglePlayback()
    {
        if (IsPlaying) { Stop(); return; }
        DrawProgress(0, Settings.Evaluate(0));
        Playback = this.TweenFloat(1, Settings.Duration, options =>
        {
            options.From = 0;
            options.Ease = Settings.Ease;
            options.Skew = Settings.Skew;
            options.BlendType = Settings.BlendType;
            options.Blend = Settings.Blend;
            options.OnUpdate = (handle, value) => DrawProgress(handle.Progress, value);
            options.OnEnd = handle =>
            {
                if (ReferenceEquals(Playback, handle)) { Playback = null; play.Text = "▶ Play"; }
            };
        });
        play.Text = "■ Stop";
    }

    public void Stop()
    {
        var handle = Playback;
        Playback = null;
        handle?.Cancel();
        if (play is not null) play.Text = "▶ Play";
    }

    public void Reset() { Scrub(0); ApplySettings(new()); }
    public override void _ExitTree() => Stop();

    private void DrawProgress(float progress, float weight)
    {
        Progress = progress; Weight = weight;
        syncing = true; scrub.Value = progress; syncing = false;
        valueOut.Text = Format(weight, "0.000");
        timeOut.Text = Format(progress * Settings.Duration, "0.00") + " s";
        graph.Progress = progress; graph.Weight = weight; graph.QueueRedraw();
        preview.Weight = weight; preview.QueueRedraw();
    }

    private void UpdateRecipe()
    {
        code.Text = Recipe;
        copyStatus.Text = "";
    }

    private static string Format(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
