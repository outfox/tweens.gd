// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
namespace testbed;

public partial class ShowreelPage : GalleryPage
{
    public override string Heading => "Showreel";
    public override string Description => "Keyframe definitions on one loop: camera moves, title cards, punches and flying 3D objects.";
    protected override GalleryEffect[] CreateEffects() => [new Showreel()];
}
