// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, readFile, writeFile, mkdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

const names = ['CanvasItemInstanceShaderParameter<Vector4>', 'GeometryInstanceShaderParameter<Vector4>'];
function fixture() {
  return { HostEnvironmentInfo: { BenchmarkDotNetVersion: 'fixture' }, Benchmarks:
    names.flatMap(name => [100, 1000].flatMap(count => ['DirectWrite', 'TweenUpdate'].map(method => ({
      Type: 'AdapterCatalogBenchmarks', Method: method,
      // Deliberately identical abbreviations: FullName must preserve identity.
      Parameters: `Count=${count}&Definition=Long(...)name`,
      FullName: `TweensBenchmarks.AdapterCatalogBenchmarks.${method}(Count: ${count}, Definition: "${name}")`,
      Statistics: { Mean: count * (method === 'DirectWrite' ? 10 : 40), ConfidenceInterval: { Margin: 2 }, OriginalValues: [1, 2, 3] },
      Memory: { BytesAllocatedPerOperation: method === 'DirectWrite' ? 0 : count * 32 },
      Metrics: ['CompletedWorkItemCount', 'LockContentionCount'].map(Id => ({ Descriptor: { Id }, Value: 0 })),
    })))) };
}

async function run(report) {
  await mkdir('artifacts', { recursive: true });
  const folder = await mkdtemp(resolve('artifacts/catalog-export-test-'));
  const input = resolve(folder, 'input.json'), output = resolve(folder, 'ranking');
  await writeFile(input, JSON.stringify(report));
  const result = spawnSync(process.execPath, ['benchmarks/summarize-catalog.mjs', input, output], { encoding: 'utf8' });
  return { ...result, output };
}

test('retains full names, pairs baselines, and exports measured diagnostics', async () => {
  const result = await run(fixture());
  assert.equal(result.status, 0, result.stderr);
  const report = JSON.parse(await readFile(`${result.output}.json`, 'utf8'));
  assert.equal(report.definition_count, 2);
  assert.equal(report.rows.length, 4);
  assert.deepEqual([...new Set(report.rows.map(r => r.definition))].sort(), names);
  for (const row of report.rows) {
    assert.equal(row.ratio_of_means, 4);
    assert.equal(row.overhead_ns_per_target, 30);
    assert.equal(row.completed_work_items, 0);
    assert.equal(row.lock_contentions, 0);
    assert.equal(row.tween_allocated_bytes, row.count * 32);
  }
});

test('rejects a failed measurement instead of silently ranking it', async () => {
  const report = fixture(); report.Benchmarks[0].Statistics = null;
  const result = await run(report);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /Missing\/failed measurement/);
});

test('rejects incomplete baselines and missing threading diagnostics', async () => {
  const incomplete = fixture(); incomplete.Benchmarks.shift();
  const missing = await run(incomplete);
  assert.notEqual(missing.status, 0);
  assert.match(missing.stderr, /Incomplete baseline pair/);
  const report = fixture(); report.Benchmarks[0].Metrics = [];
  const result = await run(report);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /Missing CompletedWorkItemCount diagnostic/);
});
