// SPDX-License-Identifier: MIT
// RustLike — minimal bit-packed writer for snapshots.
//
// Designed for delta encoding: we write a "changed" bit-mask and only the
// fields whose bit is set. Using bytes for everything would waste 6-8x bandwidth.
//
// Why custom?
//   - FishNet has its own Writer/Reader, but we want this layer to be transport
//     agnostic and to support our delta semantics (zigzag varint, half-precision
//     positions, smallest-three quaternions).
//   - Keep tight: one struct + a byte[] backing buffer. No allocations after warmup.
//
// Endianness: little-endian. We compile for x64/x86/ARM64 — all little-endian.

using System;
using System.Runtime.CompilerServices;

namespace RustLike.Net.Transport
{
    public struct BitWriter
    {
        public byte[] Buffer;
        public int    BitPos;

        public BitWriter(byte[] buffer) { Buffer = buffer; BitPos = 0; }

        public int ByteLength => (BitPos + 7) >> 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBits(uint value, int bits)
        {
            // bits in [1..32]
            for (int i = 0; i < bits; i++)
            {
                int byteIdx = BitPos >> 3;
                int bitInByte = BitPos & 7;
                if ((value & (1u << i)) != 0u)
                    Buffer[byteIdx] |= (byte)(1 << bitInByte);
                else
                    Buffer[byteIdx] &= (byte)~(1 << bitInByte);
                BitPos++;
            }
        }

        public void WriteByte(byte v)   => WriteBits(v, 8);
        public void WriteBool(bool v)   => WriteBits(v ? 1u : 0u, 1);
        public void WriteUShort(ushort v) => WriteBits(v, 16);
        public void WriteUInt(uint v)   => WriteBits(v, 32);

        /// <summary> Zigzag varint encode (good for small signed ints). </summary>
        public void WriteVarInt(int value)
        {
            uint zz = (uint)((value << 1) ^ (value >> 31));
            WriteVarUInt(zz);
        }

        public void WriteVarUInt(uint value)
        {
            while (value >= 0x80)
            {
                WriteBits((value & 0x7Fu) | 0x80u, 8);
                value >>= 7;
            }
            WriteBits(value & 0x7Fu, 8);
        }

        /// <summary> Compact float [-range..range] in `bits` bits. Resolution = 2*range / (1<<bits). </summary>
        public void WriteCompressedFloat(float v, float range, int bits)
        {
            float t = (v + range) / (2f * range);
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            uint q = (uint)(t * ((1u << bits) - 1u) + 0.5f);
            WriteBits(q, bits);
        }

        /// <summary> Write a quaternion via smallest-three (drop largest, send three components in 10 bits + 2 bit index). </summary>
        public void WriteQuaternionCompact(float qx, float qy, float qz, float qw)
        {
            // find largest abs component
            float ax = qx < 0 ? -qx : qx;
            float ay = qy < 0 ? -qy : qy;
            float az = qz < 0 ? -qz : qz;
            float aw = qw < 0 ? -qw : qw;
            int largest = 0; float lv = ax;
            if (ay > lv) { largest = 1; lv = ay; }
            if (az > lv) { largest = 2; lv = az; }
            if (aw > lv) { largest = 3; lv = aw; }

            // sign of largest -> if negative, flip all (quaternion q == -q)
            float sign = 1f;
            switch (largest)
            {
                case 0: if (qx < 0) sign = -1f; break;
                case 1: if (qy < 0) sign = -1f; break;
                case 2: if (qz < 0) sign = -1f; break;
                case 3: if (qw < 0) sign = -1f; break;
            }

            float a = sign * qx, b = sign * qy, c = sign * qz, d = sign * qw;
            WriteBits((uint)largest, 2);
            // remaining three are in [-1/sqrt(2)..1/sqrt(2)]
            const float r = 0.7071068f;
            switch (largest)
            {
                case 0: WriteCompressedFloat(b, r, 10); WriteCompressedFloat(c, r, 10); WriteCompressedFloat(d, r, 10); break;
                case 1: WriteCompressedFloat(a, r, 10); WriteCompressedFloat(c, r, 10); WriteCompressedFloat(d, r, 10); break;
                case 2: WriteCompressedFloat(a, r, 10); WriteCompressedFloat(b, r, 10); WriteCompressedFloat(d, r, 10); break;
                case 3: WriteCompressedFloat(a, r, 10); WriteCompressedFloat(b, r, 10); WriteCompressedFloat(c, r, 10); break;
            }
        }

        public ArraySegment<byte> ToSegment() => new(Buffer, 0, ByteLength);
    }
}
