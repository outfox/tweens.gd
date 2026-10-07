// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System.Text.Json;
using Godot;
using FX = tweens.gd.Tweens.FX;

namespace tweens.gd.Tests.Unit;

public class FXTests
{
    [Fact]
    public void ScalarFactoriesHaveDistinctEnvelopesAndPreservePhase()
    {
        Assert.Equal(1.53125f, FX.Punch(2, 2)(0.125f), 6);
        Assert.Equal(0, FX.Punch()(0));
        Assert.Equal(0, FX.Punch(2.3f, phase: 0.25f)(1));
        Assert.Equal(1, FX.Punch(phase: 0.25f)(0));
        Assert.Equal(0, FX.Punch(phase: 0.25f, attack: 0.1f)(0));
        Assert.Equal(0, FX.Shake()(0));
        Assert.Equal(-3, FX.Shake(0, 3, attack: 0)(0));
        Assert.Equal(0, FX.Shake()(1));
        Assert.Equal(0, FX.Breathe()(0));
        Assert.Equal(1, FX.Breathe()(0.5f));
        Assert.Equal(0, FX.Breathe()(1));
        Assert.Equal(1, FX.Breathe(0.5f)(1)); // Never snap non-integral cycles to zero.
        Assert.Equal(1, FX.Breathe(0, phase: 0.5f)(0));
    }

    [Fact]
    public void AttackReleaseUsesUnequalDurationsAndMeetsContinuously()
    {
        var envelope = FX.AttackRelease(0.2f);
        Assert.Equal(0, envelope(0));
        Assert.Equal(1, envelope(0.2f));
        Assert.Equal(0.5f, envelope(0.1f), 6);
        Assert.Equal(0.25f, envelope(0.6f), 6);
        Assert.Equal(0, envelope(1));
        Assert.InRange(Math.Abs(envelope(0.2f - 0.0001f) - envelope(0.2f + 0.0001f)), 0, 0.00001f);
        Assert.Equal(0.5625f, FX.Decay()(0.25f));
        Assert.Equal(1, FX.Decay()(-1));
        Assert.Equal(0, FX.Decay()(2));
    }

    [Fact]
    public void NoiseIsContinuousBoundedSeededAndIndependentOfSampleOrder()
    {
        var sample = FX.Shake(8, 3, seed: -17, offset: -2.5f, attack: 0);
        var expected = sample(0.375f); // Exact lattice boundary, including negative coordinates.
        for (var i = 100; i >= 0; --i) Assert.InRange(sample(i / 100f), -3, 3);
        Assert.Equal(expected, sample(0.375f));
        Assert.Equal(expected, FX.Shake(8, 3, -17, -2.5f, attack: 0)(0.375f));
        Assert.NotEqual(expected, FX.Shake(8, 3, 18, -2.5f, attack: 0)(0.375f));
        Assert.NotEqual(expected, FX.Shake(8, 3, -17, -2.25f, attack: 0)(0.375f));
        Assert.InRange(Math.Abs(sample(0.375f - 0.00001f) - sample(0.375f + 0.00001f)), 0, 0.001f);
    }

    [Fact]
    public void VectorFactoriesRespectEveryAxisParameterAndAllowSignedAmplitudes()
    {
        var amplitudes = new Vector3(2, -3, 0);
        var frequencies = new Vector3(2, 5, 8);
        var phases = new Vector3(0, 0.25f, 0.5f);
        var offsets = new Vector3(-2.5f, 7.25f, 11);
        const float t = 0.125f;
        var punch = FX.Punch3D(amplitudes, frequencies, phase: phases)(t);
        Assert.Equal(FX.Punch(2, 2)(t), punch.X);
        Assert.Equal(FX.Punch(5, -3, phase: 0.25f)(t), punch.Y);
        Assert.Equal(0, punch.Z);
        var shake = FX.Shake3D(amplitudes, frequencies, -17, offsets)(t);
        Assert.Equal(FX.Shake(2, 2, -17, -2.5f)(t), shake.X);
        Assert.Equal(FX.Shake(5, -3, -17, 7.25f)(t), shake.Y);
        Assert.Equal(0, shake.Z);
        var breathe = FX.Breathe3D(amplitudes, frequencies, phases)(t);
        Assert.Equal(FX.Breathe(5, -3, 0.25f)(t), breathe.Y);
        Assert.Equal(new Vector2(punch.X, punch.Y), FX.Punch2D(new(2, -3), new(2, 5), phase: new(0, 0.25f))(t));
        Assert.Equal(new Vector2(shake.X, shake.Y), FX.Shake2D(new(2, -3), new(2, 5), -17, new(-2.5f, 7.25f))(t));
        Assert.Equal(new Vector2(breathe.X, breathe.Y), FX.Breathe2D(new(2, -3), new(2, 5), new(0, 0.25f))(t));
        var independent = FX.Shake3D(Vector3.One)(0.37f);
        Assert.NotEqual(independent.X, independent.Y);
        Assert.NotEqual(independent.Y, independent.Z);
    }

