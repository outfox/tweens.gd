// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// SPDX-FileCopyrightText: 2020 Jeffrey Lanters

// Easing math adapted from unity-tweens. See THIRD-PARTY-NOTICES.md.
#nullable enable
using System;
using System.Numerics;
using Godot;

namespace tweens.gd {
  public static class Easing {
    /// <summary>Sample a curve. Width is a fraction of normalized time in [0, 1]; zero directly splices the halves.</summary>
    public static float Evaluate(EaseType ease, float progress, BlendType blendType = BlendType.Hermite, double blend = 0.4)
      => GetFunction(ease, blendType, blend)(Math.Clamp(progress, 0, 1));

    const float ConstantA = 1.70158f;
    const float ConstantB = ConstantA * 1.525f;
    const float ConstantC = ConstantA + 1f;
    const float ConstantD = 2f * Mathf.Pi / 3f;
    const float ConstantE = 2f * Mathf.Pi / 4.5f;
    const float ConstantF = 7.5625f;
    const float ConstantG = 2.75f;

    // Bits 0–7 are reserved for the legacy enum. One bit per curve lets us reject
    // accidental In.Sine | In.Back combinations instead of silently choosing a third curve.
    const long LegMask = (1L << 13) - 1;
    // Preserve the original flag values; extra In/Out choices occupy bits 34–41 / 42–49.
    const int CurveCount = 21;
    const long CompositionMask = ((1L << 50) - 1) & ~255L;
    static ulong LegBits(long bits, bool exit) => (ulong)(((bits >> (exit ? 21 : 8)) & LegMask)
      | (((bits >> (exit ? 42 : 34)) & 255) << 13));

    // Calibrated for 10%, 20%, ... 50% of the full range. Paired legs have half the
    // value range, so their local peak must be twice the requested overshoot.
    // Back: peak = 4*s^3 / (27*(s+1)^2).
    static readonly float[] BackSolo = [1.701540198866824f, 2.5923889015162995f, 3.3940516581445603f, 4.155744652639195f, 4.894859521133737f];
    static readonly float[] BackPaired = [2.5923889015162995f, 4.155744652639195f, 5.619622918334311f, 7.042439379340937f, 8.44353560159325f];
    const float ElasticSoloPeriod = 0.7553423501870573f;
    const float ElasticPairPeriod = 0.5074981597799941f;
    static readonly float[] ElasticSoloKick = [0, 0.6853132138892408f, 1.1091787748281363f, 1.4696240828544362f, 1.8012905799033314f];
    static readonly float[] ElasticPairKick = [0, 0.8829462755133655f, 1.4362972653938577f, 1.9263692424370968f, 2.3898041023212153f];
    static readonly Func<float, float>[] InCurves =
    [
      Linear, SineIn, QuadIn, CubicIn, QuartIn, QuintIn, ExpoIn, CircIn,
      t => BackLegIn(t, 0), t => ElasticLegIn(t, 0), BounceIn, SmoothStep, SmootherStep,
      t => BackLegIn(t, 1), t => BackLegIn(t, 2), t => BackLegIn(t, 3), t => BackLegIn(t, 4),
      t => ElasticLegIn(t, 1), t => ElasticLegIn(t, 2), t => ElasticLegIn(t, 3), t => ElasticLegIn(t, 4)
    ];
    static readonly Func<float, float>[] OutCurves =
    [
      Linear, SineOut, QuadOut, CubicOut, QuartOut, QuintOut, ExpoOut, CircOut,
      t => BackLegOut(t, 0), t => ElasticLegOut(t, 0), BounceOut, SmoothStep, SmootherStep,
      t => BackLegOut(t, 1), t => BackLegOut(t, 2), t => BackLegOut(t, 3), t => BackLegOut(t, 4),
      t => ElasticLegOut(t, 1), t => ElasticLegOut(t, 2), t => ElasticLegOut(t, 3), t => ElasticLegOut(t, 4)
    ];
    static readonly Func<float, float>[] InOutCurves =
    [
      Linear, SineInOut, QuadInOut, CubicInOut, QuartInOut, QuintInOut, ExpoInOut, CircInOut,
      t => BackPair(t, 0), t => ElasticPair(t, 0), BounceInOut, SmoothStep, SmootherStep,
      t => BackPair(t, 1), t => BackPair(t, 2), t => BackPair(t, 3), t => BackPair(t, 4),
      t => ElasticPair(t, 1), t => ElasticPair(t, 2), t => ElasticPair(t, 3), t => ElasticPair(t, 4)
    ];
    static readonly Func<float, float>[,] Compositions = CreateCompositions();

