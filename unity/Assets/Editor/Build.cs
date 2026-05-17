// SPDX-License-Identifier: MIT
// RustLike — CI build helper invoked by GameCI / unity-builder.
//
// Lives under Assets/Editor so it's only included in the editor compilation
// (not in player builds). Public methods are reachable via the
// `-executeMethod RustLike.Editor.Build.Run` Unity CLI flag.
//
// Why we need this:
//   - Default Unity build picks scenes from EditorBuildSettings.asset. We
//     don't ship a real scene yet (Phase 0 builds a scene at runtime via
//     RuntimeInitializeOnLoadMethod), so we synthesize an empty scene at
//     build time and add it to the build settings.
//   - Lets us pass platform via env var BUILD_TARGET so a single method
//     handles all CI matrix entries.
//   - Stamps the binary with a deterministic name + version so CI artifacts
//     are easy to identify.

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RustLike.Editor
{
    public static class Build
    {
        // GameCI passes the absolute output path via -customBuildPath, e.g.
        //   /github/workspace/build/StandaloneWindows64/RustLike.exe
        // If we instead use a relative path, Unity resolves it against the
        // project root (`unity/`), and the .exe ends up in `unity/build/...`,
        // which GameCI's "verify build output" step then can't find.
        // So we ALWAYS prefer the absolute -customBuildPath when present.
        public static void Run()
        {
            BuildTarget target = ResolveTarget();
            string fullPath = ResolveCustomBuildPath(target);
            string outDir   = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(outDir);
            Debug.Log($"[RustLike CI] Output path: {fullPath}");

            // Make sure we have a scene in EditorBuildSettings. We don't ship
            // a .unity asset, so generate a minimal empty one and add it.
            string bootScene = EnsureBootstrapScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootScene, enabled: true),
            };

            var options = new BuildPlayerOptions
            {
                scenes           = new[] { bootScene },
                locationPathName = fullPath,
                target           = target,
                targetGroup      = BuildPipeline.GetBuildTargetGroup(target),
                options          = BuildOptions.None,
            };

            // Optimization-friendly defaults for slim builds.
            PlayerSettings.SetScriptingBackend(options.targetGroup, ScriptingImplementation.Mono2x);
            PlayerSettings.SetIl2CppCompilerConfiguration(options.targetGroup, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(options.targetGroup, ManagedStrippingLevel.Low);
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.bundleVersion = ResolveVersion();

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[RustLike CI] Build finished: result={summary.result} totalSize={summary.totalSize} bytes path={summary.outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                // Non-zero exit so unity-builder fails the job.
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        // ---- helpers ----

        private static BuildTarget ResolveTarget()
        {
            // GameCI passes -buildTarget; Unity also exposes it via the API
            // already-applied to EditorUserBuildSettings.activeBuildTarget.
            // Fall back to env override or current active target.
            string envTarget = Environment.GetEnvironmentVariable("BUILD_TARGET");
            if (!string.IsNullOrEmpty(envTarget) &&
                Enum.TryParse(envTarget, out BuildTarget parsed))
            {
                return parsed;
            }
            return EditorUserBuildSettings.activeBuildTarget;
        }

        /// <summary>
        /// Look at -customBuildPath argv, then BUILD_PATH+BUILD_FILE env vars,
        /// and only fall back to a project-relative path if nothing else works.
        /// </summary>
        private static string ResolveCustomBuildPath(BuildTarget target)
        {
            // 1) -customBuildPath ABSOLUTE
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-customBuildPath" && !string.IsNullOrEmpty(args[i + 1]))
                    return args[i + 1];
            }
            // 2) GameCI env vars
            string envPath = Environment.GetEnvironmentVariable("BUILD_PATH");
            string envFile = Environment.GetEnvironmentVariable("BUILD_FILE");
            if (!string.IsNullOrEmpty(envPath) && !string.IsNullOrEmpty(envFile))
            {
                // BUILD_PATH is relative to GITHUB_WORKSPACE; make it absolute.
                string ws = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
                string baseDir = !string.IsNullOrEmpty(ws)
                    ? Path.Combine(ws, envPath)
                    : Path.Combine("..", envPath); // sibling of project
                return Path.Combine(baseDir, envFile);
            }
            // 3) Last-resort: project-relative.
            return Path.Combine("build", target.ToString(), "RustLike" + ExtensionFor(target));
        }

        private static string ExtensionFor(BuildTarget t) => t switch
        {
            BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64 => ".exe",
            BuildTarget.StandaloneLinux64          => ".x86_64",
            BuildTarget.StandaloneOSX              => ".app",
            _                                      => "",
        };

        private static string ResolveVersion()
        {
            // Stamp with the short SHA from CI if available.
            string sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (!string.IsNullOrEmpty(sha) && sha.Length >= 7) sha = sha.Substring(0, 7);
            else                                              sha = "dev";
            return "0.0.1+" + sha;
        }

        private static string EnsureBootstrapScene()
        {
            const string path = "Assets/_Project/Scenes/Bootstrap.unity";
            if (File.Exists(path)) return path;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Create an empty scene with no objects. ClientBootstrap will
            // populate it at runtime via [RuntimeInitializeOnLoadMethod].
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            return path;
        }
    }
}
#endif
