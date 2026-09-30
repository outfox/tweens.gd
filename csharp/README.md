# tweens.gd for C#

Beta C# support for tweening in GodotSharp and 2dog. APIs may change during beta.
Reuse tween definitions, control independent playback handles, and compose
animations with `async`/`await`.

Targets .NET 8 and .NET 10 with GodotSharp 4.7.2. Your application supplies a matching Godot
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
var movement = sprite.Tween(new Tweens.Position2D((400, 180), 0.6, Out.Cubic));

await movement.End;
GD.Print("Movement ended");
```

The first tween installs a runner automatically. No autoload is required.
Definitions are readonly record structs in the root `Tweens` namespace. Their
constructors take `(to, duration, ease, delay)`; everything after `to` is optional,
and vectors and colors also take tuples such as `(400, 180)`. Store a
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

## Composable easing

```csharp
sprite.TweenPositionX(300, 1.2, options =>
{
    options.Ease = In.Sine | Out.Cubic;
    options.Skew = 1.5;
});
```

`InOut.Sine` is `In.Sine | Out.Sine` and exactly preserves `EaseType.SineInOut`.
Single legs run on their own; pairs use In on 0–0.5 and Out on 0.5–1, with a local
Makima join over 40–60% of progress after skew. `BlendType` selects Makima,
Hermite, SmoothStep, or Linear; `Blend` sets the window width in [0, 1].
`Back` / `Back10` and `Elastic` / `Elastic10` have 10% peak overshoot. Both families
offer `20` through `50` variants in `In`, `Out`, and `InOut`, measured against the
full tween range for solo legs and matching pairs. `Bounce` aliases `Bounce10`;
`Bounce10` through `Bounce50` measure the first rebound depth in the same
full-range percentages, followed by two smaller bounces. `Jump` / `Jump10` through
`Jump50` instead launch above the target and return to it between three diminishing
peaks, with the same percentage convention. Legacy `EaseType` names
keep their original shapes. Try the
[easing composer](https://tweens.gd/csharp/easing/).
