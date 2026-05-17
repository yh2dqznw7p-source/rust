// SPDX-License-Identifier: MIT
// RustLike — Chunk streaming controller.
//
// Responsibilities:
//   - Maintain a dictionary of all chunks keyed by ChunkCoord.
//   - Track anchors (players, persistent NPCs, active bases).
//   - Schedule async loads/unloads with a max-concurrency limit.
//   - LRU soft cache: chunks that lost their last anchor stay loaded for
//     `_unloadGraceSeconds`; if no re-anchor happens, they unload.
//
// Tickable at 5 Hz: chunk decisions don't need higher frequency.
//
// Server vs Client:
//   - Server: anchors come from sleeping bags, online players, raid bases.
//             Provider chain: Persisted -> Procedural.
//   - Client: anchors come from local player + (small) preload look-ahead.
//             Provider chain: Prefab (monuments) | Procedural.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RustLike.Core.Bootstrap;
using RustLike.Core.Logging;
using RustLike.Core.Memory;
using RustLike.Core.Streaming;
using RustLike.Core.TimeSys;

namespace RustLike.World.Chunks
{
    public sealed class ChunkManager : ITickable
    {
        public int Order => SystemOrder.World;

        private readonly Dictionary<ChunkCoord, Chunk> _chunks = new(256);
        private readonly Queue<Chunk> _loadQueue = new();
        private readonly List<Chunk> _activeUnloadable = new(32);

        private readonly IChunkProvider _provider;
        private readonly StreamingBudget _budget;

        private int _inFlightLoads;
        private float _unloadGraceSeconds = 5f;
        private float _now;

        public int LoadedCount => _chunks.Count;
        public IReadOnlyDictionary<ChunkCoord, Chunk> All => _chunks;

        public ChunkManager(IChunkProvider provider, StreamingBudget budget)
        {
            _provider = provider;
            _budget = budget;
        }

        // ----- Anchor management -----

        public void AddAnchor(ChunkCoord coord)
        {
            if (!_chunks.TryGetValue(coord, out var ch))
            {
                ch = new Chunk(coord);
                _chunks[coord] = ch;
            }
            ch.AddAnchor(_now);
            if (ch.State == ChunkState.Unloaded || ch.State == ChunkState.Unloading)
                EnqueueLoad(ch);
        }

        public void RemoveAnchor(ChunkCoord coord)
        {
            if (_chunks.TryGetValue(coord, out var ch))
            {
                ch.RemoveAnchor();
                ch.LastAnchoredTime = _now;
            }
        }

        /// <summary> Replace anchor set for a player in O(N+M). Use this from PlayerAOI. </summary>
        public void UpdateAnchorSet(HashSet<ChunkCoord> previousSet, HashSet<ChunkCoord> currentSet)
        {
            // remove old anchors no longer wanted
            using (HashSetPool<ChunkCoord>.Rent() is var _) { /* no-op pool warm */ }
            foreach (var prev in previousSet)
                if (!currentSet.Contains(prev)) RemoveAnchor(prev);
            foreach (var cur in currentSet)
                if (!previousSet.Contains(cur)) AddAnchor(cur);
        }

        // ----- Tick: drive load/unload -----

        public void Tick(uint tick, float dt)
        {
            // run at ~5 Hz (every 4th 20Hz tick)
            if ((tick & 3u) != 0u) { _now += dt; return; }
            _now += dt * 4f; // approximate

            // 1. start queued loads up to concurrency budget
            while (_inFlightLoads < _budget.MaxConcurrentLoads && _loadQueue.Count > 0)
            {
                var ch = _loadQueue.Dequeue();
                if (ch.State != ChunkState.Loading) continue; // cancelled
                _inFlightLoads++;
                _ = LoadOne(ch);
            }

            // 2. find chunks eligible for unload
            _activeUnloadable.Clear();
            foreach (var kv in _chunks)
            {
                var ch = kv.Value;
                if (ch.State != ChunkState.Active) continue;
                if (ch.AnchorCount > 0) continue;
                if (_now - ch.LastAnchoredTime < _unloadGraceSeconds) continue;
                _activeUnloadable.Add(ch);
            }

            // 3. unload those (limit to keep frame budget)
            int max = 4;
            for (int i = 0; i < _activeUnloadable.Count && i < max; i++)
                UnloadNow(_activeUnloadable[i]);
        }

        // ----- internal -----

        private void EnqueueLoad(Chunk ch)
        {
            ch.State = ChunkState.Loading;
            _loadQueue.Enqueue(ch);
        }

        private async Task LoadOne(Chunk ch)
        {
            try
            {
                await _provider.LoadAsync(ch, CancellationToken.None);
                ch.State = ChunkState.Active;
                Log.Trace(LogCat.World, "Chunk active " + ch.Coord);
            }
            catch (System.Exception ex)
            {
                Log.Error(LogCat.World, "Chunk load failed " + ch.Coord, ex);
                ch.State = ChunkState.Unloaded;
            }
            finally
            {
                _inFlightLoads--;
            }
        }

        private void UnloadNow(Chunk ch)
        {
            ch.State = ChunkState.Unloading;
            _provider.Unload(ch);
            ch.State = ChunkState.Unloaded;
            // keep meta in dict for fast re-anchor; rely on LRU cap below
            EnforceSoftCacheCap();
        }

        private void EnforceSoftCacheCap()
        {
            int cap = _budget.ClientChunkSoftCache; // server uses different budget elsewhere
            if (_chunks.Count <= cap) return;

            // cheap pass: drop ANY Unloaded chunk over cap. We don't need true LRU,
            // because anchors will re-create on demand.
            using (ListPool<ChunkCoord>.RentScope(out var toRemove))
            {
                foreach (var kv in _chunks)
                    if (kv.Value.State == ChunkState.Unloaded && kv.Value.AnchorCount == 0)
                        toRemove.Add(kv.Key);
                int removeN = _chunks.Count - cap;
                for (int i = 0; i < toRemove.Count && removeN > 0; i++, removeN--)
                    _chunks.Remove(toRemove[i]);
            }
        }
    }
}
