---
title: Timing and easing
description: Duration, delay, offset, repeats, ping-pong, fill, and the easing members.
---

How long a tween plays, how often it repeats, and how it eases between its endpoints.

## Timing

See [timing and loops](/gdscript/timing/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `duration` | `float` | `0.0` | Seconds per leg |
| `delay` | `float` | `0.0` | Seconds before the first leg |
| `offset` | `float` | `0.0` | Seconds to skip at the start of the first leg |
| `repeats` | `int` | `0` | Cycles after the first; `Tweens.INFINITE` repeats until cancelled |
| `use_ping_pong` | `bool` | `false` | Play each cycle forward, then back |
| `ping_pong_interval` | `float` | `0.0` | Seconds to wait before returning |
| `repeat_interval` | `float` | `0.0` | Seconds between cycles |
| `fill` | `Tweens.Fill` | `RETAIN_FINAL_VALUE` | What the property shows during the delay and after the end |

## Easing

See [easing](/gdscript/easing/).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ease` | `int` | `LINEAR` | Composable In/Out flags or a legacy ease |
| `blend_type` | `BlendType` | `MAKIMA` | Method for joining mixed In/Out families |
| `blend` | `float` | `0.2` | Centered join width in [0, 1]; zero directly splices the halves |
| `ease_function` | `Callable` | `Callable()` | Maps normalized time to a weight; overrides `ease` |
| `curve` | `Curve` | `null` | Godot curve that overrides `ease`; set it or `ease_function`, not both |
