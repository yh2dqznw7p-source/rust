// SPDX-License-Identifier: MIT
// RustLike — runtime item catalog for the Phase 0.5 demo.
//
// In production each item is a ScriptableObject .asset (see ItemDef).
// For the demo we create them at runtime via CreateInstance, so the
// project ships with no authored assets and still has a working item set.

using UnityEngine;

namespace RustLike.Gameplay.Inventory
{
    /// <summary> Stable wire-format item ids. NEVER reuse a number after it ships. </summary>
    public static class ItemIds
    {
        // tools
        public const ushort Hatchet     = 1;
        public const ushort Pickaxe     = 2;
        public const ushort Hammer      = 3;
        // medical
        public const ushort Bandage     = 4;
        public const ushort Syringe     = 5;
        // food / drink
        public const ushort Apple       = 6;
        public const ushort CookedMeat  = 7;
        public const ushort WaterBottle = 8;
        // weapons
        public const ushort Pistol      = 9;
        public const ushort AK47        = 10;
        public const ushort SMG         = 11;
        public const ushort Shotgun     = 12;
        // ammo
        public const ushort PistolAmmo   = 13;
        public const ushort RifleAmmo    = 14;
        public const ushort ShotgunShell = 15;
        // resources
        public const ushort Wood    = 16;
        public const ushort Stone   = 17;
        public const ushort Metal   = 18;
        public const ushort HQMetal = 19;
        public const ushort Sulfur  = 20;
        public const ushort Cloth   = 21;
        public const ushort Scrap   = 22;
        // building blocks (placeable)
        public const ushort BFoundation = 23;
        public const ushort BWall       = 24;
        public const ushort BFloor      = 25;
        public const ushort BDoorway    = 26;
        public const ushort BWoodDoor   = 27;
        // deployables (storage / crafting / utility)
        public const ushort SmallBox    = 28;
        public const ushort LargeBox    = 29;
        public const ushort Locker      = 30;
        public const ushort Fridge      = 31;
        public const ushort SleepingBag = 32;
        public const ushort Furnace     = 33;
        public const ushort Workbench1  = 34;
        public const ushort Workbench2  = 35;
        public const ushort ResearchTbl = 36;
        public const ushort Cupboard    = 37;
        // components / blueprints
        public const ushort Blueprint = 38;
        public const ushort Gear      = 39;
        public const ushort Tarp      = 40;
    }

