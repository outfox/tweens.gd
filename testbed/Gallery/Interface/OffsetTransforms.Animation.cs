// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Threading.Tasks;
using tweens.gd;
namespace testbed;

// Scene setup is in OffsetTransforms.cs.
public sealed partial class OffsetTransforms
{
    private async Task AnimateAsync()
    {
        await Group.Of([
            featured.TweenOffsetTransformPosition((0, -22), Seconds, Cycle),
            featured.TweenOffsetTransformRotation(0.18, Seconds, Cycle),
            featured.TweenOffsetTransformScale(1.13, Seconds, Cycle),
        ]).End;
    }

    private void Cycle(TweenOptionsBuilder options)
    {
        options.Ease = DefaultEase;
        options.UsePingPong = true;
        options.Repeats = TweenOptions.Infinite;
        options.RepeatInterval = 0.25;
        options.PingPongInterval = 0.15;
    }
}
