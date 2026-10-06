---
title: Timing and easing
tableOfContents: true
description: Duration, delay, offset, repeats, ping-pong, fill, and the easing members.
---

How long a tween plays, how often it repeats, and how it eases between its endpoints.

## Timing

All times are in seconds, as GDScript `float` values.

| Field | Default | Meaning |
| --- | --- | --- |
| `duration` | `0.0` | Seconds per leg. Zero completes on the first eligible update. |
| `factor_duration`, `delta_duration` | `1.0`, `0.0` | Scale and shift `duration` for [variations](/gdscript/variations/): the tween lasts `factor_duration × duration + delta_duration`. |
| `delay` | `0.0` | Signed gap before the first leg; negative values pre-roll. |
| `factor_delay`, `delta_delay` | `1.0`, `0.0` | Scale and shift `delay` for [variations](/gdscript/variations/), such as a stagger: the tween waits `factor_delay × delay + delta_delay`. |
| `repeats` | `0` | Cycles after the first. `Tweens.INFINITE` (-1) repeats until cancelled. An infinite cycle that takes zero time is rejected. |
| `ping_pong` | `false` | A forward and a backward leg form one cycle. |
| `ping_pong_interval` | `0.0` | Wait at the far endpoint before returning. |
| `repeat_interval` | `0.0` | Wait between cycles, never after the last one. |
| `offset` | `0.0` | Start this many seconds into the first forward leg, between zero and `duration`. The delay still comes first. |
| `fill` | `RETAIN_FINAL_VALUE` | Values during delay and on natural completion; see [fill and restoration](#fill-and-restoration). |

The helper factories set `duration` and `delay` from their second and fourth
arguments, as in `Tweens.position_2d_y(to, 0.8, InOut.SINE, 0.2)`.
Set the other fields on the returned definition, or vary a shared one with
`with_*()` copies such as `with_repeats(2)`.

For example, `duration = 0.5`, `ping_pong = true`, `ping_pong_interval = 0.2`,
`repeat_interval = 0.3`, and `repeats = 1` take 2.7 seconds: two 1.2-second cycles
and one 0.3-second gap.

Non-finite times and negative durations, intervals, and offsets reject the start, and `Tweens.play()` returns an
already-settled handle whose `end` reports `Tweens.Reason.FAILED` when awaited. A long frame
advances to the correct phase, even across several cycles, without losing time at
boundaries, but it doesn't replay the callbacks of the cycles it skipped.

## Fill and restoration

`fill` decides what the property shows before the tween starts and after it ends.

| `Tweens.Fill` | During the initial delay | On natural completion |
| --- | --- | --- |
| `RETAIN_FINAL_VALUE` (default) | Leave the property alone | Keep the final value |
| `APPLY_FROM_DURING_DELAY` | Apply the from value | Restore the captured initial value |
| `BOTH` | Apply the from value | Keep the final value |
| `NONE` | Leave the property alone | Restore the captured initial value |

Use `BOTH` for staggered entrances, so items that are still waiting show their
`from_value` instead of their final one.

A ping-pong tween ends at its starting endpoint, so that endpoint is its final
value. Cancelling always keeps the latest value, whatever the fill mode. Shader
restoration also preserves whether an explicit override existed (see
[shader uniforms](/gdscript/materials/)).

## Process and physics

These fields belong to `TweensGdPlaybackOptions`, supplied at the start call. Definitions contain motion settings only.

`process_mode` defaults to `Tweens.Process.PROCESS`. Choose
`Tweens.Process.PHYSICS` to update with physics instead. The automatic runner uses
process and physics priority `1000`, so it runs after nodes with default priority.

`use_unscaled_time = true` ignores `Engine.time_scale`. Process updates then use
monotonic engine ticks, and physics updates use `1 / Engine.physics_ticks_per_second`
per tick, which is simulation time rather than wall-clock time during catch-up.
Neither field changes pause or ownership rules.

## Progress

A handle's `progress` is the current leg's position from 0 to 1, before easing. It
runs backwards during a ping-pong return, and it isn't the fraction of all cycles
completed.

Signed delays place Chain entries relative to the preceding entry's end; see [Chains](/gdscript/sequences/).

## Easing

See the [easing playground](/easings/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ease` | `int` | `LINEAR` | Composable In/Out flags or a legacy ease |
| `blend_type` | `BlendType` | `MAKIMA` | Method for joining mixed In/Out families |
| `blend` | `float` | `0.2` | Centered join width in [0, 1]; zero directly splices the halves |
| `ease_function` | `Callable` | `Callable()` | Maps normalized time to a weight; overrides `ease` |
| `curve` | `Curve` | `null` | Godot curve that overrides `ease`; set it or `ease_function`, not both |
