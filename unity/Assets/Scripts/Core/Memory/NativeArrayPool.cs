// SPDX-License-Identifier: MIT
// RustLike — pool of NativeArray<T> by power-of-two size.
//
// Why pool NativeArray?
//   - NativeArray<T>(capacity, Allocator.Persistent) is fast but not free.
//   - Allocator.Temp / TempJob have lifetime restrictions that hurt async jobs.
//   - We size up to next power of two so reuse rate is high.
//
// IMPORTANT:
//   - Pool stores arrays of POWER-OF-TWO sizes. Caller gets >= requested.
//   - Caller must NOT dispose the returned array; pass it back to Return().
//   - Allocator is fixed at construction time. Default: Persistent.
//
// Thread-safety: NOT thread-safe. Each job system thread gets its own instance,
// or we wrap the public surface in a lock for simplicity (TODO if needed).

using System.Collections.Generic;
using Unity.Collections;

namespace RustLike.Core.Memory
{
    public sealed class NativeArrayPool<T> where T : unmanaged
    {
        private readonly Allocator _alloc;
        // bucket index = ceil(log2(len)); arrays of len >= 2^idx
        private readonly Dictionary<int, Stack<NativeArray<T>>> _buckets = new(8);
        private readonly int _maxBucketsPerSize;

        public NativeArrayPool(Allocator alloc = Allocator.Persistent, int maxBucketsPerSize = 16)
        {
            _alloc = alloc;
            _maxBucketsPerSize = maxBucketsPerSize;
        }

        public NativeArray<T> Rent(int minLength, out int actualLength)
        {
            int bucket = NextPow2Bucket(minLength);
            actualLength = 1 << bucket;
            if (_buckets.TryGetValue(bucket, out var stack) && stack.Count > 0)
                return stack.Pop();
            return new NativeArray<T>(actualLength, _alloc, NativeArrayOptions.UninitializedMemory);
        }

        public void Return(NativeArray<T> arr)
        {
            if (!arr.IsCreated) return;
            int bucket = NextPow2Bucket(arr.Length);
            if (!_buckets.TryGetValue(bucket, out var stack))
                _buckets[bucket] = stack = new Stack<NativeArray<T>>(4);
            if (stack.Count >= _maxBucketsPerSize)
            {
                arr.Dispose();
                return;
            }
            stack.Push(arr);
        }

        public void Dispose()
        {
            foreach (var stack in _buckets.Values)
                while (stack.Count > 0)
                    stack.Pop().Dispose();
            _buckets.Clear();
        }

        private static int NextPow2Bucket(int n)
        {
            if (n < 1) return 0;
            int b = 0;
            int v = n - 1;
            while (v > 0) { v >>= 1; b++; }
            return b;
        }
    }
}
