// SPDX-License-Identifier: MIT
// RustLike — privilege (Tool Cupboard) data.
//
// A Cupboard authorizes a list of player ids within a sphere radius. Effects:
//   - blocks others from placing buildings inside the radius (build privilege),
//   - pauses decay of friendly blocks while fueled.
//
// Authorization list capped at AuthCap to bound memory & UI. Same as Rust's
// 64 limit historically, then later 16 — we use 32 by default.
//
// Cupboards are authoritative on the server; clients only see UI affordances.

using System;

namespace RustLike.Gameplay.Building
{
    public sealed class PrivilegeCupboard
    {
        public const int AuthCap = 32;
        public const float DefaultRadius = 25f;

        public int  CupboardEntityId;
        public UnityEngine.Vector3 Position;
        public float Radius = DefaultRadius;
        public bool  HasFuel;

        // small array; bsearch-by-id since we keep it sorted
        public int[] AuthorizedPlayerIds = Array.Empty<int>();

        public bool Contains(in UnityEngine.Vector3 worldPos)
        {
            var d = worldPos - Position;
            float r2 = Radius * Radius;
            return d.sqrMagnitude <= r2;
        }

        public bool IsAuthorized(int playerId)
        {
            // linear; AuthCap=32 is fine. Later: switch to sorted + binary search.
            for (int i = 0; i < AuthorizedPlayerIds.Length; i++)
                if (AuthorizedPlayerIds[i] == playerId) return true;
            return false;
        }

        public bool TryAuthorize(int playerId)
        {
            if (IsAuthorized(playerId)) return true;
            if (AuthorizedPlayerIds.Length >= AuthCap) return false;
            int n = AuthorizedPlayerIds.Length;
            Array.Resize(ref AuthorizedPlayerIds, n + 1);
            AuthorizedPlayerIds[n] = playerId;
            return true;
        }

        public bool Deauthorize(int playerId)
        {
            int n = AuthorizedPlayerIds.Length;
            for (int i = 0; i < n; i++)
            {
                if (AuthorizedPlayerIds[i] != playerId) continue;
                AuthorizedPlayerIds[i] = AuthorizedPlayerIds[n - 1];
                Array.Resize(ref AuthorizedPlayerIds, n - 1);
                return true;
            }
            return false;
        }
    }
}
