// SPDX-License-Identifier: MIT
// RustLike — minimal procedural chunk provider.
//
// This is a SKELETON. The real implementation in Phase 1 will:
//   - Sample heightmap from layered Perlin/Worley + erosion.
//   - Build a Mesh in a Burst job and assign to a runtime MeshFilter/MeshCollider.
//   - Decorate with foliage / rocks via GPU instancing batches.
//
// Today we just spawn an empty root and mark the chunk as "loaded" so
// ChunkManager and AOI code can be exercised end-to-end.

using System.Threading;
using System.Threading.Tasks;
using RustLike.Core.Logging;
using UnityEngine;

namespace RustLike.World.Chunks
{
    public sealed class ProceduralChunkProvider : IChunkProvider
    {
        private readonly Transform _worldRoot;
        private readonly ulong _worldSeed;

        public ProceduralChunkProvider(Transform worldRoot, ulong worldSeed)
        {
            _worldRoot = worldRoot;
            _worldSeed = worldSeed;
        }

        public ValueTask LoadAsync(Chunk chunk, CancellationToken ct)
        {
            // derive deterministic seed per chunk
            chunk.Seed = HashCoord(_worldSeed, chunk.Coord);

            var go = new GameObject("Chunk_" + chunk.Coord);
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = new Vector3(chunk.Coord.MinX, 0f, chunk.Coord.MinZ);
            chunk.VisualRoot = go;

            // TODO Phase 1: build heightmap mesh in Burst job, set collider, place foliage.
            Log.Trace(LogCat.World, "Procedural chunk loaded " + chunk.Coord);
            return default;
        }

        public void Unload(Chunk chunk)
        {
            if (chunk.VisualRoot != null)
            {
                Object.Destroy(chunk.VisualRoot);
                chunk.VisualRoot = null;
            }
        }

        private static ulong HashCoord(ulong seed, ChunkCoord c)
        {
            unchecked
            {
                ulong h = seed;
                h ^= (uint)c.X * 0x9E3779B97F4A7C15UL;
                h = (h << 27) | (h >> 37);
                h ^= (uint)c.Z * 0xBF58476D1CE4E5B9UL;
                h ^= h >> 31;
                return h;
            }
        }
    }
}
