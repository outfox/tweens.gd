// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using Godot;
using System.Runtime.CompilerServices;

namespace tweens.gd;

/// <summary>A single playback. Mutating operations belong to its scheduler's thread.</summary>
public abstract class TweenInstance
{
    private TaskCompletionSource<Reason>? completion;
    private bool settled;
    private int operationDepth;
    private bool paused;
    private Action? exitHandler;
    internal readonly TweenScheduler Scheduler;
    internal readonly Node? Owner;
    private readonly GodotObject? nativeTarget;
    private readonly SceneTree? tree;
    internal readonly TweenProcessMode Mode;
    private readonly TweenPauseMode pauseMode;
    internal readonly bool Unscaled;
    private protected readonly Playback Clock;

    public TweenState State { get; protected set; } = TweenState.Delayed;
    public bool IsTerminal => State is TweenState.Completed or TweenState.Cancelled or TweenState.Faulted;
    public Reason? CompletionReason { get; private set; }
    public Exception? Error { get; private set; }
    public float Progress => Clock.Progress;
    /// <summary>Set on natural completion; continuations started from it inherit the overshoot.</summary>
    internal Carry? Stamp { get; private set; }
    internal bool IsSettled => settled;
    /// <summary>Raised once the tween has settled, before its completion task is resolved.</summary>
    internal event Action<TweenInstance>? Settled;
    public bool IsPaused
    {
        get => paused;
        set { Scheduler.EnsureThread(); paused = value; }
    }

    /// <summary>Shared completion. Cancellation is a result; callback errors fault the task.</summary>
    /// <remarks>Tweens started where an await of it resumes continue its timeline, including its last overshoot.</remarks>
    public Task<Reason> End
    {
        get
        {
            Scheduler.EnsureThread();
            if (completion is null)
            {
                completion = new();
                if (settled) SetCompletion();
            }
            return completion.Task;
        }
    }

    /// <summary>Awaits this playback directly, with the same result and faults as End.</summary>
    public TaskAwaiter<Reason> GetAwaiter() => End.GetAwaiter();

    internal TweenInstance(TweenScheduler scheduler, TweenOptionsBuilder options, Node? owner, GodotObject? nativeTarget, SceneTree? tree)
    {
        Scheduler = scheduler;
        Owner = owner;
        this.nativeTarget = nativeTarget;
        this.tree = tree;
        Mode = options.ProcessMode;
        pauseMode = options.PauseMode;
        Unscaled = options.UseUnscaledTime;
        Clock = new Playback(options.ToOptions());
    }

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;
    public void Cancel()
    {
        Scheduler.EnsureThread();
        Finish(Reason.Cancelled);
    }

    /// <summary>The token cancels only this wait, not the tween. Use Cancel to stop playback.</summary>
    public Task<Reason> AwaitDecommissionAsync(CancellationToken cancellationToken = default)
        => cancellationToken.CanBeCanceled ? End.WaitAsync(cancellationToken) : End;

    internal void BindLifetime()
    {
        if (Owner is null) return;
        exitHandler = () => Finish(ReferenceEquals(Owner, nativeTarget) && Owner.IsQueuedForDeletion()
            ? Reason.TargetFreed : Reason.OwnerExited);
        Owner.TreeExiting += exitHandler;
    }

    protected bool InvalidTargetOrOwner =>
        (nativeTarget is not null && (!GodotObject.IsInstanceValid(nativeTarget) ||
            (nativeTarget is Node targetNode && targetNode.IsQueuedForDeletion()))) ||
        (Owner is not null && (!GodotObject.IsInstanceValid(Owner) || Owner.IsQueuedForDeletion() || !Owner.IsInsideTree()));

    internal bool CheckTarget()
    {
        if (IsTerminal) return false;
        if (nativeTarget is not null && (!GodotObject.IsInstanceValid(nativeTarget) ||
            (nativeTarget is Node targetNode && targetNode.IsQueuedForDeletion())))
            Finish(Reason.TargetFreed);
        else if (Owner is not null && (!GodotObject.IsInstanceValid(Owner) || Owner.IsQueuedForDeletion() || !Owner.IsInsideTree()))
            Finish(Reason.OwnerExited);
        else if (tree is not null && !GodotObject.IsInstanceValid(tree))
            Finish(Reason.RunnerDisposed);
        return !IsTerminal;
    }

    internal bool CanAdvance()
    {
        if (!CheckTarget() || paused) return false;
        if (pauseMode == TweenPauseMode.Always) return true;
        if (Owner is not null)
            return pauseMode == TweenPauseMode.SceneTree ? !Owner.GetTree().Paused : Owner.CanProcess();
        // Tree-scoped resources have no bound node; Bound and SceneTree both follow tree pause.
        return tree is null || !tree.Paused;
    }

    internal void ApplyCredit(double seconds) => Clock.Credit(seconds);

    internal abstract void Initialize();
    internal abstract void Advance(double delta);
    protected abstract void InvokeTerminal(Reason reason, bool faulted);
    protected abstract void Release();

