// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tweens.gd;

[Flags]
public enum FillMode
{
    None = 0,
    ApplyFromDuringDelay = 1,
    RetainFinalValue = 2,
    Both = ApplyFromDuringDelay | RetainFinalValue,
}

public enum TweenProcessMode { Process, Physics }
public enum TweenPauseMode { Bound, SceneTree, Always }
public enum TweenState { Delayed, Playing, Interval, Completed, Cancelled, Faulted }
public enum Reason { Completed, Cancelled, TargetFreed, OwnerExited, RunnerDisposed }

/// <summary>Immutable timing configuration. Use with expressions to vary a reusable value.</summary>
public readonly record struct TweenOptions
{
    /// <summary>A <see cref="Repeats"/> value that repeats until cancelled.</summary>
    public const int Infinite = -1;

    public Duration Duration { get; init; }
    private readonly double? factorDuration;
    /// <summary>Scales <see cref="Duration"/> at start: the tween lasts FactorDuration * Duration + DeltaDuration.</summary>
    public double FactorDuration { get => factorDuration ?? 1; init => factorDuration = value == 1 ? null : value; }
    /// <summary>Added to <see cref="Duration"/> at start, after <see cref="FactorDuration"/>.</summary>
    public Duration DeltaDuration { get; init; }
    public Duration Delay { get; init; }
    private readonly double? factorDelay;
    /// <summary>Scales <see cref="Delay"/> at start: the tween waits FactorDelay * Delay + DeltaDelay.</summary>
    public double FactorDelay { get => factorDelay ?? 1; init => factorDelay = value == 1 ? null : value; }
    /// <summary>Added to <see cref="Delay"/> at start, after <see cref="FactorDelay"/>.</summary>
    public Duration DeltaDelay { get; init; }
    public Duration PingPongInterval { get; init; }
    public Duration RepeatInterval { get; init; }
    public Duration Offset { get; init; }
    /// <summary>Cycles after the first, or <see cref="Infinite"/>. A ping-pong cycle includes both legs.</summary>
    public int Repeats { get; init; }
    public bool UsePingPong { get; init; }
    public bool UseUnscaledTime { get; init; }
    private readonly FillMode fill;
    // Encode the default so default(TweenOptions) and new TweenOptions() behave identically.
    public FillMode Fill { get => fill ^ FillMode.RetainFinalValue; init => fill = value ^ FillMode.RetainFinalValue; }
    /// <summary>Combine one In and one Out with |, use an InOut pair, or select a single curve. Legacy EaseType names retain their original shapes.</summary>
    public EaseType Ease { get; init; }
    /// <summary>How mixed In/Out legs join. Defaults to Makima; matching families keep their conventional shape.</summary>
    public BlendType BlendType { get; init; }
    private readonly double? blend;
    /// <summary>Centered transition width in [0, 1]. Defaults to 0.2 (40%–60%); zero directly splices the halves.</summary>
    public double Blend { get => blend ?? 0.2; init => blend = value == 0.2 ? null : value; }
    private readonly double? skew;
    /// <summary>Positive finite exponent applied to forward normalized time before easing. Defaults to 1 (identity).</summary>
    public double Skew { get => skew ?? 1; init => skew = value == 1 ? null : value; }
    private readonly double? weks;
    /// <summary>Positive finite exponent applied to descending ping-pong progress before easing. Defaults to 1, independently of Skew.</summary>
    public double Weks { get => weks ?? 1; init => weks = value == 1 ? null : value; }
    public Func<float, float>? EaseFunction { get; init; }
    public Godot.Curve? Curve { get; init; }
    public TweenProcessMode ProcessMode { get; init; }
    public TweenPauseMode PauseMode { get; init; }
    public bool SuppressCallbacksWhenTargetInvalid { get; init; }

    internal void CopyTo(TweenOptionsBuilder target)
    {
        target.Duration = Duration;
        target.FactorDuration = FactorDuration;
        target.DeltaDuration = DeltaDuration;
        target.Delay = Delay;
        target.FactorDelay = FactorDelay;
        target.DeltaDelay = DeltaDelay;
        target.PingPongInterval = PingPongInterval;
        target.RepeatInterval = RepeatInterval;
        target.Offset = Offset;
        target.Repeats = Repeats;
        target.UsePingPong = UsePingPong;
        target.UseUnscaledTime = UseUnscaledTime;
        target.Fill = Fill;
        target.Ease = Ease;
        target.BlendType = BlendType;
        target.Blend = Blend;
        target.Skew = Skew;
        target.Weks = Weks;
        target.EaseFunction = EaseFunction;
        target.Curve = Curve;
        target.ProcessMode = ProcessMode;
        target.PauseMode = PauseMode;
        target.SuppressCallbacksWhenTargetInvalid = SuppressCallbacksWhenTargetInvalid;
    }
}

