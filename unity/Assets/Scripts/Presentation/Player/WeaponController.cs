// SPDX-License-Identifier: MIT
// RustLike — fires the weapon currently held in the player's selected hotbar slot.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Combat;
using RustLike.Gameplay.Inventory;
using RustLike.Presentation.UI;
using UnityEngine;

namespace RustLike.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private float _meleeForwardOffset = 0.5f;

        private Transform _camera;
        private PlayerInventory _inv;
        private float _nextFireTime;

        private LineRenderer _tracer;
        private float _tracerHideTime;

        private InventoryWindow _inventoryWindow;
        private CraftingWindow _craftingWindow;
        private BuildingPlacementController _build;

        private void Awake()
        {
            ServiceLocator.TryGet(out _inv);
            var cam = GetComponentInChildren<Camera>();
            _camera = cam != null ? cam.transform : transform;

            _inventoryWindow = FindObjectOfType<InventoryWindow>();
            _craftingWindow  = FindObjectOfType<CraftingWindow>();
            _build           = GetComponent<BuildingPlacementController>();

            var tracerGo = new GameObject("[WeaponTracer]");
            tracerGo.transform.SetParent(transform, false);
            _tracer = tracerGo.AddComponent<LineRenderer>();
            _tracer.useWorldSpace = true;
            _tracer.startWidth = 0.02f;
            _tracer.endWidth = 0.005f;
            _tracer.positionCount = 0;
            _tracer.material = new Material(Shader.Find("Sprites/Default"));
            _tracer.startColor = new Color(1f, 0.9f, 0.4f, 1f);
            _tracer.endColor   = new Color(1f, 0.6f, 0.0f, 0.2f);
            _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _tracer.receiveShadows = false;
        }

        private void Update()
        {
            if (_inv == null) { ServiceLocator.TryGet(out _inv); return; }

            if (_tracerHideTime > 0f && Time.time > _tracerHideTime)
            { _tracer.positionCount = 0; _tracerHideTime = 0f; }

            if (_inventoryWindow != null && _inventoryWindow.IsOpen) return;
            if (_craftingWindow  != null && _craftingWindow.IsOpen)  return;
            if (_build != null && _build.HasGhost) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (!Input.GetMouseButton(0)) return;
            if (Time.time < _nextFireTime) return;

            var stack = _inv.GetEquippedStack();
            if (stack.IsEmpty) return;
            if (!WeaponDb.TryGet(stack.ItemId, out var stats)) return;
            if (stats.AmmoItemId != 0 && _inv.CountAll(stats.AmmoItemId) <= 0) return;

            _nextFireTime = Time.time + stats.CooldownSeconds;
            switch (stats.Kind)
            {
                case WeaponKind.Hitscan: FireHitscan(stats); break;
                case WeaponKind.Melee:   FireMelee(stats);   break;
            }
        }

        private void FireHitscan(in WeaponStats stats)
        {
            if (stats.AmmoItemId != 0)
                _inv.RemoveAll(stats.AmmoItemId, 1);

            int pellets = stats.Pellets <= 0 ? 1 : stats.Pellets;
            Vector3 origin = _camera.position;
            Vector3 lastEnd = origin + _camera.forward * stats.Range;

            for (int p = 0; p < pellets; p++)
            {
                Vector3 dir = _camera.forward;
                if (stats.Spread > 0f)
                {
                    Vector2 d = Random.insideUnitCircle * stats.Spread;
                    dir = Quaternion.AngleAxis(d.x, _camera.up) *
                          Quaternion.AngleAxis(d.y, _camera.right) *
                          dir;
                }

                Vector3 endPoint = origin + dir * stats.Range;
                if (Physics.Raycast(origin, dir, out var hit, stats.Range, _hitMask, QueryTriggerInteraction.Ignore))
                {
                    endPoint = hit.point;
                    var dmg = hit.collider.GetComponentInParent<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                        dmg.ApplyDamage(stats.Damage, hit.point, hit.normal, gameObject);
                }
                lastEnd = endPoint;
            }
            ShowTracer(origin + _camera.forward * 0.4f, lastEnd);
        }

        private void FireMelee(in WeaponStats stats)
        {
            Vector3 origin = _camera.position + _camera.forward * _meleeForwardOffset;
            if (Physics.Raycast(origin, _camera.forward, out var hit, stats.Range, _hitMask, QueryTriggerInteraction.Ignore))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                    dmg.ApplyDamage(stats.Damage, hit.point, hit.normal, gameObject);
            }
        }

        private void ShowTracer(Vector3 a, Vector3 b)
        {
            _tracer.positionCount = 2;
            _tracer.SetPosition(0, a);
            _tracer.SetPosition(1, b);
            _tracerHideTime = Time.time + 0.05f;
        }
    }
}
