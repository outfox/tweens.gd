// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tweens.gd;

/// <summary>Value behavior during delay and after natural completion. Cancellation does not restore the initial value.</summary>
/// <remarks>A relative tween that follows concurrent property changes removes only its own offset when restoring.</remarks>
[Flags]
public enum FillMode
{
    /// <summary>Leave the property untouched during delay; restore its initial value on natural completion.</summary>
    None = 0,
    /// <summary>Apply From during delay; restore the initial value on natural completion.</summary>
    ApplyFromDuringDelay = 1,
    /// <summary>Keep the final value on natural completion; leave the property untouched during delay.</summary>
    RetainFinalValue = 2,
    /// <summary>Apply From during delay and keep the final value on natural completion.</summary>
    Both = ApplyFromDuringDelay | RetainFinalValue,
}

/// <summary>The update phase used by playback.</summary>
public enum TweenProcessMode
{
    /// <summary>Advance on process ticks.</summary>
    Process,
    /// <summary>Advance on physics ticks.</summary>
    Physics,
}

/// <summary>How playback responds to scene and owner pausing.</summary>
public enum TweenPauseMode
{
    /// <summary>Follow the owner's CanProcess; without an owner, follow scene-tree pause.</summary>
    Bound,
    /// <summary>Follow scene-tree pause, independently of the owner's process mode.</summary>
    SceneTree,
    /// <summary>Ignore scene and owner pausing. Explicit handle pauses still apply.</summary>
    Always,
}

/// <summary>Current timeline state, independently of explicit pausing.</summary>
public enum TweenState
{
    /// <summary>Waiting for the initial delay.</summary>
    Delayed,
    /// <summary>Advancing a forward or return leg.</summary>
    Playing,
    /// <summary>Holding between legs or cycles.</summary>
    Interval,
    /// <summary>Ended naturally.</summary>
    Completed,
    /// <summary>Stopped before natural completion.</summary>
    Cancelled,
    /// <summary>Stopped because playback or a callback failed.</summary>
    Faulted,
}

/// <summary>Why playback ended. Faults are reported separately through Error and End.</summary>
public enum Reason
{
    /// <summary>Every scheduled cycle completed.</summary>
    Completed,
    /// <summary>Playback was explicitly cancelled.</summary>
    Cancelled,
    /// <summary>The target was freed or queued for deletion.</summary>
    TargetFreed,
    /// <summary>The owner left the scene tree.</summary>
    OwnerExited,
    /// <summary>The scheduler or scene tree was disposed.</summary>
    RunnerDisposed,
}

