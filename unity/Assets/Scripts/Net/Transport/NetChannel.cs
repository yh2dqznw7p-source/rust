// SPDX-License-Identifier: MIT
// RustLike — channel constants and send-flag enum.
//
// FishNet uses its own Channel enum (Reliable/Unreliable). We wrap with our
// own to:
//   - keep an abstraction so we can swap transports later (Mirror, NGO),
//   - encode game-level intents (UnreliableSequenced for movement is different
//     from Unreliable for fire-and-forget effects).
//
// Quick guide:
//   - Movement / snapshots         => UnreliableSequenced
//   - Hit registration             => Reliable (small + critical)
//   - Inventory ops                => Reliable
//   - Chat / RPCs                  => Reliable
//   - VFX one-shots                => Unreliable
//   - Chunk asset bundles          => ReliableSequenced (large)

namespace RustLike.Net.Transport
{
    public enum NetChannel : byte
    {
        Unreliable           = 0,
        UnreliableSequenced  = 1,
        Reliable             = 2,
        ReliableSequenced    = 3,
    }

    public static class NetLimits
    {
        /// <summary> Effective MTU for one packet payload (with safety margin). </summary>
        public const int PacketBudgetBytes = 1100;

        /// <summary> Per-client downstream budget per second. </summary>
        public const int DownstreamBytesPerSecond = 25_000;

        /// <summary> Server tick rate for snapshot generation. </summary>
        public const int SnapshotTickRate = 20;
    }
}
