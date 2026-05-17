// SPDX-License-Identifier: MIT
// RustLike — List<T> alternative with O(1) RemoveAt via swap-with-last.
//
// When to use:
//   - Iteration order DOES NOT matter,
//   - Frequent removals from middle,
//   - Want to avoid the O(n) shift cost of List<T>.RemoveAt.
//
// AOI cells, dirty entity lists, broadphase buckets — all use this.

using System;
using System.Runtime.CompilerServices;

namespace RustLike.Core.Memory
{
    public struct SwapList<T>
    {
        public T[] Items;
        public int Count;

        public SwapList(int capacity)
        {
            Items = new T[capacity > 0 ? capacity : 8];
            Count = 0;
        }

        public T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Items[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Items[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (Items == null) Items = new T[8];
            if (Count == Items.Length) Array.Resize(ref Items, Items.Length * 2);
            Items[Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwap(int index)
        {
            int last = Count - 1;
            if (index != last) Items[index] = Items[last];
            Items[last] = default;
            Count = last;
        }

        public void Clear()
        {
            if (Items != null) Array.Clear(Items, 0, Count);
            Count = 0;
        }

        public bool RemoveSwap(T item)
        {
            var cmp = System.Collections.Generic.EqualityComparer<T>.Default;
            for (int i = 0; i < Count; i++)
            {
                if (cmp.Equals(Items[i], item)) { RemoveAtSwap(i); return true; }
            }
            return false;
        }
    }
}
