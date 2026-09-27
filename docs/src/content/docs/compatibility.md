---
title: Compatibility
description: Implementation status, engine requirements, and validated targets for tweens.gd.
---

Both C# and GDScript support are in beta. The C# library targets .NET 10 and
Godot 4.7.2 .NET; the pure GDScript addon implements the same reusable-definition
model without a .NET dependency. APIs may change during beta.

| Implementation | Availability | Requirements |
| --- | --- | --- |
| C# | Beta; project reference and local NuGet package supported | .NET 10, GodotSharp 4.7.2, matching engine |
| GDScript addon | Beta; available from source or as a locally built ZIP | Godot 4.7.2; no .NET or native extension dependency |

The C# package ID and namespace are both `tweens.gd`. The local development
version is `0.1.0`, which hasn't been published to nuget.org.
[Install](/csharp/installation/) it from source or from a locally packed package.

## C# engine and platform support

| Area | Current status |
| --- | --- |
| Target framework | `net10.0` |
| Library bindings | GodotSharp 4.7.2 |
| Testbed project | Godot 4.7.2 with .NET support |
| Windows | CI builds and tests the C# solution and the testbed |
| Rendering | The testbed and dedicated shader/material tests use a real graphics context |
| Other operating systems | Not certified by the current CI |
| Other Godot versions | Not validated |
| Trimmed, AOT, mobile, and web exports | Not validated; package metadata is not an export support guarantee |

Your application supplies the engine. Use a Godot .NET engine that matches the
GodotSharp bindings. The library depends only on GodotSharp; its source generator
is a private build dependency.

## Rendering requirements

Changing a property doesn't enable a rendering feature. Configure transparency,
emission, normal maps, and particle modes before animating them. Shader default
lookup needs a working renderer, which a dummy headless renderer may not supply.
See [materials](/csharp/materials/) and [shader uniforms](/csharp/shaders/).

## GDScript validation

The addon is pure GDScript and needs no .NET runtime, autoload, or GDExtension.

| Area | Current status |
| --- | --- |
| Engine | 2dog/Godot 4.7.2 in Debug and Release, and the standard Godot 4.7.2 editor build |
| Rendering | Real OpenGL rendering, including shader uniform defaults and output |
| Exports | A Windows release export and a single-threaded Web/WASM release export, tested in Edge |
| Other engines, browsers, and devices | Not validated |
| Throughput | High tween counts need further optimization; no supported count or frame budget is promised |

The addon's differences from C# are listed in the [GDScript Core API](/gdscript/api/).
