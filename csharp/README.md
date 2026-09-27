# tweens.gd for C#

Beta C# support for tweening in GodotSharp and 2dog. APIs may change during beta.
Reuse tween definitions, control independent playback handles, and compose
animations with `async`/`await`.

Targets .NET 10 and GodotSharp 4.7.2. Your application supplies a matching Godot
engine; the package depends only on GodotSharp. Other engine versions and
trimmed/AOT/web exports have not been validated.

```xml
<PackageReference Include="tweens.gd" Version="0.1.0" />
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

var reason = await movement.End;
if (reason == Reason.Completed)
    GD.Print("Arrived");
```

The first tween installs a runner automatically. No autoload is required.
Definitions are readonly record structs in the root `Tweens` namespace. Store a
definition in a readonly field and use `definition with { Delay = 0.2 }` to vary
a copy for one playback. `TweenOptions` is an immutable value too; mutable
convenience configurators use `TweenOptionsBuilder`. Playback supports pause/resume,
cancellation, delays, loops, ping-pong, easing, and node lifetime handling.
Create and control tweens on Godot's main thread.

## Documentation

The beta pure GDScript addon lives separately in `addons/tweens_gd/` and is not included in this package.
Guides and the API reference live at [tweens.gd/csharp](https://tweens.gd/csharp/installation/).
The site's source is in the [repository](https://github.com/outfox/tweens.gd/tree/main/docs).

MIT licensed. tweens.gd was inspired by Jeffrey Lanters' unity-tweens; its easing
functions still follow that implementation, so the package includes the unity-tweens
MIT notice alongside its own license.
