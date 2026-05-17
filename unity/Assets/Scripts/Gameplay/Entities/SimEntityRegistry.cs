// SPDX-License-Identifier: MIT
// RustLike — central registry + tier scheduler for simulated entities.
//
// Loop:
//   - Every sim tick, the scheduler reclassifies a SLICE of entities into
//     Active/Reduced/Sleeping based on min distance to any player.
//   - Active list ticks every sim tick (20 Hz).
//   - Reduced list ticks at lower frequency (e.g. 4 Hz).
//   - Sleeping list never ticks.
//
// Slice reclassification: we don't reclass all entities every tick (would be
// O(N*P)). Instead we round-robin a window of entities per tick. This is a
// budget knob; default ~10% of entities per tick → full pass every ~0.5 s.

using System.Collections.Generic;
using RustLike.Core.Logging;
using RustLike.Core.Memory;
using RustLike.Core.TimeSys;
using UnityEngine;

namespace RustLike.Gameplay.Entities
{
    public sealed class SimEntityRegistry : ITickable
    {
        public int Order => SystemOrder.Loot; // we tick after loot/AI/etc

        // for "find nearest player"
        public delegate Vector3? PlayerPositionProvider(int playerIdx);
        public delegate int      PlayerCountProvider();

        private readonly List<ISimEntity> _all      = new(2048);
        private readonly List<ISimEntity> _active   = new(512);
        private readonly List<ISimEntity> _reduced  = new(1024);
        // sleeping is just _all minus the others; we don't iterate it.

        private readonly PlayerPositionProvider _getPlayerPos;
        private readonly PlayerCountProvider    _getPlayerCount;

        private int _reclassCursor;
        private readonly int _reclassPerTick = 256;

        // reduced-tier ticks every Nth sim tick
        private readonly uint _reducedTickEveryN = 5; // 20Hz/5 = 4Hz

        public SimEntityRegistry(PlayerPositionProvider getPos, PlayerCountProvider getCount)
        {
            _getPlayerPos = getPos;
            _getPlayerCount = getCount;
        }

        public void Add(ISimEntity entity)
        {
            _all.Add(entity);
        }

        public void Remove(ISimEntity entity)
        {
            // O(n) but rare; entities die infrequently relative to ticks
            _all.Remove(entity);
            _active.Remove(entity);
            _reduced.Remove(entity);
        }

        public int Count => _all.Count;

        public void Tick(uint tick, float dt)
        {
            ReclassifySlice();

            // Active tier — every tick
            for (int i = 0; i < _active.Count; i++)
            {
                try { _active[i].TickActive(dt); }
                catch (System.Exception ex) { Log.Error(LogCat.Gameplay, "Active tick threw", ex); }
            }

            // Reduced tier — every Nth tick
            if (tick % _reducedTickEveryN == 0)
            {
                float reducedDt = dt * _reducedTickEveryN;
                for (int i = 0; i < _reduced.Count; i++)
                {
                    try { _reduced[i].TickReduced(reducedDt); }
                    catch (System.Exception ex) { Log.Error(LogCat.Gameplay, "Reduced tick threw", ex); }
                }
            }
        }

        // ---- internal ----

        private void ReclassifySlice()
        {
            int n = _all.Count;
            if (n == 0) return;
            int playerCount = _getPlayerCount();
            int processed = 0;
            while (processed < _reclassPerTick && processed < n)
            {
                if (_reclassCursor >= n) _reclassCursor = 0;
                var e = _all[_reclassCursor];
                _reclassCursor++;
                processed++;

                SimTier next = ComputeTier(e.Position, playerCount);
                if (next != e.Tier)
                {
                    SimTier prev = e.Tier;
                    e.OnTierChanged(prev, next);
                    UpdateMembership(e, prev, next);
                }
            }
        }

        private SimTier ComputeTier(Vector3 pos, int playerCount)
        {
            float bestSq = float.PositiveInfinity;
            for (int p = 0; p < playerCount; p++)
            {
                var pp = _getPlayerPos(p);
                if (!pp.HasValue) continue;
                Vector3 d = pp.Value - pos;
                float distSq = d.x * d.x + d.y * d.y + d.z * d.z;
                if (distSq < bestSq) bestSq = distSq;
            }
            return SimTierThresholds.FromDistanceSq(bestSq);
        }

        private void UpdateMembership(ISimEntity e, SimTier prev, SimTier next)
        {
            // remove from previous bucket
            switch (prev)
            {
                case SimTier.Active:  _active.Remove(e);  break;
                case SimTier.Reduced: _reduced.Remove(e); break;
            }
            // add to next bucket
            switch (next)
            {
                case SimTier.Active:  _active.Add(e);     break;
                case SimTier.Reduced: _reduced.Add(e);    break;
            }
        }
    }
}
