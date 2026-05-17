// SPDX-License-Identifier: MIT
// RustLike — building stability calculation (server-only).
//
// Algorithm:
//   - Foundations have stability = 100.
//   - A non-foundation block inherits stability = max(neighbor.stability) - drop.
//     drop depends on tier (twigs lose more) and block kind (walls lose more
//     than floors when going horizontal).
//   - When a block's stability falls to 0, it collapses (HP -> 0, kid blocks
//     get re-evaluated).
//
// Performance:
//   - Per-base graph, NOT per-server. Bases are connected components.
//   - Dirty flag: only recompute the affected component when a block is added
//     or destroyed.
//   - BFS from foundations outward. Bounded by number of blocks in the base.
//   - Run as a Burst job (TODO Phase 4).
//
// This file holds the algorithm in pure managed code for now (small bases),
// with a clear TODO marker to swap in a Burst-compiled job in Phase 4.

using System.Collections.Generic;
using RustLike.Core.Memory;

namespace RustLike.Gameplay.Building
{
    public sealed class StabilityGraph
    {
        // adjacency: blockIndex -> list of neighbor block indices
        private readonly List<List<int>> _adj = new(256);
        // per-block stability [0..100]
        private readonly List<byte> _stability = new(256);
        // foundations to seed the BFS
        private readonly List<int> _foundations = new(8);
        private bool _dirty;

        public int BlockCount => _adj.Count;

        public int AddBlock(BlockKind kind)
        {
            _adj.Add(new List<int>(4));
            _stability.Add(0);
            int idx = _adj.Count - 1;
            if (kind == BlockKind.Foundation) _foundations.Add(idx);
            _dirty = true;
            return idx;
        }

        public void Connect(int a, int b)
        {
            _adj[a].Add(b);
            _adj[b].Add(a);
            _dirty = true;
        }

        public void RemoveBlock(int idx)
        {
            // we don't compact indices to keep ids stable; mark the slot empty.
            _adj[idx]?.Clear();
            _stability[idx] = 0;
            _foundations.Remove(idx);
            // remove edges from neighbors (best-effort; safe to leave dangling
            // because removed block has empty stability and no propagation)
            _dirty = true;
        }

        /// <summary>
        /// Recompute stability if dirty. Call from a 0.5 s coalesced timer
        /// after building edits, NOT every tick.
        /// Returns the list of indices that became unstable (caller damages them).
        /// </summary>
        public void RecomputeIfDirty(List<int> outNewlyCollapsed)
        {
            if (!_dirty) return;
            _dirty = false;

            int n = _adj.Count;
            // reset
            for (int i = 0; i < n; i++) _stability[i] = 0;

            // BFS from foundations
            using (ListPool<int>.RentScope(out var queue))
            {
                for (int i = 0; i < _foundations.Count; i++)
                {
                    int f = _foundations[i];
                    _stability[f] = 100;
                    queue.Add(f);
                }
                int head = 0;
                while (head < queue.Count)
                {
                    int cur = queue[head++];
                    byte myStab = _stability[cur];
                    var neigh = _adj[cur];
                    for (int e = 0; e < neigh.Count; e++)
                    {
                        int nb = neigh[e];
                        // drop: 5 from foundation, 10 horizontally, more for fragile tiers
                        // simplified single drop here; refine in Phase 4
                        const byte drop = 10;
                        byte propagated = (byte)(myStab > drop ? myStab - drop : 0);
                        if (propagated > _stability[nb])
                        {
                            _stability[nb] = propagated;
                            queue.Add(nb);
                        }
                    }
                }
            }

            for (int i = 0; i < n; i++)
                if (_stability[i] == 0 && _adj[i] != null && _adj[i].Count > 0)
                    outNewlyCollapsed.Add(i);
        }

        public byte GetStability(int idx) => _stability[idx];
    }
}