/// <summary>Immutable timing configuration. Use with expressions to vary a reusable value.</summary>
public readonly record struct TweenOptions
{
    /// <summary>A <see cref="Repeats"/> value that repeats until cancelled.</summary>
    public const int Infinite = -1;

    /// <summary>Duration of one leg, in seconds. Defaults to zero; must be finite and nonnegative.</summary>
    public Duration Duration { get; init; }
    private readonly double? factorDuration;
    /// <summary>Scales <see cref="Duration"/> at start: the tween lasts FactorDuration * Duration + DeltaDuration.</summary>
    public double FactorDuration { get => factorDuration ?? 1; init => factorDuration = value == 1 ? null : value; }
    /// <summary>Added to <see cref="Duration"/> at start, after <see cref="FactorDuration"/>.</summary>
    public Duration DeltaDuration { get; init; }
    /// <summary>Signed gap before the first leg. Positive values wait after capture; negative values overlap or pre-roll.</summary>
    public Duration Delay { get; init; }
    private readonly double? factorDelay;
    /// <summary>Scales the signed gap at start: FactorDelay * Delay + DeltaDelay.</summary>
    public double FactorDelay { get => factorDelay ?? 1; init => factorDelay = value == 1 ? null : value; }
    /// <summary>Added to <see cref="Delay"/> at start, after <see cref="FactorDelay"/>.</summary>
    public Duration DeltaDelay { get; init; }
    /// <summary>Hold between the forward and return legs, in seconds. Defaults to zero.</summary>
    public Duration PingPongInterval { get; init; }
    /// <summary>Hold between cycles, in seconds. No hold follows the final cycle.</summary>
    public Duration RepeatInterval { get; init; }
    /// <summary>Start this many seconds into the first leg, after the delay. Must not exceed the adjusted duration.</summary>
    public Duration Offset { get; init; }
    /// <summary>Cycles after the first, or <see cref="Infinite"/>. A ping-pong cycle includes both legs.</summary>
    public int Repeats { get; init; }
    /// <summary>Return to the start after each forward leg. Duration applies to each leg; defaults to false.</summary>
    public bool UsePingPong { get; init; }
    private readonly FillMode fill;
    // Encode the default so default(TweenOptions) and new TweenOptions() behave identically.
    /// <summary>Value behavior during delay and after natural completion. Defaults to RetainFinalValue.</summary>
    public FillMode Fill { get => fill ^ FillMode.RetainFinalValue; init => fill = value ^ FillMode.RetainFinalValue; }
    /// <summary>Combine one In and one Out with |, use an InOut pair, or select a single curve. Legacy EaseType names retain their original shapes.</summary>
    public EaseType Ease { get; init; }
    /// <summary>How mixed In/Out legs join. Defaults to Makima; matching families keep their conventional shape.</summary>
    public BlendType BlendType { get; init; }
    private readonly double? blend;
    /// <summary>Centered transition width in [0, 1]. Defaults to 0.2 (40%–60%); zero directly splices the halves.</summary>
    public double Blend { get => blend ?? 0.2; init => blend = value == 0.2 ? null : value; }
    private readonly double? skew;
    /// <summary>In/Out split in [0, 1]: 0 selects Out, 0.5 is balanced, 1 selects In. Applies to paired easing flags.</summary>
    public double Skew { get => skew ?? 0.5; init => skew = value == 0.5 ? null : value; }
    private readonly double? weks;
    /// <summary>Independent ping-pong return split in [0, 1]: 0 front-loads the return, 0.5 is balanced, 1 rear-loads it.</summary>
    public double Weks { get => weks ?? 0.5; init => weks = value == 0.5 ? null : value; }
    /// <summary>Custom progress-to-weight function instead of Ease. Mutually exclusive with Curve.</summary>
    public Func<float, float>? EaseFunction { get; init; }
    /// <summary>Custom progress-to-weight curve instead of Ease. Copied per playback; mutually exclusive with EaseFunction.</summary>
    public Godot.Curve? Curve { get; init; }
    /// <summary>Skip callbacks after the target or owner becomes invalid. Defaults to false.</summary>
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
        target.Fill = Fill;
        target.Ease = Ease;
        target.BlendType = BlendType;
        target.Blend = Blend;
        target.Skew = Skew;
        target.Weks = Weks;
        target.EaseFunction = EaseFunction;
        target.Curve = Curve;
        target.SuppressCallbacksWhenTargetInvalid = SuppressCallbacksWhenTargetInvalid;
    }
}

/// <summary>Mutable options used by convenience-method configurators and custom class definitions.</summary>
public class TweenOptionsBuilder
{
    /// <inheritdoc cref="TweenOptions.Infinite"/>
    public const int Infinite = TweenOptions.Infinite;