    static Func<float, float>[,] CreateCompositions() {
      var functions = new Func<float, float>[CurveCount, CurveCount];
      for (var i = 0; i < CurveCount; i++)
        for (var o = 0; o < CurveCount; o++) {
          functions[i, o] = Compose(i, o, BlendType.Hermite, 0.4f);
        }
      return functions;
    }

    // Analytic derivative of the paired first half (the second half is symmetric).
    static float PairSlope(int family, float time) {
      var t = Math.Min(time, 1 - time);
      var x = 2 * t;
      return family switch {
        0 => 1,
        1 => Mathf.Pi * Mathf.Sin(Mathf.Pi * t) / 2,
        2 => 4 * t,
        3 => 12 * t * t,
        4 => 32 * t * t * t,
        5 => 80 * t * t * t * t,
        6 => 10 * Mathf.Log(2) * Mathf.Pow(2, 20 * t - 10),
        7 => x / Mathf.Sqrt(1 - x * x),
        8 or >= 13 and <= 16 => BackSlope(x, family == 8 ? 0 : family - 12),
        9 or >= 17 and <= 20 => ElasticSlope(1 - x, family == 9 ? 0 : family - 16),
        10 => BounceSlope(1 - x),
        11 => 6 * t * (1 - t),
        _ => 30 * t * t * (1 - t) * (1 - t),
      };
    }

    static float BounceSlope(float t) {
      if (t >= 2.5f / ConstantG) t -= 2.625f / ConstantG;
      else if (t >= 2f / ConstantG) t -= 2.25f / ConstantG;
      else if (t >= 1f / ConstantG) t -= 1.5f / ConstantG;
      return 2 * ConstantF * t;
    }

    static float Hermite(float u, float y0, float y1, float m0, float m1) {
      var u2 = u * u;
      var u3 = u2 * u;
      return (2 * u3 - 3 * u2 + 1) * y0 + (u3 - 2 * u2 + u) * m0
        + (-2 * u3 + 3 * u2) * y1 + (u3 - u2) * m1;
    }

    static Func<float, float> Compose(int i, int o, BlendType method, float blend) {
      var entry = InOutCurves[i];
      var exit = InOutCurves[o];
      if (i == o) return entry;
      var h = blend / 2;
      var left = 0.5f - h;
      var right = 0.5f + h;
      if (left == 0.5f) return t => t <= 0.5f ? entry(t) : exit(t);
      var y0 = entry(left);
      var y1 = exit(right);
      var v0 = PairSlope(i, left);
      var v1 = PairSlope(o, right);
      var d0 = (0.5f - y0) / h;
      var d1 = (y1 - 0.5f) / h;
      // Solve for equal acceleration at the midpoint, then limit the shared tangent
      // to avoid introducing reversals in monotone legs. Outer tangents stay exact.
      var middle = Math.Clamp((3 * (d0 + d1) - v0 - v1) / 4, 0, 3 * Math.Max(0, Math.Min(d0, d1)));
      return t => {
        if (t <= left) return entry(t);
        if (t >= right) return exit(t);
        if (method == BlendType.Hermite)
          return t <= 0.5f
            ? Hermite((t - left) / h, y0, 0.5f, h * v0, h * middle)
            : Hermite((t - 0.5f) / h, 0.5f, y1, h * middle, h * v1);
        var u = (t - left) / blend;
        var weight = method == BlendType.SmoothStep ? SmoothStep(u) : u;
        return entry(t) * (1 - weight) + exit(t) * weight;
      };
    }

    internal static void ValidateBlend(BlendType blendType, double blend) {
      if (!Enum.IsDefined(blendType)) throw new ArgumentOutOfRangeException(nameof(blendType));
      if (!double.IsFinite(blend) || blend < 0 || blend > 1) throw new ArgumentOutOfRangeException(nameof(blend));
    }

