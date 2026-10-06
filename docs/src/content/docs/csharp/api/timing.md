---
title: Timing and easing
tableOfContents: true
description: Duration, delay, offset, repeats, ping-pong, fill, and the easing members.
---

How long a tween plays, how often it repeats, and how it eases between its endpoints.

## Timing

`Duration` is a readonly record struct in `tweens.gd` that stores `double`
seconds. It accepts `float` and `double` seconds or a `TimeSpan` implicitly, so
existing numeric calls still work. Read its `Seconds` property or convert it
implicitly to `double` to get seconds back. Its default value is zero seconds.
See [syntax sugar](/csharp/syntax-sugar/#easing-and-delay) for examples.

| Member | Default | Meaning |
| --- | --- | --- |
| `Duration` | `0` | Seconds per leg. Zero completes on the first eligible update. |
| `FactorDuration`, `DeltaDuration` | `1`, `0` | Scale and shift `Duration` for [variations](/csharp/variations/): the tween lasts `FactorDuration × Duration + DeltaDuration`. |
| `Delay` | `0` | Signed gap before the first leg; negative values pre-roll. |
| `FactorDelay`, `DeltaDelay` | `1`, `0` | Scale and shift `Delay` for [variations](/csharp/variations/), such as a stagger: the tween waits `FactorDelay × Delay + DeltaDelay`. |
| `Repeats` | `0` | Cycles after the first. `TweenOptions.Infinite` (-1) repeats until cancelled. An infinite cycle that takes zero time is rejected. |
| `PingPong` | `false` | A forward and a backward leg form one cycle. |
| `PingPongInterval` | `0` | Wait at the far endpoint before returning. |
| `RepeatInterval` | `0` | Wait between cycles, never after the last one. |
| `Offset` | `0` | Start this many seconds into the first forward leg, within `[0, Duration]`. The delay still comes first. |
| `Fill` | `RetainFinalValue` | Values during delay and on natural completion; see [fill and restoration](#fill-and-restoration). |

For example, `Duration = 0.5`, `PingPong = true`, `PingPongInterval = 0.2`,
`RepeatInterval = 0.3`, and `Repeats = 1` take 2.7 seconds: two 1.2-second cycles
and one 0.3-second gap.

Non-finite times and negative durations, intervals, and offsets are rejected. A long frame advances to the correct
phase, even across several cycles, without losing time at boundaries, but it
doesn't replay the callbacks of the cycles it skipped.

## Fill and restoration

`Fill` decides what the property shows before the tween starts and after it ends.

| `FillMode` | During the initial delay | On natural completion |
| --- | --- | --- |
| `RetainFinalValue` (default) | Leave the property alone | Keep the final value |
| `ApplyFromDuringDelay` | Apply `From` | Restore the captured initial value |
| `Both` | Apply `From` | Keep the final value |
| `None` | Leave the property alone | Restore the captured initial value |

Use `Both` for staggered entrances, so items that are still waiting show their
`From` value instead of their final one.

A ping-pong tween ends at its starting endpoint, so that endpoint is its final
value. Cancelling always keeps the latest value, whatever the fill mode. Shader
restoration also preserves whether an explicit override existed (see
[shader uniforms](/csharp/materials/)).

## Process and physics

These fields belong to `PlaybackOptions`, supplied at the start call. Definitions contain motion settings only.

`ProcessMode` defaults to `TweenProcessMode.Process`. Choose
`TweenProcessMode.Physics` to update with physics instead. The automatic runner
uses process and physics priority `1000`, so it runs after nodes with default
priority.

`UseUnscaledTime = true` ignores `Engine.TimeScale`. Process updates then use
monotonic engine ticks, and physics updates use `1 / PhysicsTicksPerSecond` per
tick, which is simulation time rather than wall-clock time during catch-up.
Neither option changes pause or ownership rules.

## Progress

A handle's `Progress` is the current leg's position from 0 to 1, before easing. It
runs backwards during a ping-pong return, and it isn't the fraction of all cycles
completed.

Signed delays place Chain entries relative to the preceding entry's end; see [Chains](/csharp/sequences/).

## Easing

See the [easing playground](/easings/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Ease` | `EaseType` | `Linear` | Composable In/Out flags or a legacy ease |
| `BlendType` | `BlendType` | `Makima` | Method for joining mixed In/Out families |
| `Blend` | `double` | `0.2` | Centered join width in [0, 1]; zero directly splices the halves |
| `EaseFunction` | `Func<float, float>?` | `null` | Custom ease that overrides `Ease` |
| `Curve` | `Curve?` | `null` | Godot curve that overrides `Ease`; set it or `EaseFunction`, not both |
