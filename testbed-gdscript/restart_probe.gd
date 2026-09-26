# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
extends Node

func ping() -> int:
	var curve := Curve.new()
	curve.add_point(Vector2.ZERO)
	curve.add_point(Vector2.ONE)
	var copy := curve.duplicate() as Curve
	if not is_equal_approx(copy.sample(0.5), curve.sample(0.5)): return -1
	return 42