    internal static Func<float, float> GetFunction(EaseType easeType, BlendType blendType = BlendType.Hermite, double blend = 0.4) {
      ValidateBlend(blendType, blend);
      var bits = (long)easeType;
      if (bits >= 256 && (bits & ~CompositionMask) == 0) {
        var entry = LegBits(bits, false);
        var exit = LegBits(bits, true);
        if ((entry == 0 || BitOperations.IsPow2(entry)) && (exit == 0 || BitOperations.IsPow2(exit))) {
          if (entry == 0) return OutCurves[BitOperations.TrailingZeroCount(exit)];
          if (exit == 0) return InCurves[BitOperations.TrailingZeroCount(entry)];
          var i = BitOperations.TrailingZeroCount(entry);
          var o = BitOperations.TrailingZeroCount(exit);
          return blendType == BlendType.Hermite && blend == 0.4 ? Compositions[i, o] : Compose(i, o, blendType, (float)blend);
        }
      }
      return easeType switch {
        EaseType.Linear => Linear,
        EaseType.SineIn => SineIn,
        EaseType.SineOut => SineOut,
        EaseType.SineInOut => SineInOut,
        EaseType.QuadIn => QuadIn,
        EaseType.QuadOut => QuadOut,
        EaseType.QuadInOut => QuadInOut,
        EaseType.CubicIn => CubicIn,
        EaseType.CubicOut => CubicOut,
        EaseType.CubicInOut => CubicInOut,
        EaseType.QuartIn => QuartIn,
        EaseType.QuartOut => QuartOut,
        EaseType.QuartInOut => QuartInOut,
        EaseType.QuintIn => QuintIn,
        EaseType.QuintOut => QuintOut,
        EaseType.QuintInOut => QuintInOut,
        EaseType.ExpoIn => ExpoIn,
        EaseType.ExpoOut => ExpoOut,
        EaseType.ExpoInOut => ExpoInOut,
        EaseType.CircIn => CircIn,
        EaseType.CircOut => CircOut,
        EaseType.CircInOut => CircInOut,
        EaseType.BackIn => BackIn,
        EaseType.BackOut => BackOut,
        EaseType.BackInOut => BackInOut,
        EaseType.ElasticIn => ElasticIn,
        EaseType.ElasticOut => ElasticOut,
        EaseType.ElasticInOut => ElasticInOut,
        EaseType.BounceIn => BounceIn,
        EaseType.BounceOut => BounceOut,
        EaseType.BounceInOut => BounceInOut,
        EaseType.SmoothStep => SmoothStep,
        EaseType.SmootherStep => SmootherStep,
        _ => throw new NotImplementedException($"EaseType {easeType} not implemented"),
      };
    }

    static float Linear(float time) {
      return time;
    }

    static float SmoothStep(float time) {
      return time * time * (3f - 2f * time);
    }

    static float SmootherStep(float time) {
      return time * time * time * (time * (6f * time - 15f) + 10f);
    }

    static float SineIn(float time) {
      return 1f - Mathf.Cos((time * Mathf.Pi) / 2f);
    }

    static float SineOut(float time) {
      return Mathf.Sin((time * Mathf.Pi) / 2f);
    }

    static float SineInOut(float time) {
      return -(Mathf.Cos(Mathf.Pi * time) - 1f) / 2f;
    }

    static float QuadIn(float time) {
      return time * time;
    }

    static float QuadOut(float time) {
      return 1 - (1 - time) * (1 - time);
    }

    static float QuadInOut(float time) {
      return time < 0.5f ? 2 * time * time : 1 - Mathf.Pow(-2 * time + 2, 2) / 2;
    }

    static float CubicIn(float time) {
      return time * time * time;
    }

    static float CubicOut(float time) {
      return 1 - Mathf.Pow(1 - time, 3);
    }

    static float CubicInOut(float time) {
      return time < 0.5f ? 4 * time * time * time : 1 - Mathf.Pow(-2 * time + 2, 3) / 2;
    }

    static float QuartIn(float time) {
      return time * time * time * time;
    }

    static float QuartOut(float time) {
      return 1 - Mathf.Pow(1 - time, 4);
    }

    static float QuartInOut(float time) {
      return time < 0.5 ? 8 * time * time * time * time : 1 - Mathf.Pow(-2 * time + 2, 4) / 2;
    }

    static float QuintIn(float time) {
      return time * time * time * time * time;
    }

    static float QuintOut(float time) {
      return 1 - Mathf.Pow(1 - time, 5);
    }

    static float QuintInOut(float time) {
      return time < 0.5f ? 16 * time * time * time * time * time : 1 - Mathf.Pow(-2 * time + 2, 5) / 2;
    }

    static float ExpoIn(float time) {
      return time == 0 ? 0 : Mathf.Pow(2, 10 * time - 10);
    }

    static float ExpoOut(float time) {
      return time == 1 ? 1 : 1 - Mathf.Pow(2, -10 * time);
    }

    static float ExpoInOut(float time) {
      return time == 0 ? 0 : time == 1 ? 1 : time < 0.5 ? Mathf.Pow(2, 20 * time - 10) / 2 : (2 - Mathf.Pow(2, -20 * time + 10)) / 2;
    }

    static float CircIn(float time) {
      return 1 - Mathf.Sqrt(1 - Mathf.Pow(time, 2));
    }

