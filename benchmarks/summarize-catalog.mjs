// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { dirname } from 'node:path';

const [input, output] = process.argv.slice(2);
if (!input || !output) throw new Error('Usage: node benchmarks/summarize-catalog.mjs <BDN full JSON> <output prefix>');
const report = JSON.parse(await readFile(input, 'utf8'));
const pairs = new Map();
for (const benchmark of report.Benchmarks) {
  if (benchmark.Type !== 'AdapterCatalogBenchmarks') continue;
  if (!benchmark.Statistics || !(benchmark.Statistics.Mean > 0))
    throw new Error(`Missing/failed measurement: ${benchmark.FullName}`);
  const params = new URLSearchParams(benchmark.Parameters);
  // Parameters/DisplayInfo can abbreviate long strings. FullName retains the
  // quoted source value, including all of the generic shader definition name.
  const fullDefinition = benchmark.FullName?.match(/Definition: ("(?:[^"\\]|\\.)*")\)$/);
  const definition = fullDefinition ? JSON.parse(fullDefinition[1]) : params.get('Definition');
  const count = Number(params.get('Count'));
  if (!definition || ![100, 1000].includes(count)) throw new Error(`Unexpected parameters: ${benchmark.Parameters}`);
  const key = `${count}:${definition}`;
  const pair = pairs.get(key) ?? { definition, count };
  if (pair[benchmark.Method]) throw new Error(`Duplicate measurement: ${benchmark.FullName}`);
  pair[benchmark.Method] = benchmark;
  pairs.set(key, pair);
}
if (!pairs.size) throw new Error('No catalog results found');
const metric = (benchmark, name) => {
  const value = benchmark.Metrics?.find(m => m.Descriptor?.Id === name)?.Value;
  if (value === undefined) throw new Error(`Missing ${name} diagnostic: ${benchmark.FullName}`);
  return value;
};
const rows = [...pairs.values()].map(pair => {
  const direct = pair.DirectWrite, tween = pair.TweenUpdate;
  if (!direct || !tween) throw new Error(`Incomplete baseline pair: ${pair.definition}, ${pair.count}`);
  return {
    definition: pair.definition, count: pair.count,
    tween_mean_ns: tween.Statistics.Mean, direct_mean_ns: direct.Statistics.Mean,
    tween_error_99_9_ns: tween.Statistics.ConfidenceInterval.Margin,
    direct_error_99_9_ns: direct.Statistics.ConfidenceInterval.Margin,
    tween_ns_per_target: tween.Statistics.Mean / pair.count,
    overhead_ns_per_target: (tween.Statistics.Mean - direct.Statistics.Mean) / pair.count,
    ratio_of_means: tween.Statistics.Mean / direct.Statistics.Mean,
    tween_allocated_bytes: tween.Memory.BytesAllocatedPerOperation,
    direct_allocated_bytes: direct.Memory.BytesAllocatedPerOperation,
    completed_work_items: metric(tween, 'CompletedWorkItemCount'),
    lock_contentions: metric(tween, 'LockContentionCount'),
    direct_completed_work_items: metric(direct, 'CompletedWorkItemCount'),
    direct_lock_contentions: metric(direct, 'LockContentionCount'),
    tween_samples_ns: tween.Statistics.OriginalValues,
    direct_samples_ns: direct.Statistics.OriginalValues,
  };
});
const quantile = (values, p) => {
  const index = (values.length - 1) * p, lower = Math.floor(index);
  return values[lower] + (values[Math.ceil(index)] - values[lower]) * (index - lower);
};
for (const count of [100, 1000]) {
  const ranked = rows.filter(r => r.count === count).sort((a, b) => b.tween_ns_per_target - a.tween_ns_per_target);
  const values = ranked.map(r => r.tween_ns_per_target).sort((a, b) => a - b);
  if (!values.length) throw new Error(`No results for count ${count}`);
  const q1 = quantile(values, 0.25), q3 = quantile(values, 0.75), fence = q3 + 3 * (q3 - q1);
  ranked.forEach((r, i) => { r.rank = i + 1; r.extreme_cost_outlier = r.tween_ns_per_target > fence; });
}
const names100 = rows.filter(r => r.count === 100).map(r => r.definition).sort();
const names1000 = rows.filter(r => r.count === 1000).map(r => r.definition).sort();
if (JSON.stringify(names100) !== JSON.stringify(names1000)) throw new Error('Counts have different definition coverage');
rows.sort((a, b) => a.count - b.count || a.rank - b.rank);
await mkdir(dirname(output), { recursive: true });
await writeFile(`${output}.json`, JSON.stringify({
  source: input, environment: report.HostEnvironmentInfo, definition_count: names100.length,
  note: 'Short-run screening. Costs are per frame unless labeled per_target; managed allocations only. Ratios are ratios of means.',
  rows,
}, null, 2) + '\n');
const columns = Object.keys(rows[0]).filter(k => !k.endsWith('_samples_ns'));
const csv = value => /[,"\n]/.test(String(value)) ? `"${String(value).replaceAll('"', '""')}"` : String(value ?? '');
await writeFile(`${output}.csv`, [columns.join(','), ...rows.map(r => columns.map(c => csv(r[c])).join(','))].join('\n') + '\n');
const lines = [
  '# Tween definition cost ranking', '',
  `${names100.length} definitions, 100 and 1,000 targets, direct-write baselines, memory and threading diagnostics.`, '',
  'Short-run screening; error is the 99.9% confidence half-width. Outlier: per-target cost above Q3 + 3×IQR within the count.', '',
  'Ratios are ratios of means. Managed allocation totals and threading counts are per benchmark invocation (one frame).', '',
];
for (const count of [100, 1000]) {
  lines.push(`## ${count.toLocaleString('en-US')} targets`, '',
    '| Rank | Definition | Tween µs ± error | Direct µs | ns/target | Overhead ns/target | Ratio | Tween B | Direct B | Work items | Contentions | Outlier |',
    '| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |');
  for (const r of rows.filter(r => r.count === count))
    lines.push(`| ${r.rank} | ${r.definition.replaceAll('<', '&lt;').replaceAll('>', '&gt;')} | ${(r.tween_mean_ns / 1000).toFixed(2)} ± ${(r.tween_error_99_9_ns / 1000).toFixed(2)} | ${(r.direct_mean_ns / 1000).toFixed(2)} | ${r.tween_ns_per_target.toFixed(1)} | ${r.overhead_ns_per_target.toFixed(1)} | ${r.ratio_of_means.toFixed(2)} | ${r.tween_allocated_bytes} | ${r.direct_allocated_bytes} | ${r.completed_work_items ?? 'n/a'} | ${r.lock_contentions ?? 'n/a'} | ${r.extreme_cost_outlier ? 'yes' : ''} |`);
  lines.push('');
}
await writeFile(`${output}.md`, lines.join('\n'));
console.log(`Wrote ${rows.length} paired rankings (${names100.length} definitions) to ${output}.{json,csv,md}`);
