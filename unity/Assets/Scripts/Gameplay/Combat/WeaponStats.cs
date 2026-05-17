// SPDX-License-Identifier: MIT
// RustLike — per-item weapon parameters (Phase 0.5).

using System.Collections.Generic;
using RustLike.Gameplay.Inventory;

namespace RustLike.Gameplay.Combat
{
    public enum WeaponKind : byte { Melee, Hitscan }

    public struct WeaponStats
    {
        public WeaponKind Kind;
        public float Damage;
        public float Range;
        public float CooldownSeconds;
        public float Spread;        // degrees (cone) for hitscan
        public int   Pellets;       // > 1 for shotguns
        public ushort AmmoItemId;   // 0 = no ammo needed
    }

    public static class WeaponDb
    {
        private static readonly Dictionary<ushort, WeaponStats> s_stats = new()
        {
            [ItemIds.Hatchet] = new WeaponStats { Kind = WeaponKind.Melee,   Damage = 25f, Range = 2.2f, CooldownSeconds = 0.50f, Pellets = 1 },
            [ItemIds.Pickaxe] = new WeaponStats { Kind = WeaponKind.Melee,   Damage = 18f, Range = 2.2f, CooldownSeconds = 0.55f, Pellets = 1 },
            [ItemIds.Hammer]  = new WeaponStats { Kind = WeaponKind.Melee,   Damage = 8f,  Range = 2.0f, CooldownSeconds = 0.55f, Pellets = 1 },

            [ItemIds.Pistol]  = new WeaponStats { Kind = WeaponKind.Hitscan, Damage = 30f, Range = 80f,  CooldownSeconds = 0.20f, Spread = 1.5f, Pellets = 1, AmmoItemId = ItemIds.PistolAmmo },
            [ItemIds.SMG]     = new WeaponStats { Kind = WeaponKind.Hitscan, Damage = 18f, Range = 70f,  CooldownSeconds = 0.08f, Spread = 3.0f, Pellets = 1, AmmoItemId = ItemIds.PistolAmmo },
            [ItemIds.AK47]    = new WeaponStats { Kind = WeaponKind.Hitscan, Damage = 45f, Range = 150f, CooldownSeconds = 0.12f, Spread = 2.0f, Pellets = 1, AmmoItemId = ItemIds.RifleAmmo },
            [ItemIds.Shotgun] = new WeaponStats { Kind = WeaponKind.Hitscan, Damage = 12f, Range = 25f,  CooldownSeconds = 0.80f, Spread = 8.0f, Pellets = 8, AmmoItemId = ItemIds.ShotgunShell },
        };

        public static bool TryGet(ushort itemId, out WeaponStats stats)
            => s_stats.TryGetValue(itemId, out stats);
    }
}
