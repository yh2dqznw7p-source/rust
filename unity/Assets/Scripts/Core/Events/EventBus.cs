// SPDX-License-Identifier: MIT
// RustLike — typed, allocation-friendly event bus.
//
// Design:
//   - Events are STRUCTS to avoid GC and to fit in tightly-packed lists.
//   - One static channel per event type (closed generic class trick).
//   - Subscribers are Action<TEvent> delegates; we DO NOT box.
//   - Iterating over a snapshot list ensures unsubscribe-during-publish is safe.
//
// Rules:
//   - DO NOT publish per-frame from hot path. EventBus is for "command-like"
//     state changes, not for streaming data (use direct interfaces for that).
//   - Handlers must be cheap (<<1 ms). Heavy work goes through queues.

using System;
using System.Collections.Generic;
using RustLike.Core.Logging;

namespace RustLike.Core.Events
{
    public static class EventBus
    {
        // closed generic = unique static field per TEvent type, lock-free reads
        private static class Channel<TEvent> where TEvent : struct
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly List<Action<TEvent>> Handlers = new(8);
            // snapshot used during publish to allow unsubscribe inside handlers
            // ReSharper disable once StaticMemberInGenericType
            public static Action<TEvent>[] Snapshot = Array.Empty<Action<TEvent>>();
            public static int SnapshotCount;
            public static bool Dirty;
        }

        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Channel<TEvent>.Handlers.Add(handler);
            Channel<TEvent>.Dirty = true;
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            if (handler == null) return;
            if (Channel<TEvent>.Handlers.Remove(handler))
                Channel<TEvent>.Dirty = true;
        }

        public static void Publish<TEvent>(in TEvent evt) where TEvent : struct
        {
            EnsureSnapshot<TEvent>();
            var arr = Channel<TEvent>.Snapshot;
            int n = Channel<TEvent>.SnapshotCount;
            for (int i = 0; i < n; i++)
            {
                try { arr[i].Invoke(evt); }
                catch (Exception ex)
                {
                    Log.Error(LogCat.Core, "EventBus handler threw for " + typeof(TEvent).Name, ex);
                }
            }
        }

        public static void ClearAll()
        {
            // for tests / scene reload only
            Channel<UnitDummy>.Handlers.Clear();
        }

        private static void EnsureSnapshot<TEvent>() where TEvent : struct
        {
            if (!Channel<TEvent>.Dirty) return;
            var src = Channel<TEvent>.Handlers;
            int n = src.Count;
            if (Channel<TEvent>.Snapshot.Length < n)
                Channel<TEvent>.Snapshot = new Action<TEvent>[Math.Max(n, 8)];
            for (int i = 0; i < n; i++) Channel<TEvent>.Snapshot[i] = src[i];
            // null-out tail to avoid leaking refs
            for (int i = n; i < Channel<TEvent>.Snapshot.Length; i++) Channel<TEvent>.Snapshot[i] = null;
            Channel<TEvent>.SnapshotCount = n;
            Channel<TEvent>.Dirty = false;
        }

        // dummy type used to force static ctor / clear hook only
        private struct UnitDummy { }
    }
}
