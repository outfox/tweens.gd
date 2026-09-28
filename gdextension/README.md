# tweens_gd GDExtension

The engine behind the GDScript API: definitions, handles, groups, schedulers, the
automatic runner, easing, interpolation and FX, in C++ against
[godot-cpp](https://github.com/godotengine/godot-cpp) 10 (`api_version` 4.4).
`addons/tweens_gd/tweens.gd` and the generated catalog stay GDScript; so do the
adapter scripts, which are the extension point for custom storage.

## Build

Needs Python with [SCons](https://scons.org/) and a C++ compiler for the target
(MSVC or MinGW on Windows, GCC or Clang on Linux, Xcode on macOS/iOS, the Android
NDK, Emscripten 4.0.11 for the Web). From the repository root:

```sh
git submodule update --init
scons -C gdextension target=template_debug
scons -C gdextension target=template_release
```

The editor and debug exports load `template_debug`, release exports
`template_release`. Pass `platform=`, `arch=` and, for the Web, `threads=no` to
cross-build. Libraries are written to `addons/tweens_gd/bin/` with the names
`addons/tweens_gd/tweens_gd.gdextension` maps to feature tags.

CI (`.github/workflows/gdextension.yml`) builds Windows x86_64/x86_32/arm64,
Linux x86_64/x86_32/arm64, macOS universal, iOS arm64, Android
arm64/arm32/x86_64/x86_32 and Web wasm32 with and without threads, then runs the
GDScript suite on Linux and macOS.

`build_profile.json` limits the generated bindings to the engine classes the
sources use. Add a class there when the code starts using a new one.
