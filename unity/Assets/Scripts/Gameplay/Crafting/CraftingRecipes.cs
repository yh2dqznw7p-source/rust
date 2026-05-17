// SPDX-License-Identifier: MIT
// RustLike — recipe table for the Phase 0.5 demo.
//
// Recipes are static data; in production they'd be ScriptableObjects.
// Each recipe has:
//   - inputs:    list of (itemId, amount)
//   - output:    (itemId, amount)
//   - timeSec:   how long the queue takes per craft
//   - workbenchTier: required workbench (0 = anywhere)
//   - locked:    must be researched first (default true except for starter recipes)

using System.Collections.Generic;
using RustLike.Gameplay.Inventory;

namespace RustLike.Gameplay.Crafting
{
    public struct RecipeIngredient
    {
        public ushort ItemId;
        public ushort Amount;
        public RecipeIngredient(ushort id, ushort amt) { ItemId = id; Amount = amt; }
    }

    public sealed class Recipe
    {
        public ushort RecipeId;
        public ushort OutputItemId;
        public ushort OutputAmount = 1;
        public RecipeIngredient[] Inputs;
        public float TimeSec = 5f;
        public byte WorkbenchTier; // 0 = anywhere
        public bool LockedByDefault = true;
    }

    public static class CraftingRecipes
    {
        private static readonly List<Recipe> s_All = new();
        private static readonly Dictionary<ushort, Recipe> s_ByOutput = new();

        public static IReadOnlyList<Recipe> All => s_All;
        public static Recipe ByOutput(ushort outputItemId)
            => s_ByOutput.TryGetValue(outputItemId, out var r) ? r : null;

        static CraftingRecipes()
        {
            ushort id = 1;

            // Tools / starter — always known
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Hatchet,
                Inputs = new[] { I(ItemIds.Wood, 100), I(ItemIds.Stone, 50) }, TimeSec = 7f,
                LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Pickaxe,
                Inputs = new[] { I(ItemIds.Wood, 100), I(ItemIds.Stone, 100) }, TimeSec = 8f,
                LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Hammer,
                Inputs = new[] { I(ItemIds.Wood, 50), I(ItemIds.Stone, 50) }, TimeSec = 5f,
                LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Bandage,
                OutputAmount = 1, Inputs = new[] { I(ItemIds.Cloth, 5) }, TimeSec = 2f,
                LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.SleepingBag,
                Inputs = new[] { I(ItemIds.Cloth, 30) }, TimeSec = 6f,
                LockedByDefault = false });

            // Building blocks
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.BFoundation,
                Inputs = new[] { I(ItemIds.Wood, 50) }, TimeSec = 2f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.BWall,
                Inputs = new[] { I(ItemIds.Wood, 25) }, TimeSec = 1f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.BFloor,
                Inputs = new[] { I(ItemIds.Wood, 25) }, TimeSec = 1f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.BDoorway,
                Inputs = new[] { I(ItemIds.Wood, 25) }, TimeSec = 1f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.BWoodDoor,
                Inputs = new[] { I(ItemIds.Wood, 100) }, TimeSec = 5f, LockedByDefault = false });

            // Storage / utility
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.SmallBox,
                Inputs = new[] { I(ItemIds.Wood, 100) }, TimeSec = 6f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.LargeBox,
                Inputs = new[] { I(ItemIds.Wood, 250) }, TimeSec = 12f, WorkbenchTier = 1 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Locker,
                Inputs = new[] { I(ItemIds.Metal, 100) }, TimeSec = 15f, WorkbenchTier = 2 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Fridge,
                Inputs = new[] { I(ItemIds.Metal, 75), I(ItemIds.Gear, 1) }, TimeSec = 18f, WorkbenchTier = 2 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Cupboard,
                Inputs = new[] { I(ItemIds.Wood, 1000) }, TimeSec = 20f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Furnace,
                Inputs = new[] { I(ItemIds.Stone, 50), I(ItemIds.Wood, 100) }, TimeSec = 10f, LockedByDefault = false });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Workbench1,
                Inputs = new[] { I(ItemIds.Wood, 500), I(ItemIds.Metal, 100), I(ItemIds.Scrap, 100) }, TimeSec = 30f });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Workbench2,
                Inputs = new[] { I(ItemIds.Wood, 500), I(ItemIds.Metal, 250), I(ItemIds.Scrap, 250) }, TimeSec = 45f, WorkbenchTier = 1 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.ResearchTbl,
                Inputs = new[] { I(ItemIds.Wood, 200), I(ItemIds.Scrap, 75) }, TimeSec = 20f });

            // Weapons (locked, must be researched)
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Pistol,
                Inputs = new[] { I(ItemIds.Metal, 75), I(ItemIds.Gear, 1) }, TimeSec = 20f, WorkbenchTier = 1 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.SMG,
                Inputs = new[] { I(ItemIds.Metal, 150), I(ItemIds.HQMetal, 5), I(ItemIds.Gear, 2) }, TimeSec = 30f, WorkbenchTier = 1 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.AK47,
                Inputs = new[] { I(ItemIds.Wood, 200), I(ItemIds.Metal, 250), I(ItemIds.HQMetal, 25), I(ItemIds.Gear, 4) }, TimeSec = 50f, WorkbenchTier = 2 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Shotgun,
                Inputs = new[] { I(ItemIds.Wood, 100), I(ItemIds.Metal, 150) }, TimeSec = 25f, WorkbenchTier = 1 });

            // Ammo
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.PistolAmmo, OutputAmount = 10,
                Inputs = new[] { I(ItemIds.Metal, 5), I(ItemIds.Sulfur, 10) }, TimeSec = 4f, WorkbenchTier = 1 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.RifleAmmo, OutputAmount = 10,
                Inputs = new[] { I(ItemIds.Metal, 10), I(ItemIds.Sulfur, 20) }, TimeSec = 5f, WorkbenchTier = 2 });
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.ShotgunShell, OutputAmount = 5,
                Inputs = new[] { I(ItemIds.Metal, 5), I(ItemIds.Sulfur, 10) }, TimeSec = 3f, WorkbenchTier = 1 });

            // Consumables
            Add(new Recipe { RecipeId = id++, OutputItemId = ItemIds.Syringe,
                Inputs = new[] { I(ItemIds.Cloth, 5), I(ItemIds.Scrap, 25) }, TimeSec = 6f, WorkbenchTier = 1 });
        }

        private static RecipeIngredient I(ushort id, int amt) => new(id, (ushort)amt);

        private static void Add(Recipe r)
        {
            s_All.Add(r);
            s_ByOutput[r.OutputItemId] = r;
        }
    }
}
