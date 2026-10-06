---
title: Effect factories
description: Punch, shake, and breathe functions for scalar, vector, and quaternion motion.
tableOfContents: true
---

`Tweens.FX` creates reusable Callables of normalized progress. Existing `ease`,
`ease_function`, and `curve` configuration stays unchanged. Scalar factories can
be assigned directly to `ease_function`:

```gdscript
var recoil := Tweens.position_2d()
recoil.by_value = [9, 5]
recoil.duration = 0.4
recoil.ease_function = Tweens.FX.punch(6.0)
Tweens.play(sprite, recoil)
```

This moves along the `by_value` vector and returns to the baseline. The factory's
scalar amplitude multiplies the tween's displacement; keep it at one when
`by_value` supplies the strength.

| Factory | Shape | Defaults |
| --- | --- | --- |
| `punch(frequency, amplitude, decay, phase, attack)` | Damped sine | `6, 1, 2, 0, 0` |
| `shake(frequency, amplitude, seed, offset, decay, attack)` | Smooth seeded value noise with an envelope | `12, 1, 0, 0, 2, 0.1` |
| `breathe(frequency, amplitude, phase)` | Raised cosine from zero to amplitude and back | `1, 1, 0` |
| `decay(power)` | Envelope `(1-t)^power` | `2` |
| `attack_release(attack, decay)` | Envelope with separately proportioned attack and release | `0.1, 2` |

Frequency is **per tween duration**: cycles for punch/breathe, noise lattice
intervals for shake. For a rate per second, multiply it by the duration when
creating the factory. Phase is in cycles; a shake offset is a noise coordinate.
Neither is a time delay. Amplitudes can be signed; zero locks that axis.

Attack is the fraction of the duration spent rising from zero to full amplitude.
For example, `attack = 0.1` allocates 10% to attack and 90% to release. Positive
attack uses quintic ramps, with zero slope at their join; the decay exponent shapes
the release. At zero attack the envelope starts at one and follows `(1-t)^decay`.
Punch still starts at zero displacement when phase is zero. Shake with zero
attack can start displaced. Punch and shake end exactly at zero. Breathe preserves
its phase: integral frequency with phase zero returns to zero, while fractional
cycles can end elsewhere.

### Independent axes and rotations

| Family | 2D | 3D | Rotation |
| --- | --- | --- | --- |
| Punch | `punch_2d` | `punch_3d` | `punch_quaternion` |
| Shake | `shake_2d` | `shake_3d` | `shake_quaternion` |
| Breathe | `breathe_2d` | `breathe_3d` | `breathe_quaternion` |

These factories take `amplitude` first, followed by optional per-axis `frequency`.
Punch then accepts `decay`, per-axis `phase`, and `attack`; shake accepts `seed`,
per-axis `offset`, `decay`, and `attack`; breathe accepts per-axis `phase`.
Use Vector2 parameters for 2D and Vector3 parameters for 3D or quaternion.
Default frequencies match the scalar versions. Default phases are zero; shake's
default offsets are `(0, 101.37)` or `(0, 101.37, 203.71)` so the axes differ.
Equal frequency and offset settings deliberately use the same noise path.

Each vector component is `amplitude * envelope * signal` with its own frequency
and phase/offset. Vector Callables return offsets, so use a linear callback tween
or a custom interpolator. They cannot go in the scalar `ease_function` slot:

```gdscript
# Use a dedicated visual child so gameplay movement has its own transform.
var rest := sprite.position
var sample := Tweens.FX.shake_2d(
    Vector2(12, 6), Vector2(10, 14), 123, Vector2(0.3, 100.7))
var driver := Tweens.value(0.0, 1.0, 0.4)
driver.on_update = func(_handle, t): sprite.position = rest + sample.call(t)
driver.on_finally = func(_handle):
    if is_instance_valid(sprite): sprite.position = rest
Tweens.play(sprite, driver)
```

Quaternion factories interpret their Vector3 amplitude as **rotation-vector
components in radians**, then convert the sampled vector to a normalized unit
quaternion. They do not interpolate or multiply the four quaternion components.
Apply local rotation as `baseline * sample.call(t)`; identity is the neutral offset:

```gdscript
var rotation := Tweens.FX.punch_quaternion(Vector3(0.1, 0.2, 0))
var turn := Tweens.custom(
    func(target): return target.quaternion,
    func(target, value): target.quaternion = value,
    func(from, _to, t): return (from * rotation.call(t)).normalized())
turn.duration = 0.4
Tweens.play(node, turn)
```

Factories capture fixed settings, use no mutable random generator or native noise
resource, and reproduce a sample regardless of call order or frame rate. Reuse
Callables between starts; create another with a different seed for another noise
pattern. Sampling clamps finite progress to `[0, 1]`; nonfinite progress produces
nonfinite output, which the tween runtime detects when used as easing or an
interpolator. Invalid configuration reports an error and returns an empty Callable.
Frequency must be nonnegative, decay positive, attack in `[0, 1)`, and seeds signed
32-bit integers. Frequency, amplitude, and phase/offset must fit finite 32-bit floats
to match C# and Godot vector storage. Check `is_valid()` if creating factories from
untrusted configuration; an empty `ease_function` otherwise means built-in easing.

These are functions, not playback controllers. Cancellation normally retains the
last sample; the callback example explicitly clears its visual offset. Cancel a
previous effect on that child before starting another. `by_value` retains its
ordinary repeat accumulation semantics, so use a callback/custom-property tween
for repeated return-to-rest effects. Custom effect profiles retain their authored pacing.
