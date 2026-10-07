---
title: Material properties
tableOfContents: true
description: Definitions and shorthand methods for BaseMaterial3D properties, for StandardMaterial3D and OrmMaterial3D.
---

Definitions for `BaseMaterial3D`, so they work with both `StandardMaterial3D` and
`OrmMaterial3D`.

A material is a resource, so its tweens need a tree or an owner node; see
[materials](/csharp/materials/) for how to start them and what each needs enabled.

25 definitions:

| Property | Definition | Shorthand method |
| --- | --- | --- |
| Albedo color | Tweens.MaterialAlbedoColor | TweenAlbedoColor |
| Albedo alpha | Tweens.MaterialAlbedoAlpha | TweenAlbedoAlpha |
| Metallic | Tweens.MaterialMetallic | TweenMetallic |
| Specular | Tweens.MaterialMetallicSpecular | TweenMetallicSpecular |
| Roughness | Tweens.MaterialRoughness | TweenRoughness |
| Emission color | Tweens.MaterialEmission | TweenEmission |
| Emission multiplier | Tweens.MaterialEmissionEnergyMultiplier | TweenEmissionEnergyMultiplier |
| Emission intensity (nits) | Tweens.MaterialEmissionIntensity | TweenEmissionIntensity |
| Normal strength | Tweens.MaterialNormalScale | TweenNormalScale |
| UV1 offset/scale | Tweens.MaterialUv1Offset / Tweens.MaterialUv1Scale | TweenUv1Offset / TweenUv1Scale |
| UV2 offset/scale | Tweens.MaterialUV2Offset / Tweens.MaterialUV2Scale | TweenUV2Offset / TweenUV2Scale |

Each UV property also has X/Y/Z variants, such as `Tweens.MaterialUv1OffsetX` /
`TweenUv1OffsetX`. A component setter leaves the other components as they are at
each write, including concurrent edits to them. Every shorthand method accepts a
SceneTree or an owner Node, plus an optional configuration callback.


## Units and constraints

- Alpha fading requires a suitable transparency mode.
- Emission and normal mapping need their feature flags enabled; normal mapping also needs a normal map.
- Emission intensity requires `rendering/lights_and_shadows/use_physical_light_units`.
- UV changes need suitable textures and mapping to be visible.

### Resource ownership

Automatic material playback requires a scene tree or an in-tree owner node. Owner-bound playback stops when the owner leaves; tree-bound playback stops with the tree. Playback keeps the original resource even when a mesh changes materials, and never duplicates or disposes it. Call `owner.CancelTweens()` to include that owner’s material playback.

A manual scheduler can play resources without an owner. Dispose it when finished. See [resource lifetimes](/csharp/materials/#choose-an-owner).

## Ordinary shader uniforms

```csharp
// shader: uniform float dissolve = 0.25;
shaderMaterial.TweenShaderParameter("dissolve", 1f, 0.5, GetTree());
shaderMaterial.TweenShaderParameter("dissolve", 1f, 0.5, mesh);

var definition = new Tweens.ShaderParameter<float>("dissolve")
{
    To = 1,
    Duration = 0.5,
    Fill = FillMode.None,
};
shaderMaterial.Tween(definition, GetTree());
```

The material is shared as usual, so every node using it sees the change. Uniform
names are case-sensitive and snapshotted when the tween is added. A missing
shader, an undeclared uniform, an incompatible type, or a non-finite endpoint fails
before the tween is registered or writes anything. If the material has no override
for the uniform, the tween captures the declared shader default rather than zero.

| C# value | Required uniform metadata |
| --- | --- |
| float, double | Float (shader float) |
| int | Int (shader int) |
| Vector2 / Vector3 / Vector4 | Matching vector type |
| Color | Color (typically vec4 with source_color hint) |

Color and Vector4 are deliberately distinct, so neither binds to a uniform of the
other type. A double uses Godot's floating Variant representation, but the shader
and GPU ultimately determine precision. During interpolation, integers round
halfway away from zero and saturate at Int32 bounds. Non-finite samples fault
playback before writing. Textures, resources, arrays, booleans, enums and
quaternion representations aren't supported.

When a tween ends with a fill mode that doesn't keep the final value (`None` or
`ApplyFromDuringDelay`), the original explicit override is restored, or the new override is removed if none originally existed.
Cancelling keeps the latest sample, as with node tweens. You can reuse a source
definition across materials with different initial values and override states.

Replacing, disposing, or editing the bound shader faults playback the next time it
samples or restores, and `End` settles with the error. Any shader change signal
counts as a binding change, even if the new declaration happens to be compatible,
so start a new tween after changing the shader. Playback doesn't scan uniform
metadata or parse string paths per frame.

Default lookup requires a working renderer. Godot's dummy headless renderer can
expose declarations but return no default value, and then capture fails rather
than inventing one. Headless property tests can use an explicit material override,
but verifying defaults and visible behavior takes rendering tests.

## Per-instance shader uniforms

Declare an `instance uniform` in the shader when nodes sharing the same material
need independent values:

```csharp
// shader: instance uniform float pulse = 0.25;
mesh.TweenInstanceShaderParameter("pulse", 1f, 0.5);
sprite.TweenInstanceShaderParameter("pulse", 0f, 0.5);
```

These methods target `GeometryInstance3D` and `CanvasItem` respectively and follow
the normal node lifetime and pause rules. The definitions are
`Tweens.GeometryInstanceShaderParameter<T>` and
`Tweens.CanvasItemInstanceShaderParameter<T>`. Value types, validation, snapshots,
and restoration work the same as for material uniforms. After restoration, an
explicit override equal to the default stays explicit, and an originally absent
override is removed.

The tween captures the effective material bindings, including inherited CanvasItem
materials, mesh surfaces, overrides, overlays and next passes. Replacing the mesh,
a material, or a pass, or editing a bound shader, faults the next write. Binding
checks are conservative, so changing a tracked slot can fault playback even if
another slot still declares the same uniform. Godot controls instance-uniform
indexing, capacity, shader compatibility and multi-material conflicts. This API
doesn't assign or reconcile those declarations.

See [ShaderMaterial](https://docs.godotengine.org/en/stable/classes/class_shadermaterial.html), [CanvasItem](https://docs.godotengine.org/en/stable/classes/class_canvasitem.html), and [GeometryInstance3D](https://docs.godotengine.org/en/stable/classes/class_geometryinstance3d.html).
See [compatibility](/compatibility/) for rendering limits and
[handles](/csharp/api/handles/#errors) for fault handling.
