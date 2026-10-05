// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Linq;
using Godot;
namespace testbed;

public readonly record struct EasingSelection(int InIndex, int OutIndex, double Skew, int BlendType = 0, double Blend = 0.2)
{
    public static EasingSelection Default => new(0, 10, 0.5);
}

public sealed partial class EasingComposer : GalleryEffect
{
    public EasingSelection InitialSelection { get; set; } = EasingSelection.Default;
    public EasingSelection Selection => new(entry.GetSelectedId(), exit.GetSelectedId(), skew.Value, blend.Selected, width.Value);
    private OptionButton entry = null!, exit = null!, blend = null!;
    private HSlider skew = null!, width = null!;
    private Label recipe = null!;
    private Line2D entryCurve = null!, exitCurve = null!, resultCurve = null!;
    private SubViewport previewView = null!;
    private Camera2D previewCamera = null!;
    private Polygon2D ball = null!, tracer = null!, region = null!;

    public override string Title => "In | Out";
    public override string Caption => "Amber: In half. Blue: Out half. Mint: result. Makima joins with continuous velocity. Width controls how much of each leg is reshaped.";

    protected override void Build()
    {
        var view = previewView = View();
        var container = (Control)view.GetParent();
        container.OffsetTop = 48;
        container.OffsetBottom = -100;
        previewCamera = view.GetChild<Camera2D>(0);
        view.SizeChanged += FitPreview;
        region = view.Add(new Polygon2D { Color = Palette.Mint with { A = 0.08f } });
        Line(view, [new(-200, -66), new(200, -66)], Palette.Outline, 1);
        Line(view, [new(-200, 66), new(200, 66)], Palette.Outline, 1);
        Line(view, [new(-200, 105), new(200, 105)], Palette.Outline, 1);
        entryCurve = Line(view, [], Palette.Amber with { A = 0.65f }, 1.5f);
        exitCurve = Line(view, [], Palette.Blue with { A = 0.65f }, 1.5f);
        resultCurve = Line(view, [], Palette.Mint, 3);
        tracer = Blob(view, 5, 5, Palette.Text, new(-200, 66));
        ball = Blob(view, 8, 8, Palette.Mint, new(-200, 105));

        var controls = Stage.Add(new HBoxContainer());
        controls.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide, margin: 10);
        controls.AddThemeConstantOverride("separation", 10);
        controls.AddChild(GalleryTheme.Label("In", 14, Palette.Amber));
        entry = controls.Add(new OptionButton { Name = "InCurve", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        controls.AddChild(GalleryTheme.Label("|", 16, Palette.Muted));
        controls.AddChild(GalleryTheme.Label("Out", 14, Palette.Blue));
        exit = controls.Add(new OptionButton { Name = "OutCurve", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        // Stable IDs keep selection and playback independent of the display order.
        var groups = Families.Select((family, id) => (family.Name, Id: id))
            .GroupBy(family => family.Id == 0 ? 0 : char.IsDigit(family.Name[^1]) ? 2 : 1)
            .OrderBy(group => group.Key);
        foreach (var group in groups)
        {
            if (group.Key != 0)
                foreach (var picker in new[] { entry, exit })
                {
                    picker.AddSeparator();
                    picker.SetItemId(picker.ItemCount - 1, -group.Key);
                }
            foreach (var (name, id) in group.OrderBy(family => family.Name, StringComparer.OrdinalIgnoreCase))
            { entry.AddItem(name, id); exit.AddItem(name, id); }
        }
        entry.Select(entry.GetItemIndex(InitialSelection.InIndex));
        exit.Select(exit.GetItemIndex(InitialSelection.OutIndex));

        var footer = Stage.Add(new VBoxContainer());
        footer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide, margin: 10);
        footer.OffsetTop = -96;
        var row = footer.Add(new HBoxContainer());
        row.AddChild(GalleryTheme.Label("Skew", 14, Palette.Muted));
        skew = row.Add(new HSlider { Name = "EasingSkew", MinValue = 0, MaxValue = 1, Step = 0.01, Value = InitialSelection.Skew,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(120, 24) });
        var join = footer.Add(new HBoxContainer());
        blend = join.Add(new OptionButton { Name = "EasingBlend" });
        foreach (var method in new[] { "Makima", "Hermite", "SmoothStep", "Linear" }) blend.AddItem(method);
        blend.Select(InitialSelection.BlendType);
        join.AddChild(GalleryTheme.Label("Width", 14, Palette.Muted));
        width = join.Add(new HSlider { Name = "EasingWidth", MinValue = 0, MaxValue = 1, Step = 0.02, Value = InitialSelection.Blend,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(120, 24) });
        recipe = footer.Add(GalleryTheme.Label("", 13, Palette.Mint));
        recipe.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void FitPreview()
    {
        if (resultCurve is null) return;
        float halfWidth = 256, halfHeight = 128;
        foreach (var curve in new[] { entryCurve, exitCurve, resultCurve })
        foreach (var point in curve.Points) halfHeight = Math.Max(halfHeight, Math.Abs(point.Y) + 20);
        foreach (var point in resultCurve.Points)
            halfWidth = Math.Max(halfWidth, Math.Abs(400 * ((66 - point.Y) / 132) - 200) + 24);
        previewCamera.Zoom = Vector2.One * Math.Min(previewView.Size.X / (2 * halfWidth), previewView.Size.Y / (2 * halfHeight));
    }

    public override Godot.Collections.Dictionary SceneTargets => new()
    {
        ["entry"] = entry, ["exit"] = exit, ["skew"] = skew, ["blend"] = blend, ["width"] = width, ["recipe"] = recipe,
        ["entryCurve"] = entryCurve, ["exitCurve"] = exitCurve, ["resultCurve"] = resultCurve,
        ["fit_preview"] = Callable.From(FitPreview),
        ["ball"] = ball, ["tracer"] = tracer, ["region"] = region,
    };

    protected override void Animate() => Sequence = Run(AnimateAsync());
}
