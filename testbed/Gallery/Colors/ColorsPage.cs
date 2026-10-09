// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

namespace testbed;

public partial class ColorsPage : GalleryPage
{
    public override string Heading => "Color interpolation";
    public override string Description => "Compare interpolation spaces and alpha handling with the same progress.";
    protected override GalleryEffect[] CreateEffects() => [new ColorInterpolation()];
}
