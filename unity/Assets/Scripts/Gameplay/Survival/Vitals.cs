// SPDX-License-Identifier: MIT
// RustLike — player vitals (Health, Hunger, Thirst, Temperature, Radiation,
// Bleeding, Bones).
//
// Layout:
//   Vitals is a struct so 100 players cost 100*sizeof(Vitals) bytes. We keep
//   it in an array on the server, indexed by playerId. Reduces cache misses
//   when the survival tick walks all players.
//
// All values are server-authoritative. The client gets only its own vitals
// via reliable RPC for HUD; nearby players' health is a separate
// "DamageProxy" component that ticks at lower frequency.
//
// Tick model:
//   - VitalsService.Tick at 1 Hz (every 20 sim ticks). Survival doesn't need
//     20 Hz precision; this saves cycles linearly.
//   - Damage is applied on event (combat hit), not in tick.
//
// Thresholds (Rust-like):
//   - Hunger 0..500, Thirst 0..250
//   - Health 0..100
//   - Below 0 hunger/thirst -> health damage per tick.
//   - Bleeding tick HP loss until bandaged.

using System.Runtime.InteropServices;

namespace RustLike.Gameplay.Survival
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vitals
    {
        // floats kept compact; we don't need doubles.
        public float Health;       // 0..100
        public float Hunger;       // 0..500 (calories pool)
        public float Thirst;       // 0..250
        public float Temperature;  // °C body core. ~37 healthy.
        public float Radiation;    // 0..500. >100 cosmetic, >250 damaging.
        public float Bleeding;     // 0..10 ticks remaining
        public byte  BrokenBones;  // bitmask: 1=leftLeg, 2=rightLeg, 4=arm
        public byte  Flags;        // 1=alive, 2=downed, 4=poisoned, 8=drowning
        public ushort Reserved;

        public bool IsAlive => (Flags & 1) != 0;
        public bool IsDowned => (Flags & 2) != 0;

        public static Vitals Default => new()
        {
            Health = 100f,
            Hunger = 250f,
            Thirst = 125f,
            Temperature = 37f,
            Radiation = 0f,
            Bleeding = 0f,
            BrokenBones = 0,
            Flags = 1, // alive
        };
    }
}
