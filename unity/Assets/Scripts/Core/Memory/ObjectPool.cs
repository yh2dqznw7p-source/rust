// SPDX-License-Identifier: MIT
// RustLike — generic object pool for plain C# objects (NOT Unity Components).
//
// Usage:
//     var pool = new ObjectPool<MyMsg>(() => new MyMsg(), m => m.Clear(), capacity: 64);
//     var m = pool.Rent();
//     ...
//     pool.Return(m);
//
// Thread-safety: NOT thread-safe by default. Net read thread should use its own
// pool instance. If you need cross-thread, wrap in ConcurrentObjectPool (TODO).
//
// Design notes:
//   - Stack<T> over Queue<T> for cache locality (LIFO).
//   - On Rent overflow we ALLOCATE a new instance instead of throwing — pools
//     should never crash gameplay; instead we log if growth exceeds soft cap.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RustLike.Core.Logging;

namespace RustLike.Core.Memory
{
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T>     _stack;
        private readonly Func<T>      _factory;
        private readonly Action<T>    _onReturn; // reset state
        private readonly Action<T>    _onRent;   // optional warm-up
        private readonly int          _softCap;
        private          int          _outstanding; // for leak diagnostics

        public int CountInactive => _stack.Count;
        public int Outstanding   => _outstanding;

        public ObjectPool(Func<T> factory,
                          Action<T> onReturn = null,
                          Action<T> onRent = null,
                          int capacity = 32,
                          int softCap = 4096)
        {
            _factory  = factory ?? throw new ArgumentNullException(nameof(factory));
            _onReturn = onReturn;
            _onRent   = onRent;
            _stack    = new Stack<T>(capacity);
            _softCap  = softCap;
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++) _stack.Push(_factory());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Rent()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _factory();
            _outstanding++;
            _onRent?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            _onReturn?.Invoke(item);
            if (_stack.Count >= _softCap)
            {
                // drop on the floor; GC will get it eventually
                Log.Warn(LogCat.Core, "ObjectPool<" + typeof(T).Name + "> soft cap reached; dropping.");
            }
            else
            {
                _stack.Push(item);
            }
            _outstanding--;
        }

        public void Clear() => _stack.Clear();
    }
}