    public static class ItemDatabase
    {
        public static ItemRegistry Build()
        {
            var reg = new ItemRegistry();

            // Tools
            reg.Register(MakeTool   (ItemIds.Hatchet,    "hatchet",  "Hatchet",  200, new Color(0.55f, 0.35f, 0.10f)));
            reg.Register(MakeTool   (ItemIds.Pickaxe,    "pickaxe",  "Pickaxe",  250, new Color(0.45f, 0.45f, 0.55f)));
            reg.Register(MakeTool   (ItemIds.Hammer,     "hammer",   "Hammer",   300, new Color(0.50f, 0.40f, 0.20f)));

            // Medical
            reg.Register(MakeMedical(ItemIds.Bandage,    "bandage",  "Bandage",       new Color(0.95f, 0.95f, 0.95f)));
            reg.Register(MakeMedical(ItemIds.Syringe,    "syringe",  "Med Syringe",   new Color(0.50f, 0.85f, 0.95f)));

            // Food / drink
            reg.Register(MakeFood   (ItemIds.Apple,      "apple",    "Apple",         new Color(0.85f, 0.20f, 0.20f)));
            reg.Register(MakeFood   (ItemIds.CookedMeat, "meat",     "Cooked Meat",   new Color(0.55f, 0.30f, 0.15f)));
            reg.Register(MakeFood   (ItemIds.WaterBottle,"water",    "Water Bottle",  new Color(0.20f, 0.55f, 0.95f)));

            // Weapons
            reg.Register(MakeWeapon (ItemIds.Pistol,     "pistol",   "Pistol",   300, new Color(0.20f, 0.20f, 0.22f)));
            reg.Register(MakeWeapon (ItemIds.AK47,       "ak47",     "AK-47",    500, new Color(0.40f, 0.25f, 0.10f)));
            reg.Register(MakeWeapon (ItemIds.SMG,        "smg",      "SMG",      350, new Color(0.30f, 0.30f, 0.35f)));
            reg.Register(MakeWeapon (ItemIds.Shotgun,    "shotgun",  "Shotgun",  400, new Color(0.45f, 0.30f, 0.15f)));

            // Ammo
            reg.Register(MakeAmmo   (ItemIds.PistolAmmo,    "pistol_ammo",   "Pistol Ammo",   64,  new Color(0.85f, 0.75f, 0.20f)));
            reg.Register(MakeAmmo   (ItemIds.RifleAmmo,     "rifle_ammo",    "Rifle Ammo",    128, new Color(0.95f, 0.60f, 0.10f)));
            reg.Register(MakeAmmo   (ItemIds.ShotgunShell,  "shotgun_shell", "Shotgun Shell", 32,  new Color(0.85f, 0.20f, 0.20f)));

            // Resources
            reg.Register(MakeRes(ItemIds.Wood,    "wood",   "Wood",        1000, new Color(0.55f, 0.35f, 0.15f)));
            reg.Register(MakeRes(ItemIds.Stone,   "stone",  "Stone",       1000, new Color(0.50f, 0.50f, 0.50f)));
            reg.Register(MakeRes(ItemIds.Metal,   "metal",  "Metal Frags", 1000, new Color(0.70f, 0.70f, 0.75f)));
            reg.Register(MakeRes(ItemIds.HQMetal, "hqm",    "HQ Metal",    100,  new Color(0.95f, 0.85f, 0.30f)));
            reg.Register(MakeRes(ItemIds.Sulfur,  "sulfur", "Sulfur",      1000, new Color(0.95f, 0.85f, 0.20f)));
            reg.Register(MakeRes(ItemIds.Cloth,   "cloth",  "Cloth",       1000, new Color(0.85f, 0.75f, 0.55f)));
            reg.Register(MakeRes(ItemIds.Scrap,   "scrap",  "Scrap",       1000, new Color(0.55f, 0.50f, 0.45f)));

            // Building blocks
            reg.Register(MakeBuilding(ItemIds.BFoundation, "foundation", "Foundation",  new Color(0.65f, 0.55f, 0.40f)));
            reg.Register(MakeBuilding(ItemIds.BWall,       "wall",       "Wall",        new Color(0.55f, 0.45f, 0.30f)));
            reg.Register(MakeBuilding(ItemIds.BFloor,      "floor",      "Floor",       new Color(0.60f, 0.50f, 0.35f)));
            reg.Register(MakeBuilding(ItemIds.BDoorway,    "doorway",    "Doorway",     new Color(0.50f, 0.40f, 0.25f)));
            reg.Register(MakeBuilding(ItemIds.BWoodDoor,   "wood_door",  "Wood Door",   new Color(0.45f, 0.30f, 0.15f)));

            // Deployables
            reg.Register(MakeDeployable(ItemIds.SmallBox,    "small_box",   "Small Box",       new Color(0.60f, 0.45f, 0.25f)));
            reg.Register(MakeDeployable(ItemIds.LargeBox,    "large_box",   "Large Box",       new Color(0.50f, 0.35f, 0.20f)));
            reg.Register(MakeDeployable(ItemIds.Locker,      "locker",      "Locker",          new Color(0.40f, 0.50f, 0.55f)));
            reg.Register(MakeDeployable(ItemIds.Fridge,      "fridge",      "Fridge",          new Color(0.85f, 0.85f, 0.90f)));
            reg.Register(MakeDeployable(ItemIds.SleepingBag, "sleeping_bag","Sleeping Bag",    new Color(0.70f, 0.30f, 0.30f)));
            reg.Register(MakeDeployable(ItemIds.Furnace,     "furnace",     "Furnace",         new Color(0.45f, 0.45f, 0.50f)));
            reg.Register(MakeDeployable(ItemIds.Workbench1,  "workbench1",  "Workbench T1",    new Color(0.50f, 0.40f, 0.25f)));
            reg.Register(MakeDeployable(ItemIds.Workbench2,  "workbench2",  "Workbench T2",    new Color(0.55f, 0.45f, 0.30f)));
            reg.Register(MakeDeployable(ItemIds.ResearchTbl, "research",    "Research Table",  new Color(0.45f, 0.35f, 0.20f)));
            reg.Register(MakeDeployable(ItemIds.Cupboard,    "cupboard",    "Tool Cupboard",   new Color(0.40f, 0.30f, 0.15f)));

            // Components
            reg.Register(MakeComponent(ItemIds.Blueprint, "blueprint", "Blueprint", 100, new Color(0.20f, 0.45f, 0.70f)));
            reg.Register(MakeComponent(ItemIds.Gear,      "gear",      "Gear",      100, new Color(0.65f, 0.65f, 0.70f)));
            reg.Register(MakeComponent(ItemIds.Tarp,      "tarp",      "Tarp",      100, new Color(0.30f, 0.30f, 0.30f)));

            return reg;
        }

