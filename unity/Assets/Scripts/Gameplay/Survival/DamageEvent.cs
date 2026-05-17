// SPDX-License-Identifier: MIT
// RustLike — damage event types.
//
// Damage in Rust is more nuanced than "subtract HP". We model:
//   - Source (player, NPC, environment, fall, hunger, etc.)
//   - Type (melee/bullet/explosion/cold/...)
//   - Body part hit (head/torso/arm/leg) — drives damage multipliers and bone breaks.
//
// Apply order:
//   raw -> body part multiplier -> armor reduction -> headshot bonus -> health.

namespace RustLike.Gameplay.Survival
{
    public enum DamageType : byte
    {
        Generic     = 0,
        Bullet      = 1,
        Melee       = 2,
        Stab        = 3,
        Slash       = 4,
        Blunt       = 5,
        Explosion   = 6,
        Fire        = 7,
        Cold        = 8,
        Hunger      = 9,
        Thirst      = 10,
        Radiation   = 11,
        Drowning    = 12,
        Fall        = 13,
        Poison      = 14,
        Bleeding    = 15,
    }

    public enum BodyPart : byte
    {
        Generic = 0,
        Head    = 1,
        Torso   = 2,
        ArmLeft = 3,
        ArmRight= 4,
        LegLeft = 5,
        LegRight= 6,
    }

    public struct DamageInfo
    {
        public int        TargetEntityId;
        public int        AttackerEntityId; // 0 = environment
        public DamageType Type;
        public BodyPart   Part;
        public float      Amount;     // raw damage before mitigation
        public float      ArmorPierce; // 0..1
        public bool       CausesBleed;
        public bool       CanBreakBone;
    }

    public struct VitalsChangedEvent
    {
        public int   PlayerEntityId;
        public float Health;
        public bool  Died;
    }
}
