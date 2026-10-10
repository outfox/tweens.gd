// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#pragma once

#include <godot_cpp/variant/color.hpp>
#include <godot_cpp/variant/vector4.hpp>

namespace godot {
// Space: OKLab=0, sRGB=1, linear RGB=2. Alpha: premultiplied=0, straight=1. Encoding: sRGB=0, linear=1.
Vector4 encode_color(Color p_color, int64_t p_space, int64_t p_alpha, int64_t p_encoding);
Color decode_color(Vector4 p_value, int64_t p_space, int64_t p_alpha, int64_t p_encoding);
} // namespace godot