    private protected void BeginOperation() => operationDepth++;
    private protected void EndOperation()
    {
        operationDepth--;
        if (IsTerminal && !settled && operationDepth == 0) Settle();
    }

    internal void Finish(Reason reason, Exception? error = null)
    {
        if (IsTerminal)
        {
            // A callback may cancel itself and then throw. Its awaiters still need the error.
            if (error is not null && !settled)
            {
                Error = Error is null ? error : new AggregateException(Error, error);
                State = TweenState.Faulted;
            }
            else if (error is not null) Scheduler.Report(error);
            return;
        }
        CompletionReason = reason;
        Error = error;
        State = error is not null ? TweenState.Faulted : reason == Reason.Completed
            ? TweenState.Completed : TweenState.Cancelled;
        if (State == TweenState.Completed)
            Stamp = new Carry(Scheduler, Mode, Unscaled, Scheduler.Tick(Mode), Clock.Overshoot);
        try
        {
            // Tweens started by OnEnd continue this timeline.
            using var scope = TweenCarry.Enter(Stamp);
            InvokeTerminal(reason, error is not null);
        }
        catch (Exception callbackError)
        {
            Error = Error is null ? callbackError : new AggregateException(Error, callbackError);
            State = TweenState.Faulted;
        }
        finally
        {
            if (exitHandler is not null && GodotObject.IsInstanceValid(Owner))
                Owner!.TreeExiting -= exitHandler;
            exitHandler = null;
            try { Release(); }
            catch (Exception releaseError)
            {
                Error = Error is null ? releaseError : new AggregateException(Error, releaseError);
                State = TweenState.Faulted;
            }
            finally { if (operationDepth == 0) Settle(); }
        }
    }

    private void Settle()
    {
        settled = true;
        if (Error is not null) Scheduler.Report(Error);
        try { Settled?.Invoke(this); }
        catch (Exception error) { Scheduler.Report(error); }
        Settled = null;
        // Awaiting code resumes inline here; tweens it starts continue this timeline.
        using var scope = TweenCarry.Enter(Error is null ? Stamp : null);
        SetCompletion();
    }

    private void SetCompletion()
    {
        if (Error is not null) completion?.TrySetException(Error);
        else completion?.TrySetResult(CompletionReason!.Value);
    }
}

