// SPDX-License-Identifier: MIT
// RustLike — ghost preview for building placement (Phase 0.5).
//
// Active when the player has a building or deployable item in the equipped
// hotbar slot:
//   - A semi-transparent ghost mesh follows the camera's forward raycast.
//   - Green ghost = legal (on terrain or another foundation), red = invalid.
//   - LMB places, consumes 1 of the equipped item, spawns a real prop.
//   - R rotates the ghost.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Inventory;
using RustLike.Gameplay.Loot;
using RustLike.Presentation.Bootstrap;
using RustLike.Presentation.UI;
using UnityEngine;

namespace RustLike.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class BuildingPlacementController : MonoBehaviour
    {
        public bool HasGhost => _ghost != null;

        [SerializeField] private float _placeDistance = 5f;
        [SerializeField] private LayerMask _placeMask = ~0;

        private Transform _camera;
        private PlayerInventory _inv;
        private InventoryWindow _inventoryWindow;
        private CraftingWindow _craftingWindow;

        private GameObject _ghost;
        private MeshRenderer[] _ghostRenderers;
        private MaterialPropertyBlock _ghostMpb;
        private ushort _ghostItemId;
        private float _yawDeg;

        private void Awake()
        {
            ServiceLocator.TryGet(out _inv);
            var cam = GetComponentInChildren<Camera>();
            _camera = cam != null ? cam.transform : transform;
            _inventoryWindow = FindObjectOfType<InventoryWindow>();
            _craftingWindow  = FindObjectOfType<CraftingWindow>();
            _ghostMpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (_inv != null) _inv.EquippedChanged += OnEquippedChanged;
        }
        private void OnDisable()
        {
            if (_inv != null) _inv.EquippedChanged -= OnEquippedChanged;
            DestroyGhost();
        }

        private void OnEquippedChanged(ItemDef def, ItemStack stack)
        {
            DestroyGhost();
            if (def == null) return;
            if (def.Category != ItemCategory.Building && def.Category != ItemCategory.Furniture) return;
            CreateGhost(def);
        }

        private void Update()
        {
            // Don't drive while UI is open
            if (_inventoryWindow != null && _inventoryWindow.IsOpen) { if (_ghost) _ghost.SetActive(false); return; }
            if (_craftingWindow  != null && _craftingWindow.IsOpen)  { if (_ghost) _ghost.SetActive(false); return; }

            if (_inv == null) { ServiceLocator.TryGet(out _inv); return; }

            // Equipped changed without firing event? Re-check.
            var eq = _inv.GetEquippedDef();
            if (eq == null || (eq.Category != ItemCategory.Building && eq.Category != ItemCategory.Furniture))
            {
                if (_ghost != null) DestroyGhost();
                return;
            }
            if (_ghost == null) CreateGhost(eq);

            if (_ghost == null) return;
            _ghost.SetActive(true);

            if (Input.GetKeyDown(KeyCode.R)) _yawDeg += 90f;

            // Raycast position
            Vector3 origin = _camera.position;
            Vector3 dir = _camera.forward;
            bool hit = Physics.Raycast(origin, dir, out var rh, _placeDistance, _placeMask, QueryTriggerInteraction.Ignore);
            // Ignore ghost itself
            bool valid = hit && rh.collider != null && !rh.collider.transform.IsChildOf(_ghost.transform);

            Vector3 pos = valid ? rh.point : origin + dir * _placeDistance;
            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yawDeg, 0f));
            ApplyGhostColor(valid);

            if (valid && Input.GetMouseButtonDown(0))
                Place(eq, pos, _yawDeg);
        }

        private void Place(ItemDef def, Vector3 pos, float yawDeg)
        {
            // Consume 1 of the equipped item
            ushort removed = _inv.RemoveAll(def.ItemId, 1);
            if (removed == 0) return;

            // Spawn the actual world object
            GameObject go = BuildPropSpawner.Spawn(def, pos, Quaternion.Euler(0f, yawDeg, 0f));
            if (go == null) return;

            // Storage deployables get a Container attached
            if (TryGetStorageCapacity(def.ItemId, out int cap))
            {
                var sc = go.AddComponent<StorageContainer>();
                sc.Initialize(_inv.Registry, cap, def.DisplayName);
            }
        }

        private void CreateGhost(ItemDef def)
        {
            DestroyGhost();
            _ghostItemId = def.ItemId;
            _ghost = BuildPropSpawner.Spawn(def, transform.position + transform.forward * _placeDistance, Quaternion.identity);
            if (_ghost == null) return;
            _ghost.name = "[BuildGhost]";
            // Strip colliders so we don't block our own raycast
            foreach (var c in _ghost.GetComponentsInChildren<Collider>(includeInactive: true))
                Destroy(c);
            // Strip lights / scripts that shouldn't run on a ghost
            foreach (var l in _ghost.GetComponentsInChildren<Light>(includeInactive: true))
                Destroy(l);

            _ghostRenderers = _ghost.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            _yawDeg = 0f;
            ApplyGhostColor(true);
        }

        private void DestroyGhost()
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null;
            _ghostRenderers = null;
            _ghostItemId = 0;
        }

        private void ApplyGhostColor(bool valid)
        {
            if (_ghostRenderers == null) return;
            Color c = valid
                ? new Color(0.30f, 0.95f, 0.30f, 0.55f)
                : new Color(0.95f, 0.30f, 0.30f, 0.55f);
            _ghostMpb.SetColor("_BaseColor", c);
            _ghostMpb.SetColor("_Color",     c);
            for (int i = 0; i < _ghostRenderers.Length; i++)
                _ghostRenderers[i].SetPropertyBlock(_ghostMpb);
        }

        private static bool TryGetStorageCapacity(ushort itemId, out int capacity)
        {
            switch (itemId)
            {
                case ItemIds.SmallBox: capacity = 12; return true;
                case ItemIds.LargeBox: capacity = 30; return true;
                case ItemIds.Locker:   capacity = 16; return true;
                case ItemIds.Fridge:   capacity = 24; return true;
                default: capacity = 0; return false;
            }
        }
    }
}
