// SPDX-License-Identifier: MIT
// RustLike — harvestable resource node (tree, stone, ore).
//
// Implements IDamageable so the player's tools/weapons hit it via the same
// raycast pipeline. On hit:
//   - emit `yieldPerHit` of resource into the player inventory,
//   - flash white briefly,
//   - if HP <= 0, despawn (deferred) — server respawns later (Phase 1+).
//
// "Crystals" / glowing dot for ore nodes — implemented as a tiny child
// GameObject with an emissive material + Light component so they read as
// "loot point" at distance.

using RustLike.Core.Bootstrap;
using RustLike.Core.Logging;
using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Gameplay.Loot
{
    public enum ResourceKind : byte { Tree, Stone, Metal, Sulfur, HQM }

    [DisallowMultipleComponent]
    public sealed class ResourceNode : MonoBehaviour, RustLike.Gameplay.Combat.IDamageable
    {
        public ResourceKind Kind = ResourceKind.Tree;
        public float Health = 200f;
        public ushort YieldPerHit = 8;
        public ushort YieldItemId; // resolved in Awake from Kind if zero

        // Visual flash on hit
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _flashUntil;
        private Color _baseColor;

        public bool IsAlive => Health > 0f;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: false);
            _mpb = new MaterialPropertyBlock();
            _baseColor = ColorForKind(Kind);
            ApplyColor(_baseColor);

            if (YieldItemId == 0) YieldItemId = ItemIdForKind(Kind);

            // Glowing crystal for valuable ores
            if (Kind == ResourceKind.Metal || Kind == ResourceKind.Sulfur || Kind == ResourceKind.HQM)
                AddCrystal();
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time > _flashUntil)
            {
                _flashUntil = 0f;
                ApplyColor(_baseColor);
            }
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject attacker)
        {
            if (!IsAlive) return;
            Health -= amount;
            ApplyColor(Color.white);
            _flashUntil = Time.time + 0.07f;

            if (ServiceLocator.TryGet(out PlayerInventory inv) && YieldItemId != 0)
                inv.PickUp(YieldItemId, YieldPerHit);

            if (Health <= 0f)
            {
                Log.Trace(LogCat.Loot, "ResourceNode depleted: " + Kind);
                Destroy(gameObject);
            }
        }

        // ---- helpers ----

        private void ApplyColor(Color c)
        {
            if (_renderers == null) return;
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color",     c);
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].SetPropertyBlock(_mpb);
        }

        private void AddCrystal()
        {
            var crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "[Crystal]";
            var col = crystal.GetComponent<Collider>();
            if (col != null) Destroy(col); // crystal is purely visual
            crystal.transform.SetParent(transform, false);
            crystal.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            crystal.transform.localScale = Vector3.one * 0.30f;

            var rend = crystal.GetComponent<Renderer>();
            var mpb = new MaterialPropertyBlock();
            Color glow = Kind switch
            {
                ResourceKind.Metal  => new Color(0.95f, 0.85f, 0.30f),
                ResourceKind.Sulfur => new Color(0.95f, 0.95f, 0.20f),
                ResourceKind.HQM    => new Color(0.20f, 0.95f, 0.95f),
                _ => Color.white,
            };
            mpb.SetColor("_BaseColor", glow);
            mpb.SetColor("_Color",     glow);
            mpb.SetColor("_EmissionColor", glow * 2f);
            rend.SetPropertyBlock(mpb);

            // Point light beacon
            var lightGo = new GameObject("[CrystalLight]");
            lightGo.transform.SetParent(crystal.transform, false);
            var li = lightGo.AddComponent<Light>();
            li.type = LightType.Point;
            li.color = glow;
            li.intensity = 2.5f;
            li.range = 6f;
            li.shadows = LightShadows.None;
        }

        private static Color ColorForKind(ResourceKind k) => k switch
        {
            ResourceKind.Tree   => new Color(0.30f, 0.50f, 0.20f),
            ResourceKind.Stone  => new Color(0.55f, 0.55f, 0.55f),
            ResourceKind.Metal  => new Color(0.55f, 0.50f, 0.40f),
            ResourceKind.Sulfur => new Color(0.65f, 0.55f, 0.20f),
            ResourceKind.HQM    => new Color(0.40f, 0.55f, 0.65f),
            _ => Color.gray,
        };

        private static ushort ItemIdForKind(ResourceKind k) => k switch
        {
            ResourceKind.Tree   => ItemIds.Wood,
            ResourceKind.Stone  => ItemIds.Stone,
            ResourceKind.Metal  => ItemIds.Metal,
            ResourceKind.Sulfur => ItemIds.Sulfur,
            ResourceKind.HQM    => ItemIds.HQMetal,
            _ => 0,
        };
    }
}
