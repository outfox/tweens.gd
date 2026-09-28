// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "interpolation.hpp"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/math.hpp>
#include <godot_cpp/templates/hash_map.hpp>
#include <godot_cpp/templates/hash_set.hpp>
#include <godot_cpp/templates/hashfuncs.hpp>
#include <godot_cpp/variant/dictionary.hpp>
#include <godot_cpp/variant/packed_string_array.hpp>
#include <godot_cpp/variant/typed_array.hpp>

namespace godot {

namespace {

struct ClassKey {
	StringName type;
	uint64_t script = 0;

	bool operator==(const ClassKey &p_other) const { return type == p_other.type && script == p_other.script; }
};

struct ClassKeyHasher {
	static uint32_t hash(const ClassKey &p_key) { return hash_murmur3_one_64(p_key.script, p_key.type.hash()); }
};

using PropertyNames = HashSet<StringName>;

// Property lists are costly to build, so names are cached per native class and script.
HashMap<ClassKey, PropertyNames, ClassKeyHasher> *listed_properties = nullptr;

PropertyNames list_properties(Object *p_target) {
	PropertyNames result;
	const TypedArray<Dictionary> list = p_target->get_property_list();
	for (int64_t index = 0; index < list.size(); index++) {
		const Dictionary entry = list[index];
		result.insert(entry["name"]);
	}
	return result;
}

bool is_listed(Object *p_target, const StringName &p_name) {
	if (listed_properties == nullptr) {
		listed_properties = memnew((HashMap<ClassKey, PropertyNames, ClassKeyHasher>));
	}
	const Variant script = p_target->get_script();
	const Object *script_object = script;
	const ClassKey key{ p_target->get_class(), script_object != nullptr ? uint64_t(script_object->get_instance_id()) : 0 };
	PropertyNames *names = listed_properties->getptr(key);
	if (names == nullptr) {
		names = &listed_properties->insert(key, list_properties(p_target))->value;
	}
	if (names->has(p_name)) {
		return true;
	}
	// Metadata and dynamic lists differ per instance.
	return list_properties(p_target).has(p_name);
}

bool has_component(Variant::Type p_type, const String &p_name) {
	switch (p_type) {
		case Variant::VECTOR2:
			return p_name == "x" || p_name == "y";
		case Variant::VECTOR3:
			return p_name == "x" || p_name == "y" || p_name == "z";
		case Variant::VECTOR4:
		case Variant::QUATERNION:
			return p_name == "x" || p_name == "y" || p_name == "z" || p_name == "w";
		case Variant::COLOR:
			return p_name == "r" || p_name == "g" || p_name == "b" || p_name == "a";
		case Variant::RECT2:
			return p_name == "position" || p_name == "size" || p_name == "end";
		default:
			return false;
	}
}

} // namespace

void TweensGdInterpolation::_bind_methods() {
	const char *name = "TweensGdInterpolation";
	ClassDB::bind_static_method(name, D_METHOD("supported", "value"), &TweensGdInterpolation::supported);
	ClassDB::bind_static_method(name, D_METHOD("compatible", "initial", "endpoint"), &TweensGdInterpolation::compatible);
	ClassDB::bind_static_method(name, D_METHOD("finite", "value"), &TweensGdInterpolation::finite);
	ClassDB::bind_static_method(name, D_METHOD("interpolate", "from", "to", "weight", "value_type"), &TweensGdInterpolation::interpolate);
	ClassDB::bind_static_method(name, D_METHOD("zero", "value_type"), &TweensGdInterpolation::zero);
	ClassDB::bind_static_method(name, D_METHOD("add", "value", "offset"), &TweensGdInterpolation::add);
	ClassDB::bind_static_method(name, D_METHOD("remove", "value", "offset"), &TweensGdInterpolation::remove);
	ClassDB::bind_static_method(name, D_METHOD("read_property", "target", "path"), &TweensGdInterpolation::read_property);
}

bool TweensGdInterpolation::supported(const Variant &p_value) {
	switch (p_value.get_type()) {
		case Variant::INT:
		case Variant::FLOAT:
		case Variant::VECTOR2:
		case Variant::VECTOR3:
		case Variant::VECTOR4:
		case Variant::COLOR:
		case Variant::QUATERNION:
		case Variant::RECT2:
			return true;
		default:
			return false;
	}
}

bool TweensGdInterpolation::compatible(const Variant &p_initial, const Variant &p_endpoint) {
	const Variant::Type initial = p_initial.get_type();
	const Variant::Type endpoint = p_endpoint.get_type();
	if (initial == Variant::INT || initial == Variant::FLOAT) {
		return endpoint == Variant::INT || endpoint == Variant::FLOAT;
	}
	return initial == endpoint;
}

bool TweensGdInterpolation::finite(const Variant &p_value) {
	switch (p_value.get_type()) {
		case Variant::INT:
			return true;
		case Variant::FLOAT:
			return Math::is_finite(double(p_value));
		case Variant::VECTOR2:
			return Vector2(p_value).is_finite();
		case Variant::VECTOR3:
			return Vector3(p_value).is_finite();
		case Variant::VECTOR4:
			return Vector4(p_value).is_finite();
		case Variant::QUATERNION:
			return Quaternion(p_value).is_finite();
		case Variant::COLOR: {
			const Color color = p_value;
			return Math::is_finite(color.r) && Math::is_finite(color.g) && Math::is_finite(color.b) && Math::is_finite(color.a);
		}
		case Variant::RECT2: {
			const Rect2 rect = p_value;
			return rect.position.is_finite() && rect.size.is_finite();
		}
		default:
			return false;
	}
}

Variant TweensGdInterpolation::interpolate(const Variant &p_from, const Variant &p_to, double p_weight, int64_t p_value_type) {
	switch (p_value_type) {
		case Variant::INT: {
			// Keep exact integer endpoints and saturate overshoot at Variant int64 limits.
			if (p_weight == 0.0 && p_from.get_type() == Variant::INT) {
				return p_from;
			}
			if (p_weight == 1.0 && p_to.get_type() == Variant::INT) {
				return p_to;
			}
			const double from = p_from;
			const double to = p_to;
			const double number = Math::round(from + (to - from) * p_weight);
			if (number >= 9223372036854775807.0) {
				return INT64_MAX;
			}
			if (number <= -9223372036854775808.0) {
				return INT64_MIN;
			}
			return int64_t(number);
		}
		case Variant::FLOAT: {
			const double from = p_from;
			const double to = p_to;
			return from + (to - from) * p_weight;
		}
		case Variant::QUATERNION:
			return Quaternion(p_from).normalized().slerp(Quaternion(p_to).normalized(), p_weight).normalized();
		case Variant::RECT2: {
			const Rect2 from = p_from;
			const Rect2 to = p_to;
			return Rect2(from.position.lerp(to.position, p_weight), from.size.lerp(to.size, p_weight));
		}
		case Variant::VECTOR2:
			return Vector2(p_from).lerp(p_to, p_weight);
		case Variant::VECTOR3:
			return Vector3(p_from).lerp(p_to, p_weight);
		case Variant::VECTOR4:
			return Vector4(p_from).lerp(p_to, p_weight);
		case Variant::COLOR:
			return Color(p_from).lerp(p_to, p_weight);
		default:
			return Variant();
	}
}

Variant TweensGdInterpolation::zero(int64_t p_value_type) {
	switch (p_value_type) {
		case Variant::INT:
			return int64_t(0);
		case Variant::FLOAT:
			return 0.0;
		case Variant::VECTOR2:
			return Vector2();
		case Variant::VECTOR3:
			return Vector3();
		case Variant::VECTOR4:
			return Vector4();
		case Variant::COLOR:
			return Color(0, 0, 0, 0);
		case Variant::QUATERNION:
			return Quaternion();
		case Variant::RECT2:
			return Rect2();
		default:
			return Variant();
	}
}

Variant TweensGdInterpolation::add(const Variant &p_value, const Variant &p_offset) {
	switch (p_value.get_type()) {
		case Variant::QUATERNION:
			if (p_offset.get_type() != Variant::QUATERNION) {
				return Variant();
			}
			return (Quaternion(p_value).normalized() * Quaternion(p_offset)).normalized();
		case Variant::RECT2: {
			if (p_offset.get_type() != Variant::RECT2) {
				return Variant();
			}
			const Rect2 value = p_value;
			const Rect2 offset = p_offset;
			return Rect2(value.position + offset.position, value.size + offset.size);
		}
		default: {
			Variant result;
			bool valid = false;
			Variant::evaluate(Variant::OP_ADD, p_value, p_offset, result, valid);
			return valid ? result : Variant();
		}
	}
}

Variant TweensGdInterpolation::remove(const Variant &p_value, const Variant &p_offset) {
	switch (p_value.get_type()) {
		case Variant::QUATERNION:
			if (p_offset.get_type() != Variant::QUATERNION) {
				return Variant();
			}
			return (Quaternion(p_value).normalized() * Quaternion(p_offset).inverse()).normalized();
		case Variant::RECT2: {
			if (p_offset.get_type() != Variant::RECT2) {
				return Variant();
			}
			const Rect2 value = p_value;
			const Rect2 offset = p_offset;
			return Rect2(value.position - offset.position, value.size - offset.size);
		}
		default: {
			Variant result;
			bool valid = false;
			Variant::evaluate(Variant::OP_SUBTRACT, p_value, p_offset, result, valid);
			return valid ? result : Variant();
		}
	}
}

Variant TweensGdInterpolation::read_property(Object *p_target, const NodePath &p_path) {
	if (p_target == nullptr) {
		return Variant();
	}
	const PackedStringArray parts = String(p_path).split(":");
	if (parts.is_empty() || parts[0].is_empty()) {
		return Variant();
	}
	const StringName property = parts[0];
	if (!is_listed(p_target, property)) {
		return Variant();
	}
	Variant value = p_target->get(property);
	for (int64_t index = 1; index < parts.size(); index++) {
		if (!has_component(value.get_type(), parts[index])) {
			return Variant();
		}
		bool valid = false;
		value = value.get_named(StringName(parts[index]), valid);
		if (!valid) {
			return Variant();
		}
	}
	return value;
}

bool TypedLerp::prepare(const Variant &p_from, const Variant &p_to, Variant::Type p_type) {
	type = p_type;
	switch (p_type) {
		case Variant::INT:
			exact_from = p_from.get_type() == Variant::INT;
			exact_to = p_to.get_type() == Variant::INT;
			from_integer = exact_from ? int64_t(p_from) : 0;
			to_integer = exact_to ? int64_t(p_to) : 0;
			[[fallthrough]];
		case Variant::FLOAT:
			from_scalar = p_from;
			to_scalar = p_to;
			return true;
		case Variant::VECTOR2: {
			const Vector2 from = p_from;
			const Vector2 to = p_to;
			from_components[0] = from.x;
			from_components[1] = from.y;
			to_components[0] = to.x;
			to_components[1] = to.y;
			return true;
		}
		case Variant::VECTOR3: {
			const Vector3 from = p_from;
			const Vector3 to = p_to;
			for (int index = 0; index < 3; index++) {
				from_components[index] = from[index];
				to_components[index] = to[index];
			}
			return true;
		}
		case Variant::VECTOR4: {
			const Vector4 from = p_from;
			const Vector4 to = p_to;
			for (int index = 0; index < 4; index++) {
				from_components[index] = from[index];
				to_components[index] = to[index];
			}
			return true;
		}
		case Variant::COLOR: {
			const Color from = p_from;
			const Color to = p_to;
			for (int index = 0; index < 4; index++) {
				from_components[index] = from[index];
				to_components[index] = to[index];
			}
			return true;
		}
		case Variant::RECT2: {
			const Rect2 from = p_from;
			const Rect2 to = p_to;
			from_components[0] = from.position.x;
			from_components[1] = from.position.y;
			from_components[2] = from.size.x;
			from_components[3] = from.size.y;
			to_components[0] = to.position.x;
			to_components[1] = to.position.y;
			to_components[2] = to.size.x;
			to_components[3] = to.size.y;
			return true;
		}
		case Variant::QUATERNION:
			from_rotation = Quaternion(p_from).normalized();
			to_rotation = Quaternion(p_to).normalized();
			return true;
		default:
			type = Variant::NIL;
			return false;
	}
}

bool TypedLerp::sample(double p_weight, Variant &r_value) const {
	const real_t *a = from_components;
	const real_t *b = to_components;
	switch (type) {
		case Variant::INT: {
			if (p_weight == 0.0 && exact_from) {
				r_value = Variant(from_integer);
				return true;
			}
			if (p_weight == 1.0 && exact_to) {
				r_value = Variant(to_integer);
				return true;
			}
			const double number = Math::round(from_scalar + (to_scalar - from_scalar) * p_weight);
			if (number >= 9223372036854775807.0) {
				r_value = Variant(INT64_MAX);
			} else if (number <= -9223372036854775808.0) {
				r_value = Variant(INT64_MIN);
			} else {
				r_value = Variant(int64_t(number));
			}
			return true;
		}
		case Variant::FLOAT: {
			const double result = from_scalar + (to_scalar - from_scalar) * p_weight;
			r_value = Variant(result);
			return Math::is_finite(result);
		}
		case Variant::VECTOR2: {
			const Vector2 result = Vector2(a[0], a[1]).lerp(Vector2(b[0], b[1]), p_weight);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::VECTOR3: {
			const Vector3 result = Vector3(a[0], a[1], a[2]).lerp(Vector3(b[0], b[1], b[2]), p_weight);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::VECTOR4: {
			const Vector4 result = Vector4(a[0], a[1], a[2], a[3]).lerp(Vector4(b[0], b[1], b[2], b[3]), p_weight);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::COLOR: {
			const Color result = Color(a[0], a[1], a[2], a[3]).lerp(Color(b[0], b[1], b[2], b[3]), p_weight);
			r_value = Variant(result);
			return Math::is_finite(result.r) && Math::is_finite(result.g) && Math::is_finite(result.b) && Math::is_finite(result.a);
		}
		case Variant::RECT2: {
			const Rect2 result(Vector2(a[0], a[1]).lerp(Vector2(b[0], b[1]), p_weight), Vector2(a[2], a[3]).lerp(Vector2(b[2], b[3]), p_weight));
			r_value = Variant(result);
			return result.position.is_finite() && result.size.is_finite();
		}
		case Variant::QUATERNION: {
			const Quaternion result = from_rotation.slerp(to_rotation, p_weight).normalized();
			r_value = Variant(result);
			return result.is_finite();
		}
		default:
			r_value = Variant();
			return false;
	}
}

void TweensGdInterpolation::clear_caches() {
	if (listed_properties != nullptr) {
		memdelete(listed_properties);
		listed_properties = nullptr;
	}
}

} // namespace godot
