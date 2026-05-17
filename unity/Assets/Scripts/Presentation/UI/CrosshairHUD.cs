// SPDX-License-Identifier: MIT
// RustLike — minimal crosshair (Phase 0.5).
//
// Two thin rectangles in the screen center. Hidden when any modal UI is open.

using UnityEngine;

namespace RustLike.Presentation.UI
{
    public sealed class CrosshairHUD : MonoBehaviour
    {
        private InventoryWindow _inv;
        private CraftingWindow _craft;

        private void Awake()
        {
            _inv   = FindObjectOfType<InventoryWindow>();
            _craft = FindObjectOfType<CraftingWindow>();
        }

        private void OnGUI()
        {
            if (_inv != null && _inv.IsOpen) return;
            if (_craft != null && _craft.IsOpen) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            const float L = 8f, T = 2f;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - L, cy - T * 0.5f, L * 2f, T), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - T * 0.5f, cy - L, T, L * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
