---
title: Advanced development
description: Combine installation methods, work on tweens.gd itself, or import the addon headlessly.
tableOfContents: true
---

Customize how tweens.gd fits into your project, or work on the library itself.

## Using NuGet together with the GDScript addon

To use both languages while keeping NuGet for C#, exclude the addon's C# sources
in your game's project file to avoid duplicate types:

```xml
<ItemGroup>
  <Compile Remove="addons/tweens_gd/csharp/**/*.cs" />
</ItemGroup>
```

Alternatively, omit the addon's `csharp/` directory.

## Working on the library itself

To build the library together with your game, clone the repository and add a
project reference instead of installing from [Godot Store or NuGet](/csharp/installation/).
Adjust both paths:

```powershell
dotnet add path/to/YourGame.csproj reference path/to/tweens.gd/csharp/tweens.gd.csproj
```

If you also keep the addon for GDScript, [exclude its C# sources](#using-nuget-together-with-the-gdscript-addon).

## Importing without the editor window

To run a fresh checkout headlessly, first run `godot --headless --editor --import`
to register the extension and populate the script-class cache. Godot 4.7.2 can
crash on exit from this first import after it has registered the extension;
later runs start normally.