/// <summary>Mutable options used by convenience-method configurators and custom class definitions.</summary>
public class TweenOptionsBuilder
{
    public const int Infinite = TweenOptions.Infinite;

    public Duration Duration { get; set; }
    /// <inheritdoc cref="TweenOptions.FactorDuration"/>
    public double FactorDuration { get; set; } = 1;
    /// <inheritdoc cref="TweenOptions.DeltaDuration"/>
    public Duration DeltaDuration { get; set; }
    public Duration Delay { get; set; }
    /// <inheritdoc cref="TweenOptions.FactorDelay"/>
    public double FactorDelay { get; set; } = 1;
    /// <inheritdoc cref="TweenOptions.DeltaDelay"/>
    public Duration DeltaDelay { get; set; }
    public Duration PingPongInterval { get; set; }
    public Duration RepeatInterval { get; set; }
    public Duration Offset { get; set; }
    public int Repeats { get; set; }
    public bool UsePingPong { get; set; }
    public bool UseUnscaledTime { get; set; }
    public FillMode Fill { get; set; } = FillMode.RetainFinalValue;
    public EaseType Ease { get; set; }
    public BlendType BlendType { get; set; }
    public double Blend { get; set; } = 0.2;
    /// <inheritdoc cref="TweenOptions.Skew"/>
    public double Skew { get; set; } = 1;
    /// <inheritdoc cref="TweenOptions.Weks"/>
    public double Weks { get; set; } = 1;
    public Func<float, float>? EaseFunction { get; set; }
    public Godot.Curve? Curve { get; set; }
    public TweenProcessMode ProcessMode { get; set; }
    public TweenPauseMode PauseMode { get; set; }
    public bool SuppressCallbacksWhenTargetInvalid { get; set; }

    internal TweenOptions ToOptions() => new()
    {
        Duration = Duration,
        FactorDuration = FactorDuration,
        DeltaDuration = DeltaDuration,
        Delay = Delay,
        FactorDelay = FactorDelay,
        DeltaDelay = DeltaDelay,
        PingPongInterval = PingPongInterval,
        RepeatInterval = RepeatInterval,
        Offset = Offset,
        Repeats = Repeats,
        UsePingPong = UsePingPong,
        UseUnscaledTime = UseUnscaledTime,
        Fill = Fill,
        Ease = Ease,
        BlendType = BlendType,
        Blend = Blend,
        Skew = Skew,
        Weks = Weks,
        EaseFunction = EaseFunction,
        Curve = Curve,
        ProcessMode = ProcessMode,
        PauseMode = PauseMode,
        SuppressCallbacksWhenTargetInvalid = SuppressCallbacksWhenTargetInvalid,
    };
}

internal sealed class Playback
{
    private readonly double duration, delay, turn, offset, span, total;
    private readonly bool pingPong;
    private readonly int repeats;
    private double elapsed;
    internal float Progress { get; private set; }
    /// <summary>True on the ping-pong return leg and its final hold.</summary>
    internal bool Returning { get; private set; }
    /// <summary>Index of the current cycle; relative tweens add one full offset per cycle before it.</summary>
    internal double Cycle { get; private set; }
    internal bool Started { get; private set; }
    internal bool Completed { get; private set; }
    /// <summary>Time past the end of the timeline in the completing update.</summary>
    internal double Overshoot { get; private set; }
    internal TweenState State { get; private set; } = TweenState.Delayed;
    /// <summary>Whether the adjusted delay is longer than zero.</summary>
    internal bool HasDelay => delay > 0;

