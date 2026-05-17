// SPDX-License-Identifier: MIT
// RustLike — chunk coordinate type.
//
// World is divided into 64x64 m chunks on the XZ plane.
// Y is NOT chunked at world level (terrain is single layer); caves use a
// separate y-layer with its own ChunkCoord index.
//
// Why a struct, not a Vector2Int:
//   - explicit semantics (chunks vs cells vs tiles),
//   - cheap morton/hash for grid lookups,
//   - serializable through fixed-size int pair (no Unity types in Core deps).
//
// Range: signed int. With CHUNK_SIZE = 64m, world spans ±64*2^31 m, way over 4 km².

using System;
using System.Runtime.CompilerServices;

namespace RustLike.World.Chunks
{
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public const int SizeMeters = 64;

        public readonly int X;
        public readonly int Z;

        public ChunkCoord(int x, int z) { X = x; Z = z; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkCoord FromWorld(float worldX, float worldZ)
            => new(Floor(worldX / SizeMeters), Floor(worldZ / SizeMeters));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Floor(float v) => v >= 0f ? (int)v : (int)v - (v == (int)v ? 0 : 1);

        public float CenterX => (X + 0.5f) * SizeMeters;
        public float CenterZ => (Z + 0.5f) * SizeMeters;
        public float MinX    => X * SizeMeters;
        public float MinZ    => Z * SizeMeters;

        public bool Equals(ChunkCoord o) => X == o.X && Z == o.Z;
        public override bool Equals(object o) => o is ChunkCoord c && Equals(c);
        public override int GetHashCode() => unchecked((X * 73856093) ^ (Z * 19349663));
        public override string ToString() => "(" + X + "," + Z + ")";

        public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
        public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);

        /// <summary> Chebyshev distance (max of |dx|,|dz|) — matches square radius rings. </summary>
        public int ChebyshevDistance(ChunkCoord o)
        {
            int dx = X - o.X; if (dx < 0) dx = -dx;
            int dz = Z - o.Z; if (dz < 0) dz = -dz;
            return dx > dz ? dx : dz;
        }
    }
}
