// Shared numeric fixtures run in C#, GDScript, and the actual website easing code.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { composeEase, FAMILIES, legEase, pairedLegEase, ease } from '../src/scripts/motion.ts';

const data = JSON.parse(readFileSync(new URL('../../tests/conformance/easing.json', import.meta.url), 'utf8'));
for (const sample of data.cases) {
  assert.ok(Math.abs(composeEase(sample.in, sample.out, sample.progress, sample.skew, sample.blendType, sample.blend) - sample.expected) < data.tolerance,
    JSON.stringify(sample));
}
for (const family of FAMILIES) {
  const legacy = ['Linear', 'SmoothStep', 'SmootherStep'].includes(family) ? family : family + 'InOut';
  for (let i = 0; i <= 1000; i++) assert.equal(composeEase(family, family, i / 1000), ease(legacy, i / 1000));
}
for (const entry of ['None', ...FAMILIES]) {
  for (const exit of ['None', ...FAMILIES]) {
    assert.ok(Math.abs(composeEase(entry, exit, -1)) < 1e-10);
    assert.ok(Math.abs(composeEase(entry, exit, 2) - 1) < 1e-10);
    assert.ok(Number.isNaN(composeEase(entry, exit, NaN)));
    if (entry !== 'None' && exit !== 'None') assert.ok(Math.abs(composeEase(entry, exit, 0.5) - 0.5) < 1e-10);
    for (let i = 0; i <= 100; i++) {
      const t = i / 100;
      const value = composeEase(entry, exit, t);
      assert.ok(Number.isFinite(value));
      if (exit === 'None') assert.equal(value, legEase(entry, 'In', t));
      else if (entry === 'None') assert.equal(value, legEase(exit, 'Out', t));
      else {
        if (t <= 0.3) assert.equal(value, pairedLegEase(entry, t));
        if (t >= 0.7) assert.equal(value, pairedLegEase(exit, t));
      }
    }
  }
}
console.log(`Easing: ${data.cases.length} shared samples and all 169 pairs plus missing legs passed.`);

// Test behavior, rather than duplicating the interpolation formula.
const monotone = FAMILIES.filter(f => !['Back', 'Elastic', 'Bounce'].includes(f));
for (const a of monotone) for (const b of monotone) for (const width of [0, .01, .2, .4, .8, 1]) {
  let previous = 0;
  for (let i=0; i<=1000; i++) {
    const y = composeEase(a,b,i/1000,1,'Hermite',width);
    assert.ok(y >= previous-1e-12 && y <= 1+1e-12, JSON.stringify({a,b,width,i,y,previous}));
    previous=y;
  }
}
for (const a of FAMILIES) for (const b of FAMILIES) {
  if (a===b) continue; // Preserve authored corners/singularities in matching families.
  for (const width of [.1,.4,.8]) for (const t of [.5-width/2,.5,.5+width/2]) {
    const h=1e-6, f=p=>composeEase(a,b,p,1,'Hermite',width);
    assert.ok(Math.abs((f(t)-f(t-h))/h-(f(t+h)-f(t))/h)<.01, JSON.stringify({a,b,width,t}));
  }
}
console.log('Hermite: monotonicity, bounds and velocity continuity passed.');
