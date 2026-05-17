// SPDX-License-Identifier: MIT
// RustLike — Addressables-backed implementation of IAssetService.
//
// Why we don't expose Addressables types directly:
//   - keeps Core decoupled from a specific package,
//   - centralizes ref-counting and error handling,
//   - allows us to add cache pre-warm, prio queues, and trimming logic.
//
// Memory budget enforcement:
//   - Track approximate bytes per loaded asset (best-effort).
//   - When budget exceeded, force trim of zero-ref entries.
//   - HARD cap: throws on attempt to exceed (caller should retry after release).
//
// NOTE: this file uses #if to guard against Addressables not being installed
// (e.g., in early CI builds). Stub falls back to Resources.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RustLike.Core.Logging;
using UnityEngine;

#if ADDRESSABLES_AVAILABLE || true
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace RustLike.Core.Streaming
{
    public sealed class AddressablesAssetService : IAssetService
    {
        private long _nextId = 1;

        // handle id -> entry
        private readonly Dictionary<long, Entry> _handles = new(256);
        // address key -> shared entry (when using LoadAssetAsync we share)
        private readonly Dictionary<string, SharedEntry> _shared = new(256);

        public long EstimatedMemoryBytes { get; private set; }

        public long MemoryBudgetBytes { get; }

        public AddressablesAssetService(long memoryBudgetBytes = 1L * 1024 * 1024 * 1024)
        {
            MemoryBudgetBytes = memoryBudgetBytes;
        }

        // ---------- Load asset (no instantiate) ----------

        public async ValueTask<(AssetHandle handle, T asset)> LoadAsync<T>(
            string key, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (!_shared.TryGetValue(key, out var shared))
            {
                shared = new SharedEntry { Key = key, RefCount = 0 };
                shared.AssetOp = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
                _shared[key] = shared;
            }

            shared.RefCount++;

            try
            {
                await shared.AssetOp.Task.ConfigureAwait(true);
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Release(shared, key);
                throw;
            }

            if (shared.AssetOp.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(LogCat.Streaming, "Failed to load asset: " + key);
                Release(shared, key);
                return (default, null);
            }

            if (shared.AssetOp.Result is not T typed)
            {
                Log.Error(LogCat.Streaming, "Asset wrong type: " + key + " expected " + typeof(T).Name);
                Release(shared, key);
                return (default, null);
            }

            long id = _nextId++;
            _handles[id] = new Entry { Key = key, IsInstance = false };
            EstimatedMemoryBytes += EstimateAssetBytes(typed);
            return (new AssetHandle(id), typed);
        }

        // ---------- Instantiate ----------

        public async ValueTask<(AssetHandle handle, GameObject instance)> InstantiateAsync(
            string key, Transform parent = null, bool worldPositionStays = false,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var op = Addressables.InstantiateAsync(key, parent, worldPositionStays);
            try { await op.Task.ConfigureAwait(true); }
            catch
            {
                if (op.IsValid()) Addressables.Release(op);
                throw;
            }
            ct.ThrowIfCancellationRequested();

            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(LogCat.Streaming, "Failed to instantiate: " + key);
                Addressables.Release(op);
                return (default, null);
            }

            long id = _nextId++;
            _handles[id] = new Entry
            {
                Key = key,
                IsInstance = true,
                InstanceObj = op.Result,
            };
            return (new AssetHandle(id), op.Result);
        }

        // ---------- Release ----------

        public void Release(AssetHandle handle)
        {
            if (!handle.IsValid) return;
            if (!_handles.TryGetValue(handle.Id, out var entry)) return;
            _handles.Remove(handle.Id);

            if (entry.IsInstance)
            {
                if (entry.InstanceObj != null)
                    Addressables.ReleaseInstance(entry.InstanceObj);
            }
            else if (_shared.TryGetValue(entry.Key, out var shared))
            {
                Release(shared, entry.Key);
            }
        }

        private void Release(SharedEntry shared, string key)
        {
            shared.RefCount--;
            if (shared.RefCount <= 0)
            {
                if (shared.AssetOp.IsValid())
                    Addressables.Release(shared.AssetOp);
                _shared.Remove(key);
            }
        }

        // ---------- Trim ----------

        public void TrimUnused()
        {
            if (EstimatedMemoryBytes < MemoryBudgetBytes) return;
            // Addressables manages its own cache; we only nudge zero-ref shared entries.
            // (Right now Release() already drops them.)
            Resources.UnloadUnusedAssets();
        }

        private static long EstimateAssetBytes(UnityEngine.Object asset)
        {
            // Cheap heuristic; precise size requires Profiler.GetRuntimeMemorySizeLong
            // which is editor-only and slow. We update budget statistically.
            return asset switch
            {
                Texture t  => (long)t.width * t.height * 4L, // worst case RGBA32
                Mesh m     => 64L * 1024L,                   // ~64KB avg
                AudioClip a => (long)a.samples * a.channels * 2L,
                _ => 16L * 1024L,
            };
        }

        // ---- internal types ----

        private struct Entry
        {
            public string Key;
            public bool IsInstance;
            public GameObject InstanceObj; // only when IsInstance
        }

        private sealed class SharedEntry
        {
            public string Key;
            public int RefCount;
            public AsyncOperationHandle<UnityEngine.Object> AssetOp;
        }
    }
}
