---
title: Timing and easing
description: Duration, delay, offset, repeats, ping-pong, fill, and the easing members.
---

How long a tween plays, how often it repeats, and how it eases between its endpoints.

## Timing

See [timing and loops](/csharp/timing/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Duration` | `double` | `0` | Seconds per leg |
| `Delay` | `double` | `0` | Seconds before the first leg |
| `Offset` | `double` | `0` | Seconds to skip at the start of the first leg |
| `Repeats` | `int` | `0` | Cycles after the first; `TweenOptions.Infinite` repeats until cancelled |
| `UsePingPong` | `bool` | `false` | Play each cycle forward, then back |
| `PingPongInterval` | `double` | `0` | Seconds to wait before returning |
| `RepeatInterval` | `double` | `0` | Seconds between cycles |
| `Fill` | `FillMode` | `RetainFinalValue` | What the property shows during the delay and after the end |

## Easing

See [easing](/csharp/easing/).

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Ease` | `EaseType` | `Linear` | One of the 33 built-in eases |
| `EaseFunction` | `Func<float, float>?` | `null` | Custom ease that overrides `Ease` |
| `Curve` | `Curve?` | `null` | Godot curve that overrides `Ease`; set it or `EaseFunction`, not both |
