# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name InOut
extends RefCounted
## Matching half-duration legs. Back/Elastic/Jump overshoot and Bounce first-rebound depth use the full tween
## range.

const LINEAR := In.LINEAR | Out.LINEAR
const SINE := In.SINE | Out.SINE
const QUAD := In.QUAD | Out.QUAD
const CUBIC := In.CUBIC | Out.CUBIC
const QUART := In.QUART | Out.QUART
const QUINT := In.QUINT | Out.QUINT
const EXPO := In.EXPO | Out.EXPO
const CIRC := In.CIRC | Out.CIRC
const BACK := In.BACK | Out.BACK
const ELASTIC := In.ELASTIC | Out.ELASTIC
const BOUNCE := In.BOUNCE | Out.BOUNCE
const SMOOTH_STEP := In.SMOOTH_STEP | Out.SMOOTH_STEP
const SMOOTHER_STEP := In.SMOOTHER_STEP | Out.SMOOTHER_STEP
const BACK10 := BACK
const ELASTIC10 := ELASTIC
const BACK20 := In.BACK20 | Out.BACK20
const BACK30 := In.BACK30 | Out.BACK30
const BACK40 := In.BACK40 | Out.BACK40
const BACK50 := In.BACK50 | Out.BACK50
const ELASTIC20 := In.ELASTIC20 | Out.ELASTIC20
const ELASTIC30 := In.ELASTIC30 | Out.ELASTIC30
const ELASTIC40 := In.ELASTIC40 | Out.ELASTIC40
const ELASTIC50 := In.ELASTIC50 | Out.ELASTIC50
const BOUNCE10 := BOUNCE
const BOUNCE20 := In.BOUNCE20 | Out.BOUNCE20
const BOUNCE30 := In.BOUNCE30 | Out.BOUNCE30
const BOUNCE40 := In.BOUNCE40 | Out.BOUNCE40
const BOUNCE50 := In.BOUNCE50 | Out.BOUNCE50
const JUMP := In.JUMP | Out.JUMP
const JUMP10 := JUMP
const JUMP20 := In.JUMP20 | Out.JUMP20
const JUMP30 := In.JUMP30 | Out.JUMP30
const JUMP40 := In.JUMP40 | Out.JUMP40
const JUMP50 := In.JUMP50 | Out.JUMP50
