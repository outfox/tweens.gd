---
title: Compatibility
description: Implementation status, engine requirements, and validated targets for tweens.gd.
---

Both C# and GDScript support are in beta. The C# library targets .NET 10 and
Godot 4.7.2 .NET; the GDScript API implements the same reusable-definition model on
a bundled native GDExtension, without a .NET dependency. Both ship in one addon. APIs
may change during beta.

| Implementation | Availability | Requirements |
| --- | --- | --- |
| C# | Beta; addon source install, optional project reference or NuGet | .NET 10, GodotSharp 4.7.2, matching engine |
| GDScript addon | Beta; available from source or as a locally built ZIP | Godot 4.7.2; bundled GDExtension, no .NET dependency |

The C# package ID and namespace are both `tweens.gd`. The local development
version is `0.1.0-pre`.
[Install](/csharp/installation/) it from source or from a locally packed package.

## C# support

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

## GDScript support

The GDScript API runs on the `tweens_gd` GDExtension included in the addon. It needs
no .NET runtime, plugin, or autoload, even though the same addon also includes C# sources.

| Area | Current status |
| --- | --- |
| Native libraries | Windows (x86_64, x86_32, arm64), Linux (x86_64, x86_32, arm64), macOS (universal), iOS (arm64), Android (arm64, arm32, x86_64, x86_32), and Web (wasm32, with and without threads), built by CI |
| Engine | Godot 4.7.2 in Debug and Release through 2dog, the embedded Godot host the tests run in, and the standard Godot 4.7.2 builds for Windows and Linux |
| Rendering | Real OpenGL rendering, including shader uniform defaults and output |
| Exports | A Windows release export and Web/WASM release exports with and without threads, tested in Edge; Web exports need Extensions Support |
| iOS and Android | Built, not tested on devices |
| Other engines, browsers, and devices | Not validated |
| Throughput | Playback updates run natively; no supported count or frame budget is promised |

The addon's differences from C# are listed in the [GDScript Core API](/gdscript/api/#differences-from-c).

## Rendering requirements

Changing a property doesn't enable a rendering feature. Configure transparency,
emission, normal maps, and particle modes before animating them. Shader default
lookup needs a working renderer, which a dummy headless renderer may not supply.
See materials ([C#](/csharp/materials/), [GDScript](/gdscript/materials/)) and
shader uniforms ([C#](/csharp/shaders/), [GDScript](/gdscript/shaders/)).
