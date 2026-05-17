// SPDX-License-Identifier: MIT
// RustLike — Phase 0 client entry point.
//
// Hooks in at AfterSceneLoad (so Core's Bootstrap.Run already ran at
// BeforeSceneLoad, GameLoop exists, ServiceLocator is available).
//
// Builds an "in-code scene": ground, lights, player, HUD. This way the
// project is playable from a clean clone without authoring a .unity file.
//
// Replace this in Phase 1+ with a real MainMenu → Game scene flow.

using RustLike.Core.Bootstrap;
using RustLike.Core.Logging;
using RustLike.Gameplay.Survival;
using RustLike.Presentation.Player;
using RustLike.Presentation.UI;
using UnityEngine;
using AppBootstrap = RustLike.Core.Bootstrap.Bootstrap;

namespace RustLike.Presentation.Bootstrap
{
    public static class ClientBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnLoad()
        {
            if (AppBootstrap.Mode == AppMode.Server) return;

            // --- 1. Register gameplay services (server would do same in ServerBootstrap)
            var vitals = new VitalsService();
            ServiceLocator.Register(vitals);
            const int localEntityId = 1;
            vitals.RegisterPlayer(localEntityId);
            GameLoop.Instance.Register(vitals);

            // --- 2. World root (everything we spawn here lives under it)
            var worldRoot = new GameObject("[World]");

            // Default Unity scene ships with a Main Camera and Directional Light;
            // remove them so we don't end up with duplicate cameras / two
            // AudioListeners (Unity warns about that).
            CleanDefaultSceneObjects();

            // --- 3. Ground + lights + props
            PlaygroundWorldBuilder.Build(worldRoot.transform);

            // --- 4. Player
            var player = SpawnPlayer(localEntityId);

            // --- 5. HUD
            var hudGo = new GameObject("[HUD]");
            Object.DontDestroyOnLoad(hudGo);
            var hud = hudGo.AddComponent<SurvivalHUD>();
            hud.EntityId = localEntityId;

            Log.Info(LogCat.Boot, "ClientBootstrap done. Spawned player at " + player.transform.position);
        }

        private static GameObject SpawnPlayer(int entityId)
        {
            var player = new GameObject("[Player]");
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Head transform (parents the camera so look pitch is local).
            var head = new GameObject("[Head]");
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // Camera
            var camGo = new GameObject("[MainCamera]");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(head.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 350f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.65f, 0.78f);
            camGo.AddComponent<AudioListener>();

            // Reorder children: FirstPersonController expects the head to be child 0.
            head.transform.SetSiblingIndex(0);

            var fpc = player.AddComponent<FirstPersonController>();
            fpc.EntityId = entityId;
            return player;
        }

        private static void CleanDefaultSceneObjects()
        {
            // Tagged main camera (default scene).
            var existingCam = Camera.main;
            if (existingCam != null) Object.Destroy(existingCam.gameObject);

            // Any leftover AudioListeners (we'll add ours on the player camera).
            foreach (var al in Object.FindObjectsOfType<AudioListener>())
                Object.Destroy(al);

            // Any leftover Directional lights (we add our own sun).
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) Object.Destroy(l.gameObject);
        }
    }
}
