---
title: Node and value catalog
description: The 306 built-in definitions and their shorthand methods, grouped into 2D, 3D, UI, material, animation and audio, and callback value pages.
---

The catalog has a definition for every animatable property it covers, and each
definition has a typed shorthand method. They read and write Godot properties
directly, with no reflection or string property paths.

## Find a definition

| Page | What it covers |
| --- | --- |
| [2D nodes](/csharp/nodes/2d/) | `CanvasItem`, `Node2D`, sprites, cameras, particles, lights, lines, paths, and polygons |
| [3D nodes](/csharp/nodes/3d/) | `Node3D`, `GeometryInstance3D`, 3D sprites, cameras, particles, decals, fog, lights, and paths |
| [UI controls](/csharp/nodes/ui/) | `Control` layout and transforms, `Range`, labels, and progress bars |
| [Material properties](/csharp/nodes/materials/) | `BaseMaterial3D` colors, emission, roughness, and UVs |
| [Animation and audio](/csharp/nodes/audio/) | `AnimationPlayer` speed and the audio players' pitch, volume, and range |
| [Callback values](/csharp/nodes/values/) | `Tweens.Float` and the other definitions that write no property |

## Usage

The example assumes `using Godot;` and `using tweens.gd;`.

```csharp
// sprite: Sprite2D, camera: Camera2D, label: Label; all inside the tree.
var movement = sprite.TweenPosition((300, 120), 0.5, Out.Cubic);
var fade = sprite.TweenModulateAlpha(0, 0.2);
var zoom = camera.TweenZoom((2, 2), 0.4);
var reveal = label.TweenVisibleRatio(1, 1.5, options => options.From = 0);
await Group.Of(movement, fade).End;
```

Each shorthand method, such as `TweenPosition`, takes `(to, duration, configure = null)`,
`(to, duration, ease, delay = 0)`, or `(to, duration, options)`; [syntax sugar](/csharp/syntax-sugar/)
shows the shorter endpoint forms. The configure callback runs before playback starts
and can set `From`, `To`, `Duration`, or any other definition setting, including
callbacks. For motion you reuse, start a definition instead:
`sprite.Tween(new Tweens.Position2D(to, 0.5))`. Both forms return the same kind
of handle, with the same [owner lifetime](/csharp/lifetime/).

Shorthand methods on a base class work on every node derived from it: `Node2D` and
`Node3D` carry the transforms, `CanvasItem` the 2D modulation, `Control` the
layout, `SpriteBase3D` the 3D sprite appearance, `GeometryInstance3D` the
transparency, and `Range` the `Value`. A property-specific method takes precedence
over the generic callback value methods, so on a node with a color property,
`TweenColor` animates that property. Use `Tweens.Color` explicitly when you want
callback values.

## Units and engine constraints

Each group page lists the constraints specific to it. These apply everywhere:

- Rotation, skew, and texture rotation use radians. Camera `Fov`, light and
  emission angles, and radial progress angles use degrees. Ratios use the native
  property range; positions, sizes, paths, and offsets use the Godot units of
  each property.
- Value types match the native property types.
- Integer properties, such as sprite `Frame` and `VisibleCharacters`, interpolate
  continuously, round to nearest with ties away from zero, and saturate at
  `Int32` limits. Native constraints still apply.
- Setting a property doesn't enable a rendering feature or create a resource.
  Renderer support and sorting limitations are Godot's.
- For shader parameters, see [shader uniforms](/csharp/materials/).
