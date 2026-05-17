// SPDX-License-Identifier: MIT
// RustLike — building primitives.
//
// Block is a STRUCT (not GameObject). The whole base lives in a packed
// SwapList<BuildingBlock> on the server, indexed by an integer block id.
// Visuals are separate and attached only when the chunk is active.
//
// Memory layout target: 32 bytes per block.
//   - 8B position (int16 x, int16 y, int16 z, int16 rot index) packed,
//   - 1B kind, 1B tier, 1B damageState, 1B flags,
//   - 2B health (uint16 0..2000),
//   - 2B chunkLocalIndex,
//   - 4B ownerEntityId (cupboard auth set is per-cupboard, not per-block),
//   - 4B lastDecayTick,
//   - 4B padding/reserved.
// 50,000 blocks = 1.5 MB. Confortable budget.

using System.Runtime.InteropServices;

namespace RustLike.Gameplay.Building
{
    public enum BlockKind : byte
    {
        Foundation = 0,
        Wall       = 1,
        Floor      = 2,
        Doorway    = 3,
        Window     = 4,
        Roof       = 5,
        Stairs     = 6,
        Door       = 7,
        // ... extend with foundation_triangle, half_wall, etc.
    }

    public enum BlockTier : byte
    {
        Twigs = 0,
        Wood  = 1,
        Stone = 2,
        Metal = 3,
        HQM   = 4,
    }

    [System.Flags]
    public enum BlockFlags : byte
    {
        None        = 0,
        Locked      = 1 << 0, // door locked
        Powered     = 1 << 1, // powered (electricity)
        Opened      = 1 << 2, // door open
        DecayPaused = 1 << 3, // inside privilege radius
    }

    /// <summary> Packed block coordinate inside a chunk. 1m grid; 64 fits in 7 bits but we keep room for half-blocks. </summary>
    public readonly struct BlockCoord
    {
        public readonly short LocalX;
        public readonly short LocalY;
        public readonly short LocalZ;
        public readonly byte  RotIndex; // 0..3 (90°), or extend to 8

        public BlockCoord(short lx, short ly, short lz, byte rot)
        {
            LocalX = lx; LocalY = ly; LocalZ = lz; RotIndex = rot;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BuildingBlock
    {
        public BlockCoord Coord;       // 7 bytes
        public BlockKind  Kind;        // 1
        public BlockTier  Tier;        // 1
        public byte       DamageState; // 1: 0..3 (intact, scratched, broken, gone)
        public BlockFlags Flags;       // 1
        public ushort     Health;      // 2
        public ushort     ChunkLocalId;// 2  — block index within parent chunk's list
        public int        OwnerEntityId; // 4 — auth via privilege cupboard
        public uint       LastDecayTick; // 4
    }

    /// <summary> Per-tier base health (without decay multipliers). </summary>
    public static class TierStats
    {
        public static ushort BaseHealth(BlockTier tier) => tier switch
        {
            BlockTier.Twigs => 50,
            BlockTier.Wood  => 250,
            BlockTier.Stone => 500,
            BlockTier.Metal => 1000,
            BlockTier.HQM   => 2000,
            _ => 100,
        };

        public static float DecayPerHour(BlockTier tier) => tier switch
        {
            BlockTier.Twigs => 100f, // dies in ~30 min
            BlockTier.Wood  => 50f,
            BlockTier.Stone => 30f,
            BlockTier.Metal => 20f,
            BlockTier.HQM   => 10f,
            _ => 50f,
        };
    }
}
