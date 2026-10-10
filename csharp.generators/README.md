# C# API generation

The addon owns the hand-written adapters and options. NuGet builds run these Roslyn
generators directly; addon installations compile the checked-in generated C#.
Run `./scripts/Generate-CSharpDefinitions.ps1` followed by
`node scripts/generate-gdscript-catalog.mjs` after changing an adapter, option, or generator.

`StructuredDefinitionGenerator` discovers adapters by their semantic base types.
Common builder options forward onto every immutable definition. An option marked
`[TweenValueOption(typeof(...))]` forwards only onto definitions with that concrete
endpoint type. Generic definitions retain the complete `Options` value. Target
classes do not determine whether a value option applies: a scalar alpha channel
does not have whole-color settings, even on a node with color properties.

The generic shader and custom-property models also emit closed color definitions.
These reuse their existing adapters, expose color options directly, and omit the
custom property's interpolator argument. They add no per-node specializations.

At runtime, storage uses `ITweenBinding<TTarget, TValue>` and sampling uses
`ITweenSampler<TValue>`. Existing mutable definitions implement both facets, so
virtual custom hooks remain authoritative without extra adapter allocations.
`TweenTiming` and `ColorPolicy` are independent values behind `TweenOptions`.
`Through` snapshots a definition's binding and callbacks and attaches a prepared
curve. Named keyframe channels use the same sampled definition with a validated
property-path binding. Prepared curves can be shared; playback binding resources
and captured starts cannot.

`AdapterBindings` reads getter operations, including aliases, component access,
compound values, and inherited properties. Its manifest records target and value
types, binding kind, paths, and the Godot classes declaring those paths. GDScript
catalog generation and cross-language tests consume `tests/conformance/adapters.json`;
the JavaScript generator does not parse C# source text.

Roslyn emits only C# sources, so `TweenCatalog.g.cs` transports the manifest in a
comment. The PowerShell generation script validates and extracts that JSON, and
excludes the transport file from the addon. It adds no runtime types or storage.

`KeyframeApiGenerator` keeps the public channel aliases and reads their property
types from Godot symbols, walking base classes. This includes keyframe-only
properties such as `RotationDegrees`. The canonical base-class API supplies inherited
channels; derived Godot nodes do not need duplicate extension methods.

Generator unit tests compile independent C# 12 inputs. They cover option applicability,
semantic bindings, inherited channels, constructor forms, and incremental updates.
