// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in CombinedTransforms.cs.
public sealed partial class CombinedTransforms
{
    private async Task AnimateAsync()
    {
        var tweens = new List<TweenInstance>();
        foreach (var target in new[] { shape, shadow })
        {
            tweens.Add(target.TweenSkew(0.5, Seconds, Cycle));
            tweens.Add(target.TweenRotation(Mathf.Pi, Seconds, Cycle));
            tweens.Add(target.TweenScaleX(1.8, Seconds, Cycle));
            tweens.Add(target.TweenScaleY(0.6, Seconds, Cycle));
        }
        await Group.Of([.. tweens]).End;
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
