// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in CameraPan.cs.
public sealed partial class CameraPan
{
    private async Task AnimateAsync()
    {
        const double pulse = 1.2;
        await Group.Of([
            camera.TweenZoom((1.8, 1.8), Seconds, Cycle),
            camera.TweenOffset((90, 25), Seconds, Cycle),
            beacon.TweenScale(2.4, pulse, options =>
            {
                options.From = Vector2.One;
                options.Ease = Out.Quart;
                options.Repeats = TweenOptions.Infinite;
            }),
            beacon.TweenModulateAlpha(0, pulse, options =>
            {
                options.From = 1;
                options.Ease = In.Quad;
                options.Repeats = TweenOptions.Infinite;
            }),
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
