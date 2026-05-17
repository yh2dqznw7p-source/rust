// SPDX-License-Identifier: MIT
// RustLike — Unity GameObject prefab pool.
//
// Pools per-prefab. Used for:
//   - bullets, casings, decals, particle systems,
//   - dropped items, loot bags,
//   - UI inventory slot widgets, damage numbers,
//   - NPC visual proxies (visual hull spawned on entity wake).
//
// IMPORTANT: do NOT use this for player-controlled entities or networked
// objects spawned via FishNet — FishNet has its own pooling (NetworkObjectPool).
// This pool is for client-side cosmetic/local objects.
//
// Spawn / Despawn behaviour:
//   - On spawn: SetActive(true), call IPoolable.OnSpawn() if implemented.
//   - On despawn: call IPoolable.OnDespawn(), SetActive(false), reparent under hidden root.
//
// Threading: main thread only.

using System.Collections.Generic;
using RustLike.Core.Logging;
using UnityEngine;

namespace RustLike.Core.Memory
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    public sealed class PrefabPool
    {
        private readonly Dictionary<int, Stack<GameObject>> _pools = new(64);
        private readonly Transform _hiddenRoot;
        private readonly int _softCapPerPrefab;

        public PrefabPool(int softCapPerPrefab = 256)
        {
            _softCapPerPrefab = softCapPerPrefab;
            var go = new GameObject("[PrefabPool/Hidden]");
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            _hiddenRoot = go.transform;
        }

        public void Prewarm(GameObject prefab, int count)
        {
            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var stack))
                _pools[key] = stack = new Stack<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var inst = Object.Instantiate(prefab, _hiddenRoot);
                inst.SetActive(false);
                stack.Push(inst);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            int key = prefab.GetInstanceID();
            GameObject inst;
            if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                inst = stack.Pop();
                var t = inst.transform;
                t.SetParent(parent, false);
                t.SetPositionAndRotation(pos, rot);
            }
            else
            {
                inst = Object.Instantiate(prefab, pos, rot, parent);
                // remember which pool this instance belongs to
                var marker = inst.GetComponent<PoolMarker>() ?? inst.AddComponent<PoolMarker>();
                marker.PrefabKey = key;
            }
            inst.SetActive(true);
            var poolable = inst.GetComponent<IPoolable>();
            poolable?.OnSpawn();
            return inst;
        }

        public void Despawn(GameObject inst)
        {
            if (inst == null) return;
            var marker = inst.GetComponent<PoolMarker>();
            if (marker == null)
            {
                Log.Warn(LogCat.Core, "Despawn() on non-pooled object: " + inst.name);
                Object.Destroy(inst);
                return;
            }
            var poolable = inst.GetComponent<IPoolable>();
            poolable?.OnDespawn();
            inst.SetActive(false);
            inst.transform.SetParent(_hiddenRoot, false);
            if (!_pools.TryGetValue(marker.PrefabKey, out var stack))
                _pools[marker.PrefabKey] = stack = new Stack<GameObject>(16);
            if (stack.Count >= _softCapPerPrefab)
            {
                Object.Destroy(inst);
                return;
            }
            stack.Push(inst);
        }

        public void Clear()
        {
            foreach (var stack in _pools.Values)
            {
                while (stack.Count > 0)
                {
                    var go = stack.Pop();
                    if (go != null) Object.Destroy(go);
                }
            }
            _pools.Clear();
        }

        // Internal marker so we can find which pool to return to.
        // Prefer this over name parsing or path lookups.
        public sealed class PoolMarker : MonoBehaviour
        {
            public int PrefabKey;
        }
    }
}
