# Easing playground

The website's easing composer as a native Godot Control scene, hosted by 2dog.
The app references the repository's C# tweens.gd project directly. Both the
graph and scrubbing use `Easing.Evaluate`; Play runs a real `TweenFloat` with
the selected easing, skew, blend width, and blend method.

Choose independent In and Out curves (including the 10–50% variants), or None
for a single leg. Blend controls become available when two different curves
meet away from the timeline endpoints. The graph and motion lane automatically
fit overshoot. Blend defaults to 10% and uses a logarithmic slider that snaps to
whole percentages across the full 0–100% range. Vertical grid lines mark every 0.25 seconds.
Editing settings, scrubbing, Stop, Reset, and leaving the scene
cancel active playback. Play starts a fresh pass; completion leaves the preview
at its endpoint.

The desktop window is fixed at 1280 × 1024. The layout also stacks in narrow
browser viewports and scrolls vertically. The C# / GDScript mode switch changes
the recipe language and accents: blue for C#, green for GDScript. The recipe can
be selected or copied to the clipboard.

## Editing the layout

Open [main.tscn](main.tscn) in the Godot editor. The complete Control hierarchy,
labels, slider ranges, panels, recipe editor, and initial appearance are authored
in the scene. The background and scrolling page fill the window using anchors;
containers arrange the page sections and fields. The `Composer` grid switches
between two columns and one below 860 pixels, including in the editor.

Use `Main/Decorations` for artwork behind the page, or `GraphDecorations` inside
the graph panel for overlays. Both are anchored to fill their parent and ignore
mouse input. Decorative children can use anchors and offsets without changing
the page's container layout. Set their mouse filter to Ignore as well when they
should not intercept clicks.

[Themes/playground.tres](Themes/playground.tres) contains the shared styles,
fonts, label variations, and graph colors, with blue C# accents by default.
[Themes/gdscript.tres](Themes/gdscript.tres) overrides only the language accents;
the controller merges it into the base theme. Plotted curves keep the fixed
gallery colors in both modes. The recipe's syntax highlighter is a
scene subresource. The graph and motion preview have C# tool scripts that draw
the default Elastic curve directly in the editor after the project is built.

[EasingPlayground.cs](EasingPlayground.cs) binds scene-unique node names and
handles signals, settings, playback, and recipes. Keep the unique names on its
interactive controls when moving them within the scene. The library's curve
catalog is populated at runtime; the scene shows Elastic as its initial editor
preview. [EasingGraph.cs](EasingGraph.cs) and [MotionPreview.cs](MotionPreview.cs)
handle the dynamic drawing, while [EasingSettings.cs](EasingSettings.cs) contains
the catalog, easing evaluation, and source code recipes.

## Running and testing

Run from this directory:

```powershell
dotnet run --project playground.2dog
dotnet publish playground.web
dotnet test playground.tests
dotnet test playground.tests -c Release -p:RenderingTests=true
```

The browser bundle is written to `playground.web/AppBundle`. Serve that folder
over HTTP to open it in a browser. Publishing requires the `wasm-tools` workload.
The rendering tests run separately from the headless tests, since each process
hosts one Godot engine. To save rendering screenshots, set
`PLAYGROUND_SCREENSHOT_DIR` to an output directory before running them.

The bundled JetBrains Mono font is licensed under the SIL Open Font License;
see [Fonts/OFL.txt](Fonts/OFL.txt).
