// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Threading.Tasks;
using tweens.gd;
namespace testbed;

// Scene setup is in ParticleStream.cs.
public sealed partial class ParticleStream
{
    private async Task AnimateAsync()
    {
        await Group.Of([
            particles.TweenSpread(75, Seconds, Cycle),
            particles.TweenGravity((15, -55), Seconds, Cycle),
            particles.TweenColor(Palette.Amber, Seconds, Cycle),
            particles.TweenPositionY(-30, Seconds, Cycle),
        ]).End;
    }

    private void Cycle(TweenOptionsBuilder options)
    {
        options.Ease = DefaultEase;
        options.PingPong = true;
        options.Repeats = TweenOptions.Infinite;
        options.RepeatInterval = 0.25;
        options.PingPongInterval = 0.15;
    }
}
