// SPDX-License-Identifier: MIT
// RustLike — central item lookup by id and shortname.
//
// Loaded once at boot from Addressables label "items". After that, all access
// is O(1) via dense array (ItemId -> ItemDef) for hot paths and Dictionary
// for shortname.
//
// Server and client both load the SAME registry to keep ids consistent.

using System.Collections.Generic;
using RustLike.Core.Logging;

namespace RustLike.Gameplay.Inventory
{
    public sealed class ItemRegistry
    {
        // index by ItemId. Ids are dense small uints; we use an array.
        // Hole-tolerant: missing ids are null.
        private ItemDef[] _byId = new ItemDef[256];
        private readonly Dictionary<string, ItemDef> _byShortname = new(256);

        public void Register(ItemDef def)
        {
            if (def == null) return;
            if (def.ItemId == 0)
            {
                Log.Error(LogCat.Inventory, "ItemDef has ItemId=0: " + def.name);
                return;
            }
            if (def.ItemId >= _byId.Length)
            {
                int newLen = _byId.Length;
                while (newLen <= def.ItemId) newLen *= 2;
                System.Array.Resize(ref _byId, newLen);
            }
            if (_byId[def.ItemId] != null && _byId[def.ItemId] != def)
            {
                Log.Error(LogCat.Inventory, "Duplicate ItemId " + def.ItemId
                    + " between " + _byId[def.ItemId].name + " and " + def.name);
                return;
            }
            _byId[def.ItemId] = def;
            _byShortname[def.Shortname] = def;
        }

        public ItemDef GetById(ushort id)
        {
            if (id == 0 || id >= _byId.Length) return null;
            return _byId[id];
        }

        public ItemDef GetByShortname(string shortname)
        {
            return _byShortname.TryGetValue(shortname, out var d) ? d : null;
        }

        public int Count
        {
            get { int n = 0; for (int i = 0; i < _byId.Length; i++) if (_byId[i] != null) n++; return n; }
        }
    }
}
