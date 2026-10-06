// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Threading.Tasks;
using tweens.gd;
namespace testbed;

// Scene setup is in EmissionPulse.cs.
public sealed partial class EmissionPulse
{
    private async Task AnimateAsync()
    {
        await Group.Of([
            material.TweenEmission((0.12, 0.08, 0.3), Seconds, Stage, Cycle),
            material.TweenEmissionEnergyMultiplier(2, Seconds, Stage, Cycle),
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
