---
title: Effect factories
description: Punch, shake, and breathe functions for scalar, vector, and quaternion motion.
tableOfContents: true
---

`Tweens.FX` creates reusable functions of normalized progress. It leaves `Ease`,
`EaseFunction`, and `Curve` unchanged. Scalar factories return `Func<float, float>`
and can be assigned directly to `EaseFunction`:

```csharp
var recoil = new Tweens.Position2D
{
    By = new Vector2(9, 5),
    Duration = 0.4,
    EaseFunction = Tweens.FX.Punch(frequency: 6, decay: 2),
};
sprite.Tween(recoil);
```

This moves along the `By` vector and returns to the baseline. The factory's scalar
amplitude multiplies the tween's displacement; keep it at one when `By` already
specifies the strength.

| Factory | Shape | Defaults |
| --- | --- | --- |
| `Punch(frequency, amplitude, decay, phase, attack)` | Damped sine | `6, 1, 2, 0, 0` |
| `Shake(frequency, amplitude, seed, offset, decay, attack)` | Smooth seeded value noise with an envelope | `12, 1, 0, 0, 2, 0.1` |
| `Breathe(frequency, amplitude, phase)` | Raised cosine from zero to amplitude and back | `1, 1, 0` |
| `Decay(power)` | Envelope `(1-t)^power` | `2` |
| `AttackRelease(attack, decay)` | Envelope with separately proportioned attack and release | `0.1, 2` |

Frequency is **per tween duration**: cycles for Punch/Breathe, noise lattice
intervals for Shake. For a rate per second, multiply it by the duration when
creating the factory. Phase is in cycles; a Shake offset is a noise coordinate.
Neither is a time delay. Amplitudes can be signed; zero locks that axis.

Attack is the fraction of the duration spent rising from zero to full amplitude.
For example, `attack: 0.1` allocates 10% to attack and 90% to release. Positive
attack uses quintic ramps, with zero slope at their join; `decay` shapes the
release. At zero attack the envelope starts at one and follows `(1-t)^decay`.
Punch still starts at zero displacement when phase is zero. Shake with zero
attack can start displaced. Punch and Shake end exactly at zero. Breathe preserves
its phase: integral frequency with phase zero returns to zero, while fractional
cycles can end elsewhere.

### Independent axes and rotations

The `2D`, `3D`, and `Quaternion` suffixes select the output type:

| Family | 2D | 3D | Rotation |
| --- | --- | --- | --- |
| Punch | `Punch2D` | `Punch3D` | `PunchQuaternion` |
| Shake | `Shake2D` | `Shake3D` | `ShakeQuaternion` |
| Breathe | `Breathe2D` | `Breathe3D` | `BreatheQuaternion` |

These factories take `amplitude` first, followed by optional per-axis `frequency`.
Punch then accepts `decay`, per-axis `phase`, and `attack`; Shake accepts `seed`,
per-axis `offset`, `decay`, and `attack`; Breathe accepts per-axis `phase`.
Use Vector2 parameters for 2D and Vector3 parameters for 3D or Quaternion.
Default frequencies match the scalar versions. Default phases are zero; Shake's
default offsets are `(0, 101.37)` or `(0, 101.37, 203.71)` so the axes differ.
Equal frequency and offset settings deliberately use the same noise path.

Each vector component is `amplitude * envelope * signal` with its own frequency
and phase/offset. Vector functions return offsets, so use a linear callback tween
or a custom interpolator. They cannot go in the scalar `EaseFunction` slot:

```csharp
// Use a dedicated visual child so gameplay movement has its own transform.
var rest = sprite.Position;
var sample = Tweens.FX.Shake2D(
    amplitude: new Vector2(12, 6),
    frequency: new Vector2(10, 14),
    seed: 123, offset: new Vector2(0.3f, 100.7f));
sprite.Tween(new Tweens.Float
{
    From = 0, To = 1, Duration = 0.4,
    OnUpdate = (_, t) => sprite.Position = rest + sample(t),
    OnFinally = _ =>
    {
        if (GodotObject.IsInstanceValid(sprite)) sprite.Position = rest;
    },
});
```

Quaternion factories interpret their Vector3 amplitude as **rotation-vector
components in radians**, then convert the sampled vector to a normalized unit
quaternion. They do not interpolate or multiply the four quaternion components.
Apply local rotation as `baseline * sample(t)`; identity is the neutral offset:

```csharp
var rotation = Tweens.FX.PunchQuaternion(new Vector3(0.1f, 0.2f, 0));
mesh.Tween(new Tweens.Property<Node3D, Quaternion>(
    target => target.Quaternion,
    (target, value) => target.Quaternion = value,
    (from, _, t) => (from * rotation(t)).Normalized())
{
    Duration = 0.4,
});
```

Factories capture fixed settings, use no mutable random generator or native noise
resource, and reproduce a sample regardless of call order or frame rate. Reuse
functions between starts; create another with a different seed for another noise
pattern. Sampling clamps finite progress to `[0, 1]`. Invalid parameters or
nonfinite progress throw `ArgumentOutOfRangeException`; frequency must be
nonnegative, decay positive, and attack in `[0, 1)`.

These are functions, not playback controllers. Cancellation normally retains the
last sample; the callback example explicitly clears its visual offset. Cancel a
previous effect on that child before starting another. `By` retains its ordinary
repeat accumulation semantics, so use a callback/custom-property tween for
repeated return-to-rest effects. `Skew`/`Weks` warp the entire effect when used
as easing; leave the callback driver linear to preserve the factory's timing.
