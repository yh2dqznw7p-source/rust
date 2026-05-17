// SPDX-License-Identifier: MIT
// RustLike — entity simulation tier (sleep/wake).
//
// Tiers:
//   - Active   : within 30 m of any player. Full tick rate, full systems.
//   - Reduced  : within 30..80 m of any player. Reduced tick rate, no animator,
//                no expensive AI senses (shared/cached).
//   - Sleeping : > 80 m or in unloaded chunk. Skipped entirely; only state is kept.
//
// Why distance-based rather than visibility-based:
//   - cheaper to compute (one square-distance check via AOI),
//   - works on the server which has no rendering,
//   - tracks consistent gameplay rules ("X meters away things stop happening").
//
// Tier transitions are events you can subscribe to (e.g. NPC visual proxy
// spawns when entering Active, despawns on entering Sleeping).

namespace RustLike.Gameplay.Entities
{
    public enum SimTier : byte
    {
        Sleeping = 0,
        Reduced  = 1,
        Active   = 2,
    }

    public static class SimTierThresholds
    {
        public const float ActiveDistance   = 30f;
        public const float ReducedDistance  = 80f;
        public const float ActiveDistanceSq  = ActiveDistance * ActiveDistance;
        public const float ReducedDistanceSq = ReducedDistance * ReducedDistance;

        public static SimTier FromDistanceSq(float distSq)
        {
            if (distSq <= ActiveDistanceSq)  return SimTier.Active;
            if (distSq <= ReducedDistanceSq) return SimTier.Reduced;
            return SimTier.Sleeping;
        }
    }
}