    /// <inheritdoc cref="TweenOptions.Duration"/>
    public Duration Duration { get; set; }
    /// <inheritdoc cref="TweenOptions.FactorDuration"/>
    public double FactorDuration { get; set; } = 1;
    /// <inheritdoc cref="TweenOptions.DeltaDuration"/>
    public Duration DeltaDuration { get; set; }
    /// <inheritdoc cref="TweenOptions.Delay"/>
    public Duration Delay { get; set; }
    /// <inheritdoc cref="TweenOptions.FactorDelay"/>
    public double FactorDelay { get; set; } = 1;
    /// <inheritdoc cref="TweenOptions.DeltaDelay"/>
    public Duration DeltaDelay { get; set; }
    /// <inheritdoc cref="TweenOptions.PingPongInterval"/>
    public Duration PingPongInterval { get; set; }
    /// <inheritdoc cref="TweenOptions.RepeatInterval"/>
    public Duration RepeatInterval { get; set; }
    /// <inheritdoc cref="TweenOptions.Offset"/>
    public Duration Offset { get; set; }
    /// <inheritdoc cref="TweenOptions.Repeats"/>
    public int Repeats { get; set; }
    /// <inheritdoc cref="TweenOptions.UsePingPong"/>
    public bool UsePingPong { get; set; }
    /// <inheritdoc cref="TweenOptions.Fill"/>
    public FillMode Fill { get; set; } = FillMode.RetainFinalValue;
    /// <inheritdoc cref="TweenOptions.Ease"/>
    public EaseType Ease { get; set; }
    /// <inheritdoc cref="TweenOptions.BlendType"/>
    public BlendType BlendType { get; set; }
    /// <inheritdoc cref="TweenOptions.Blend"/>
    public double Blend { get; set; } = 0.2;
    /// <inheritdoc cref="TweenOptions.Skew"/>
    public double Skew { get; set; } = 0.5;
    /// <inheritdoc cref="TweenOptions.Weks"/>
    public double Weks { get; set; } = 0.5;
    /// <inheritdoc cref="TweenOptions.EaseFunction"/>
    public Func<float, float>? EaseFunction { get; set; }
    /// <inheritdoc cref="TweenOptions.Curve"/>
    public Godot.Curve? Curve { get; set; }
    /// <inheritdoc cref="TweenOptions.SuppressCallbacksWhenTargetInvalid"/>
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
        Fill = Fill,
        Ease = Ease,
        BlendType = BlendType,
        Blend = Blend,
        Skew = Skew,
        Weks = Weks,
        EaseFunction = EaseFunction,
        Curve = Curve,
        SuppressCallbacksWhenTargetInvalid = SuppressCallbacksWhenTargetInvalid,
    };
}

internal sealed class Playback
{
    private readonly double duration, delay, turn, offset, span, total;
    private readonly bool pingPong;
    private readonly int repeats;
    private double elapsed;
    internal double Gap => delay;
    internal double InnerDelay => Math.Max(0, delay);
    internal double Remaining => total - offset;
    internal float Progress { get; private set; }
    /// <summary>True on the ping-pong return leg and its final hold.</summary>
    internal bool Returning { get; private set; }
    /// <summary>Index of the current cycle; relative tweens add one full offset per cycle before it.</summary>
    internal double Cycle { get; private set; }
    internal bool Started { get; private set; }
    internal bool Completed { get; private set; }
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
        if (!double.IsFinite(options.Delay)) throw new ArgumentOutOfRangeException(nameof(options.Delay));
        if (!double.IsFinite(options.FactorDelay)) throw new ArgumentOutOfRangeException(nameof(options.FactorDelay));
        if (!double.IsFinite(options.DeltaDelay)) throw new ArgumentOutOfRangeException(nameof(options.DeltaDelay));
        delay = options.FactorDelay * options.Delay + options.DeltaDelay;
        if (!double.IsFinite(delay)) throw new ArgumentOutOfRangeException(nameof(options.Delay));
        turn = Nonnegative(options.PingPongInterval, nameof(options.PingPongInterval));
        var repeat = Nonnegative(options.RepeatInterval, nameof(options.RepeatInterval));
        offset = Nonnegative(options.Offset, nameof(options.Offset));
        if (!double.IsFinite(options.Skew) || options.Skew < 0 || options.Skew > 1)
            throw new ArgumentOutOfRangeException(nameof(options.Skew), "Skew must be finite and in [0, 1].");
        if (!double.IsFinite(options.Weks) || options.Weks < 0 || options.Weks > 1)
            throw new ArgumentOutOfRangeException(nameof(options.Weks), "Weks must be finite and in [0, 1].");
        if (offset > duration) throw new ArgumentOutOfRangeException(nameof(options.Offset));
        if (options.Repeats < TweenOptions.Infinite) throw new ArgumentOutOfRangeException(nameof(options.Repeats));
        if ((options.Fill & ~FillMode.Both) != 0)
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


    internal static double Nonnegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    internal void Advance(double delta) => SampleAt(Math.Min(double.MaxValue, elapsed + delta));

    internal void SampleAt(double localTime)
    {
        elapsed = localTime;
        Started = Completed = Returning = false;
        Progress = 0;
        Cycle = 0;
        State = TweenState.Delayed;
        if (elapsed < InnerDelay) return;
        Started = true;
        var time = Math.Min(double.MaxValue, elapsed - InnerDelay + offset);
        if (time >= total)
        {
            Returning = pingPong;
            Progress = pingPong ? 0 : 1;
            Cycle = repeats;
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
