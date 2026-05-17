// SPDX-License-Identifier: MIT
// RustLike — deterministic xorshift32/64 RNG.
//
// Why not System.Random / UnityEngine.Random?
//   - System.Random: not deterministic across .NET versions, allocates.
//   - UnityEngine.Random: global state, not seedable per-system.
// We pass FastRng *by ref* to avoid copying state.

using System.Runtime.CompilerServices;

namespace RustLike.Core.MathSys
{
    public struct FastRng
    {
        // xorshift64*
        private ulong _state;

        public FastRng(ulong seed)
        {
            // splitmix64 to scramble seed (avoid poor seeds like 0)
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            _state = z ^ (z >> 31);
            if (_state == 0) _state = 0xDEADBEEFCAFEBABEUL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextU64()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextU32() => (uint)(NextU64() >> 32);

        /// <summary> [0, max) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Range(int max) => max <= 0 ? 0 : (int)(NextU32() % (uint)max);

        /// <summary> [min, max) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Range(int min, int max) => min + Range(max - min);

        /// <summary> [0, 1) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat01()
        {
            // 24-bit mantissa to avoid bias
            return (NextU32() >> 8) * (1.0f / 16777216.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool() => (NextU32() & 1u) != 0;
    }
}
