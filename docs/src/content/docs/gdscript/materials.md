---
title: Material tweens
description: Animate shared material properties with explicit ownership and rendering prerequisites.
---

Material tweens write directly to the resource you supply. If several nodes share
that resource, all of them see the change. Playback never clones, reassigns, or
frees your material, shader, textures, or mesh.

The snippets assume `const Tweens = preload("res://addons/tweens_gd/tweens.gd")` and
run in a Node method on Godot's main thread, with configured materials and an
in-tree `MeshInstance3D` named `mesh`.

## Choose the playback lifetime

A material is a resource, not a node, so `Tweens.play()` needs an owner as its
third argument:

```gdscript
# material and shared are StandardMaterial3D; mesh is an in-tree MeshInstance3D.
# Shared resource, with playback scoped to this SceneTree.
var fade := Tweens.play(material, Tweens.material_albedo_alpha(0.0, 0.5), get_tree())

# Same resource semantics, but stop when this node leaves the tree.
var roughness := Tweens.play(material, Tweens.material_roughness(0.2, 1.0), mesh)

# Definitions carry the ordinary timing and easing fields.
var emission := Tweens.material_emission_energy_multiplier(3.0, 1.0)
emission.ease = Tweens.Ease.CUBIC_OUT
Tweens.play(material, emission, mesh)

# One definition works with either lifetime.
var smooth := Tweens.material_roughness(0.5, 1.0)
Tweens.play(material, smooth, get_tree())
Tweens.play(material, smooth, mesh)
```

Passing `get_tree()` binds playback to the tree's root node. A tree-scoped tween
keeps running when a mesh is removed or gets a different material, because it
retains the resource you originally supplied. A tween with a node owner ends with
`Tweens.Reason.OWNER_EXITED` when that owner leaves the tree. Without any owner,
automatic playback is rejected, and the returned handle reports `FAILED`.

`Tweens.cancel_tweens(owner)` cancels only that owner's automatic tweens, material
tweens included, and `Tweens.cancel_tweens(owner, true)` adds its descendants'
tweens. Other owners are unaffected. Tree-scoped tweens belong to the root node.

Tweens on a freed resource end with `TARGET_FREED`, even while paused. Runner or
tree teardown settles pending work with `RUNNER_DISPOSED`. Cleanup releases
playback bindings but never the resources you supplied, and handles retain their
`target` for inspection.

With a node owner, `Tweens.Pause.BOUND` follows `owner.can_process()` and
`SCENE_TREE` follows tree pause. Tree-scoped playback follows tree pause with
either mode. `ALWAYS` ignores both, although pausing the handle always stops
advancement. All access to native resources has to happen on Godot's main thread.

For manual scheduling, call `scheduler.add(material, definition)` or
`scheduler.add(material, definition, owner)` on a `Tweens.Scheduler`. Without an
owner, a manual scheduler has no tree pause policy. Call `dispose()` on the
scheduler when you're finished with it.

## Built-in material properties

All 25 helpers target `BaseMaterial3D`, so they work with both `StandardMaterial3D`
and `ORMMaterial3D`. The target class and the captured value type are checked when
the tween starts.

| Property | Helper |
| --- | --- |
| Albedo color | `Tweens.material_albedo_color` |
| Albedo alpha | `Tweens.material_albedo_alpha` |
| Metallic | `Tweens.material_metallic` |
| Specular | `Tweens.material_metallic_specular` |
| Roughness | `Tweens.material_roughness` |
| Emission color | `Tweens.material_emission` |
| Emission multiplier | `Tweens.material_emission_energy_multiplier` |
| Emission intensity (nits) | `Tweens.material_emission_intensity` |
| Normal strength | `Tweens.material_normal_scale` |
| UV1 offset/scale | `Tweens.material_uv1_offset` / `Tweens.material_uv1_scale` |
| UV2 offset/scale | `Tweens.material_uv2_offset` / `Tweens.material_uv2_scale` |

Each UV helper also has `_x`, `_y`, and `_z` variants, such as
`Tweens.material_uv1_offset_x`. A component write reads the other components at
each write, including concurrent edits to them. All helpers take
`(to = null, seconds = 0.0, easing = LINEAR, delay = 0.0)`, and a null endpoint
uses the captured value. The
[helper catalog](/gdscript/nodes/) lists them with the rest.

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
