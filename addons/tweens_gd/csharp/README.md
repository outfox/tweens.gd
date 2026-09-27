# tweens.gd for C#

Download the addon from the
[Godot Asset Store](https://store.godotengine.org/asset/outfox/tweens/) (recommended),
or download the addon ZIP from
[GitHub releases](https://github.com/outfox/tweens.gd/releases).
Unpack it into your Godot .NET project root, keeping the entire `addons/tweens_gd/` directory.
The project's normal C# build automatically includes these sources. No package
reference, analyzer installation, plugin activation or autoload is needed.
The validated target is Godot .NET 4.7.2 with .NET 10 (`net10.0`).

```csharp
using Godot;
using tweens.gd;

// From _Ready or later, with the target in the tree.
await sprite.Tween(new Tweens.Position2D(new Vector2(400, 180), 0.6)).End;
```

Use `using tweens.gd;` for extension methods and `Tweens.*` for reusable,
immutable definitions. GDScript has its own API and runtime; the two can coexist.
See the [C# guide](https://tweens.gd/csharp/quickstart/).

## Generated definitions

`Generated/` contains the ordinary C# output of our Roslyn generator, prepared
and committed by maintainers before release. Keep those files with the addon.
Godot's own source generators still run through your project's Godot.NET.Sdk;
their engine-specific output is deliberately not bundled here. The sources
declare their own imports and nullable context, independent of your settings.

## Optional NuGet installation

C# users can use the `tweens.gd` NuGet package instead. It contains
the compiled C# library and depends on GodotSharp, with no runtime dependency on
our generator. Install it from your game's project directory:

```powershell
dotnet add package tweens.gd
```

If you keep the addon for GDScript while using NuGet (or a project reference),
exclude the bundled C# sources in your game's `.csproj` to avoid duplicate types:

```xml
<ItemGroup>
  <Compile Remove="addons/tweens_gd/csharp/**/*.cs" />
</ItemGroup>
```

Alternatively, omit the `csharp/` directory when installing the addon. A
GDScript-only project can leave it in place and does not need .NET.
