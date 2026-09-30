// Shared numeric fixtures run in C#, GDScript, and the actual website easing code.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { composeEase, canonicalFamily, FAMILIES, legEase, pairedLegEase, ease } from '../src/scripts/motion.ts';

const data = JSON.parse(readFileSync(new URL('../../tests/conformance/easing.json', import.meta.url), 'utf8'));
for (const sample of data.cases) {
  assert.ok(Math.abs(composeEase(sample.in, sample.out, sample.progress, sample.skew, sample.blendType, sample.blend) - sample.expected) < data.tolerance,
    JSON.stringify(sample));
}
const calibrated = f => /^(Back|Elastic|Bounce|Jump)/.test(f);
for (const family of FAMILIES.filter(f => !calibrated(f))) {
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
        if (t <= 0.4) assert.equal(value, pairedLegEase(entry, t));
        if (t >= 0.6) assert.equal(value, pairedLegEase(exit, t));
      }
    }
  }
}
console.log(`Easing: ${data.cases.length} shared samples and all ${new Set(FAMILIES.map(canonicalFamily)).size ** 2} pairs plus missing legs passed.`);

// Test behavior, rather than duplicating the interpolation formula.
const monotone = FAMILIES.filter(f => !calibrated(f));
for (const method of ['Makima', 'Hermite']) {
  for (const a of monotone) for (const b of monotone) for (const width of [0, .01, .2, .4, .8, 1]) {
    let previous = 0;
    for (let i=0; i<=1000; i++) {
      const y = composeEase(a,b,i/1000,1,method,width);
      assert.ok(y >= previous-1e-12 && y <= 1+1e-12, JSON.stringify({method,a,b,width,i,y,previous}));
      previous=y;
    }
  }
  for (const a of FAMILIES) for (const b of FAMILIES) {
    if (canonicalFamily(a)===canonicalFamily(b)) continue; // Preserve authored corners/singularities in matching families.
    for (const width of [.1,.4,.8]) {
      const h=1e-6, f=p=>composeEase(a,b,p,1,method,width);
      for (const t of [.5-width/2,.5,.5+width/2])
        assert.ok(Math.abs((f(t)-f(t-h))/h-(f(t+h)-f(t))/h)<.01, JSON.stringify({method,a,b,width,t}));
      if (method !== 'Makima') continue;
      // Makima weights are nonnegative, so the midpoint velocity is a weighted mean of the two secants.
      const d0 = (.5-f(.5-width/2))/(width/2), d1 = (f(.5+width/2)-.5)/(width/2), v = (f(.5+h)-f(.5-h))/(2*h);
      assert.ok(v >= Math.min(d0,d1)-1e-3 && v <= Math.max(d0,d1)+1e-3, JSON.stringify({a,b,width,v,d0,d1}));
    }
  }
}
console.log('Makima and Hermite: monotonicity, bounds and velocity continuity passed.');

for (const base of ['Back', 'Elastic', 'Jump']) for (const percent of [10,20,30,40,50]) {
  const family=base+percent, amount=percent/100;
  let soloLow=0, soloHigh=1, pairLow=0, pairHigh=1;
  for (let i=0; i<=10000; i++) {
    const t=i/10000, a=composeEase(family,'None',t), b=composeEase('None',family,t), pair=composeEase(family,family,t);
    assert.ok(Math.abs(a-(1-composeEase('None',family,1-t)))<1e-12);
    soloLow=Math.min(soloLow,a); soloHigh=Math.max(soloHigh,b);
    pairLow=Math.min(pairLow,pair); pairHigh=Math.max(pairHigh,pair);
    if (percent===10) {
      assert.equal(b,composeEase('None',base,t));
      assert.equal(pair,composeEase(base,family,t));
    }
  }
  for (const peak of [-soloLow,soloHigh-1,-pairLow,pairHigh-1]) assert.ok(Math.abs(peak-amount)<.00002, family+': '+peak);
}
assert.ok(ease('ElasticOut',.13474)>1.37);
console.log('Back/Elastic/Jump: 10%-50% solo and paired peaks, symmetry, aliases, and legacy preservation passed.');

for (const percent of [10,20,30,40,50]) {
  const family='Bounce'+percent;
  for (const paired of [false,true]) {
    const values=Array.from({length:10001},(_,i)=>composeEase(paired?family:'None',family,(paired?.5:0)+(paired?.5:1)*i/10000));
    const depths=values.filter((v,i)=>i>0&&i<values.length-1&&v<values[i-1]&&v<=values[i+1]).map(v=>1-v);
    assert.equal(depths.length,3);
    depths.forEach((v,i)=>assert.ok(Math.abs(v-percent/100/4**i)<.00002,JSON.stringify({family,paired,i,v})));
  }
  for (let i=0;i<=1000;i++) {
    const t=i/1000, a=composeEase(family,'None',t), b=composeEase('None',family,t), pair=composeEase(family,family,t);
    assert.ok(Math.abs(a-(1-composeEase('None',family,1-t)))<1e-12);
    for (const y of [a,b,pair]) assert.ok(y>=-1e-12&&y<=1+1e-12);
    if (percent===10) {
      assert.equal(b,composeEase('None','Bounce',t));
      assert.equal(pair,composeEase('Bounce','Bounce10',t));
    }
  }
}
assert.equal(ease('BounceOut',6/11),.75);
assert.equal(ease('BounceInOut',17/22),.875);
console.log('Bounce: 10%-50% first rebounds, three diminishing bounces, bounds, mirrors, aliases, and legacy preservation passed.');

for (const percent of [10,20,30,40,50]) {
  const family='Jump'+percent;
  for (const paired of [false,true]) {
    const values=Array.from({length:10001},(_,i)=>composeEase(paired?family:'None',family,(paired?.5:0)+(paired?.5:1)*i/10000));
    const peaks=[];
    let landed=false;
    for (let i=1;i<values.length-1;i++) {
      if (values[i]>values[i-1]&&values[i]>=values[i+1]) peaks.push(i);
      if (values[i]>=1) landed=true;
      if (landed) assert.ok(values[i]>=1-1e-12);
    }
    assert.equal(peaks.length,3);
    peaks.forEach((p,i)=>{
      assert.ok(Math.abs(values[p]-1-percent/100/4**i)<.00002,JSON.stringify({family,paired,i,value:values[p]}));
      if (i>0) assert.ok(Math.abs(Math.min(...values.slice(peaks[i-1],p+1))-1)<.001);
    });
    for (let i=1;i<=peaks[0];i++) assert.ok(values[i]>=values[i-1]);
  }
}
console.log('Jump: three peaks above the target, diminishing heights, intervening landings and a direct launch passed.');
