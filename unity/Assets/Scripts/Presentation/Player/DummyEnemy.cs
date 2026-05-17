// SPDX-License-Identifier: MIT
// RustLike — IDamageable target dummy (Phase 0.5).
//
// Simple red cube that tracks HP, flashes on hit, and despawns at 0.

using RustLike.Gameplay.Combat;
using UnityEngine;

namespace RustLike.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class DummyEnemy : MonoBehaviour, IDamageable
    {
        public float MaxHp = 100f;
        public float Hp = 100f;

        private Renderer _rend;
        private MaterialPropertyBlock _mpb;
        private float _flashUntil;

        public bool IsAlive => Hp > 0f;

        private void Awake()
        {
            _rend = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            Hp = MaxHp;
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time > _flashUntil)
            {
                _flashUntil = 0f;
                ApplyColor(BaseColor());
            }
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject attacker)
        {
            if (!IsAlive) return;
            Hp -= amount;
            ApplyColor(Color.white);
            _flashUntil = Time.time + 0.06f;
            if (Hp <= 0f) Destroy(gameObject);
        }

        private Color BaseColor() =>
            new(0.85f * Mathf.Clamp01(Hp / MaxHp + 0.2f), 0.20f, 0.20f);

        private void ApplyColor(Color c)
        {
            if (_rend == null) return;
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color",     c);
            _rend.SetPropertyBlock(_mpb);
        }
    }
}
