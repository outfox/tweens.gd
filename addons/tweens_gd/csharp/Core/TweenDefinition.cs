// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tweens.gd;

/// <summary>A reusable definition for <typeparamref name="TTarget"/>, regardless of its value type.</summary>
public interface ITweenDefinition<in TTarget> where TTarget : class
{
    internal TweenInstance Snapshot(TweenScheduler scheduler, TTarget target, Godot.Node? owner, Godot.SceneTree? tree, PlaybackOptions options);
}

/// <summary>A reusable definition with a typed target and value. Each start creates independent playback.</summary>
public interface ITweenDefinition<TTarget, TValue> : ITweenDefinition<TTarget>
    where TTarget : class where TValue : struct
{
    internal TweenDefinition<TTarget, TValue> CreatePlayback();
    TweenInstance ITweenDefinition<TTarget>.Snapshot(TweenScheduler scheduler, TTarget target, Godot.Node? owner, Godot.SceneTree? tree, PlaybackOptions options)
        => new TweenInstance<TTarget, TValue>(scheduler, target, CreatePlayback(), owner, tree, options);
}

/// <summary>A mutable, reusable definition. Starting snapshots configuration; activation captures the current value.</summary>
/// <remarks>Override <see cref="Read"/>, <see cref="Write"/>, and <see cref="Interpolate"/> for custom storage.
/// In GDScript, use TweensGdDefinition with a TweensGdAdapter. Immutable C# definitions live in the Tweens namespace.</remarks>
public abstract class TweenDefinition<TTarget, TValue> : TweenOptionsBuilder, ITweenDefinition<TTarget, TValue>
    where TTarget : class where TValue : struct
{
    TweenDefinition<TTarget, TValue> ITweenDefinition<TTarget, TValue>.CreatePlayback() => Snapshot();

    /// <summary>Start value. Null uses the value captured at start, before the delay.</summary>
    public TValue? From { get; set; }
    /// <summary>End value. Null uses the value captured at start. Mutually exclusive with <see cref="By"/>.</summary>
    public TValue? To { get; set; }
    /// <summary>A relative offset instead of <see cref="To"/>, added on top of other changes to the property while
    /// it plays. With <see cref="From"/> it is added to that start once. Each repeat adds it again.</summary>
    public TValue? By { get; set; }
    /// <summary>The start becomes FactorFrom * (From or the captured value) + DeltaFrom, once, when playback starts.
    /// Factors scale away from zero (a quaternion's rotation angle); deltas add like <see cref="By"/>.</summary>
    public double FactorFrom { get; set; } = 1;
    /// <inheritdoc cref="FactorFrom"/>
    public TValue? DeltaFrom { get; set; }
    /// <summary>The end becomes FactorTo * (To or the captured value) + DeltaTo, once, when playback starts.</summary>
    public double FactorTo { get; set; } = 1;
    /// <inheritdoc cref="FactorTo"/>
    public TValue? DeltaTo { get; set; }
    /// <summary>The offset becomes FactorBy * By + DeltaBy, once, when playback starts. Requires <see cref="By"/>.</summary>
    public double FactorBy { get; set; } = 1;
    /// <inheritdoc cref="FactorBy"/>
    public TValue? DeltaBy { get; set; }
    /// <summary>Called once at activation after capture, before any delay fill is applied. Receives the handle.</summary>
    public Action<TweenInstance<TTarget, TValue>>? OnAdd { get; set; }
    /// <summary>Called once when playback reaches its first active update, after the delay. Receives the handle.</summary>
    public Action<TweenInstance<TTarget, TValue>>? OnStart { get; set; }
    /// <summary>Called after each value write, including delay fill. Receives the handle and value.</summary>
    public Action<TweenInstance<TTarget, TValue>, TValue>? OnUpdate { get; set; }
    /// <summary>Called on natural completion, before <see cref="TweenInstance.End"/> resolves. Receives the handle.</summary>
    public Action<TweenInstance<TTarget, TValue>>? OnEnd { get; set; }
    /// <summary>Called when playback stops early without a fault, such as cancellation or owner exit. Receives the handle.</summary>
    public Action<TweenInstance<TTarget, TValue>>? OnCancel { get; set; }
    /// <summary>Called last when playback ends, including faults. Receives the handle; may be suppressed for invalid targets.</summary>
    public Action<TweenInstance<TTarget, TValue>>? OnFinally { get; set; }

    /// <summary>Reads the current value from the target.</summary>
    protected abstract TValue Read(TTarget target);
    /// <summary>Writes the interpolated value to the target.</summary>
    protected abstract void Write(TTarget target, TValue value);
    /// <summary>Interpolates endpoints using eased weight, which may overshoot the range [0, 1].</summary>
    protected abstract TValue Interpolate(TValue from, TValue to, float weight);

    /// <summary>Prepare per-playback bindings on the private snapshot, before its initial read.</summary>
    protected virtual void Prepare(TTarget target) { }
    /// <summary>Restore captured state at non-retaining completion. May remove an override instead of writing a value.</summary>
    protected virtual void Restore(TTarget target, TValue initial) => Write(target, initial);
    /// <summary>Release only resources owned by this playback snapshot, including after failed preparation.</summary>
    protected virtual void Release() { }
    /// <summary>Whether Read returns what Write stored. If not, <see cref="By"/> is added to the captured start
    /// instead of to the current value.</summary>
    protected virtual bool ReadsWrittenValue => true;

    internal void PrepareTarget(TTarget target) => Prepare(target);
    internal bool FollowsTarget => ReadsWrittenValue;
    internal void RestoreValue(TTarget target, TValue initial) => Restore(target, initial);
    internal void ReleaseSnapshot() => Release();

    // Clone custom configuration as well, so subsequent property edits do not change bindings.
    internal TweenDefinition<TTarget, TValue> Snapshot() => (TweenDefinition<TTarget, TValue>)MemberwiseClone();
    internal TValue ReadValue(TTarget target) => Read(target);
    internal void WriteValue(TTarget target, TValue value) => Write(target, value);
    internal TValue InterpolateValue(TValue from, TValue to, float weight) => Interpolate(from, to, weight);
}

/// <summary>Animate a custom property without reflection. Captured mutable objects remain shared.</summary>
public class PropertyTween<TTarget, TValue>(Func<TTarget, TValue> getter,
    Action<TTarget, TValue> setter, Func<TValue, TValue, float, TValue> interpolate)
    : TweenDefinition<TTarget, TValue> where TTarget : class where TValue : struct
{
    private readonly Func<TTarget, TValue> getter = getter ?? throw new ArgumentNullException(nameof(getter));
    private readonly Action<TTarget, TValue> setter = setter ?? throw new ArgumentNullException(nameof(setter));
    private readonly Func<TValue, TValue, float, TValue> interpolate = interpolate ?? throw new ArgumentNullException(nameof(interpolate));
    protected override TValue Read(TTarget target) => getter(target);
    protected override void Write(TTarget target, TValue value) => setter(target, value);
    protected override TValue Interpolate(TValue from, TValue to, float weight) => interpolate(from, to, weight);
}
