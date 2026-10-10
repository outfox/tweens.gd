// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// SPDX-FileCopyrightText: 2020 Björn Ottosson
// Adapted from Oklab; see THIRD-PARTY-NOTICES.md.
#include "color_interpolation.hpp"
#include <godot_cpp/variant/vector3.hpp>
#include <cmath>

namespace godot {

Vector4 encode_color(Color p_color, int64_t p_space, int64_t p_alpha, int64_t p_encoding) {
	if (p_space == 1) {
		if (p_encoding == 1) p_color = p_color.linear_to_srgb();
	} else if (p_encoding == 0) p_color = p_color.srgb_to_linear();
	Vector3 rgb(p_color.r, p_color.g, p_color.b);
	if (p_space == 0) {
		const float l = std::cbrt(0.4122214708f * rgb.x + 0.5363325363f * rgb.y + 0.0514459929f * rgb.z);
		const float m = std::cbrt(0.2119034982f * rgb.x + 0.6806995451f * rgb.y + 0.1073969566f * rgb.z);
		const float s = std::cbrt(0.0883024619f * rgb.x + 0.2817188376f * rgb.y + 0.6299787005f * rgb.z);
		rgb = Vector3(0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
				1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
				0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
	}
	if (p_alpha == 0) rgb *= p_color.a;
	return Vector4(rgb.x, rgb.y, rgb.z, p_color.a);
}

Color decode_color(Vector4 p_value, int64_t p_space, int64_t p_alpha, int64_t p_encoding) {
	Vector3 rgb(p_value.x, p_value.y, p_value.z);
	if (p_alpha == 0) rgb = p_value.w == 0 ? Vector3() : rgb / p_value.w;
	if (p_space == 0) {
		float l = rgb.x + 0.3963377774f * rgb.y + 0.2158037573f * rgb.z;
		float m = rgb.x - 0.1055613458f * rgb.y - 0.0638541728f * rgb.z;
		float s = rgb.x - 0.0894841775f * rgb.y - 1.2914855480f * rgb.z;
		l *= l * l; m *= m * m; s *= s * s;
		rgb = Vector3(4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
				-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
				-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
	}
	const Color result(rgb.x, rgb.y, rgb.z, p_value.w);
	if (p_space == 1) return p_encoding == 0 ? result : result.srgb_to_linear();
	return p_encoding == 1 ? result : result.linear_to_srgb();
}
} // namespace godot
