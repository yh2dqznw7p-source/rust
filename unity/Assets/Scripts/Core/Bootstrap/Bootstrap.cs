// SPDX-License-Identifier: MIT
// RustLike — entry point.
//
// Mode detection:
//   - `UNITY_SERVER` define     → server build
//   - `-server` CLI arg         → run as server in editor / client build
//   - otherwise                 → client
//
// Order of operations (intentional):
//   1. Logging set up.
//   2. Quality + frame rate.
//   3. ServiceLocator: register cross-cutting singletons (no scene refs yet).
//   4. GameLoop spawned (DontDestroyOnLoad).
//   5. Mode-specific bootstrap (Server.ServerBootstrap or Client bootstrap)
//      registers its systems and loads the first scene.
//
// We DO NOT touch URP, UI, audio, etc. here. That's Presentation's job.

using RustLike.Core.Logging;
using UnityEngine;

namespace RustLike.Core.Bootstrap
{
    public enum AppMode { Client, Server, ListenServer }

    public static class Bootstrap
    {
        public static AppMode Mode { get; private set; }

        // Called from Unity via [RuntimeInitializeOnLoadMethod] in BootstrapEntry
        public static void Run()
        {
            Mode = DetectMode();
            Log.Info(LogCat.Boot, "RustLike starting in mode=", Mode);

            // 1. Frame settings (server gets a sane default; client overrides via Quality)
            Application.runInBackground = true;
            if (Mode == AppMode.Server)
            {
                Application.targetFrameRate = 60; // matches 20 Hz sim with headroom
                QualitySettings.vSyncCount = 0;
            }

            // 2. ServiceLocator stays unlocked until mode bootstrap finishes.
            ServiceLocator.Reset();

            // 3. GameLoop host
            var host = new GameObject("[RustLike]");
            Object.DontDestroyOnLoad(host);
            GameLoop.Boot(host);

            // 4. Mode-specific bootstrap is invoked by an entry component below.
            //    Server.ServerBootstrap and Presentation.ClientBootstrap each
            //    subscribe via their own [RuntimeInitializeOnLoadMethod].
            //    This keeps Core free of upper-layer references.
        }

        private static AppMode DetectMode()
        {
#if UNITY_SERVER
            return AppMode.Server;
#else
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-server")       return AppMode.Server;
                if (args[i] == "-listenserver") return AppMode.ListenServer;
            }
            return AppMode.Client;
#endif
        }
    }

    /// <summary> Hook into Unity load order. Stays in Core. </summary>
    internal static class BootstrapEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnLoad() => Bootstrap.Run();
    }
}
