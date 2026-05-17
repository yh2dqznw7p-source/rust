// SPDX-License-Identifier: MIT
// RustLike — runtime memory & streaming budgets.
//
// Used by ChunkManager and AssetService to decide:
//   - how many chunks to keep in soft cache,
//   - when to TrimUnused(),
//   - when to back off pre-warm of LOD0 (load LOD3 only).
//
// Values are conservative. Exposed via ServiceLocator so quality presets
// (Low/Mid/High) can override at runtime.

namespace RustLike.Core.Streaming
{
    public sealed class StreamingBudget
    {
        // RAM
        public long ClientRamBudgetBytes  { get; set; } = 5_500L * 1024 * 1024; // 5.5 GB
        public long ServerRamBudgetBytes  { get; set; } = 5_500L * 1024 * 1024;

        // Chunks
        public int  ClientChunkRadius     { get; set; } = 6;     // chunks
        public int  ClientChunkSoftCache  { get; set; } = 32;    // LRU
        public int  ServerChunkSoftCache  { get; set; } = 256;   // server keeps more

        // Async loading concurrency
        public int  MaxConcurrentLoads    { get; set; } = 4;

        // Quality presets fold into this
        public static StreamingBudget LowSpec()
        {
            return new StreamingBudget
            {
                ClientRamBudgetBytes  = 4_500L * 1024 * 1024,
                ClientChunkRadius     = 4,
                ClientChunkSoftCache  = 16,
                MaxConcurrentLoads    = 2,
            };
        }

        public static StreamingBudget MidSpec() => new StreamingBudget(); // defaults

        public static StreamingBudget HighSpec()
        {
            return new StreamingBudget
            {
                ClientRamBudgetBytes  = 7_000L * 1024 * 1024,
                ClientChunkRadius     = 8,
                ClientChunkSoftCache  = 64,
                MaxConcurrentLoads    = 8,
            };
        }
    }
}
