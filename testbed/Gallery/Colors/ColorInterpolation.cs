// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
namespace testbed;

public sealed partial class ColorInterpolation : GalleryEffect
{
    private readonly ColorRect[] swatches = new ColorRect[24];
    private HSlider progress = null!;
    private CheckButton automatic = null!;
    private Button midpoint = null!;
    private static readonly Color[] Starts = [Colors.Red, Colors.Red, new(1, 0, 0, 0), Colors.Red];
    private static readonly Color[] Ends = [Colors.Blue, new(0, 0, 0, 0), Colors.Blue, Colors.Blue];

    public override string Title => "RGB, OKLab & alpha";
    public override string Caption => "Scrub or press 50% to pause. The last column changes an opaque tint and fades its alpha separately.";

    protected override void Build()
    {
        var layout = Fill(new VBoxContainer(), 8);
        layout.AddThemeConstantOverride("separation", 4);
        var controls = layout.Add(new HBoxContainer());
        automatic = controls.Add(new CheckButton { Text = "Animate", ButtonPressed = true });
        midpoint = controls.Add(new Button { Text = "50%", TooltipText = "Pause at the exact midpoint" });
        automatic.AddThemeFontSizeOverride("font_size", 12);
        midpoint.AddThemeFontSizeOverride("font_size", 12);
        progress = controls.Add(new HSlider
        {
            MinValue = 0, MaxValue = 1, Step = 0.001, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Shared linear progress; scrubbing pauses playback",
        });
        var grid = layout.Add(new GridContainer
        {
            Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        });
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 3);
        foreach (var title in new[] { "Space / alpha", "Red → blue", "Red → clear black", "Clear red → blue", "Tint + fade" })
        {
            var label = grid.Add(GalleryTheme.Label(title, 11, Palette.Muted));
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(40, 0);
        }
        var checker = Checker(16, 8, new Color("aeb6c2"), new Color("505a6a"));
        string[] names = ["sRGB · straight", "sRGB · premul", "Linear RGB · straight", "Linear RGB · premul", "OKLab · straight", "OKLab · premul"];
        for (var row = 0; row < names.Length; row++)
        {
            grid.Add(GalleryTheme.Label(names[row], 12));
            for (var column = 0; column < Starts.Length; column++)
            {
                var cell = grid.Add(new Control
                {
                    CustomMinimumSize = new Vector2(40, 16),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                });
                var background = cell.Add(new TextureRect
                {
                    Texture = checker, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.Tile, MouseFilter = Control.MouseFilterEnum.Ignore,
                });
                background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                var swatch = cell.Add(new ColorRect { Color = Colors.White, Modulate = Starts[column] });
                swatch.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                swatches[row * Starts.Length + column] = swatch;
            }
        }
        var note = layout.Add(GalleryTheme.Label("sRGB inputs → chosen interpolation space → sRGB output. Checkerboard shows transparency; hover for RGBA.", 12, Palette.Muted));
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        void FitNote() => note.Visible = Stage.Size.Y >= 300;
        Stage.Resized += FitNote;
        FitNote();
    }

    public override Godot.Collections.Dictionary SceneTargets => new()
    {
        ["swatches"] = new Godot.Collections.Array<Node>(swatches),
        ["progress"] = progress, ["automatic"] = automatic, ["midpoint"] = midpoint,
        ["starts"] = new Godot.Collections.Array<Color>(Starts),
        ["ends"] = new Godot.Collections.Array<Color>(Ends),
    };

    protected override void Animate() => Sequence = Run(AnimateAsync());
}
