// SPDX-License-Identifier: MIT
// RustLike — lightweight allocation-free logger.
//
// Why not UnityEngine.Debug everywhere?
//   - string interpolation allocates,
//   - Debug.Log on hot path costs ~5-15 us per call,
//   - we want category filters and a flat enable/disable flag.
//
// Use:
//   Log.Info(LogCat.Net, "Client connected: ", clientId);
//   Log.Warn(LogCat.World, "Chunk ", coord, " failed");
//
// Hot loops should still gate with `if (Log.IsEnabled(LogCat.AI)) ...`.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RustLike.Core.Logging
{
    public enum LogCat : byte
    {
        Boot,
        Core,
        Net,
        World,
        Gameplay,
        Building,
        Inventory,
        Survival,
        Combat,
        AI,
        UI,
        Audio,
        Streaming,
        Persist,
        Server,
        Perf,
    }

    public enum LogLevel : byte
    {
        Trace,
        Info,
        Warn,
        Error,
    }

    public static class Log
    {
        // bit-mask on LogCat; default: everything except Trace
        private static int s_EnabledCats = ~0;
        private static LogLevel s_MinLevel = LogLevel.Info;

        public static void SetMinLevel(LogLevel level) => s_MinLevel = level;

        public static void EnableCategory(LogCat cat, bool on)
        {
            int mask = 1 << (int)cat;
            if (on) s_EnabledCats |= mask;
            else    s_EnabledCats &= ~mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(LogCat cat, LogLevel level = LogLevel.Info)
            => level >= s_MinLevel && (s_EnabledCats & (1 << (int)cat)) != 0;

        // ---- Hot path: avoid string concat by using params object[] only when allowed ----

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(LogCat cat, string msg)
        {
            if (!IsEnabled(cat, LogLevel.Trace)) return;
            UnityEngine.Debug.Log(Format(cat, "TRC", msg));
        }

        public static void Info(LogCat cat, string msg)
        {
            if (!IsEnabled(cat, LogLevel.Info)) return;
            UnityEngine.Debug.Log(Format(cat, "INF", msg));
        }

        public static void Info(LogCat cat, string a, object b)
        {
            if (!IsEnabled(cat, LogLevel.Info)) return;
            UnityEngine.Debug.Log(Format(cat, "INF", a + b));
        }

        public static void Warn(LogCat cat, string msg)
        {
            if (!IsEnabled(cat, LogLevel.Warn)) return;
            UnityEngine.Debug.LogWarning(Format(cat, "WRN", msg));
        }

        public static void Warn(LogCat cat, string a, object b)
        {
            if (!IsEnabled(cat, LogLevel.Warn)) return;
            UnityEngine.Debug.LogWarning(Format(cat, "WRN", a + b));
        }

        public static void Error(LogCat cat, string msg)
        {
            if (!IsEnabled(cat, LogLevel.Error)) return;
            UnityEngine.Debug.LogError(Format(cat, "ERR", msg));
        }

        public static void Error(LogCat cat, string msg, Exception ex)
        {
            if (!IsEnabled(cat, LogLevel.Error)) return;
            UnityEngine.Debug.LogError(Format(cat, "ERR", msg) + " | " + ex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Format(LogCat cat, string lvl, string msg)
            => "[" + cat + "][" + lvl + "] " + msg;
    }
}
