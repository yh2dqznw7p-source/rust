// SPDX-License-Identifier: MIT
// RustLike — world-placed storage chest / locker / fridge etc.
//
// Each StorageContainer wraps one Container instance and exposes it for
// the player to open via the E key when looking at the deployable.
//
// On Phase 0.5 we don't sync this over the network — it's a local-only
// proxy so the demo can show "loot a box".

using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Gameplay.Loot
{
    public sealed class StorageContainer : MonoBehaviour
    {
        public string DisplayName = "Storage";
        public int Capacity = 12;

        public Container Container { get; private set; }

        private static int s_nextContainerId = 1000;

        public void Initialize(ItemRegistry registry, int capacity, string displayName)
        {
            Capacity = capacity;
            DisplayName = displayName;
            Container = new Container(s_nextContainerId++, capacity, registry);
        }

        public void Initialize(ItemRegistry registry)
        {
            Container = new Container(s_nextContainerId++, Capacity, registry);
        }
    }
}
