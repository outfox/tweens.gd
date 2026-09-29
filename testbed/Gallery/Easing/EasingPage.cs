// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
namespace testbed;

public partial class EasingPage : GalleryPage
{
    public EasingSelection Selection { get; set; } = EasingSelection.Default;
    public override string Heading => "Easing";
    public override string Description => "Compose In and Out curves. Duration is above; skew reshapes the blend below.";
    protected override GalleryEffect[] CreateEffects() => [new EasingComposer { InitialSelection = Selection }];
}
