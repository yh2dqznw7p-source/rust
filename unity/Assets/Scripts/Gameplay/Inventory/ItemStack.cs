// SPDX-License-Identifier: MIT
// RustLike — item stack value type.
//
// 16 bytes: ItemId (2) + Amount (2) + Durability (2) + Reserved (2) + InstanceId (8).
// InstanceId is non-zero for items that carry per-instance state (weapons with
// attachments, blueprints with recipes); the actual extra state lives in a
// side table keyed by InstanceId. This keeps the slot array tightly packed.

using System.Runtime.InteropServices;

namespace RustLike.Gameplay.Inventory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ItemStack
    {
        public ushort ItemId;
        public ushort Amount;
        public ushort Durability; // 0..MaxDurability
        public ushort Reserved;   // for future flags
        public ulong  InstanceId; // 0 == "no extra state"

        public bool IsEmpty => ItemId == 0 || Amount == 0;

        public static readonly ItemStack Empty = default;

        public static ItemStack Of(ushort id, ushort amount = 1)
            => new() { ItemId = id, Amount = amount };
    }
}
