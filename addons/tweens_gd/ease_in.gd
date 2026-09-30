# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name In
extends RefCounted
## In curves. Combine one In and one Out with |, as in [code]In.SINE | Out.CUBIC[/code]; a single curve runs
## on its own.

const NONE := 0
const LINEAR := 1 << 8
const SINE := 1 << 9
const QUAD := 1 << 10
const CUBIC := 1 << 11
const QUART := 1 << 12
const QUINT := 1 << 13
const EXPO := 1 << 14
const CIRC := 1 << 15
const BACK := 1 << 16
const ELASTIC := 1 << 17
const BOUNCE := 1 << 18
const SMOOTH_STEP := 1 << 19
const SMOOTHER_STEP := 1 << 20
const BACK10 := BACK
const ELASTIC10 := ELASTIC
const BACK20 := 1 << 34
const BACK30 := 1 << 35
const BACK40 := 1 << 36
const BACK50 := 1 << 37
const ELASTIC20 := 1 << 38
const ELASTIC30 := 1 << 39
const ELASTIC40 := 1 << 40
const ELASTIC50 := 1 << 41
const BOUNCE10 := BOUNCE
const BOUNCE20 := 1 << 50
const BOUNCE30 := 1 << 51
const BOUNCE40 := 1 << 52
const BOUNCE50 := 1 << 53
const JUMP := (1 << 58) | (1 << 59)
const JUMP10 := JUMP
const JUMP20 := (1 << 58) | (1 << 60)
const JUMP30 := (1 << 59) | (1 << 60)
const JUMP40 := (1 << 58) | (1 << 61)
const JUMP50 := (1 << 59) | (1 << 61)
