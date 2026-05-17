// SPDX-License-Identifier: MIT
// RustLike — minimal IMGUI HUD for the Phase 0 playable demo.
//
// Why IMGUI and not uGUI/UI Toolkit?
//   - No Canvas, EventSystem, or sprite atlas to author yet.
//   - Zero scene/asset setup; works in any opened scene.
//   - Replaced in Phase 3 by a real uGUI HUD with pooling.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Survival;
using UnityEngine;
using AppBootstrap = RustLike.Core.Bootstrap.Bootstrap;

namespace RustLike.Presentation.UI
{
    public sealed class SurvivalHUD : MonoBehaviour
    {
        public int EntityId = 1;

        private VitalsService _vitals;
        private GUIStyle _label;
        private GUIStyle _bg;

        private void Awake()
        {
            ServiceLocator.TryGet(out _vitals);
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal   = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft,
            };
            _bg = new GUIStyle(GUI.skin.box);
        }

        private void OnGUI()
        {
            EnsureStyles();

            // top-left: controls hint
            GUI.Box(new Rect(10, 10, 280, 26), "WASD move · Shift sprint · Space jump · Esc cursor", _label);

            // bottom-left: vitals bars
            if (_vitals != null && _vitals.TryGet(EntityId, out var v))
            {
                float x = 12f;
                float y = Screen.height - 110f;
                Bar(x, y +  0, "HP",     v.Health, 100f, new Color(0.85f, 0.20f, 0.20f));
                Bar(x, y + 26, "Hunger", v.Hunger, 500f, new Color(0.85f, 0.55f, 0.10f));
                Bar(x, y + 52, "Thirst", v.Thirst, 250f, new Color(0.20f, 0.55f, 0.90f));
                Bar(x, y + 78, "Temp",   v.Temperature, 50f, new Color(0.7f, 0.7f, 0.7f));

                if (!v.IsAlive)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
                    GUI.color = prev;
                    var dead = new GUIStyle(_label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(0, Screen.height/2 - 30, Screen.width, 60), "YOU DIED", dead);
                }
            }

            // top-right: build / mode tag
            GUI.Box(new Rect(Screen.width - 220, 10, 210, 26),
                "RustLike phase0  ·  mode=" + AppBootstrap.Mode, _label);
        }

        private void Bar(float x, float y, string title, float value, float max, Color color)
        {
            const float W = 220f, H = 22f;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(new Rect(x, y, W, H), GUIContent.none);
            GUI.color = color;
            float t = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            GUI.Box(new Rect(x + 2, y + 2, (W - 4) * t, H - 4), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 6, y + 2, W, H), title + "  " + Mathf.RoundToInt(value), _label);
        }
    }
}