        // ------- factories -------

        private static Sprite MakeIconSprite(Color c)
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[s * s];
            byte r = (byte)Mathf.Clamp(c.r * 255f, 0, 255);
            byte g = (byte)Mathf.Clamp(c.g * 255f, 0, 255);
            byte b = (byte)Mathf.Clamp(c.b * 255f, 0, 255);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool border = x == 0 || y == 0 || x == s - 1 || y == s - 1;
                pixels[y * s + x] = border ? new Color32(0, 0, 0, 255) : new Color32(r, g, b, 255);
            }
            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), pixelsPerUnit: 32f);
        }

        private static ItemDef Common(ushort id, string sn, string name, ItemCategory cat, Color c)
        {
            var d = ScriptableObject.CreateInstance<ItemDef>();
            d.ItemId = id; d.Shortname = sn; d.DisplayName = name; d.Category = cat;
            d.Icon = MakeIconSprite(c);
            return d;
        }

        private static ItemDef MakeTool(ushort id, string sn, string name, int dur, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Tool, c);
            d.Flags = ItemFlags.HasDurability | ItemFlags.Equippable | ItemFlags.QuickSlotOk;
            d.MaxStack = 1; d.MaxDurability = dur;
            return d;
        }
        private static ItemDef MakeWeapon(ushort id, string sn, string name, int dur, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Weapon, c);
            d.Flags = ItemFlags.HasDurability | ItemFlags.Equippable | ItemFlags.QuickSlotOk;
            d.MaxStack = 1; d.MaxDurability = dur;
            return d;
        }
        private static ItemDef MakeAmmo(ushort id, string sn, string name, int stack, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Ammo, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.QuickSlotOk;
            d.MaxStack = stack;
            return d;
        }
        private static ItemDef MakeMedical(ushort id, string sn, string name, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Medical, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.QuickSlotOk;
            d.MaxStack = 10;
            return d;
        }
        private static ItemDef MakeFood(ushort id, string sn, string name, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Food, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.FoodConsumable | ItemFlags.QuickSlotOk;
            d.MaxStack = 10;
            return d;
        }
        private static ItemDef MakeRes(ushort id, string sn, string name, int stack, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Resource, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.Researchable;
            d.MaxStack = stack;
            return d;
        }
        private static ItemDef MakeBuilding(ushort id, string sn, string name, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Building, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.QuickSlotOk;
            d.MaxStack = 25;
            return d;
        }
        private static ItemDef MakeDeployable(ushort id, string sn, string name, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Furniture, c);
            d.Flags = ItemFlags.Stackable | ItemFlags.QuickSlotOk | ItemFlags.Researchable;
            d.MaxStack = 5;
            return d;
        }
        private static ItemDef MakeComponent(ushort id, string sn, string name, int stack, Color c)
        {
            var d = Common(id, sn, name, ItemCategory.Component, c);
            d.Flags = ItemFlags.Stackable;
            d.MaxStack = stack;
            return d;
        }
    }
}
