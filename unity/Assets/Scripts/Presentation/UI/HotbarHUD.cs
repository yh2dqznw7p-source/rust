// SPDX-License-Identifier: MIT
// RustLike — bottom-of-screen hotbar (Phase 0.5).
//
// 6 slots, ~64 px each, centered horizontally above the vitals bars.
// Number 1..6 keys (or scroll wheel) select. We use IMGUI.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Presentation.UI
{
    public sealed class HotbarHUD : MonoBehaviour
    {
        private PlayerInventory _inv;
        private GUIStyle _slotLabel, _slotCount;
        private bool _stylesReady;

        private const float SlotSize = 64f;
        private const float SlotSpacing = 4f;

        private void Awake() => ServiceLocator.TryGet(out _inv);

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _slotLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.85f) },
            };
            _slotCount = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight,
                normal = { textColor = Color.white },
            };
            _stylesReady = true;
        }

        private void Update()
        {
            if (_inv == null) { ServiceLocator.TryGet(out _inv); return; }
            for (int i = 0; i < PlayerInventory.HotbarCapacity; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) _inv.SelectHotbarSlot(i);

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && Cursor.lockState == CursorLockMode.Locked)
            {
                int cur = _inv.SelectedHotbarSlot < 0 ? 0 : _inv.SelectedHotbarSlot;
                int dir = scroll > 0 ? -1 : 1;
                int next = (cur + dir + PlayerInventory.HotbarCapacity) % PlayerInventory.HotbarCapacity;
                _inv.SelectHotbarSlot(next);
            }
        }

        private void OnGUI()
        {
            if (_inv == null) return;
            EnsureStyles();

            int n = PlayerInventory.HotbarCapacity;
            float totalW = n * SlotSize + (n - 1) * SlotSpacing;
            float startX = (Screen.width - totalW) * 0.5f;
            float y = Screen.height - 22f - SlotSize;

            for (int i = 0; i < n; i++)
            {
                var rect = new Rect(startX + i * (SlotSize + SlotSpacing), y, SlotSize, SlotSize);
                DrawSlot(rect, i, _inv.Hotbar[i], i == _inv.SelectedHotbarSlot);
            }
        }

        private void DrawSlot(Rect r, int idx, ItemStack stack, bool active)
        {
            var prev = GUI.color;
            GUI.color = active
                ? new Color(0.10f, 0.10f, 0.10f, 0.85f)
                : new Color(0.05f, 0.05f, 0.05f, 0.65f);
            GUI.Box(r, GUIContent.none);

            if (active)
            {
                GUI.color = new Color(1f, 0.85f, 0.20f, 1f);
                DrawBorder(r, 2f);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 4, r.y + 2, 16, 16), (idx + 1).ToString(), _slotLabel);

            if (!stack.IsEmpty)
            {
                var def = _inv.Registry.GetById(stack.ItemId);
                if (def != null && def.Icon != null && def.Icon.texture != null)
                {
                    var inner = new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16);
                    GUI.DrawTexture(inner, def.Icon.texture, ScaleMode.ScaleToFit);
                }
                if (stack.Amount > 1)
                    GUI.Label(new Rect(r.x, r.y, r.width - 4, r.height - 2),
                              "x" + stack.Amount, _slotCount);
            }
            GUI.color = prev;
        }

        private static void DrawBorder(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }
    }
}
