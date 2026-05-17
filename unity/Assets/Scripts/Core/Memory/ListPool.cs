// SPDX-License-Identifier: MIT
// RustLike — pool for List<T>, HashSet<T>, Dictionary<TK,TV>, StringBuilder.
//
// Pattern (idiomatic):
//     var list = ListPool<int>.Rent();
//     try { /* use list */ } finally { ListPool<int>.Return(list); }
//
// Or with disposable wrapper:
//     using (var scope = ListPool<int>.RentScope(out var list)) { ... }
//
// Generic-class trick: each closed generic gets its own static pool.

using System.Collections.Generic;
using System.Text;

namespace RustLike.Core.Memory
{
    public static class ListPool<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<List<T>> s_Pool = new(16);
        private const int SoftCap = 256;

        public static List<T> Rent()
        {
            return s_Pool.Count > 0 ? s_Pool.Pop() : new List<T>(16);
        }

        public static void Return(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            if (s_Pool.Count < SoftCap) s_Pool.Push(list);
        }

        public static Scope RentScope(out List<T> list)
        {
            list = Rent();
            return new Scope(list);
        }

        public readonly struct Scope : System.IDisposable
        {
            private readonly List<T> _list;
            public Scope(List<T> list) { _list = list; }
            public void Dispose() => Return(_list);
        }
    }

    public static class HashSetPool<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<HashSet<T>> s_Pool = new(8);
        public static HashSet<T> Rent()
            => s_Pool.Count > 0 ? s_Pool.Pop() : new HashSet<T>();
        public static void Return(HashSet<T> set)
        {
            if (set == null) return;
            set.Clear();
            if (s_Pool.Count < 64) s_Pool.Push(set);
        }
    }

    public static class DictionaryPool<TK, TV>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<Dictionary<TK, TV>> s_Pool = new(8);
        public static Dictionary<TK, TV> Rent()
            => s_Pool.Count > 0 ? s_Pool.Pop() : new Dictionary<TK, TV>();
        public static void Return(Dictionary<TK, TV> dict)
        {
            if (dict == null) return;
            dict.Clear();
            if (s_Pool.Count < 64) s_Pool.Push(dict);
        }
    }

    public static class StringBuilderPool
    {
        private static readonly Stack<StringBuilder> s_Pool = new(8);
        public static StringBuilder Rent()
            => s_Pool.Count > 0 ? s_Pool.Pop() : new StringBuilder(128);
        public static void Return(StringBuilder sb)
        {
            if (sb == null) return;
            sb.Clear();
            if (s_Pool.Count < 32) s_Pool.Push(sb);
        }
        public static string ToStringAndReturn(StringBuilder sb)
        {
            string s = sb.ToString();
            Return(sb);
            return s;
        }
    }
}
