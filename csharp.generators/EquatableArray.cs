// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace tweens.gd.Generators;

// Incremental models compare their contents, independently of semantic-symbol lifetimes.
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
{
    internal ImmutableArray<T> Items { get; }
    internal EquatableArray(IEnumerable<T> items) => Items = items.ToImmutableArray();
    public bool Equals(EquatableArray<T> other) => Items.SequenceEqual(other.Items);
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);
    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in Items) hash = unchecked(hash * 31 + (item is null ? 0 : item.GetHashCode()));
        return hash;
    }
}
