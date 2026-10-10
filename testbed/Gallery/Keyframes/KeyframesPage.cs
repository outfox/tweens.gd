// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
namespace testbed;

public partial class KeyframesPage : GalleryPage
{
    public override string Heading => "Reusable keyframes";
    public override string Description => "One sparse definition, three independent captured starts.";
    protected override GalleryEffect[] CreateEffects() => [new KeyframeFlight()];
}