    [Fact]
    public void QuaternionFactoriesProduceUnitRotationOffsetsAndIdentityAtRest()
    {
        var factories = new[] { FX.PunchQuaternion(new(0.2f, 0.3f, 0.4f)),
            FX.ShakeQuaternion(new(0.2f, 0.3f, 0.4f)), FX.BreatheQuaternion(new(0.2f, 0.3f, 0.4f)) };
        foreach (var sample in factories)
        {
            Assert.True(sample(0).IsEqualApprox(Quaternion.Identity));
            Assert.True(sample(1).IsEqualApprox(Quaternion.Identity));
            for (var i = 0; i <= 100; i++) Assert.True(sample(i / 100f).IsNormalized());
        }
        Assert.True(FX.BreatheQuaternion(new(0, 0.6f, 0))(0.5f).IsEqualApprox(new Quaternion(Vector3.Up, 0.6f)));
        Assert.Equal(Quaternion.Identity, FX.ShakeQuaternion(Vector3.Zero)(0.3f));
        Assert.True(FX.BreatheQuaternion(new(float.MaxValue, float.MaxValue, float.MaxValue))(0.5f).IsNormalized());
    }

    [Fact]
    public void ConfigurationAndNonFiniteProgressAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Punch(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Shake(amplitude: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Shake(offset: float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Breathe(phase: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.AttackRelease(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.AttackRelease(-0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Decay(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Decay(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Shake2D(Vector2.One, new(-1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.Punch()(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => FX.ShakeQuaternion(Vector3.One)(float.PositiveInfinity));
    }

    private sealed class Target
    {
        public float Amount;
        public Vector2 Position;
        public Quaternion Rotation = Quaternion.Identity;
    }

    [Fact]
    public void FunctionsWorkWithExistingEasingCallbacksAndCustomInterpolators()
    {
        using var scheduler = new TweenScheduler();
        var target = new Target { Amount = 10, Position = new(4, 5) };
        var scalar = new Tweens.Property<Target, float>(x => x.Amount, (x, v) => x.Amount = v, Interpolators.Float)
            { By = 2, Duration = 1, EaseFunction = FX.Punch(2) };
        scheduler.Add(target, scalar);
        var motion = FX.Shake2D(new(8, 4));
        var baseline = target.Position;
        scheduler.Add(target, new Tweens.Property<Target, float>(_ => 0, (_, _) => { }, Interpolators.Float)
            { From = 0, To = 1, Duration = 1, OnUpdate = (_, t) => target.Position = baseline + motion(t) });
        var turn = FX.PunchQuaternion(new(0, 0.3f, 0));
        scheduler.Add(target, new Tweens.Property<Target, Quaternion>(x => x.Rotation, (x, v) => x.Rotation = v,
            (from, _, t) => from * turn(t)) { Duration = 1 });
        scheduler.Update(0.125);
        Assert.Equal(11.53125f, target.Amount, 5);
        Assert.Equal(baseline + motion(0.125f), target.Position);
        Assert.True(target.Rotation.IsEqualApprox(turn(0.125f)));
        scheduler.Update(1); // Overshoot must still sample the exact terminal time.
        Assert.Equal(10, target.Amount);
        Assert.Equal(baseline, target.Position);
        Assert.Equal(Quaternion.Identity, target.Rotation);
    }

    [Fact]
    public void NoiseMatchesLanguageNeutralFixtures()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance", "fx.json")));
        foreach (var row in data.RootElement.GetProperty("shake").EnumerateArray())
        {
            var sample = FX.Shake(row.GetProperty("frequency").GetSingle(), row.GetProperty("amplitude").GetSingle(),
                row.GetProperty("seed").GetInt32(), row.GetProperty("offset").GetSingle(),
                row.GetProperty("decay").GetSingle(), row.GetProperty("attack").GetSingle());
            Assert.InRange(Math.Abs(sample(row.GetProperty("t").GetSingle()) - row.GetProperty("value").GetSingle()), 0, 0.00001f);
        }
    }
}
