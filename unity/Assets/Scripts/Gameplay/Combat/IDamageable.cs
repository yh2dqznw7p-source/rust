// SPDX-License-Identifier: MIT
// RustLike — anything that can take damage exposes this.
//
// Both DummyEnemy and (later) NetworkedPlayer implement it. The combat
// system stays decoupled from concrete entity types — it just calls
// IDamageable.ApplyDamage on the GameObject the raycast hit.

using UnityEngine;

namespace RustLike.Gameplay.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject attacker);
    }
}
