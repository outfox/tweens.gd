---
title: Material tweens
description: Animate shared material properties with explicit ownership and rendering prerequisites.
---

Material tweens write directly to the resource you supply. If several nodes share
that resource, all of them see the change. Playback never clones, reassigns, or
frees your material, shader, textures, or mesh.

The snippets assume the global `Tweens` class and
run in a Node method on Godot's main thread, with configured materials and an
in-tree `MeshInstance3D` named `mesh`.

## Choose the playback lifetime

A material is a resource, not a node, so it has no place in the tree that could
end its tweens. `Tweens.play()` takes that lifetime as its third argument: the
scene tree or an owner node. Without one, automatic playback is rejected and the
handle reports `FAILED`.

```gdscript
# material and shared are StandardMaterial3D; mesh is an in-tree MeshInstance3D.
# Scoped to the scene tree.
var fade := Tweens.play(material, Tweens.material_albedo_alpha(0.0, 0.5), get_tree())

# Stops when mesh leaves the tree.
var roughness := Tweens.play(material, Tweens.material_roughness(0.2, 1.0), mesh)

# Helpers take the ordinary timing and easing arguments.
var emission := Tweens.material_emission_energy_multiplier(3.0, 1.0, Tweens.Ease.CUBIC_OUT)
Tweens.play(material, emission, mesh)

# One definition works with either lifetime.
var smooth := Tweens.material_roughness(0.5, 1.0)
Tweens.play(material, smooth, get_tree())
Tweens.play(material, smooth, mesh)
```

| Lifetime | Stops early when | Pause follows |
| --- | --- | --- |
| `get_tree()` | The tree shuts down: `RUNNER_DISPOSED` | Tree pause |
| An owner node | The owner leaves the tree: `OWNER_EXITED` | `owner.can_process()` with the default `BOUND` mode, or tree pause with `SCENE_TREE` |

`Tweens.Pause.ALWAYS` ignores both, and pausing the handle always stops it.
Either way, a tween on a freed resource ends with `TARGET_FREED`.

A tree-scoped tween keeps running when a mesh is removed or gets a different
material, because it holds the resource you supplied. Passing `get_tree()` binds
playback to the tree's root node, so `Tweens.cancel_tweens(get_tree().root)`
cancels it. `Tweens.cancel_tweens(owner)` cancels that owner's tweens, material
tweens included, and `Tweens.cancel_tweens(owner, true)` adds its descendants'.
Cleanup releases playback bindings but never the resources you supplied. See
[lifetime and ownership](/gdscript/lifetime/) for the full rules.

### Manual scheduling

Call `scheduler.add(material, definition)` on a `Tweens.Scheduler`, or pass an
owner as the third argument. Without an owner, a manual scheduler has no tree
pause to follow. Call `dispose()` on the scheduler when you're finished with it.

## Built-in material properties

All 25 helpers target `BaseMaterial3D`, so they work with both `StandardMaterial3D`
and `ORMMaterial3D`. The [material properties](/gdscript/nodes/materials/) page
lists them. The target class and the captured value type are checked when the
tween starts.

Other material properties can use a property path, such as
`Tweens.property(^"rim", 1.0, 0.5)`, played with the same owner argument. A path
selects a property and optional components on the material itself; it can't reach
into another resource.

Tweens don't change rendering modes or flags, so enable the features you animate
yourself:

- Alpha fading needs a suitable transparency mode.
- Emission needs `emission_enabled`.
- Normal strength needs a normal map and `normal_enabled`.
- `material_emission_intensity` requires
  `rendering/lights_and_shadows/use_physical_light_units`.
- UV animation only becomes visible with suitable textures and mapping.

Native setters keep their usual limits and renderer-specific behavior. See
[BaseMaterial3D](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html).

To give one node its own values on an ordinary material, duplicate the material
once during scene setup, assign the duplicate, and use that for later tweens:

```gdscript
var unique: StandardMaterial3D = shared.duplicate()
mesh.material_override = unique
Tweens.play(unique, Tweens.material_albedo_color(Color.RED, 1.0), mesh)
Tweens.play(unique, Tweens.material_roughness(0.2, 1.0), mesh)
```

Continue with [shader uniforms](/gdscript/shaders/) for shared and per-instance parameters.
