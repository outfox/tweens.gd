// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using tweens.gd;
namespace testbed;

// Scene setup is in JellyButton.cs.
public sealed partial class JellyButton
{
    private const int Points = 10;

    private async Task Pop()
    {
        await Task.WhenAll([Squish(), Flash(), .. shards.Select(Throw), FloatBonus(), AddToScore()]);
    }

    /// <summary>Flattens the button, then springs it back only after successful completion.</summary>
    private async Task Squish()
    {
        squish?.Cancel();
        squish = Flatten();
        if (await Group.Of(squish).End != Reason.Completed) return;
        squish = SpringBack();
        await Group.Of(squish).End;
    }

    private async Task Flash()
    {
        var flash = 0.6 * Tempo;
        await Group.Of([
            shaker.TweenPosition((9, 5), 0.4 * Tempo, options =>
            {
                options.From = Vector2.Zero;
                options.EaseFunction = Tweens.FX.Punch(frequency: 3.0f);
            }),
            burst.TweenScale(2.1, flash, options =>
            {
                options.From = new Vector2(0.7f, 0.7f);
                options.Ease = Out.Quart;
            }),
            burst.TweenModulateAlpha(0, flash, options =>
            {
                options.From = 1;
                options.Ease = In.Quad;
            }),
        ]).End;
    }

    private TweenInstance Flatten() =>
        button.TweenScale((1.3, 0.7), 0.07 * Tempo, Out.Quad);

    private TweenInstance SpringBack() =>
        button.TweenScale(Vector2.One, 0.8 * Tempo, EaseType.ElasticOut);

    private async Task Throw(Polygon2D shard)
    {
        var flight = 0.9 * Tempo;
        var angle = (float)(random.NextDouble() * MathF.PI * 1.2 + MathF.PI * 0.9);
        var reach = 90 + (float)random.NextDouble() * 110;
        var landing = new Vector2(MathF.Cos(angle) * reach, 70 + (float)random.NextDouble() * 20);
        var spin = angle + (float)(random.NextDouble() * 12 - 6);
        var lift = 1 + (float)random.NextDouble();

        shard.Position = Vector2.Zero;
        shard.Rotation = angle;
        shard.Modulate = Colors.White;
        await Group.Of([
            shard.TweenPositionX(landing.X, flight, Out.Quart),
            shard.TweenPositionY(landing.Y, flight, options =>
            {
                // This curve dips below zero before landing at 1, forming a launch arc.
                options.EaseFunction = progress => (2 + lift) * progress * progress - (1 + lift) * progress;
            }),
            shard.TweenRotation(spin, flight, Out.Quad),
            shard.TweenModulateAlpha(0, 0.3 * Tempo, options => options.Delay = 0.6 * Tempo),
        ]).End;
    }

    private async Task FloatBonus()
    {
        bonus.Modulate = Colors.White;
        await Group.Of([
            bonus.TweenPositionY(-95, 0.7 * Tempo, options =>
            {
                options.From = -50;
                options.Ease = Out.Quart;
            }),
            bonus.TweenModulateAlpha(0, 0.3 * Tempo, options => options.Delay = 0.4 * Tempo),
        ]).End;
    }

    /// <summary>Rolls the displayed score up from whatever it currently shows.</summary>
    private async Task AddToScore()
    {
        total += Points;
        await Group.Of([
            Stage.TweenFloat(total, 0.5 * Tempo, options =>
            {
                options.From = shown;
                options.Ease = Out.Cubic;
                options.OnUpdate = (_, value) =>
                {
                    shown = value;
                    score.Text = $"{value:000}";
                };
            }),
            score.TweenScale(Vector2.One, 0.6 * Tempo, options =>
            {
                options.From = new Vector2(1.45f, 1.45f);
                options.Ease = EaseType.ElasticOut;
            }),
        ]).End;
    }

}
