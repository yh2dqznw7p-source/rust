// SPDX-License-Identifier: MIT
// RustLike — item definition (ScriptableObject).
//
// All item types are defined as data assets. Engineers add new items WITHOUT
// touching code: create asset → fill fields → add to addressables group.
//
// Numeric ItemId is the network-stable handle. NEVER reuse ids; mark items as
// deprecated in code instead.
//
// Stack rules:
//   - MaxStack == 1 means non-stackable (weapons, tools).
//   - Items with Durability != 0 are non-stackable regardless.
//
// Categories drive UI filters and crafting station compatibility.

using UnityEngine;

namespace RustLike.Gameplay.Inventory
{
    public enum ItemCategory : byte
    {
        Misc      = 0,
        Resource  = 1,
        Food      = 2,
        Medical   = 3,
        Weapon    = 4,
        Ammo      = 5,
        Tool      = 6,
        Clothing  = 7,
        Building  = 8,
        Component = 9,
        Furniture = 10,
        Electric  = 11,
    }

    [System.Flags]
    public enum ItemFlags : ushort
    {
        None             = 0,
        Stackable        = 1 << 0,
        HasDurability    = 1 << 1,
        Researchable     = 1 << 2,
        Recyclable       = 1 << 3,
        FoodConsumable   = 1 << 4,
        Equippable       = 1 << 5,
        QuickSlotOk      = 1 << 6,
        DropOnDeath      = 1 << 7,
    }

    [CreateAssetMenu(menuName = "RustLike/Item", fileName = "Item_New")]
    public sealed class ItemDef : ScriptableObject
    {
        [Header("Identity")]
        public ushort ItemId;        // network-stable, NEVER reuse
        public string Shortname;     // for commands ("hammer", "pistol_ammo")
        public string DisplayName;
        public string Description;
        public Sprite Icon;

        [Header("Classification")]
        public ItemCategory Category;
        public ItemFlags Flags;

        [Header("Stacking & weight")]
        public int  MaxStack = 1;
        public int  Weight   = 1;     // grams
        public int  MaxDurability = 0;

        [Header("World drop prefab (Addressables address)")]
        public string DropPrefabAddress;

        public bool IsStackable => MaxStack > 1 && (Flags & ItemFlags.HasDurability) == 0;

        public override string ToString() => "Item(" + ItemId + ":" + Shortname + ")";
    }
}
