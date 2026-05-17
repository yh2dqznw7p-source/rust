// SPDX-License-Identifier: MIT
// RustLike — owner of a player's containers (Phase 0.5).
//
// A player has three logical containers:
//   - Main:      24 slots (the inventory grid you see when you press Tab).
//   - Hotbar:     6 slots (the row at the bottom of the screen).
//   - Equipment:  4 slots (head/torso/legs/feet — placeholder for now).

using System;

namespace RustLike.Gameplay.Inventory
{
    public sealed class PlayerInventory
    {
        public const int MainCapacity      = 24;
        public const int HotbarCapacity    = 6;
        public const int EquipmentCapacity = 4;

        public readonly Container Main;
        public readonly Container Hotbar;
        public readonly Container Equipment;
        public readonly ItemRegistry Registry;

        public int SelectedHotbarSlot { get; private set; } = -1;

        public event Action<ItemDef, ItemStack> EquippedChanged;

        public PlayerInventory(ItemRegistry registry)
        {
            Registry = registry;
            Main      = new Container(1, MainCapacity,      registry);
            Hotbar    = new Container(2, HotbarCapacity,    registry);
            Equipment = new Container(3, EquipmentCapacity, registry);
        }

        public void SelectHotbarSlot(int slot)
        {
            if (slot < -1 || slot >= HotbarCapacity) return;
            if (slot == SelectedHotbarSlot) return;
            SelectedHotbarSlot = slot;
            FireEquippedChanged();
        }

        public ItemStack GetEquippedStack()
        {
            if (SelectedHotbarSlot < 0) return ItemStack.Empty;
            return Hotbar[SelectedHotbarSlot];
        }

        public ItemDef GetEquippedDef()
        {
            var s = GetEquippedStack();
            return s.IsEmpty ? null : Registry.GetById(s.ItemId);
        }

        /// <summary> Add to hotbar first, overflow to main. Returns leftover. </summary>
        public ushort PickUp(ushort itemId, ushort amount)
        {
            ushort leftover = Hotbar.TryAdd(itemId, amount);
            if (leftover > 0) leftover = Main.TryAdd(itemId, leftover);
            if (SelectedHotbarSlot >= 0 && Hotbar[SelectedHotbarSlot].ItemId == itemId)
                FireEquippedChanged();
            return leftover;
        }

        public int CountAll(ushort itemId)
            => Main.CountOf(itemId) + Hotbar.CountOf(itemId) + Equipment.CountOf(itemId);

        public ushort RemoveAll(ushort itemId, ushort amount)
        {
            ushort removedHotbar = Hotbar.TryRemove(itemId, amount);
            ushort left = (ushort)(amount - removedHotbar);
            ushort removedMain = left == 0 ? (ushort)0 : Main.TryRemove(itemId, left);
            if (SelectedHotbarSlot >= 0 && removedHotbar > 0) FireEquippedChanged();
            return (ushort)(removedHotbar + removedMain);
        }

        public void NotifySlotChanged(Container c, int slotIndex)
        {
            if (c == Hotbar && slotIndex == SelectedHotbarSlot)
                FireEquippedChanged();
        }

        private void FireEquippedChanged()
        {
            var stack = GetEquippedStack();
            var def = stack.IsEmpty ? null : Registry.GetById(stack.ItemId);
            EquippedChanged?.Invoke(def, stack);
        }
    }
}
