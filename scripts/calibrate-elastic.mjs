// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
// Reproduce the peak-calibrated Elastic parameters shared by C#, C++ and the website.
import assert from 'node:assert/strict';

const profiles = {
  solo: { decay: 17.553423501870573, tail: 8, period: 0.43031056027706766, range: 1 },
  pair: { decay: 15.074981597799942 / 2, tail: 0, period: 0.33664927316001425 * 2, range: 0.5 },
};

function value(t, kick, { decay, tail, period }) {
  const omega = 2 * Math.PI / period;
  const residual = 2 ** (-decay + tail) * (Math.cos(omega) - kick * Math.sin(omega));
  return (1 - 2 ** (-decay * t + tail * t * t) * (Math.cos(omega * t) - kick * Math.sin(omega * t))) / (1 - residual);
}

function peak(kick, profile) {
  // The first overshoot is the largest; bracket it, then maximize without sampling error.
  let left = 0, right = profile.period;
  for (let i = 0; i < 100; i++) {
    const a = left + (right - left) / 3, b = right - (right - left) / 3;
    if (value(a, kick, profile) < value(b, kick, profile)) left = a;
    else right = b;
  }
  return value((left + right) / 2, kick, profile) - 1;
}

for (const [name, profile] of Object.entries(profiles)) {
  const kicks = [];
  for (const percent of [10, 20, 30, 40, 50]) {
    let left = -0.4, right = 8;
    const amount = percent / 100 / profile.range;
    assert.ok(peak(left, profile) < amount && peak(right, profile) > amount);
    for (let i = 0; i < 100; i++) {
      const middle = (left + right) / 2;
      if (peak(middle, profile) < amount) left = middle;
      else right = middle;
    }
    const kick = (left + right) / 2;
    kicks.push(kick);
    assert.ok(Math.abs(peak(kick, profile) - amount) < 1e-12);
    let previous = value(0, kick, profile), peaks = [];
    for (let i = 1; i < 10000; i++) {
      const t = i / 10000, y = value(t, kick, profile), next = value(t + 0.0001, kick, profile);
      if (y > previous && y >= next) peaks.push([+t.toFixed(4), +((y - 1) * profile.range).toFixed(6)]);
      previous = y;
    }
    console.log(`${name} ${percent}% peaks: ${JSON.stringify(peaks)}`);
  }
  console.log(`${name}: decay=${profile.decay}, tail=${profile.tail}, period=${profile.period}`);
  console.log(`${name} kicks: ${JSON.stringify(kicks)}`);
}
