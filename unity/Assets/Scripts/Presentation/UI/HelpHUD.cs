// SPDX-License-Identifier: MIT
// RustLike — top-left controls hint (Phase 0.5).

using UnityEngine;

namespace RustLike.Presentation.UI
{
    public sealed class HelpHUD : MonoBehaviour
    {
        private GUIStyle _label;
        private bool _ready;

        private void OnGUI()
        {
            if (!_ready)
            {
                _label = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.9f) },
                };
                _ready = true;
            }
            const float W = 380f, H = 124f;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(new Rect(8, 8, W, H), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(16, 12, W, H),
                "WASD move · Shift sprint · Space jump · Ctrl crouch\n" +
                "Mouse look · Esc release cursor\n" +
                "1..6 / Wheel select hotbar slot\n" +
                "LMB swing/shoot · E interact · R rotate ghost\n" +
                "Tab inventory · Q crafting", _label);
        }
    }
}
