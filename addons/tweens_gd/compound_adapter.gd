# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends "adapter.gd"
## Reads and writes compound properties for generated helpers.
##
## Ordinary animation uses named [Tweens] factories. This adapter combines scalar paths
## into Vector2 or Vector4 values, or converts global Euler rotation to a Quaternion.

## Ordered scalar component paths for a Vector2 or Vector4 value.
var paths: Array[NodePath] = []
## Animate global_rotation through normalized quaternion interpolation instead of [member paths].
var global_quaternion := false

func read(target: Object) -> Variant:
	if global_quaternion: return Quaternion.from_euler(target.global_rotation)
	if paths.size() == 2: return Vector2(target.get_indexed(paths[0]), target.get_indexed(paths[1]))
	return Vector4(target.get_indexed(paths[0]), target.get_indexed(paths[1]), target.get_indexed(paths[2]), target.get_indexed(paths[3]))

func write(target: Object, value: Variant) -> String:
	if global_quaternion:
		target.global_rotation = value.get_euler()
	else:
		for index in range(paths.size()):
			if not is_instance_valid(target): return "The target was freed during a compound write."
			target.set_indexed(paths[index], value[index])
	return ""