public sealed class TweenInstance<TTarget, TValue> : TweenInstance
    where TTarget : class where TValue : struct
{
    private TweenDefinition<TTarget, TValue>? definition;
    private Func<float, float>? ease;
    private Curve? curve;
    private readonly TValue initial, from, to, by;
    // A By tween writes origin + applied. Following tweens move origin along with outside changes to the property.
    private readonly bool relative, follows, pingPong;
    private TValue origin, applied;
    private bool started;
    public TTarget Target { get; }
    public TValue Value { get; private set; }

    internal TweenInstance(TweenScheduler scheduler, TTarget target,
        TweenDefinition<TTarget, TValue> source, Node? owner, SceneTree? tree)
        : base(scheduler, source, owner, target as GodotObject, tree)
    {
        Target = target;
        definition = source;
        try
        {
            if (definition.Curve is not null && definition.EaseFunction is not null)
                throw new ArgumentException("Specify either Curve or EaseFunction, not both.", nameof(source));
            if (definition.By is not null && definition.To is not null)
                throw new ArgumentException("Specify either To or By, not both.", nameof(source));
            var adjustsFrom = definition.FactorFrom != 1 || definition.DeltaFrom is not null;
            var adjustsTo = definition.FactorTo != 1 || definition.DeltaTo is not null;
            var adjustsBy = definition.FactorBy != 1 || definition.DeltaBy is not null;
            if (definition.By is null && adjustsBy)
                throw new ArgumentException("FactorBy and DeltaBy need By.", nameof(source));
            if (definition.By is not null && adjustsTo)
                throw new ArgumentException("FactorTo and DeltaTo do not apply to a By tween.", nameof(source));
            if (!double.IsFinite(definition.FactorFrom) || !double.IsFinite(definition.FactorTo) || !double.IsFinite(definition.FactorBy))
                throw new ArgumentOutOfRangeException(nameof(source), "Factors must be finite.");
            if ((definition.By is not null || adjustsFrom || adjustsTo) && !Offsets<TValue>.Supported)
                throw Offsets<TValue>.Unsupported();
            definition.PrepareTarget(target);
            if (InvalidTargetOrOwner)
                throw new ArgumentException("The target or owner became invalid during tween preparation.", nameof(target));
            initial = definition.ReadValue(target);
            Value = initial;
            from = Adjust(definition.From ?? initial, definition.FactorFrom, definition.DeltaFrom);
            to = Adjust(definition.To ?? initial, definition.FactorTo, definition.DeltaTo);
            if (definition.By is { } offset)
            {
                (relative, by, origin, applied) = (true, Adjust(offset, definition.FactorBy, definition.DeltaBy), from,
                    Offsets<TValue>.Zero);
                // An adjusted start is fixed, like an explicit From.
                follows = definition.From is null && !adjustsFrom && definition.FollowsTarget;
                pingPong = definition.UsePingPong;
            }
            ease = definition.EaseFunction ?? Easing.GetFunction(definition.Ease);
            if (definition.Curve is not null)
            {
                curve = (Curve)definition.Curve.Duplicate();
                ease = curve.Sample;
            }
        }
        catch (Exception preparationError)
        {
            curve?.Dispose();
            try { definition.ReleaseSnapshot(); }
            catch (Exception releaseError) { throw new AggregateException(preparationError, releaseError); }
            throw;
        }
    }

    internal override void Initialize()
    {
        BeginOperation();
        try
        {
            definition!.OnAdd?.Invoke(this);
            if (!CheckTarget()) return;
            if (Clock.HasDelay && definition.Fill.HasFlag(FillMode.ApplyFromDuringDelay)) Apply(from);
        }
        catch (Exception error) { Finish(Reason.Cancelled, error); }
        finally { EndOperation(); }
    }

    internal override void Advance(double delta)
    {
        BeginOperation();
        try
        {
            Clock.Advance(delta);
            State = Clock.State == TweenState.Completed ? TweenState.Playing : Clock.State;
            if (!Clock.Started) return;
            if (!started)
            {
                started = true;
                definition!.OnStart?.Invoke(this);
                if (!CheckTarget()) return;
            }
            var time = Math.Clamp(Progress, 0, 1);
            if (definition!.Skew != 1) time = (float)Math.Pow(time, definition.Skew);
            var weight = ease!(time);
            if (!float.IsFinite(weight)) throw new InvalidOperationException("Easing returned a non-finite value.");
            if (!CheckTarget()) return;
            var value = relative ? Offset(weight) : definition!.InterpolateValue(from, to, weight);
            if (!CheckTarget()) return;
            Apply(value);
            if (!CheckTarget() || !Clock.Completed) return;
            if (!definition!.Fill.HasFlag(FillMode.RetainFinalValue)) RestoreInitial();
            if (CheckTarget()) Finish(Reason.Completed);
        }
        catch (Exception error) { Finish(Reason.Cancelled, error); }
        finally { EndOperation(); }
    }

    private void Apply(TValue value)
    {
        if (!CheckTarget()) return;
        definition!.WriteValue(Target, value);
        Value = value;
        if (CheckTarget()) definition!.OnUpdate?.Invoke(this, value);
    }

    // factor * value + delta. The factor scales away from zero; for a quaternion that scales its rotation angle.
    private TValue Adjust(TValue value, double factor, TValue? delta)
    {
        if (factor != 1) value = definition!.InterpolateValue(Offsets<TValue>.Zero, value, (float)factor);
        return delta is { } offset ? Offsets<TValue>.Add(value, offset) : value;
    }

    // Everything By has added so far: one offset per finished cycle, unless ping-pong brought it back, plus this one.
    private TValue Offset(float weight)
    {
        Follow();
        var offset = definition!.InterpolateValue(Offsets<TValue>.Zero, by, weight);
        if (!pingPong && Clock.Cycle > 0)
            offset = Offsets<TValue>.Add(definition.InterpolateValue(Offsets<TValue>.Zero, by, (float)Clock.Cycle), offset);
        applied = offset;
        return Offsets<TValue>.Add(origin, offset);
    }

    // A value other than the last one written means something else changed the property; keep that change.
    private void Follow()
    {
        if (!follows) return;
        var current = definition!.ReadValue(Target);
        if (!EqualityComparer<TValue>.Default.Equals(current, Value)) origin = Offsets<TValue>.Remove(current, applied);
    }

    private void RestoreInitial()
    {
        if (!CheckTarget()) return;
        // A following By tween takes only its own offset back out.
        Follow();
        if (!CheckTarget()) return;
        var value = follows ? origin : initial;
        definition!.RestoreValue(Target, value);
        Value = value;
        if (CheckTarget()) definition.OnUpdate?.Invoke(this, value);
    }

    protected override void InvokeTerminal(Reason reason, bool faulted)
    {
        var snapshot = definition!;
        if (snapshot.SuppressCallbacksWhenTargetInvalid &&
            (InvalidTargetOrOwner || reason is Reason.TargetFreed or Reason.OwnerExited)) return;
        Exception? failure = null;
        try
        {
            if (!faulted)
            {
                if (reason == Reason.Completed) snapshot.OnEnd?.Invoke(this);
                else snapshot.OnCancel?.Invoke(this);
            }
        }
        catch (Exception error) { failure = error; }
        try { snapshot.OnFinally?.Invoke(this); }
        catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        if (failure is not null) throw failure;
    }

    protected override void Release()
    {
        try { definition?.ReleaseSnapshot(); }
        finally
        {
            definition = null;
            ease = null;
            curve?.Dispose();
            curve = null;
        }
    }
}
