# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
class_name TweensGdTypes
extends RefCounted

const INFINITE := -1
enum Fill { NONE = 0, APPLY_FROM_DURING_DELAY = 1, RETAIN_FINAL_VALUE = 2, BOTH = 3 }
enum Process { PROCESS, PHYSICS }
enum Pause { BOUND, SCENE_TREE, ALWAYS }
enum State { DELAYED, PLAYING, INTERVAL, COMPLETED, CANCELLED, FAULTED }
enum Reason { COMPLETED, CANCELLED, TARGET_FREED, OWNER_EXITED, RUNNER_DISPOSED, FAILED, WAIT_CANCELLED }
enum BlendType { HERMITE, SMOOTH_STEP, LINEAR }
enum Ease {
	LINEAR = 0,
	SINE_IN = 10, SINE_OUT, SINE_IN_OUT,
	QUAD_IN = 20, QUAD_OUT, QUAD_IN_OUT,
	CUBIC_IN = 30, CUBIC_OUT, CUBIC_IN_OUT,
	QUART_IN = 40, QUART_OUT, QUART_IN_OUT,
	QUINT_IN = 50, QUINT_OUT, QUINT_IN_OUT,
	EXPO_IN = 60, EXPO_OUT, EXPO_IN_OUT,
	CIRC_IN = 70, CIRC_OUT, CIRC_IN_OUT,
	BACK_IN = 80, BACK_OUT, BACK_IN_OUT,
	ELASTIC_IN = 90, ELASTIC_OUT, ELASTIC_IN_OUT,
	BOUNCE_IN = 100, BOUNCE_OUT, BOUNCE_IN_OUT,
	SMOOTH_STEP = 110, SMOOTHER_STEP = 120,
}

## Select at most one curve per leg. A missing leg leaves the other curve unchanged.
enum In {
	NONE = 0,
	LINEAR = 1 << 8,
	SINE = 1 << 9,
	QUAD = 1 << 10,
	CUBIC = 1 << 11,
	QUART = 1 << 12,
	QUINT = 1 << 13,
	EXPO = 1 << 14,
	CIRC = 1 << 15,
	BACK = 1 << 16,
	ELASTIC = 1 << 17,
	BOUNCE = 1 << 18,
	SMOOTH_STEP = 1 << 19,
	SMOOTHER_STEP = 1 << 20,
}

## Select at most one curve per leg. A missing leg leaves the other curve unchanged.
enum Out {
	NONE = 0,
	LINEAR = 1 << 21,
	SINE = 1 << 22,
	QUAD = 1 << 23,
	CUBIC = 1 << 24,
	QUART = 1 << 25,
	QUINT = 1 << 26,
	EXPO = 1 << 27,
	CIRC = 1 << 28,
	BACK = 1 << 29,
	ELASTIC = 1 << 30,
	BOUNCE = 1 << 31,
	SMOOTH_STEP = 1 << 32,
	SMOOTHER_STEP = 1 << 33,
}

## Matching half-duration legs. Reproduces the conventional InOut curve exactly.
enum InOut {
	LINEAR = In.LINEAR | Out.LINEAR,
	SINE = In.SINE | Out.SINE,
	QUAD = In.QUAD | Out.QUAD,
	CUBIC = In.CUBIC | Out.CUBIC,
	QUART = In.QUART | Out.QUART,
	QUINT = In.QUINT | Out.QUINT,
	EXPO = In.EXPO | Out.EXPO,
	CIRC = In.CIRC | Out.CIRC,
	BACK = In.BACK | Out.BACK,
	ELASTIC = In.ELASTIC | Out.ELASTIC,
	BOUNCE = In.BOUNCE | Out.BOUNCE,
	SMOOTH_STEP = In.SMOOTH_STEP | Out.SMOOTH_STEP,
	SMOOTHER_STEP = In.SMOOTHER_STEP | Out.SMOOTHER_STEP,
}
