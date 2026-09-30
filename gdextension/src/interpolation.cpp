// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "interpolation.hpp"

#include <godot_cpp/classes/class_db_singleton.hpp>
#include <godot_cpp/classes/script.hpp>
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

// Property lists are costly to build. Class and script declarations appear in every instance's list, so they are
// cached; anything else, such as metadata or _get_property_list() entries, is checked against the instance itself.
HashMap<ClassKey, PropertyNames, ClassKeyHasher> *declared_properties = nullptr;

void add_names(PropertyNames &r_names, const TypedArray<Dictionary> &p_list) {
	for (int64_t index = 0; index < p_list.size(); index++) {
		const Dictionary entry = p_list[index];
		r_names.insert(entry["name"]);
	}
}

bool instance_lists(Object *p_target, const StringName &p_name) {
	const TypedArray<Dictionary> list = p_target->get_property_list();
	for (int64_t index = 0; index < list.size(); index++) {
		const Dictionary entry = list[index];
		if (StringName(entry["name"]) == p_name) {
			return true;
		}
	}
	return false;
}

bool is_listed(Object *p_target, const StringName &p_name) {
	if (declared_properties == nullptr) {
		declared_properties = memnew((HashMap<ClassKey, PropertyNames, ClassKeyHasher>));
	}
	Script *script = Object::cast_to<Script>(p_target->get_script().get_validated_object());
	const ClassKey key{ p_target->get_class(), script != nullptr ? uint64_t(script->get_instance_id()) : 0 };
	PropertyNames *names = declared_properties->getptr(key);
	if (names == nullptr) {
		PropertyNames declared;
		add_names(declared, ClassDBSingleton::get_singleton()->class_get_property_list(key.type));
		if (script != nullptr) {
			add_names(declared, script->get_script_property_list());
		}
		names = &declared_properties->insert(key, declared)->value;
	}
	return names->has(p_name) || instance_lists(p_target, p_name);
}

// The usual lerp, unless finite endpoints near opposite ends of the range overflow their difference.
// real_t components use the same arithmetic as Vector2::lerp() and friends otherwise.
template <typename T>
T lerp_scalar(T p_from, T p_to, T p_weight) {
	const T difference = p_to - p_from;
	if (Math::is_finite(difference)) {
		return p_from + difference * p_weight;
	}
	return p_from * (T(1) - p_weight) + p_to * p_weight;
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

Variant TweensGdInterpolation::coerce(const Variant &p_value, Variant::Type p_type) {
	if (p_value.get_type() != Variant::ARRAY) {
		return p_value;
	}
	const Array values = p_value;
	for (int64_t index = 0; index < values.size(); index++) {
		const Variant::Type type = values[index].get_type();
		if (type != Variant::INT && type != Variant::FLOAT) {
			return p_value;
		}
	}
	const int64_t size = values.size();
	const auto at = [&values](int64_t p_index) { return double(values[p_index]); };
	switch (p_type) {
		case Variant::VECTOR2:
			return size == 2 ? Variant(Vector2(at(0), at(1))) : p_value;
		case Variant::VECTOR3:
			return size == 3 ? Variant(Vector3(at(0), at(1), at(2))) : p_value;
		case Variant::VECTOR4:
			return size == 4 ? Variant(Vector4(at(0), at(1), at(2), at(3))) : p_value;
		case Variant::COLOR:
			if (size == 3) {
				return Color(at(0), at(1), at(2));
			}
			return size == 4 ? Variant(Color(at(0), at(1), at(2), at(3))) : p_value;
		default:
			return p_value;
	}
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
	TypedLerp lerp;
	Variant result;
	if (lerp.prepare(p_from, p_to, Variant::Type(p_value_type))) {
		lerp.sample(p_weight, result);
	}
	return result;
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
	real_t c[4];
	const auto components = [&](int p_count) {
		for (int index = 0; index < p_count; index++) {
			c[index] = lerp_scalar(from_components[index], to_components[index], real_t(p_weight));
		}
	};
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
			const double number = Math::round(lerp_scalar(from_scalar, to_scalar, p_weight));
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
			const double result = lerp_scalar(from_scalar, to_scalar, p_weight);
			r_value = Variant(result);
			return Math::is_finite(result);
		}
		case Variant::VECTOR2: {
			components(2);
			const Vector2 result(c[0], c[1]);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::VECTOR3: {
			components(3);
			const Vector3 result(c[0], c[1], c[2]);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::VECTOR4: {
			components(4);
			const Vector4 result(c[0], c[1], c[2], c[3]);
			r_value = Variant(result);
			return result.is_finite();
		}
		case Variant::COLOR: {
			components(4);
			const Color result(c[0], c[1], c[2], c[3]);
			r_value = Variant(result);
			return Math::is_finite(result.r) && Math::is_finite(result.g) && Math::is_finite(result.b) && Math::is_finite(result.a);
		}
		case Variant::RECT2: {
			components(4);
			const Rect2 result(c[0], c[1], c[2], c[3]);
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
	if (declared_properties != nullptr) {
		memdelete(declared_properties);
		declared_properties = nullptr;
	}
}

} // namespace godot
