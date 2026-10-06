---
title: Timing and easing
description: Duration, delay, offset, repeats, ping-pong, fill, and the easing members.
---

How long a tween plays, how often it repeats, and how it eases between its endpoints.

## Timing

See [timing and loops](/csharp/timing/).

`Duration` is a readonly record struct in `tweens.gd` that stores `double`
seconds. It accepts `float` and `double` seconds or a `TimeSpan` implicitly, so
existing numeric calls still work. Read its `Seconds` property or convert it
implicitly to `double` to get seconds back. Its default value is zero seconds.
See [syntax sugar](/csharp/syntax-sugar/#easing-and-delay) for examples.

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Duration` | `Duration` | `0` | Seconds per leg |
| `Delay` | `Duration` | `0` | Signed gap before the first leg; negative values pre-roll |
| `Offset` | `Duration` | `0` | Seconds to skip at the start of the first leg |
| `Repeats` | `int` | `0` | Cycles after the first; `TweenOptions.Infinite` repeats until cancelled |
| `UsePingPong` | `bool` | `false` | Play each cycle forward, then back |
| `PingPongInterval` | `Duration` | `0` | Seconds to wait before returning |
| `RepeatInterval` | `Duration` | `0` | Seconds between cycles |
| `Fill` | `FillMode` | `RetainFinalValue` | What the property shows during the delay and after the end |

## Easing

See the [easing playground](/easings/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Ease` | `EaseType` | `Linear` | Composable In/Out flags or a legacy ease |
| `BlendType` | `BlendType` | `Makima` | Method for joining mixed In/Out families |
| `Blend` | `double` | `0.2` | Centered join width in [0, 1]; zero directly splices the halves |
| `EaseFunction` | `Func<float, float>?` | `null` | Custom ease that overrides `Ease` |
| `Curve` | `Curve?` | `null` | Godot curve that overrides `Ease`; set it or `EaseFunction`, not both |
