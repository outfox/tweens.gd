# Easing playground

The website's easing composer as a native Godot Control scene, hosted by 2dog.
The app references the repository's C# tweens.gd project directly. Both the
graph and scrubbing use `Easing.Evaluate`; Play runs a real `TweenFloat` with
the selected easing, skew, blend width, and blend method.

Choose independent In and Out curves (including the 10–50% variants), or None
for a single leg. Blend controls become available when two different curves
meet away from the timeline endpoints. The graph and motion lane automatically
fit overshoot. Editing settings, scrubbing, Stop, Reset, and leaving the scene
cancel active playback. Play starts a fresh pass; completion leaves the preview
at its endpoint.

The layout stacks on narrow windows and scrolls vertically. The recipe can be
switched between C# and GDScript, selected, or copied to the clipboard.

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
