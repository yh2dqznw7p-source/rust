// SPDX-License-Identifier: MIT
// RustLike — Chunk runtime data.
//
// A Chunk is the unit of streaming and persistence. It owns:
//   - terrain visual root (mesh + collider, instantiated on Activate),
//   - lists of static decorations, building blocks, entities living in it,
//   - reference counter (anchors): how many players want this chunk loaded.
//
// State machine:
//   Unloaded -> Loading -> Active -> Unloading -> Unloaded
//   Active   -> Suspended (server only; entities sleep) -> Active
//
// We KEEP the Chunk object alive while in LRU cache even if Unloaded; the
// heavy assets (mesh/textures) are released, but the metadata stays so we
// can re-activate cheaply if the player walks back.

using System.Collections.Generic;
using RustLike.Core.Memory;
using RustLike.Core.Streaming;
using UnityEngine;

namespace RustLike.World.Chunks
{
    public enum ChunkState : byte
    {
        Unloaded,
        Loading,
        Active,
        Suspended,
        Unloading,
    }

    public sealed class Chunk
    {
        public readonly ChunkCoord Coord;
        public ChunkState State;

        // anchor count: how many players (or systems) require this chunk loaded.
        // When it drops to 0 with grace period elapsed, chunk gets unloaded.
        public int AnchorCount;
        public float LastAnchoredTime; // SimClock.TimeSeconds, used for grace period

        // Visual root spawned on Activate; null when Unloaded/Loading.
        public GameObject VisualRoot;
        public AssetHandle TerrainHandle;

        // Things that live inside the chunk. Net code iterates these for AOI.
        public readonly SwapList<int> EntityIds = new(64);   // server entity ids
        public readonly SwapList<int> BuildingBlockIds = new(128);
        public readonly SwapList<int> ItemDropIds = new(16);

        // Server-only: deterministic seed for procedural decoration.
        public ulong Seed;

        public Chunk(ChunkCoord coord)
        {
            Coord = coord;
            State = ChunkState.Unloaded;
        }

        public void AddAnchor(float time) { AnchorCount++; LastAnchoredTime = time; }
        public void RemoveAnchor() { if (AnchorCount > 0) AnchorCount--; }
    }
}
