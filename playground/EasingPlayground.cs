// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Globalization;
using System.Linq;
using Godot;
using tweens.gd;

namespace playground;

/// <summary>Binds the authored scene to curve selection, playback, and recipe generation.</summary>
[Tool]
public partial class EasingPlayground : Control
{
    [Export] public Theme GDScriptAccentOverrides { get; set; } = null!;
    [Export] public CodeHighlighter CSharpSyntax { get; set; } = null!;
    [Export] public CodeHighlighter GDScriptSyntax { get; set; } = null!;

    public EasingSettings Settings { get; private set; } = new();
    public TweenInstance? Playback { get; private set; }
    public float Progress { get; private set; }
    public float Weight { get; private set; }
    public bool IsPlaying => Playback is { IsTerminal: false };
    public bool UsesGDScript { get; private set; }
    public string Recipe => Settings.Recipe(UsesGDScript);

    // Scene-unique names allow the layout to be rearranged without changing node paths here.
    private OptionButton inCurvePicker = null!;
    private OptionButton outCurvePicker = null!;
    private OptionButton blendTypePicker = null!;
    private Button csharpModeButton = null!;
    private Button gdscriptModeButton = null!;
    private Theme csharpTheme = null!;
    private Theme gdscriptTheme = null!;

    private HSlider durationSlider = null!;
    private HSlider skewSlider = null!;
    private HSlider blendWidthSlider = null!;
    private HSlider progressSlider = null!;

    private Label durationValue = null!;
    private Label skewValue = null!;
    private Label blendValue = null!;
    private Label weightValue = null!;
    private Label elapsedTimeValue = null!;
    private Label copyStatus = null!;

    private Button playButton = null!;
    private TextEdit recipeEditor = null!;
    private EasingGraph curveGraph = null!;
    private MotionPreview motionPreview = null!;
    private GridContainer composer = null!;
    private bool syncingControls;

    public override void _Ready()
    {
        BindSceneNodes();

        // Preview the same responsive layout in the editor without interactive playback.
        Resized += FitLayout;
        FitLayout();
        if (Engine.IsEditorHint())
            return;

        PopulateCurvePicker(inCurvePicker, "None (Out only)");
        PopulateCurvePicker(outCurvePicker, "None (In only)");
        PrepareLanguageThemes();
        ConnectSignals();
        SetLanguage(gdscript: false);
        ApplySettings(new EasingSettings());
    }

    private void BindSceneNodes()
    {
        inCurvePicker = GetNode<OptionButton>("%InCurve");
        outCurvePicker = GetNode<OptionButton>("%OutCurve");
        blendTypePicker = GetNode<OptionButton>("%BlendType");
        csharpModeButton = GetNode<Button>("%CSharpMode");
        gdscriptModeButton = GetNode<Button>("%GDScriptMode");

        durationSlider = GetNode<HSlider>("%Duration");
        skewSlider = GetNode<HSlider>("%Skew");
        blendWidthSlider = GetNode<HSlider>("%BlendWidth");
        progressSlider = GetNode<HSlider>("%Progress");

        durationValue = GetNode<Label>("%DurationValue");
        skewValue = GetNode<Label>("%SkewValue");
        blendValue = GetNode<Label>("%BlendValue");
        weightValue = GetNode<Label>("%WeightValue");
        elapsedTimeValue = GetNode<Label>("%ElapsedTimeValue");
        copyStatus = GetNode<Label>("%CopyStatus");

        playButton = GetNode<Button>("%Play");
        recipeEditor = GetNode<TextEdit>("%Recipe");
        curveGraph = GetNode<EasingGraph>("%CurveGraph");
        motionPreview = GetNode<MotionPreview>("%MotionPreview");
        composer = GetNode<GridContainer>("%Composer");
    }

    private void ConnectSignals()
    {
        GetNode<Button>("%Reset").Pressed += Reset;
        GetNode<Button>("%Copy").Pressed += CopyRecipe;
        playButton.Pressed += TogglePlayback;

        inCurvePicker.ItemSelected += OnCurveSelected;
        outCurvePicker.ItemSelected += OnCurveSelected;
        blendTypePicker.ItemSelected += OnCurveSelected;
        csharpModeButton.Pressed += () => SetLanguage(gdscript: false);
        gdscriptModeButton.Pressed += () => SetLanguage(gdscript: true);

        durationSlider.ValueChanged += OnSettingChanged;
        skewSlider.ValueChanged += OnSettingChanged;
        blendWidthSlider.ValueChanged += OnSettingChanged;
        progressSlider.ValueChanged += OnProgressChanged;
    }

    private static void PopulateCurvePicker(OptionButton picker, string missingLegCaption)
    {
        picker.Clear();
        picker.AddItem(missingLegCaption, 0);

        // IDs follow the library catalog, independently of the alphabetical display order.
        var catalog = EasingSettings.Families.Select((family, id) => (Family: family, Id: id));
        foreach (var numberedVariants in new[] { false, true })
        {
            picker.AddSeparator();
            var group = catalog
                .Where(item => item.Id != 0 && char.IsDigit(item.Family.Name[^1]) == numberedVariants)
                .OrderBy(item => item.Family.Name, StringComparer.Ordinal);

            foreach (var item in group)
                picker.AddItem(item.Family.Name, item.Id);
        }
    }

    private void FitLayout() => composer.Columns = Size.X < 860 ? 1 : 2;
    private void OnCurveSelected(long index) => RefreshFromControls();
    private void OnSettingChanged(double value) => RefreshFromControls();

    private void PrepareLanguageThemes()
    {
        // Merge a small accent resource into the authored theme, preserving neutral styles.
        csharpTheme = Theme;
        gdscriptTheme = (Theme)csharpTheme.Duplicate();
        gdscriptTheme.MergeWith(GDScriptAccentOverrides);
    }

