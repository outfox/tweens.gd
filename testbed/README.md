# tweens.gd testbed

Nine pages and 33 examples run against either the beta C# library or the beta
GDScript addon. The global language selector changes the running implementation
and the source shown beside it.

```powershell
dotnet run --project testbed/testbed.2dog
dotnet run --project testbed/testbed.2dog -- --gallery-language gdscript
```

## Compare implementations

- Choose **C# · Beta** or **GDScript · Beta** in the header. Switching rebuilds
  the current page with fresh initial values and ends the previous playback.
- The selected page, open example and leg duration survive a language change.
  Navigation and Restart page keep the selected language.
- Click **View C#** or **View GDScript** to read the animation that runs. Opening
  source does not restart playback. Copy file copies the complete selected file.
- Scene construction is shared C# code, labeled **Shared scene · C#** in GDScript
  mode. This keeps geometry, materials, resources and initial values identical.
  Animation, sequencing, callbacks and button interactions use the selected library.

This gallery is a .NET host in either mode. The GDScript addon itself needs no .NET;
its engine is the addon's GDExtension, so build the library for your platform first
(see `../gdextension/README.md`). `../testbed-gdscript/` is the standalone non-.NET
addon/export test project.
Building the gallery stages the canonical `../addons/tweens_gd/` into its ignored
`addons/` directory. There is no separately maintained copy of the addon.

## Easing composer

The Easing page lets you select independent In and Out curves and tune skew while
the preview runs in the selected language. The global leg-duration slider controls
its duration. In occupies 0–0.5 and Out 0.5–1; matching families reproduce the
conventional InOut curve exactly. Curves and skew survive duration changes, restarts, navigation and
language changes. Amber and blue show the original halves; mint shows the result.
Choose Hermite, SmoothStep, or Linear and adjust the join width. The shaded
region marks the join window after skew has warped time.
Choose None on either side to inspect a single curve.

## Sources

Each example has a C# scene file, a C# `.Animation.cs` file, and a GDScript file in
`Gallery/GDScript/`. `SceneTargets` explicitly supplies native scene objects to
the GDScript animation. No reflection or C# tween calls implement GDScript motion.

Animation sources are embedded in the build. GDScript mode compiles and runs the
embedded animation text displayed by the viewer. Each GDScript example includes
the animation utilities it uses (`cycle`, `options`, `wait`, and `shake`), intentionally
duplicated so their behavior is visible in the same file. The shared lifecycle and
scene bindings remain in `Gallery/GDScript/GalleryAnimation.gd`, available as
**Gallery host · GDScript** in the source viewer's file menu. New examples need both
implementations and should include any animation utilities they use.

## Validation and known differences

```powershell
dotnet test testbed/testbed.tests/testbed.tests.csproj -c Release
dotnet test testbed/testbed.tests/testbed.tests.csproj -c Release -p:RenderingTests=true
```

Run these suites separately: they build different tests into the same output.
Headless comparison tests sample both implementations at the same fixed times,
check finite completion and scene teardown, and exercise global switching,
rapid selections, source identity and button clicks. Rendering tests run all
nine pages in both languages, including the four shader examples.

The easing race exposes a numeric difference at Expo/Elastic endpoints: C# uses
single-precision progress for easing; GDScript uses double precision. A value
just below an endpoint can round to that endpoint in C# and take a different
formula branch. The observed difference on this 240-pixel track is about
0.1171875 pixels. Comparison tests bound this specific case separately; they
do not change either library's easing behavior. Confetti trajectories are random
in both languages and are excluded from deterministic trajectory comparisons.

## Capture a page

Page and example indices are zero-based. Shaders need a real renderer.

```powershell
dotnet run --project testbed/testbed.2dog -- --gallery-language gdscript --gallery-page 7 --gallery-source 0 --snapshot artifacts/gdscript-shader.png --rendering-method gl_compatibility
dotnet run --project testbed/testbed.2dog -- --gallery-language csharp --gallery-snapshots artifacts/gallery-csharp --rendering-method gl_compatibility --fixed-fps 60
```
