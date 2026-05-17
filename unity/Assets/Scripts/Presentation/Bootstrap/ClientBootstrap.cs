// SPDX-License-Identifier: MIT
// RustLike — Phase 0.5 client entry point.
//
// Hooks at AfterSceneLoad (Core's Bootstrap.Run already created GameLoop).
// Builds an in-code playable scene:
//   - Playground world (terrain, props, trees, stones, ores, dummies)
//   - Player rig (CharacterController + camera + controllers)
//   - HUDs (Survival, Crosshair, Hotbar, Inventory, Crafting)
//   - Inventory + crafting services in ServiceLocator
//   - Starter inventory (hatchet, building parts, etc.)

using RustLike.Core.Bootstrap;
using RustLike.Core.Logging;
using RustLike.Gameplay.Crafting;
using RustLike.Gameplay.Inventory;
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

            // 1. Item registry
            var registry = ItemDatabase.Build();

            // 2. Player inventory + crafting service
            var inv = new PlayerInventory(registry);
            ServiceLocator.Register(inv);

            var craft = new CraftingService(inv);
            ServiceLocator.Register(craft);
            GameLoop.Instance.Register(craft);

            // 3. Vitals
            var vitals = new VitalsService();
            ServiceLocator.Register(vitals);
            const int localEntityId = 1;
            vitals.RegisterPlayer(localEntityId);
            GameLoop.Instance.Register(vitals);

            // 4. Starter loadout for the demo
            inv.PickUp(ItemIds.Hatchet,    1);
            inv.PickUp(ItemIds.Pickaxe,    1);
            inv.PickUp(ItemIds.Pistol,     1);
            inv.PickUp(ItemIds.PistolAmmo, 24);
            inv.PickUp(ItemIds.Bandage,    3);
            inv.PickUp(ItemIds.Apple,      3);
            inv.PickUp(ItemIds.WaterBottle,2);
            inv.PickUp(ItemIds.BFoundation,5);
            inv.PickUp(ItemIds.BWall,      8);
            inv.SelectHotbarSlot(0);

            // 5. World
            CleanDefaultSceneObjects();
            var worldRoot = new GameObject("[World]");
            PlaygroundWorldBuilder.Build(worldRoot.transform);

            // 6. HUDs FIRST so player controllers can find them via FindObjectOfType.
            var hudGo = new GameObject("[HUD]");
            Object.DontDestroyOnLoad(hudGo);
            var hud = hudGo.AddComponent<SurvivalHUD>();
            hud.EntityId = localEntityId;
            hudGo.AddComponent<HotbarHUD>();
            hudGo.AddComponent<CrosshairHUD>();
            hudGo.AddComponent<HelpHUD>();
            hudGo.AddComponent<InventoryWindow>();
            hudGo.AddComponent<CraftingWindow>();

            // 7. Player rig
            var player = SpawnPlayer(localEntityId);

            Log.Info(LogCat.Boot, "ClientBootstrap done at " + player.transform.position);
        }

        private static GameObject SpawnPlayer(int entityId)
        {
            var player = new GameObject("[Player]");
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Head transform parents the camera so look pitch is local.
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

            head.transform.SetSiblingIndex(0);

            var fpc = player.AddComponent<FirstPersonController>();
            fpc.EntityId = entityId;
            player.AddComponent<WeaponController>();
            player.AddComponent<BuildingPlacementController>();
            player.AddComponent<InteractController>();
            return player;
        }

        private static void CleanDefaultSceneObjects()
        {
            var existingCam = Camera.main;
            if (existingCam != null) Object.Destroy(existingCam.gameObject);
            foreach (var al in Object.FindObjectsOfType<AudioListener>())
                Object.Destroy(al);
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) Object.Destroy(l.gameObject);
        }
    }
}
