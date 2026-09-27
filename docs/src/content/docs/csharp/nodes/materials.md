---
title: Material properties
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
