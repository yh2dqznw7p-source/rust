// SPDX-License-Identifier: MIT
// RustLike — asset service abstraction.
//
// We hide Addressables behind an interface so:
//   - tests can use a stub (no Addressables init in unit tests),
//   - we can swap to a different system (Resources, AssetBundles directly,
//     or future Unity Cloud Content Delivery) without touching call sites.
//
// Async API uses ValueTask to avoid Task allocations in hot streaming paths.
//
// Reference counting:
//   - Each LoadAsync() must be paired with Release() once the user no longer
//     needs the asset. The implementation tracks ref counts internally.
//
// Cancellation:
//   - We pass a CancellationToken through to the implementation. If the chunk
//     is unloaded mid-flight, we cancel the load to avoid wasting RAM.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RustLike.Core.Streaming
{
    /// <summary> Opaque handle returned by LoadAsync. Pass back to Release(). </summary>
    public readonly struct AssetHandle : IEquatable<AssetHandle>
    {
        public readonly long Id;
        public AssetHandle(long id) { Id = id; }
        public bool IsValid => Id != 0;
        public bool Equals(AssetHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is AssetHandle h && Equals(h);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public interface IAssetService
    {
        /// <summary> Load any UnityEngine.Object subclass by Addressables key. </summary>
        ValueTask<(AssetHandle handle, T asset)> LoadAsync<T>(
            string key, CancellationToken ct = default) where T : UnityEngine.Object;

        /// <summary> Load + instantiate as a GameObject (parent optional). </summary>
        ValueTask<(AssetHandle handle, GameObject instance)> InstantiateAsync(
            string key, Transform parent = null, bool worldPositionStays = false,
            CancellationToken ct = default);

        /// <summary> Drop ref-count. When count reaches 0 the bundle may unload. </summary>
        void Release(AssetHandle handle);

        /// <summary> Estimated working set bytes from this service's loaded assets. </summary>
        long EstimatedMemoryBytes { get; }

        /// <summary> Force-unload anything with refcount == 0 (e.g. on chunk unload). </summary>
        void TrimUnused();
    }
}
