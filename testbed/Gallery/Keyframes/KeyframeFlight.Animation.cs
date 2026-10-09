// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in KeyframeFlight.cs.
public sealed partial class KeyframeFlight
{
    private async Task AnimateAsync()
    {
        var flight = new Tweens.Keyframes([
            Tweens.Keyframe.At(0, scale: 1, rotationDegrees: 0, modulate: Colors.Coral),
            Tweens.Keyframe.At(35, x: 20, scale: 1.5, rotationDegrees: -20, modulate: Colors.Gold),
            Tweens.Keyframe.At(65, x: 135, rotationDegrees: 20, interpolation: Out.Quad),
            Tweens.Keyframe.At(100, x: 180, scale: 1, rotationDegrees: 0, modulate: new Color(0, 0, 0, 0)),
        ], options: new TweenOptions { Duration = Seconds, PingPong = true, Repeats = TweenOptions.Infinite, RepeatInterval = 0.4 });

        var plays = new Group[flyers.Length];
        for (var i = 0; i < flyers.Length; i++) plays[i] = flyers[i].Tween(flight);
        await Task.WhenAll(plays[0].End, plays[1].End, plays[2].End);
    }
}
