# tweens.gd for C#

Beta C# support for tweening in GodotSharp and 2dog. APIs may change during beta.
Reuse tween definitions, control independent playback handles, and compose
animations with `async`/`await`.

Targets .NET 10 and GodotSharp 4.7.2. Your application supplies a matching Godot
engine; the package depends only on GodotSharp. Other engine versions and
trimmed/AOT/web exports have not been validated.

The recommended installation is the unified addon from the
[Godot Asset Store](https://store.godotengine.org/asset/outfox/tweens/).
You can also download the addon ZIP from
[GitHub releases](https://github.com/outfox/tweens.gd/releases).
Unpack it into your project root, keeping the entire `addons/tweens_gd/`
directory. It includes C# sources and prepared definitions alongside GDScript.

For C#, you can instead install this NuGet package from your game's project directory:

```powershell
dotnet add package tweens.gd
```

```csharp
using Godot;
using tweens.gd;

// Call from _Ready or later, on Godot's main thread.
var movement = sprite.Tween(new Tweens.Position2D
{
    To = new Vector2(400, 180),
    Duration = 0.6,
    Ease = EaseType.CubicOut,
});

await movement;
GD.Print("Movement ended");
```

The first tween installs a runner automatically. No autoload is required.
Definitions are readonly record structs in the root `Tweens` namespace. Their
constructors take `(to, duration, ease, delay)`, all optional. Store a
definition in a readonly field and use `definition with { Delay = 0.2 }` to vary
a copy for one playback. Set `By` instead of `To` for relative motion that
keeps other changes to the property. `TweenOptions` is an immutable value too; mutable
convenience configurators use `TweenOptionsBuilder`. Playback supports pause/resume,
cancellation, delays, loops, ping-pong, easing, and node lifetime handling.
Create and control tweens on Godot's main thread.

## Documentation

The unified addon lives in `addons/tweens_gd/`. This NuGet package contains only
the compiled C# implementation. If you also install the addon for GDScript, add
`<Compile Remove="addons/tweens_gd/csharp/**/*.cs" />` to an `ItemGroup` in your
game's project file to avoid compiling duplicate types.
Guides and the API reference live at [tweens.gd/csharp](https://tweens.gd/csharp/installation/).
The site's source is in the [repository](https://github.com/outfox/tweens.gd/tree/main/docs).

MIT licensed. tweens.gd was inspired by Jeffrey Lanters' unity-tweens; its easing
functions still follow that implementation, so the package includes the unity-tweens
MIT notice alongside its own license.
