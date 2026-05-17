// SPDX-License-Identifier: MIT
// RustLike — item container (inventory, hotbar, loot box, equipment).
//
// One Container == fixed-size array of ItemStack. Operations are O(1)/O(N).
//
// Server-authoritative:
//   - Clients send INTENT (move slot 3 → slot 7), server validates and broadcasts.
//   - Container itself doesn't talk to the network; that's done in
//     ContainerNetworkBridge (Phase 3 implementation).
//
// All allocations are at construction time. Operations don't allocate.

using System;
using System.Runtime.CompilerServices;
using RustLike.Core.Events;

namespace RustLike.Gameplay.Inventory
{
    public sealed class Container
    {
        public readonly int Capacity;
        private readonly ItemStack[] _slots;
        private readonly ItemRegistry _registry;
        public readonly int ContainerId; // unique id for net sync

        // dirty mask for snapshot replication
        private ulong _dirtyMaskLow;  // slots 0..63
        private ulong _dirtyMaskHigh; // slots 64..127

        public Container(int containerId, int capacity, ItemRegistry registry)
        {
            if (capacity <= 0 || capacity > 128)
                throw new ArgumentException("Capacity must be 1..128");
            ContainerId = containerId;
            Capacity = capacity;
            _slots = new ItemStack[capacity];
            _registry = registry;
        }

        // ---- read ----

        public ItemStack this[int slot]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _slots[slot];
        }

        public bool IsSlotEmpty(int slot) => _slots[slot].IsEmpty;

        // ---- write helpers ----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkDirty(int slot)
        {
            if (slot < 64) _dirtyMaskLow  |= 1UL << slot;
            else            _dirtyMaskHigh |= 1UL << (slot - 64);
        }

        public void ClearDirty()
        {
            _dirtyMaskLow = 0;
            _dirtyMaskHigh = 0;
        }

        public bool HasDirty => _dirtyMaskLow != 0 || _dirtyMaskHigh != 0;

        // ---- core operations ----

        /// <summary> Try to add an entire stack. Returns leftover (0 == fully consumed). </summary>
        public ushort TryAdd(ushort itemId, ushort amount)
        {
            var def = _registry.GetById(itemId);
            if (def == null) return amount;

            // 1) merge into existing stacks first if stackable
            if (def.IsStackable)
            {
                for (int i = 0; i < _slots.Length && amount > 0; i++)
                {
                    ref var s = ref _slots[i];
                    if (s.ItemId != itemId) continue;
                    int free = def.MaxStack - s.Amount;
                    if (free <= 0) continue;
                    int take = free < amount ? free : amount;
                    s.Amount = (ushort)(s.Amount + take);
                    amount = (ushort)(amount - take);
                    MarkDirty(i);
                }
            }

            // 2) place into empty slots
            for (int i = 0; i < _slots.Length && amount > 0; i++)
            {
                ref var s = ref _slots[i];
                if (!s.IsEmpty) continue;
                int take = def.IsStackable ? (def.MaxStack < amount ? def.MaxStack : amount) : 1;
                s.ItemId = itemId;
                s.Amount = (ushort)take;
                s.Durability = (ushort)def.MaxDurability;
                amount = (ushort)(amount - take);
                MarkDirty(i);
            }
            return amount;
        }

        /// <summary> Remove `amount` of itemId across slots. Returns actually removed. </summary>
        public ushort TryRemove(ushort itemId, ushort amount)
        {
            ushort removed = 0;
            for (int i = 0; i < _slots.Length && amount > 0; i++)
            {
                ref var s = ref _slots[i];
                if (s.ItemId != itemId) continue;
                int take = s.Amount < amount ? s.Amount : amount;
                s.Amount = (ushort)(s.Amount - take);
                amount = (ushort)(amount - take);
                removed = (ushort)(removed + take);
                if (s.Amount == 0) s = ItemStack.Empty;
                MarkDirty(i);
            }
            return removed;
        }

        public bool MoveSlot(int from, int to)
        {
            if ((uint)from >= (uint)Capacity || (uint)to >= (uint)Capacity) return false;
            if (from == to) return true;

            var a = _slots[from];
            var b = _slots[to];

            // Try merge first if same id and stackable
            if (!b.IsEmpty && a.ItemId == b.ItemId)
            {
                var def = _registry.GetById(a.ItemId);
                if (def != null && def.IsStackable)
                {
                    int total = a.Amount + b.Amount;
                    int into  = total > def.MaxStack ? def.MaxStack : total;
                    int leftover = total - into;
                    b.Amount = (ushort)into;
                    a.Amount = (ushort)leftover;
                    if (a.Amount == 0) a = ItemStack.Empty;
                    _slots[from] = a;
                    _slots[to] = b;
                    MarkDirty(from); MarkDirty(to);
                    return true;
                }
            }

            // Plain swap
            _slots[from] = b;
            _slots[to] = a;
            MarkDirty(from); MarkDirty(to);
            return true;
        }

        public bool SplitSlot(int from, int to, ushort amount)
        {
            if ((uint)from >= (uint)Capacity || (uint)to >= (uint)Capacity) return false;
            ref var a = ref _slots[from];
            if (a.IsEmpty || amount == 0 || amount >= a.Amount) return false;
            if (!_slots[to].IsEmpty) return false; // destination must be empty
            var def = _registry.GetById(a.ItemId);
            if (def == null || !def.IsStackable) return false;

            _slots[to] = new ItemStack
            {
                ItemId = a.ItemId,
                Amount = amount,
                Durability = a.Durability,
                InstanceId = 0, // splits never copy instance state
            };
            a.Amount = (ushort)(a.Amount - amount);
            MarkDirty(from); MarkDirty(to);
            return true;
        }

        public int CountOf(ushort itemId)
        {
            int n = 0;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].ItemId == itemId) n += _slots[i].Amount;
            return n;
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
                if (!_slots[i].IsEmpty) { _slots[i] = ItemStack.Empty; MarkDirty(i); }
        }
    }
}
