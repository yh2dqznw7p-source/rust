// SPDX-License-Identifier: MIT
// RustLike — Area-of-Interest grid (uniform XZ).
//
// Used by the server to efficiently answer:
//   - "which entities are near player P?" (for snapshot building)
//   - "which players see entity E?"        (for change broadcast)
//
// Grid cell size is independent of chunk size (typically smaller, e.g. 32 m).
// Why not reuse chunks? Chunks are streaming units. AOI cells are per-tick
// query units; finer granularity reduces snapshot waste.
//
// Storage: open-addressing dictionary of cell -> SwapList<int entityId>.
// Lookup is O(1) for "give me cell". Range query is O(R²) cells, each tiny.

using System.Collections.Generic;
using RustLike.Core.Memory;
using UnityEngine;

namespace RustLike.Net.AOI
{
    public sealed class AreaOfInterestGrid
    {
        public readonly int CellSizeMeters;
        public readonly int InterestRadiusCells; // for default queries

        // dense int key for cell coord (x in low 32 bits, z in high 32 bits)
        private readonly Dictionary<long, SwapList<int>> _cells = new(1024);
        // per-entity tracked cell (so we can update on move)
        private readonly Dictionary<int, long> _entityCell = new(2048);

        public AreaOfInterestGrid(int cellSizeMeters = 32, int interestRadiusCells = 3)
        {
            CellSizeMeters = cellSizeMeters;
            InterestRadiusCells = interestRadiusCells;
        }

        public static long Key(int cx, int cz) => ((long)(uint)cx) | (((long)(uint)cz) << 32);

        public int CellX(float worldX) => Mathf.FloorToInt(worldX / CellSizeMeters);
        public int CellZ(float worldZ) => Mathf.FloorToInt(worldZ / CellSizeMeters);

        public void AddOrUpdate(int entityId, float worldX, float worldZ)
        {
            int cx = CellX(worldX), cz = CellZ(worldZ);
            long key = Key(cx, cz);

            if (_entityCell.TryGetValue(entityId, out var existing))
            {
                if (existing == key) return;
                if (_cells.TryGetValue(existing, out var oldList))
                    oldList.RemoveSwap(entityId);
            }

            if (!_cells.TryGetValue(key, out var list))
            {
                list = new SwapList<int>(8);
                _cells[key] = list;
            }
            list.Add(entityId);
            // SwapList is a struct — write back
            _cells[key] = list;
            _entityCell[entityId] = key;
        }

        public void Remove(int entityId)
        {
            if (!_entityCell.TryGetValue(entityId, out var key)) return;
            _entityCell.Remove(entityId);
            if (_cells.TryGetValue(key, out var list))
            {
                list.RemoveSwap(entityId);
                _cells[key] = list;
            }
        }

        /// <summary> Append all entity ids inside `radiusCells` of (worldX,worldZ). </summary>
        public void QueryRadius(float worldX, float worldZ, int radiusCells, List<int> outIds)
        {
            int cx = CellX(worldX), cz = CellZ(worldZ);
            for (int dz = -radiusCells; dz <= radiusCells; dz++)
            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            {
                if (!_cells.TryGetValue(Key(cx + dx, cz + dz), out var list)) continue;
                for (int i = 0; i < list.Count; i++) outIds.Add(list.Items[i]);
            }
        }

        public void Clear()
        {
            _cells.Clear();
            _entityCell.Clear();
        }
    }
}
