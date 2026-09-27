---
title: Material tweens
description: Animate shared material properties with explicit ownership and rendering prerequisites.
---

Material tweens write directly to the resource you supply. If several nodes share
that resource, all of them see the change. Playback never clones, reassigns, or
disposes your material, shader, textures, or mesh.

Import `Godot` and `tweens.gd`. The snippets run in a Node method on Godot's main
thread with configured materials and an in-tree `MeshInstance3D` named `mesh`.

## Choose the playback lifetime

A material is a resource, not a node, so it has no place in the tree that could
end its tweens. Each tween on it takes that lifetime from the scene tree or from
an owner node:

```csharp
// material and shared are StandardMaterial3D; mesh is an in-tree MeshInstance3D.
// Scoped to the scene tree.
var fade = material.TweenAlbedoAlpha(0, 0.5, GetTree());

// Stops when mesh leaves the tree.
var roughness = material.TweenRoughness(0.2f, 1, mesh);

// Shorthand methods take the usual configure callback after the tree or owner.
var emission = material.TweenEmissionEnergyMultiplier(3, 1, GetTree(),
    options => options.Ease = EaseType.CubicOut);

// One definition works with either lifetime.
var definition = new Tweens.MaterialRoughness(0.5f, 1);
_ = material.Tween(definition, GetTree());
_ = material.Tween(definition, mesh);
_ = mesh.Tween(material, definition); // The same as the line above, owner first.
```

| Lifetime | Stops early when | Pause follows |
| --- | --- | --- |
| `GetTree()` | The tree shuts down: `RunnerDisposed` | Tree pause |
| An owner node | The owner leaves the tree: `OwnerExited` | `owner.CanProcess()` with the default `Bound` mode, or tree pause with `SceneTree` |

`TweenPauseMode.Always` ignores both, and pausing the handle always stops it.
Either way, a tween on a disposed resource ends with `TargetFreed`.

A tree-scoped tween keeps running when a mesh is removed or gets a different
material, because it holds the resource you supplied. `owner.CancelTweens()`
cancels that owner's tweens, material tweens included, and
`includeChildren: true` adds its descendants'. Cleanup releases playback bindings
but never the resources you supplied. See
[lifetime and ownership](/csharp/lifetime/) for the full rules.

### Manual scheduling

Call `scheduler.Add(material, definition)`, or pass an owner as the third
argument. Without an owner, a manual scheduler has no tree pause to follow.
Dispose the scheduler when you're finished with it.

## Built-in material properties

All 25 material definitions target `BaseMaterial3D`, so they work with both
`StandardMaterial3D` and `OrmMaterial3D`. The
[material properties](/csharp/nodes/materials/) page lists them with their
shorthand methods.

Tweens don't change rendering modes or flags, so enable the features you animate
yourself:

- Alpha fading needs a suitable transparency mode.
- Emission needs `EmissionEnabled`.
- Normal strength needs a normal map and `NormalEnabled`.
- `EmissionIntensity` requires `rendering/lights_and_shadows/use_physical_light_units`.
- UV animation only becomes visible with suitable textures and mapping.

Native setters keep their usual limits and renderer-specific behavior. See
[BaseMaterial3D](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html).

To give one node its own values on an ordinary material, duplicate the material
once during scene setup, assign the duplicate, and use that for later tweens:

```csharp
var unique = (StandardMaterial3D)shared.Duplicate();
mesh.MaterialOverride = unique;
_ = unique.TweenAlbedoColor(Colors.Red, 1, mesh);
_ = unique.TweenRoughness(0.2f, 1, mesh);
```

Continue with [shader uniforms](/csharp/shaders/) for shared and per-instance parameters.
