// SPDX-License-Identifier: MIT
// RustLike — server-side building service (placement, damage, decay).
//
// Public API used by gameplay code:
//   - TryPlace(player, kind, tier, worldPos, rot)
//   - Damage(blockId, amount)
//   - Repair(blockId, amount)
//   - Demolish(blockId)
//   - Upgrade(blockId, newTier)
//
// Internal state is partitioned by chunk (BuildingBlock list lives in Chunk.BuildingBlockIds);
// the actual block data lives in this service so we can iterate bases with cache locality.
// Cupboards are stored separately (rare, fewer instances).
//
// All methods are server-authoritative. Clients send INTENT packets and the
// server validates auth + privilege + collision.

using System.Collections.Generic;
using RustLike.Core.Logging;
using RustLike.Core.Memory;
using UnityEngine;

namespace RustLike.Gameplay.Building
{
    public enum PlaceResult { Ok, BlockedByPrivilege, NoSnap, Collision, NotAuthorized }

    public sealed class BuildingService
    {
        // dense array of all blocks
        public readonly List<BuildingBlock> Blocks = new(2048);
        public readonly List<PrivilegeCupboard> Cupboards = new(64);

        // per-base stability graphs (sparse: keyed by base id; baseId resolved on placement)
        private readonly Dictionary<int, StabilityGraph> _stabilityByBase = new(64);

        private int _nextBlockId = 1;
        private int _nextBaseId = 1;

        // ---- place / upgrade / damage ----

        public PlaceResult TryPlace(int playerId, BlockKind kind, BlockTier tier,
                                    Vector3 worldPos, byte rotIndex, out int blockId)
        {
            blockId = 0;

            // 1) Build privilege check
            for (int i = 0; i < Cupboards.Count; i++)
            {
                var cb = Cupboards[i];
                if (!cb.Contains(worldPos)) continue;
                if (!cb.IsAuthorized(playerId))
                    return PlaceResult.BlockedByPrivilege;
            }

            // 2) TODO snap-point + collision check (Phase 4)

            // 3) Allocate
            var block = new BuildingBlock
            {
                Coord       = new BlockCoord(
                                  (short)Mathf.RoundToInt(worldPos.x),
                                  (short)Mathf.RoundToInt(worldPos.y),
                                  (short)Mathf.RoundToInt(worldPos.z),
                                  rotIndex),
                Kind        = kind,
                Tier        = tier,
                DamageState = 0,
                Flags       = BlockFlags.None,
                Health      = TierStats.BaseHealth(tier),
                ChunkLocalId= 0, // assigned by ChunkBuildings
                OwnerEntityId = playerId,
                LastDecayTick = 0,
            };
            Blocks.Add(block);
            blockId = _nextBlockId++;
            // TODO assign to chunk + add to stability graph (Phase 4)
            return PlaceResult.Ok;
        }

        public void Damage(int blockIndex, int amount)
        {
            if ((uint)blockIndex >= (uint)Blocks.Count) return;
            var b = Blocks[blockIndex];
            if (b.Health <= amount) { Demolish(blockIndex); return; }
            b.Health = (ushort)(b.Health - amount);
            // bump damage state for cosmetic mesh swap
            b.DamageState = ComputeDamageState(b);
            Blocks[blockIndex] = b;
        }

        public void Repair(int blockIndex, int amount)
        {
            if ((uint)blockIndex >= (uint)Blocks.Count) return;
            var b = Blocks[blockIndex];
            ushort max = TierStats.BaseHealth(b.Tier);
            int newHp = b.Health + amount;
            if (newHp > max) newHp = max;
            b.Health = (ushort)newHp;
            b.DamageState = ComputeDamageState(b);
            Blocks[blockIndex] = b;
        }

        public bool TryUpgrade(int playerId, int blockIndex, BlockTier newTier)
        {
            if ((uint)blockIndex >= (uint)Blocks.Count) return false;
            var b = Blocks[blockIndex];
            if (newTier <= b.Tier) return false;
            // owner / privilege check
            if (b.OwnerEntityId != playerId &&
                !IsPlayerAuthorizedAt(playerId, new Vector3(b.Coord.LocalX, b.Coord.LocalY, b.Coord.LocalZ)))
                return false;
            b.Tier = newTier;
            b.Health = TierStats.BaseHealth(newTier);
            b.DamageState = 0;
            Blocks[blockIndex] = b;
            return true;
        }

        public void Demolish(int blockIndex)
        {
            if ((uint)blockIndex >= (uint)Blocks.Count) return;
            // mark as gone; we keep the slot to avoid index churn
            var b = Blocks[blockIndex];
            b.Health = 0;
            b.DamageState = 3;
            Blocks[blockIndex] = b;
            Log.Trace(LogCat.Building, "Demolished " + blockIndex);
            // TODO: stability graph update + neighbor BFS
        }

        // ---- decay (called from a low-frequency ITickable) ----

        public void DecayPass(uint tick, float dtSeconds)
        {
            // process a slice each call to keep frame budget tight
            // skip blocks under privilege protection
            for (int i = 0; i < Blocks.Count; i++)
            {
                var b = Blocks[i];
                if (b.Health == 0) continue;
                if ((b.Flags & BlockFlags.DecayPaused) != 0) continue;
                // approximate hourly damage
                float dps = TierStats.DecayPerHour(b.Tier) / 3600f;
                int dmg = Mathf.CeilToInt(dps * dtSeconds);
                if (dmg <= 0) continue;
                if (b.Health <= dmg) Demolish(i);
                else
                {
                    b.Health = (ushort)(b.Health - dmg);
                    Blocks[i] = b;
                }
            }
        }

        // ---- queries ----

        public bool IsPlayerAuthorizedAt(int playerId, Vector3 pos)
        {
            for (int i = 0; i < Cupboards.Count; i++)
            {
                var cb = Cupboards[i];
                if (!cb.Contains(pos)) continue;
                if (cb.IsAuthorized(playerId)) return true;
            }
            return true; // no cupboards = open ground
        }

        // ---- helpers ----

        private static byte ComputeDamageState(in BuildingBlock b)
        {
            float max = TierStats.BaseHealth(b.Tier);
            float ratio = b.Health / max;
            if (ratio > 0.66f) return 0;
            if (ratio > 0.33f) return 1;
            if (ratio > 0f)    return 2;
            return 3;
        }
    }
}
