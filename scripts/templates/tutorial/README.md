# tweens.gd tutorial

The complete five-step tutorial, with C# and GDScript lessons and the tweens.gd
addon included. No separate addon download, NuGet library installation, or 2dog
tooling is needed.

## Open and run

1. Install Godot **.NET 4.7.2** and the **.NET 8 SDK or later**.
2. Extract this entire ZIP into a new folder.
3. Import `project.godot` in Godot's project manager and open it. Let the editor
   finish importing the included assets and native extension.
4. Build the C# project, then press **F5** to run the tutorial. Choose C# or
   GDScript in the header.

The first C# build restores the Godot SDK from NuGet, so it needs internet access.
Both tutorial modes require the .NET edition of Godot because the shared UI uses
C#. The bundled addon itself also works in GDScript-only projects.

## Explore the lessons

`Lessons/` contains a C# scene and a GDScript scene for each step. Open a lesson
scene and press **F6** to run it on its own. `Pages/`, `Shell/`, and `Themes/`
provide the tutorial UI; `Art/` and `Fonts/` provide its assets.

Keep `addons/tweens_gd/` intact: it contains both APIs, generated definitions,
prebuilt native libraries, documentation, and licenses. Everything the tutorial
needs is inside this project; its build does not reference a repository checkout.

Documentation: https://tweens.gd/tutorial/

## License

MIT; see `LICENSE` and `addons/tweens_gd/THIRD-PARTY-NOTICES.md`. Font licenses are
included in `Fonts/Figtree-OFL.txt` and `Fonts/JetBrainsMono-OFL.txt`.
