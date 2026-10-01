// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
#include "awaiting.hpp"
#include "chain.hpp"
#include "common.hpp"
#include "definition.hpp"
#include "easing.hpp"
#include "fx.hpp"
#include "group.hpp"
#include "handle.hpp"
#include "interpolation.hpp"
#include "playback.hpp"
#include "playback_options.hpp"
#include "runner.hpp"
#include "scheduler.hpp"

#include <gdextension_interface.h>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/godot.hpp>

using namespace godot;

static void initialize_tweens_gd(ModuleInitializationLevel p_level) {
	if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
		return;
	}
	tweens::initialize();
	GDREGISTER_CLASS(TweensGdDefinition);
	GDREGISTER_CLASS(TweensGdPlaybackOptions);
	GDREGISTER_ABSTRACT_CLASS(TweensGdChain);
	GDREGISTER_CLASS(TweensGdScheduler);
	GDREGISTER_CLASS(TweensGdCancellation);
	// Created by the engine, never with new().
	GDREGISTER_ABSTRACT_CLASS(TweensGdHandle);
	GDREGISTER_ABSTRACT_CLASS(TweensGdGroup);
	GDREGISTER_ABSTRACT_CLASS(TweensGdRunner);
	GDREGISTER_ABSTRACT_CLASS(TweensGdPlayback);
	GDREGISTER_ABSTRACT_CLASS(TweensGdEasing);
	GDREGISTER_ABSTRACT_CLASS(TweensGdInterpolation);
	GDREGISTER_ABSTRACT_CLASS(TweensGdFX);
	GDREGISTER_INTERNAL_CLASS(TweensGdGroupWatcher);
	GDREGISTER_INTERNAL_CLASS(TweensGdAwaiting);
}

static void uninitialize_tweens_gd(ModuleInitializationLevel p_level) {
	if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
		return;
	}
	TweensGdInterpolation::clear_caches();
	tweens::uninitialize();
}

extern "C" {

GDExtensionBool GDE_EXPORT tweens_gd_library_init(GDExtensionInterfaceGetProcAddress p_get_proc_address,
		GDExtensionClassLibraryPtr p_library, GDExtensionInitialization *r_initialization) {
	GDExtensionBinding::InitObject init(p_get_proc_address, p_library, r_initialization);
	init.register_initializer(initialize_tweens_gd);
	init.register_terminator(uninitialize_tweens_gd);
	init.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);
	return init.init();
}
}