    static float CircOut(float time) {
      return Mathf.Sqrt(1 - Mathf.Pow(time - 1, 2));
    }

    static float CircInOut(float time) {
      return time < 0.5 ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * time, 2))) / 2 : (Mathf.Sqrt(1 - Mathf.Pow(-2 * time + 2, 2)) + 1) / 2;
    }

    static float BackIn(float time) {
      return ConstantC * time * time * time - ConstantA * time * time;
    }

    static float BackOut(float time) {
      return 1f + ConstantC * Mathf.Pow(time - 1, 3) + ConstantA * Mathf.Pow(time - 1, 2);
    }

    static float BackInOut(float time) {
      return time < 0.5 ?
        Mathf.Pow(2 * time, 2) * ((ConstantB + 1) * 2 * time - ConstantB) / 2 :
        (Mathf.Pow(2 * time - 2, 2) * ((ConstantB + 1) * (time * 2 - 2) + ConstantB) + 2) / 2;
    }

    static float BackLegIn(float t, int level, bool paired = false) {
      if (t == 0 || t == 1) return t;
      var s = (paired ? BackPaired : BackSolo)[level];
      return (s + 1) * t * t * t - s * t * t;
    }

    static float BackLegOut(float t, int level, bool paired = false) => 1 - BackLegIn(1 - t, level, paired);
    static float BackPair(float t, int level) => t < 0.5f ? BackLegIn(2 * t, level, true) / 2
      : 0.5f + BackLegOut(2 * t - 1, level, true) / 2;

    static float BackSlope(float t, int level) {
      var s = BackPaired[level];
      return 3 * (s + 1) * t * t - 2 * s * t;
    }

    // A damped oscillator with an adjustable sine term keeps both endpoints fixed.
    // The periods set the 10% baseline; the kicks set each larger peak analytically.
    static float ElasticLegOut(float t, int level, bool paired = false) {
      if (t == 0 || t == 1) return t;
      var angle = Mathf.Tau * t / (paired ? ElasticPairPeriod : ElasticSoloPeriod);
      var kick = (paired ? ElasticPairKick : ElasticSoloKick)[level];
      return 1 - Mathf.Pow(2, -10 * t) * (Mathf.Cos(angle) - kick * Mathf.Sin(angle));
    }

    static float ElasticLegIn(float t, int level, bool paired = false) => 1 - ElasticLegOut(1 - t, level, paired);
    static float ElasticPair(float t, int level) => t < 0.5f ? ElasticLegIn(2 * t, level, true) / 2
      : 0.5f + ElasticLegOut(2 * t - 1, level, true) / 2;

    static float ElasticSlope(float t, int level) {
      var omega = Mathf.Tau / ElasticPairPeriod;
      var decay = 10 * Mathf.Log(2);
      var kick = ElasticPairKick[level];
      return Mathf.Pow(2, -10 * t) * ((decay + kick * omega) * Mathf.Cos(omega * t)
        + (omega - kick * decay) * Mathf.Sin(omega * t));
    }

    static float ElasticIn(float time) {
      return time == 0 ? 0 : time == 1 ? 1 : -Mathf.Pow(2, 10 * time - 10) * Mathf.Sin((time * 10f - 10.75f) * ConstantD);
    }

    static float ElasticOut(float time) {
      return time == 0 ? 0 : time == 1 ? 1 : Mathf.Pow(2, -10 * time) * Mathf.Sin((time * 10 - 0.75f) * ConstantD) + 1;
    }

    static float ElasticInOut(float time) {
      return time == 0 ? 0 : time == 1 ? 1 : time < 0.5 ? -(Mathf.Pow(2, 20 * time - 10) * Mathf.Sin((20 * time - 11.125f) * ConstantE)) / 2 : Mathf.Pow(2, -20 * time + 10) * Mathf.Sin((20 * time - 11.125f) * ConstantE) / 2 + 1;
    }

    static float BounceIn(float time) {
      return 1 - BounceOut(1 - time);
    }

    static float BounceOut(float time) {
      if (time < 1 / ConstantG)
        return ConstantF * time * time;
      else if (time < 2 / ConstantG)
        return ConstantF * (time -= 1.5f / ConstantG) * time + 0.75f;
      else if (time < 2.5f / ConstantG)
        return ConstantF * (time -= 2.25f / ConstantG) * time + 0.9375f;
      else
        return ConstantF * (time -= 2.625f / ConstantG) * time + 0.984375f;
    }

    static float BounceInOut(float time) {
      return time < 0.5f ? (1 - BounceOut(1 - 2 * time)) / 2 : (1 + BounceOut(2 * time - 1)) / 2;
    }
  }
}
