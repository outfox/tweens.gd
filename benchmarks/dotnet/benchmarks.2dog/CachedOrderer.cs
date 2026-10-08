// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Collections.Immutable;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace TweensBenchmarks;

// BDN 0.15.8 rescans all cases when computing each logical-group key. Baseline
// table generation requests those keys repeatedly; a large catalog otherwise
// spends minutes rebuilding identical keys after all measurements have finished.
// Cache by both case universe and case: implicit grouping depends on the former.
internal sealed class CachedOrderer : IOrderer
{
    private readonly IOrderer inner = DefaultOrderer.Instance;
    private readonly Dictionary<(ImmutableArray<BenchmarkCase>, BenchmarkCase), string?> keys = new();
    public bool SeparateLogicalGroups => inner.SeparateLogicalGroups;
    public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> cases, IEnumerable<BenchmarkLogicalGroupRule>? order = null)
        => inner.GetExecutionOrder(cases, order);
    public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> cases, Summary summary)
        => inner.GetSummaryOrder(cases, summary);
    public string? GetHighlightGroupKey(BenchmarkCase benchmark) => inner.GetHighlightGroupKey(benchmark);
    public string? GetLogicalGroupKey(ImmutableArray<BenchmarkCase> cases, BenchmarkCase benchmark)
    {
        var key = (cases, benchmark);
        if (!keys.TryGetValue(key, out var result)) keys.Add(key, result = inner.GetLogicalGroupKey(cases, benchmark));
        return result;
    }
    public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> groups, IEnumerable<BenchmarkLogicalGroupRule>? order = null)
        => inner.GetLogicalGroupOrder(groups, order);

    internal static void Verify()
    {
        var all = BenchmarkConverter.TypeToBenchmarks(typeof(AdapterCatalogBenchmarks)).BenchmarksCases.ToImmutableArray();
        var updates = all.Where(b => !b.Descriptor.Baseline).ToImmutableArray();
        var orderer = new CachedOrderer();
        foreach (var cases in new[] { all, updates, all })
            foreach (var benchmark in cases)
                if (orderer.GetLogicalGroupKey(cases, benchmark) != DefaultOrderer.Instance.GetLogicalGroupKey(cases, benchmark))
                    throw new InvalidOperationException("Cached grouping changed baseline pairing.");
        var groups = all.GroupBy(b => orderer.GetLogicalGroupKey(all, b)).ToArray();
        if (groups.Length != AdapterCatalogBenchmarks.Catalog.Count * 2 || groups.Any(g => g.Count() != 2 || g.Count(b => b.Descriptor.Baseline) != 1))
            throw new InvalidOperationException("Every count/definition needs exactly one direct-write baseline and one update.");
        Console.WriteLine($"PASS {all.Length} cases, {groups.Length} baseline pairs; grouping matches BDN with and without baselines.");
    }
}
