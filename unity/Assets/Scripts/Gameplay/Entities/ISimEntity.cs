// SPDX-License-Identifier: MIT
// RustLike — server-side simulation entity contract.
//
// Every server-simulated thing (NPC, dropped item, vehicle, building block) is
// an ISimEntity. Snapshot networking, sleep/wake, persistence iterate by this.
//
// IMPORTANT: ISimEntity is a plain class/struct contract; it is NOT a Unity
// component. Visual representation (if any) lives separately and is wired up
// when the entity wakes.

using UnityEngine;

namespace RustLike.Gameplay.Entities
{
    public interface ISimEntity
    {
        int  EntityId { get; }
        Vector3 Position { get; }

        SimTier Tier { get; }
        void OnTierChanged(SimTier prev, SimTier next);

        /// <summary> Active / Reduced tier tick. Call frequency depends on tier. </summary>
        void TickActive(float dt);
        void TickReduced(float dt);
    }
}
