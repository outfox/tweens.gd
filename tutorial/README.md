# Tutorial

The website's five-step [tutorial](https://tweens.gd/tutorial/) as a Godot project: install, your first tween,
reusable definitions, syntax and sugar, and control and completion. Choose C# or GDScript in the header; every
step runs its lesson in the chosen language and shows the code that runs.

## Layout

- `Lessons/` holds what the tutorial teaches, one folder per step. Each lesson is a scene with a C# script and its
  GDScript twin, such as `Quickstart/ClickToMove.tscn` and `Quickstart/click_to_move.tscn`. Open one and press F6.
- `Pages/` holds the pages around the lessons: the overview with the ferret, and one page per step.
- `Shell/` holds the shared parts: the window, the stage that runs a lesson in its own viewport, code panels,
  prose, cards, and the language switch.
- `Themes/tutorial.tres` uses the website's colors with blue C# accents; `Themes/gdscript.tres` overrides the
  accents in green and is merged in, as in the playground.

Lessons follow the website's listings. Where a page has controls, they set the lesson script's exported
properties, as the Inspector would; code panels show those values in place. Panels read the lesson files embedded
in the build, so they always match what runs.

The build stages the repository's `addons/tweens_gd/` into the ignored `addons/` directory for GDScript, and
references `../csharp/tweens.gd.csproj` for C#. Build the GDExtension first (see `../gdextension/README.md`).

## Running and testing

```powershell
dotnet run --project tutorial.2dog
dotnet test tutorial.tests -c Release
dotnet test tutorial.tests -c Debug
dotnet test tutorial.tests -c Release -p:RenderingTests=true
```

Debug runs check the GDScript lessons for warnings. The rendering tests run separately, since each process hosts
one Godot engine; set `TUTORIAL_SCREENSHOT_DIR` to save screenshots of every page in both languages.

Figtree and JetBrains Mono are licensed under the SIL Open Font License; see `Fonts/`.
