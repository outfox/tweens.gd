// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using Godot;
namespace testbed;

public sealed partial class KeyframeFlight : GalleryEffect
{
    private readonly Node2D[] flyers = new Node2D[3];
    public override string Title => "Shared flight";
    public override string Caption => "Sparse position, scale, rotation and color curves. Each flyer captures its own start.";
    protected override void Build()
    {
        var view = View();
        for (var i = 0; i < flyers.Length; i++)
        {
            var y = (i - 1) * 65;
            Line(view, [new(-190, y), new(190, y)], Palette.Outline);
            flyers[i] = view.Add(new Node2D { Position = new Vector2(-160 + i * 45, y) });
            flyers[i].Add(new Polygon2D { Polygon = [new(-15, -12), new(19, 0), new(-15, 12), new(-8, 0)], Color = Colors.White, Antialiased = true });
            Blob(flyers[i], 3, 3, Palette.Background);
        }
    }
    public override Godot.Collections.Dictionary SceneTargets => new() { ["flyers"] = new Godot.Collections.Array<Node2D>(flyers) };
    protected override void Animate() => Sequence = Run(AnimateAsync());
}
