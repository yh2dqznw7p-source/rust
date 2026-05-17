// SPDX-License-Identifier: MIT
// RustLike — server-side vitals tick + damage application.
//
// Design:
//   - Vitals[] indexed by entityId-derived index (we use a sparse map).
//   - Tick at 1 Hz applies hunger/thirst/temperature/radiation/bleeding deltas.
//   - ApplyDamage is called from combat code (synchronous).
//
// Tunables are constants here; later we'll move them to a ScriptableObject
// (SurvivalConfig) for designers to tweak per-server.

using System.Collections.Generic;
using RustLike.Core.Events;
using RustLike.Core.TimeSys;

namespace RustLike.Gameplay.Survival
{
    public sealed class VitalsService : ITickable
    {
        public int Order => SystemOrder.Survival;

        // sparse map id -> index into _vitals
        private readonly Dictionary<int, int> _idToIndex = new(128);
        private readonly List<int>            _ids = new(128);
        private readonly List<Vitals>         _vitals = new(128);

        // 1Hz tick: at 20Hz sim, that's every 20th tick
        private const uint OneHzInterval = 20;

        // tunables (per-second deltas)
        private const float HungerDecayPerSec = 0.05f;
        private const float ThirstDecayPerSec = 0.10f;
        private const float StarvationHpPerSec = 0.5f;
        private const float DehydrationHpPerSec = 1.0f;
        private const float BleedingHpPerSec    = 1.0f;
        private const float RadiationHpPerSec   = 1.0f;

        public void RegisterPlayer(int entityId)
        {
            if (_idToIndex.ContainsKey(entityId)) return;
            _idToIndex[entityId] = _vitals.Count;
            _ids.Add(entityId);
            _vitals.Add(Vitals.Default);
        }

        public void RemovePlayer(int entityId)
        {
            if (!_idToIndex.TryGetValue(entityId, out int idx)) return;
            int last = _vitals.Count - 1;
            if (idx != last)
            {
                _vitals[idx] = _vitals[last];
                int movedId = _ids[last];
                _ids[idx] = movedId;
                _idToIndex[movedId] = idx;
            }
            _vitals.RemoveAt(last);
            _ids.RemoveAt(last);
            _idToIndex.Remove(entityId);
        }

        public bool TryGet(int entityId, out Vitals v)
        {
            if (_idToIndex.TryGetValue(entityId, out int idx)) { v = _vitals[idx]; return true; }
            v = default; return false;
        }

        public void ApplyDamage(in DamageInfo dmg)
        {
            if (!_idToIndex.TryGetValue(dmg.TargetEntityId, out int idx)) return;
            var v = _vitals[idx];
            if (!v.IsAlive) return;

            // body-part multiplier
            float mult = dmg.Part switch
            {
                BodyPart.Head    => 2.0f,
                BodyPart.Torso   => 1.0f,
                BodyPart.ArmLeft  or BodyPart.ArmRight  => 0.7f,
                BodyPart.LegLeft  or BodyPart.LegRight  => 0.7f,
                _ => 1.0f,
            };
            float final = dmg.Amount * mult;

            // TODO armor mitigation (Phase 5)

            v.Health -= final;
            if (dmg.CausesBleed) v.Bleeding = System.MathF.Max(v.Bleeding, 5f);
            if (dmg.CanBreakBone) v.BrokenBones |= dmg.Part switch
            {
                BodyPart.LegLeft  => 1,
                BodyPart.LegRight => 2,
                BodyPart.ArmLeft or BodyPart.ArmRight => 4,
                _ => (byte)0,
            };

            bool died = v.Health <= 0f;
            if (died)
            {
                v.Health = 0f;
                v.Flags &= unchecked((byte)~1); // clear alive
            }
            _vitals[idx] = v;

            EventBus.Publish(new VitalsChangedEvent
            {
                PlayerEntityId = dmg.TargetEntityId,
                Health = v.Health,
                Died = died,
            });
        }

        public void Tick(uint tick, float dt)
        {
            if ((tick % OneHzInterval) != 0) return;
            float dtSec = dt * OneHzInterval;

            for (int i = 0; i < _vitals.Count; i++)
            {
                var v = _vitals[i];
                if (!v.IsAlive) continue;

                // hunger/thirst drain
                v.Hunger = System.MathF.Max(0f, v.Hunger - HungerDecayPerSec * dtSec);
                v.Thirst = System.MathF.Max(0f, v.Thirst - ThirstDecayPerSec * dtSec);

                // damage from starvation/dehydration
                if (v.Hunger <= 0f) v.Health -= StarvationHpPerSec  * dtSec;
                if (v.Thirst <= 0f) v.Health -= DehydrationHpPerSec * dtSec;

                // bleeding
                if (v.Bleeding > 0f)
                {
                    v.Health   -= BleedingHpPerSec * dtSec;
                    v.Bleeding -= dtSec;
                    if (v.Bleeding < 0f) v.Bleeding = 0f;
                }

                // radiation
                if (v.Radiation > 250f)
                    v.Health -= RadiationHpPerSec * dtSec;

                // passive heal when full hunger AND hydrated
                if (v.Hunger > 200f && v.Thirst > 100f && v.Bleeding == 0f)
                    v.Health = System.MathF.Min(100f, v.Health + 0.5f * dtSec);

                // death
                if (v.Health <= 0f)
                {
                    v.Health = 0f;
                    v.Flags &= unchecked((byte)~1);
                    EventBus.Publish(new VitalsChangedEvent
                    {
                        PlayerEntityId = _ids[i],
                        Health = 0f,
                        Died = true,
                    });
                }

                _vitals[i] = v;
            }
        }
    }
}
