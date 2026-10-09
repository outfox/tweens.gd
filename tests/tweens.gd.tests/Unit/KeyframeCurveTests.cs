// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Text.Json;
using Godot;

namespace tweens.gd.Tests.Unit;

public class KeyframeCurveTests
{
    [Fact]
    public void SharedConformanceIncludesReversingProgressAndColorPolicies()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "conformance/keyframes.json")));
        foreach (var item in json.RootElement.GetProperty("cases").EnumerateArray())
        {
            var stops = item.GetProperty("stops").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            var values = item.GetProperty("values").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            var modes = item.TryGetProperty("modes", out var m) ? m.EnumerateArray().Select(v => v.GetInt32()).ToArray() : null;
            var eases = item.TryGetProperty("eases", out var e) ? e.EnumerateArray().Select(v => v.GetInt32()).ToArray() : null;
            var keys = stops.Select((v, i) => new CurveKey<double>(v, values[i], modes is null || modes[i] == -1 ? null : Mode(modes[i], eases![i]))).ToArray();
            var curve = new KeyframeCurve<double>(keys, Mode(item.GetProperty("mode").GetInt32()));
            if (item.TryGetProperty("initial", out var initial)) curve = curve.CaptureStart(initial.GetDouble());
            var scale = item.TryGetProperty("sample_scale", out var s) ? s.GetDouble() : 1;
            foreach (var sample in item.GetProperty("samples").EnumerateArray())
                Assert.InRange(Math.Abs(curve.Sample(sample[0].GetDouble()) / scale - sample[1].GetDouble() / scale), 0, 0.00001);
        }
        foreach (var item in json.RootElement.GetProperty("colors").EnumerateArray())
        {
            Color Read(string key) { var v = item.GetProperty(key); return new(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle(), v[3].GetSingle()); }
            var from = Read("from"); var to = Read("to");
            var space = (ColorSpace)item.GetProperty("space").GetInt32(); var alpha = (AlphaMode)item.GetProperty("alpha").GetInt32();
            var encoding = (ColorEncoding)item.GetProperty("encoding").GetInt32(); var at = item.GetProperty("at").GetSingle();
            var curve = KeyframeCurve<Color>.EvenlySpaced([from, to], Interpolation.Linear, space, alpha, encoding);
            var value = curve.Sample(at); var expected = Read("expected");
            Assert.InRange(Math.Abs(value.R - expected.R) + Math.Abs(value.G - expected.G) + Math.Abs(value.B - expected.B) + Math.Abs(value.A - expected.A), 0, 0.00001);
            Assert.True(value.IsEqualApprox(Interpolators.Color(from, to, at, space, alpha, encoding)));
        }
    }

    private static Interpolation Mode(int mode, int ease = 0) => mode switch
    { 0 => Interpolation.Smooth, 1 => Interpolation.Linear, 2 => Interpolation.Step, _ => (EaseType)ease };

    [Fact]
    public void SmoothIsBoundedOnUnevenStopsIncludingPlateausAndTurningPoints()
    {
        double[] stops = [0, 0.01, 4, 17, 43, 43.1, 81, 100];
        double[][] sequences = [[0, 0, 1, 1, 0, -1, -1, 2], [0, 1, 2, 3, 4, 5, 6, 7], [9, 8, 7, 6, 5, 4, 3, 2], [0, 0, 0, 0, 0, 0, 0, 0]];
        foreach (var values in sequences)
        {
            var curve = new KeyframeCurve<double>(stops.Select((v, i) => new CurveKey<double>(v, values[i])).ToArray());
            for (var segment = 0; segment < stops.Length - 1; segment++)
                for (var i = 0; i <= 100; i++)
                    Assert.InRange(curve.Sample((stops[segment] + (stops[segment + 1] - stops[segment]) * i / 100) / 100),
                        Math.Min(values[segment], values[segment + 1]) - 1e-10, Math.Max(values[segment], values[segment + 1]) + 1e-10);
            foreach (var key in stops.Select((v, i) => (v, i))) Assert.Equal(values[key.i], curve.Sample(key.v / 100), 10);
        }
    }

    [Fact]
    public void CapturesAndSourceArraysAreIndependent()
    {
        CurveKey<double>[] keys = [new(50, 10)];
        var source = new KeyframeCurve<double>(keys, Interpolation.Linear);
        keys[0] = new(50, 99);
        Assert.True(source.NeedsStart);
        Assert.Equal(2, source.Count);
        Assert.Throws<InvalidOperationException>(() => source.Sample(0));
        var a = source.CaptureStart(0); var b = source.CaptureStart(4);
        Assert.Equal(5, a.Sample(0.25)); Assert.Equal(7, b.Sample(0.25));
        Assert.Same(a, a.CaptureStart(99));
        Assert.Equal(10, a.Sample(0.9));
        var hidden = new Color(1, 0, 0, 0);
        var colors = new KeyframeCurve<Color>([new(0, Colors.White), new(0.23, hidden), new(100, Colors.Blue)]);
        Assert.Equal(hidden, colors.Sample(0.23 / 100));
    }

    [Fact]
    public void ValueTypesAndQuaternionShortestArc()
    {
        Assert.Equal(1f, KeyframeCurve<float>.EvenlySpaced([0, 2]).Sample(0.5));
        Assert.Equal(1, KeyframeCurve<int>.EvenlySpaced([0, 1]).Sample(0.5));
        Assert.Equal(new Vector2(1, 2), KeyframeCurve<Vector2>.EvenlySpaced([Vector2.Zero, new(2, 4)]).Sample(0.5));
        Assert.Equal(new Vector3(1, 2, 3), KeyframeCurve<Vector3>.EvenlySpaced([Vector3.Zero, new(2, 4, 6)]).Sample(0.5));
        Assert.Equal(new Vector4(1, 2, 3, 4), KeyframeCurve<Vector4>.EvenlySpaced([Vector4.Zero, new(2, 4, 6, 8)]).Sample(0.5));
        Assert.Equal(new Rect2(1, 2, 3, 4), KeyframeCurve<Rect2>.EvenlySpaced([default, new(2, 4, 6, 8)]).Sample(0.5));
        var end = new Quaternion(Vector3.Up, 1);
        foreach (var mode in new[] { Interpolation.Smooth, Interpolation.Linear, (Interpolation)EaseType.QuadIn })
        {
            var q = KeyframeCurve<Quaternion>.EvenlySpaced([Quaternion.Identity * 2, -end], mode);
            Assert.True(q.Sample(0).IsNormalized());
            var weight = mode == (Interpolation)EaseType.QuadIn ? 0.25f : 0.5f;
            Assert.True(Math.Abs(q.Sample(0.5).Dot(Quaternion.Identity.Slerp(end, weight))) > 0.99999f);
            Assert.True(q.Sample(-0.1).IsNormalized()); Assert.True(q.Sample(1.1).IsNormalized());
        }
        var step = KeyframeCurve<Quaternion>.EvenlySpaced([Quaternion.Identity, end], Interpolation.Step);
        Assert.Equal(Quaternion.Identity, step.Sample(0.5)); Assert.True(step.Sample(1).IsEqualApprox(end));
    }

    [Fact]
    public void Int64KeysPreserveExactStopsRoundAndSaturateSamples()
    {
        var curve = KeyframeCurve<long>.EvenlySpaced([0, 5_000_000_000], Interpolation.Linear);
        Assert.Equal(2_500_000_000, curve.Sample(0.5));
        Assert.Equal(1, KeyframeCurve<long>.EvenlySpaced([0, 1]).Sample(0.5));
        Assert.Equal(-1, KeyframeCurve<long>.EvenlySpaced([0, -1]).Sample(0.5));
        var limits = KeyframeCurve<long>.EvenlySpaced([long.MinValue, long.MaxValue], Interpolation.Linear);
        Assert.Equal(long.MinValue, limits.Sample(0)); Assert.Equal(long.MaxValue, limits.Sample(1));
        Assert.Equal(long.MinValue, limits.Sample(-0.1)); Assert.Equal(long.MaxValue, limits.Sample(1.1));
    }

    [Fact]
    public void SmoothHugeValuesRemainFiniteAndBoundedOnShortSegments()
    {
        double[] stops = [0, 0.001, 1, 99, 100];
        double[] values = [-double.MaxValue, double.MaxValue, -double.MaxValue, 0, double.MaxValue];
        var curve = new KeyframeCurve<double>(stops.Select((v, i) => new CurveKey<double>(v, values[i])).ToArray());
        for (var segment = 0; segment < stops.Length - 1; segment++)
            for (var i = 1; i < 100; i++)
                Assert.InRange(curve.Sample((stops[segment] + (stops[segment + 1] - stops[segment]) * i / 100) / 100),
                    Math.Min(values[segment], values[segment + 1]), Math.Max(values[segment], values[segment + 1]));
    }

    [Fact]
    public void InvalidDataFailsBeforeSampling()
    {
        Assert.Throws<ArgumentException>(() => new KeyframeCurve<double>([]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<double>.EvenlySpaced([1]));
        foreach (var stop in new[] { -1, 101, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentException>(() => new KeyframeCurve<double>([new(stop, 0)]));
        Assert.Throws<ArgumentException>(() => new KeyframeCurve<double>([new(50, 1), new(40, 2)]));
        Assert.Throws<ArgumentException>(() => new KeyframeCurve<double>([new(0, 1), new(0, 2)]));
        Assert.Throws<ArgumentException>(() => new KeyframeCurve<double>([new(0, 1, Interpolation.Linear)]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<double>.EvenlySpaced([0, double.NaN]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<Color>.EvenlySpaced([new(1e30f, 1e30f, 1e30f), Colors.Blue]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<Quaternion>.EvenlySpaced([default, Quaternion.Identity]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<Quaternion>.EvenlySpaced([new(float.MaxValue, 0, 0, 1), Quaternion.Identity]));
        Assert.Throws<ArgumentException>(() => KeyframeCurve<Quaternion>.EvenlySpaced([new(float.Epsilon, 0, 0, 0), Quaternion.Identity]));
        Assert.Throws<NotSupportedException>(() => KeyframeCurve<DateTime>.EvenlySpaced([default, default]));
        var curve = KeyframeCurve<double>.EvenlySpaced([0, 1], EaseType.QuadIn);
        Assert.Throws<ArgumentOutOfRangeException>(() => curve.Sample(double.NaN));
        Assert.InRange(curve.Sample(-0.1), -0.001, 0);
        Assert.InRange(curve.Sample(1.1), 1.199, 1.201);
    }

    [Fact]
    public void PreparedSamplingAllocatesZeroManagedBytes()
    {
        var scalar = KeyframeCurve<double>.EvenlySpaced([0, 2, 1, 3]);
        var color = KeyframeCurve<Color>.EvenlySpaced([Colors.Red, Colors.Blue, Colors.Green]);
        for (var i = 0; i < 2000; i++) { scalar.Sample(0.3); color.Sample(0.3); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        double sum = 0;
        for (var i = 0; i < 10000; i++) { sum += scalar.Sample(i % 100 / 100.0); sum += color.Sample(i % 100 / 100.0).R; }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(double.IsFinite(sum)); Assert.Equal(0, allocated);
    }
}
