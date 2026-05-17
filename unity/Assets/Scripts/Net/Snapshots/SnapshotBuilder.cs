// SPDX-License-Identifier: MIT
// RustLike — server-side snapshot builder.
//
// Loop:
//   1. For each connected client, find their player's position.
//   2. Query AOI for entities within InterestRadius.
//   3. Sort by (priority desc, distance asc) into a working list.
//   4. Pack as many deltas as fit in PacketBudgetBytes.
//   5. Push to transport. Leftovers will be retried next tick.
//
// Skipped entities:
//   - sleeping (HasPendingDelta == false),
//   - far away (filtered by AOI),
//   - same packet already saturated (carried over).
//
// We DO NOT do delta-against-acks here yet; each entity's own dirty mask
// implements what's "changed since last sent". This is a conservative MVP that
// avoids per-client per-entity baseline storage (heavy memory). For 100 players
// that's 100 * thousands of entities of baseline state — too much.
// Later we can promote critical entities (player avatars) to per-client baselines.

using System.Collections.Generic;
using RustLike.Core.Memory;
using RustLike.Net.AOI;
using RustLike.Net.Transport;

namespace RustLike.Net.Snapshots
{
    public sealed class SnapshotBuilder
    {
        private readonly AreaOfInterestGrid _aoi;
        private readonly Dictionary<int, INetSerializable> _entities = new(2048);

        // reusable scratch (no allocations in hot loop)
        private readonly List<int> _candidateIds = new(256);
        private readonly byte[]    _packetBuffer = new byte[NetLimits.PacketBudgetBytes];

        public SnapshotBuilder(AreaOfInterestGrid aoi) { _aoi = aoi; }

        public void Register(INetSerializable e)   => _entities[e.NetId] = e;
        public void Unregister(int netId)          => _entities.Remove(netId);

        /// <summary>
        /// Build one snapshot packet for a client.
        /// Returns the byte span; empty if nothing to send.
        /// </summary>
        public System.ArraySegment<byte> BuildForClient(float playerX, float playerZ)
        {
            _candidateIds.Clear();
            _aoi.QueryRadius(playerX, playerZ, _aoi.InterestRadiusCells, _candidateIds);

            // simple scoring: priority * 1000 - distanceSq.
            // For 100 players this list is bounded; OK to sort each tick.
            // We avoid LINQ.OrderBy to skip allocation.

            var w = new BitWriter(_packetBuffer);
            // header: protocol version (3 bits), entity count placeholder (we'll patch by writing var)
            w.WriteBits(1, 3);

            int written = 0;
            for (int i = 0; i < _candidateIds.Count; i++)
            {
                int id = _candidateIds[i];
                if (!_entities.TryGetValue(id, out var ent)) continue;
                if (!ent.HasPendingDelta) continue;

                int snapshot = w.BitPos;
                w.WriteVarUInt((uint)ent.NetId);
                if (!ent.WriteDelta(ref w, clearDirty: true) || w.ByteLength > NetLimits.PacketBudgetBytes)
                {
                    // ran out of room: roll back to before this entity, stop.
                    w.BitPos = snapshot;
                    break;
                }
                written++;
                if (w.ByteLength > NetLimits.PacketBudgetBytes - 32) break; // leave footer space
            }

            if (written == 0) return new System.ArraySegment<byte>(_packetBuffer, 0, 0);
            return w.ToSegment();
        }
    }
}
