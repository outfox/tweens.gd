# Structured definition generation

The property adapters in `addons/tweens_gd/csharp/Tweens/` also serve as the mutable builders for
convenience methods. The [Roslyn incremental generator](../../csharp.generators/StructuredDefinitionGenerator.cs)
discovers those adapters and the public configuration properties in
`TweenOptionsBuilder` using C# symbols, then produces the readonly record structs
in the `Tweens` namespace. Each gets a constructor taking `to` and then
`Duration`, `Ease` and `Delay` when the builder declares them, all optional.
Shader and custom-property definitions use explicit templates in the same
generator, and their constructors lead with the uniform name or property
operations.

Generation runs automatically during builds and in the IDE. After adding an
adapter or changing configuration properties, build from the repository root:

```sh
dotnet build csharp/tweens.gd.csproj
```

NuGet consumers receive compiled definitions. The unified Godot addon instead
ships ordinary C# files in `addons/tweens_gd/csharp/Generated/`, so addon consumers
need no tweens.gd analyzer or package reference. Regenerate and commit these files
after changing adapters or options:

```powershell
./scripts/Generate-CSharpAddon.ps1
```

`./scripts/Generate-CSharpAddon.ps1 -Check` verifies freshness without writing
the addon. It rebuilds with `EmitCompilerGeneratedFiles` into a fresh temporary
directory and selects only `StructuredDefinitionGenerator` output. Godot's own
generated glue is deliberately excluded; each consumer's Godot SDK generates
that for its assembly. `Pack-Addon.ps1` runs this check before packaging.
The generator remains a private build dependency of the NuGet project. That
project excludes the prepared files and generates its own definitions from
the same addon sources. See
[the authoritative release guide](../../docs-internal/RELEASING.md).

Runtime binding state stays in the private per-playback class instance. A
structured definition creates that instance directly; a mutable class definition
creates it by snapshotting. Both use the same scheduler and property operations.
