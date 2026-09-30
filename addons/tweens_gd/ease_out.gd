# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name Out
extends RefCounted
## Out curves. Combine one In and one Out with |, as in [code]In.SINE | Out.CUBIC[/code]; a single curve runs
## on its own.

const NONE := 0
const LINEAR := 1 << 21
const SINE := 1 << 22
const QUAD := 1 << 23
const CUBIC := 1 << 24
const QUART := 1 << 25
const QUINT := 1 << 26
const EXPO := 1 << 27
const CIRC := 1 << 28
const BACK := 1 << 29
const ELASTIC := 1 << 30
const BOUNCE := 1 << 31
const SMOOTH_STEP := 1 << 32
const SMOOTHER_STEP := 1 << 33
const BACK10 := BACK
const ELASTIC10 := ELASTIC
const BACK20 := 1 << 42
const BACK30 := 1 << 43
const BACK40 := 1 << 44
const BACK50 := 1 << 45
const ELASTIC20 := 1 << 46
const ELASTIC30 := 1 << 47
const ELASTIC40 := 1 << 48
const ELASTIC50 := 1 << 49
const BOUNCE10 := BOUNCE
const BOUNCE20 := 1 << 54
const BOUNCE30 := 1 << 55
const BOUNCE40 := 1 << 56
const BOUNCE50 := 1 << 57
const JUMP := (1 << 62) | (1 << 63)
const JUMP10 := JUMP
const JUMP20 := (1 << 62) | (1 << 6)
const JUMP30 := (1 << 63) | (1 << 6)
const JUMP40 := (1 << 62) | (1 << 7)
const JUMP50 := (1 << 63) | (1 << 7)
