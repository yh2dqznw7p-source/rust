// SPDX-License-Identifier: MIT
// RustLike — companion to BitWriter.

using System.Runtime.CompilerServices;

namespace RustLike.Net.Transport
{
    public ref struct BitReader
    {
        public readonly System.ReadOnlySpan<byte> Buffer;
        public int BitPos;

        public BitReader(System.ReadOnlySpan<byte> buffer) { Buffer = buffer; BitPos = 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadBits(int bits)
        {
            uint v = 0;
            for (int i = 0; i < bits; i++)
            {
                int byteIdx = BitPos >> 3;
                int bitInByte = BitPos & 7;
                if ((Buffer[byteIdx] & (1 << bitInByte)) != 0) v |= 1u << i;
                BitPos++;
            }
            return v;
        }

        public bool ReadBool() => ReadBits(1) != 0;
        public byte ReadByte() => (byte)ReadBits(8);
        public ushort ReadUShort() => (ushort)ReadBits(16);
        public uint   ReadUInt()   => ReadBits(32);

        public uint ReadVarUInt()
        {
            uint result = 0;
            int shift = 0;
            while (true)
            {
                byte b = ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
                if (shift >= 35) return result; // safety
            }
        }

        public int ReadVarInt()
        {
            uint zz = ReadVarUInt();
            return (int)((zz >> 1) ^ -(zz & 1));
        }

        public float ReadCompressedFloat(float range, int bits)
        {
            uint q = ReadBits(bits);
            float t = q / (float)((1u << bits) - 1u);
            return t * 2f * range - range;
        }
    }
}
