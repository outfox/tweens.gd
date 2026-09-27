---
title: Shader uniforms
description: Material and per-instance uniforms, binding validation, defaults, and restoration.
---

Animate shader uniforms by name, either on a shared `ShaderMaterial` or per node
with instance uniforms. Names and types are checked before playback starts.

The examples assume `const Tweens = preload("res://addons/tweens_gd/tweens.gd")` and
run in a Node method on the main thread. `shader_material` is a configured
ShaderMaterial. `mesh` and `sprite` are in-tree MeshInstance3D and Sprite2D nodes
using the declared shaders.

## Ordinary shader uniforms

```gdscript
# shader: uniform float dissolve = 0.25;
Tweens.play(shader_material, Tweens.shader_parameter(&"dissolve", 1.0, 0.5), get_tree())
Tweens.play(shader_material, Tweens.shader_parameter(&"dissolve", 1.0, 0.5), mesh)

var dissolve := Tweens.shader_parameter(&"dissolve", 1.0, 0.5)
dissolve.fill = Tweens.Fill.NONE
Tweens.play(shader_material, dissolve, get_tree())
```

`Tweens.shader_parameter(name, to = null, seconds = 0.0, easing, delay)` targets a
`ShaderMaterial`. As with other resources, pass an owner node or the `SceneTree` as
the third argument of `Tweens.play()`. The material is shared as usual, so every
node using it sees the change. Uniform names are case-sensitive and captured when
the tween starts. A missing shader, an undeclared uniform, an incompatible type, or
a non-finite endpoint rejects the start before anything is written; the handle
settles with `Tweens.Reason.FAILED` and the message in `handle.error`. If the
material has no override for the uniform, the tween captures the declared shader
default rather than zero.

| GDScript value | Uniform type |
| --- | --- |
| `float` | float |
| `int` | int |
| `Vector2` / `Vector3` / `Vector4` | Matching vector type |
| `Color` | Color (typically vec4 with source_color hint) |

:::caution[Match the uniform type exactly]
Endpoints must have the uniform's exact type. Write `1.0`, not `1`, for a float
uniform: an `int` endpoint doesn't bind to it. Color and Vector4 are distinct too,
so neither binds to a uniform of the other type.
:::

During interpolation, integers saturate at signed 32-bit bounds. Non-finite
samples fail playback before writing. Textures, resources, arrays, booleans, and
quaternions aren't supported.

On natural completion without `Tweens.Fill.RETAIN_FINAL_VALUE`, the original
explicit override is restored, or the new override is removed if none originally
existed. Cancelling keeps the latest sample, as with node tweens. You can reuse a
definition across materials with different initial values and override states,
because each start captures its own.

Replacing, freeing, or editing the bound shader fails playback the next time it
samples or restores, and the handle settles with `FAILED`. Any shader change
signal counts as a binding change, even if the new declaration happens to be
compatible, so start a new tween after changing the shader.

Default lookup requires a working renderer. Godot's dummy headless renderer can
expose declarations but return no default value, and then the start is rejected
rather than inventing one. Headless tests can use an explicit material override,
but verifying defaults and visible behavior takes rendering tests.

## Per-instance shader uniforms

Declare an `instance uniform` in the shader when nodes sharing the same material
need independent values:

```gdscript
# shader: instance uniform float pulse = 0.25;
Tweens.play(mesh, Tweens.instance_shader_parameter(&"pulse", 1.0, 0.5))
Tweens.play(sprite, Tweens.instance_shader_parameter(&"pulse", 0.0, 0.5))
```

`Tweens.instance_shader_parameter()` targets a `GeometryInstance3D` or a
`CanvasItem`, which is also the owner, so these tweens follow the normal node
lifetime and pause rules. Value types, validation, captures, and restoration work
the same as for material uniforms. After restoration, an explicit override stays
explicit, and an originally absent override is removed.

The tween captures the effective material bindings, including inherited
CanvasItem materials, mesh surfaces, overrides, overlays and next passes.
Replacing the mesh, a material, or a pass, or editing a bound shader, fails the
next write. Binding checks are conservative, so changing a tracked slot can fail
playback even if another slot still declares the same uniform. Godot controls
instance-uniform indexing, capacity, shader compatibility and multi-material
conflicts. The addon doesn't assign or reconcile those declarations.

See [ShaderMaterial](https://docs.godotengine.org/en/stable/classes/class_shadermaterial.html), [CanvasItem](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html), and [GeometryInstance3D](https://docs.godotengine.org/en/stable/classes/class_geometryinstance3d.html).
See [compatibility](/compatibility/) for rendering limits and
[control and completion](/gdscript/playback/) for failure handling.