    public void SetLanguage(bool gdscript)
    {
        UsesGDScript = gdscript;
        csharpModeButton.SetPressedNoSignal(!gdscript);
        gdscriptModeButton.SetPressedNoSignal(gdscript);
        Theme = gdscript ? gdscriptTheme : csharpTheme;

        recipeEditor.SyntaxHighlighter = gdscript ? GDScriptSyntax : CSharpSyntax;

        UpdateRecipe();
        curveGraph.QueueRedraw();
        motionPreview.QueueRedraw();
    }

    private void OnProgressChanged(double value)
    {
        if (!syncingControls)
            Scrub((float)value);
    }

    public void ApplySettings(EasingSettings settings)
    {
        Stop();
        syncingControls = true;

        var catalog = EasingSettings.Families.ToList();
        inCurvePicker.Select(inCurvePicker.GetItemIndex(catalog.IndexOf(settings.Entry)));
        outCurvePicker.Select(outCurvePicker.GetItemIndex(catalog.IndexOf(settings.Exit)));
        durationSlider.Value = settings.Duration;
        skewSlider.Value = settings.Skew;
        // A positive 1..101 range gives a logarithmic thumb while preserving 0..100% blend.
        blendWidthSlider.Value = 1 + 100 * settings.Blend;
        blendTypePicker.Select((int)settings.BlendType);

        syncingControls = false;
        RefreshFromControls();
    }

    private void RefreshFromControls()
    {
        if (syncingControls)
            return;

        Stop();
        Settings = new EasingSettings
        {
            Entry = EasingSettings.Families[inCurvePicker.GetSelectedId()],
            Exit = EasingSettings.Families[outCurvePicker.GetSelectedId()],
            Duration = durationSlider.Value,
            Skew = skewSlider.Value,
            Blend = Math.Round((blendWidthSlider.Value - 1) / 100, 2),
            BlendType = (BlendType)blendTypePicker.Selected,
        };

        UpdateSettingReadouts();
        UpdateControlAvailability();
        curveGraph.Refresh(Settings);
        motionPreview.Refresh(Settings, curveGraph.Reach);
        UpdateRecipe();
        DrawProgress(Progress, Settings.Evaluate(Progress));
    }

    private void UpdateSettingReadouts()
    {
        durationValue.Text = Format(Settings.Duration, "0.0") + " s";
        skewValue.Text = Format(Settings.Skew, "0.00");
        blendValue.Text = Format(Settings.Blend * 100, "0.#") + "%";
    }

    private void UpdateControlAvailability()
    {
        skewSlider.Editable = Settings.HasBothLegs;
        blendWidthSlider.Editable = Settings.CanBlend;
        blendTypePicker.Disabled = !Settings.CanBlend;

        skewSlider.TooltipText = Settings.HasBothLegs
            ? "In/Out split along the timeline"
            : "Skew needs both an In and an Out curve";
        var blendHint = Settings.CanBlend
            ? "Blend two different curves around the split"
            : "Blend needs different In/Out curves and a skew between 0 and 1";
        blendWidthSlider.TooltipText = blendHint;
        blendTypePicker.TooltipText = blendHint;

        SetFieldEnabled(skewSlider, Settings.HasBothLegs);
        SetFieldEnabled(blendWidthSlider, Settings.CanBlend);
        SetFieldEnabled(blendTypePicker, Settings.CanBlend);
    }

    private static void SetFieldEnabled(Control input, bool enabled)
    {
        input.GetParent<Control>().Modulate = Colors.White with { A = enabled ? 1 : 0.45f };
    }

    public void Scrub(float progress)
    {
        Stop();
        progress = Math.Clamp(progress, 0, 1);
        DrawProgress(progress, Settings.Evaluate(progress));
    }

    public void TogglePlayback()
    {
        if (IsPlaying)
        {
            Stop();
            return;
        }

        DrawProgress(0, Settings.Evaluate(0));
        Playback = this.TweenFloat(1, Settings.Duration, options =>
        {
            options.From = 0;
            options.Ease = Settings.Ease;
            options.Skew = Settings.Skew;
            options.BlendType = Settings.BlendType;
            options.Blend = Settings.Blend;
            options.OnUpdate = (handle, value) => DrawProgress(handle.Progress, value);
            options.OnEnd = OnPlaybackEnded;
        });
        playButton.Text = "■ Stop";
    }

    private void OnPlaybackEnded(TweenInstance<Node, float> handle)
    {
        if (!ReferenceEquals(Playback, handle))
            return;

        Playback = null;
        playButton.Text = "▶ Play";
    }

    public void Stop()
    {
        var handle = Playback;
        Playback = null;
        handle?.Cancel();

        if (playButton is not null)
            playButton.Text = "▶ Play";
    }

    public void Reset()
    {
        Scrub(0);
        ApplySettings(new EasingSettings());
    }

    public override void _ExitTree() => Stop();

    private void DrawProgress(float progress, float weight)
    {
        Progress = progress;
        Weight = weight;

        syncingControls = true;
        progressSlider.Value = progress;
        syncingControls = false;

        weightValue.Text = Format(weight, "0.000");
        elapsedTimeValue.Text = Format(progress * Settings.Duration, "0.00") + " s";
        curveGraph.SetPreview(progress, weight);
        motionPreview.SetWeight(weight);
    }

    private void UpdateRecipe()
    {
        recipeEditor.Text = Recipe;
        copyStatus.Text = "";
    }

    private void CopyRecipe()
    {
        DisplayServer.ClipboardSet(Recipe);
        copyStatus.Text = "Copied to clipboard";
    }

    private static string Format(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
