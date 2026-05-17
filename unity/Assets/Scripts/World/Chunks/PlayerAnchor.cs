// SPDX-License-Identifier: MIT
// RustLike — anchors chunks based on a player's position.
//
// Each player (server-side) and the local player (client-side) attaches one of
// these. It tracks a square radius of ChunkCoord around the position and
// updates ChunkManager when the player crosses chunk borders.
//
// This is a plain class (not MonoBehaviour) because:
//   - server has no MonoBehaviour for sleeping/AI-only entities,
//   - we want to call Tick() from the deterministic GameLoop, not Update.

using System.Collections.Generic;
using RustLike.Core.Memory;
using UnityEngine;

namespace RustLike.World.Chunks
{
    public sealed class PlayerAnchor
    {
        public readonly int PlayerId;
        public Vector3 Position;
        public int Radius;            // in chunks (Chebyshev)

        private readonly ChunkManager _mgr;
        private readonly HashSet<ChunkCoord> _current = new(64);
        private readonly HashSet<ChunkCoord> _next = new(64);
        private ChunkCoord _lastCenter;
        private bool _initialized;

        public PlayerAnchor(int playerId, ChunkManager mgr, int radius)
        {
            PlayerId = playerId;
            _mgr = mgr;
            Radius = radius;
        }

        public IReadOnlyCollection<ChunkCoord> CurrentSet => _current;

        public void Update()
        {
            var center = ChunkCoord.FromWorld(Position.x, Position.z);
            if (_initialized && center == _lastCenter) return; // no border crossed
            _initialized = true;
            _lastCenter = center;

            _next.Clear();
            for (int dz = -Radius; dz <= Radius; dz++)
            for (int dx = -Radius; dx <= Radius; dx++)
                _next.Add(new ChunkCoord(center.X + dx, center.Z + dz));

            _mgr.UpdateAnchorSet(_current, _next);

            // swap sets
            (var tmp, _) = (_current, 0);
            _current.Clear();
            foreach (var c in _next) _current.Add(c);
        }

        public void Detach()
        {
            // remove all anchors held
            foreach (var c in _current) _mgr.RemoveAnchor(c);
            _current.Clear();
        }
    }
}