    internal Playback(TweenOptions options)
    {
        Easing.ValidateBlend(options.BlendType, options.Blend);
        Nonnegative(options.Duration, nameof(options.Duration));
        if (!double.IsFinite(options.FactorDuration)) throw new ArgumentOutOfRangeException(nameof(options.FactorDuration));
        if (!double.IsFinite(options.DeltaDuration)) throw new ArgumentOutOfRangeException(nameof(options.DeltaDuration));
        duration =Nonnegative(options.FactorDuration * options.Duration + options.DeltaDuration, nameof(options.Duration));
        Nonnegative(options.Delay, nameof(options.Delay));
        if (!double.IsFinite(options.FactorDelay)) throw new ArgumentOutOfRangeException(nameof(options.FactorDelay));
        if (!double.IsFinite(options.DeltaDelay)) throw new ArgumentOutOfRangeException(nameof(options.DeltaDelay));
        delay = Nonnegative(options.FactorDelay * options.Delay + options.DeltaDelay, nameof(options.Delay));
        turn = Nonnegative(options.PingPongInterval, nameof(options.PingPongInterval));
        var repeat = Nonnegative(options.RepeatInterval, nameof(options.RepeatInterval));
        offset = Nonnegative(options.Offset, nameof(options.Offset));
        if (!double.IsFinite(options.Skew) || options.Skew <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Skew), "Skew must be finite and greater than zero.");
        if (!double.IsFinite(options.Weks) || options.Weks <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Weks), "Weks must be finite and greater than zero.");
        if (offset > duration) throw new ArgumentOutOfRangeException(nameof(options.Offset));
        if (options.Repeats < TweenOptions.Infinite) throw new ArgumentOutOfRangeException(nameof(options.Repeats));
        if (!Enum.IsDefined(options.ProcessMode) || !Enum.IsDefined(options.PauseMode) ||
            (options.Fill & ~FillMode.Both) != 0)
            throw new ArgumentException("Invalid tween mode.", nameof(options));
        pingPong = options.UsePingPong;
        repeats = options.Repeats;
        var infinite = options.Repeats == TweenOptions.Infinite;
        span = duration + (pingPong ? turn + duration : 0) + repeat;
        // Double arithmetic keeps int.MaxValue repeats from overflowing.
        total = infinite ? double.PositiveInfinity : span * ((double)options.Repeats + 1) - repeat;
        if (!double.IsFinite(span + delay) || !infinite && !double.IsFinite(total + delay))
            throw new ArgumentOutOfRangeException(nameof(options.Duration), "Timeline is too long.");
        if (infinite && span == 0)
            throw new ArgumentException("An infinite tween must have a nonzero cycle duration.", nameof(options));
    }

    /// <summary>Starts the timeline this many seconds in, e.g. where a predecessor's timeline ended.</summary>
    internal void Credit(double seconds) => elapsed = seconds;

    internal static double Nonnegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    internal void Advance(double delta)
    {
        // Saturation also makes extremely large finite deltas well-defined.
        elapsed = Math.Min(double.MaxValue, elapsed + delta);
        if (elapsed < delay) return;
        Started = true;
        var time = Math.Min(double.MaxValue, elapsed - delay + offset);
        if (time >= total)
        {
            Returning = pingPong;
            Progress = pingPong ? 0 : 1;
            Cycle = repeats;
            Overshoot = time - total;
            Completed = true;
            State = TweenState.Completed;
            return;
        }
        var local = time % span;
        Cycle = Math.Round((time - local) / span);
        // At a cycle boundary, display the previous endpoint before restarting.
        if (local == 0 && time > 0)
        {
            local = span;
            Cycle--;
        }
        State = TweenState.Playing;
        Returning = pingPong && local > duration && local >= duration + turn;
        if (duration > 0 && local <= duration)
            Progress = (float)(local / duration);
        else if (!pingPong)
        {
            Progress = 1;
            State = TweenState.Interval;
        }
        else if (local < duration + turn)
        {
            Progress = 1;
            State = TweenState.Interval;
        }
        else if (duration > 0 && local <= duration * 2 + turn)
            Progress = (float)(1 - (local - duration - turn) / duration);
        else
        {
            Progress = 0;
            State = TweenState.Interval;
        }
    }
}
