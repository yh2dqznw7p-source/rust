// SPDX-License-Identifier: MIT
// RustLike — interfaces that decouple ChunkManager from terrain/loader implementations.
//
// We have at least three implementations across server/client:
//   - ProceduralChunkProvider: generates from seed (used on server and client).
//   - PrefabChunkProvider:     loads pre-baked monument chunks via Addressables.
//   - PersistedChunkProvider:  on server, loads diff state from DB on top of procedural.
//
// ChunkManager picks one (composite chain) based on chunk metadata.

using System.Threading;
using System.Threading.Tasks;

namespace RustLike.World.Chunks
{
    public interface IChunkProvider
    {
        /// <summary> Async load all assets for this chunk and populate Chunk.VisualRoot. </summary>
        ValueTask LoadAsync(Chunk chunk, CancellationToken ct);

        /// <summary> Tear down the chunk: release assets, despawn visuals. </summary>
        void Unload(Chunk chunk);
    }
}
