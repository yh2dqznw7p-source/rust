// SPDX-License-Identifier: MIT
// RustLike — Tab-toggled inventory window with drag&drop (Phase 0.5).
//
// Layout (centered, opens on Tab):
//   ┌───────────── Inventory ────────────┐
//   │  [Equipment 4]                      │
//   │  ─────────────                      │
//   │  [Main grid 6x4 = 24]               │
//   │  ─────────────                      │
//   │  [Hotbar 6]                         │
//   └─────────────────────────────────────┘
//
// Drag&drop:
//   - Mouse down on an occupied slot starts a drag, remembering (container, slot).
//   - Mouse up over another slot drops it. Same container -> Container.MoveSlot;
//     across containers we swap or merge.
//
// Right-click on a slot splits the stack in half into the first empty slot
// of the same container.
//
// Optional `ExternalContainer` — set by InteractController when player
// opens a storage box; UI shows it on the right side.

using System;
using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Presentation.UI
{
    public sealed class InventoryWindow : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private PlayerInventory _inv;

        // External container (storage chest currently opened). Null = none.
        public Container ExternalContainer { get; private set; }
        public string ExternalTitle { get; private set; }

        private Container _dragContainer;
        private int _dragSlot = -1;
        private Container _hoverContainer;
        private int _hoverSlot = -1;

        private GUIStyle _title, _slotLabel, _stackCount, _tooltip;
        private bool _stylesReady;

        private const float SlotSize = 56f;
        private const float SlotGap = 4f;
        private const int MainCols = 6;
        private const int MainRows = 4;

        public event Action OpenedChanged;

        private void Awake() => ServiceLocator.TryGet(out _inv);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) Toggle();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                if (ExternalContainer != null) CloseExternal();
                else Toggle();
            }
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
            if (!IsOpen) { CancelDrag(); ExternalContainer = null; }
            ApplyCursor();
            OpenedChanged?.Invoke();
        }

        public void OpenWithExternal(Container external, string title)
        {
            ExternalContainer = external;
            ExternalTitle = title;
            if (!IsOpen) Toggle();
            else { ApplyCursor(); OpenedChanged?.Invoke(); }
        }

        public void CloseExternal()
        {
            ExternalContainer = null;
            if (IsOpen) Toggle();
        }

        private void ApplyCursor()
        {
            Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsOpen;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            _slotLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) },
            };
            _stackCount = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight,
                normal = { textColor = Color.white },
            };
            _tooltip = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12, alignment = TextAnchor.MiddleLeft, wordWrap = true,
                normal = { textColor = Color.white },
            };
            _stylesReady = true;
        }

        private void OnGUI()
        {
            if (!IsOpen || _inv == null) return;
            EnsureStyles();
            _hoverContainer = null; _hoverSlot = -1;

            // Backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;

            // Player window (left)
            float winW = MainCols * SlotSize + (MainCols - 1) * SlotGap + 24f;
            float winH = 60f
                + SlotSize + SlotGap
                + 16f
                + MainRows * SlotSize + (MainRows - 1) * SlotGap
                + 16f
                + SlotSize
                + 24f;
            float winX = (Screen.width - winW) * 0.5f - (ExternalContainer != null ? winW * 0.55f : 0f);
            float winY = (Screen.height - winH) * 0.5f;

            DrawPlayerWindow(winX, winY, winW, winH);

            // External window (right)
            if (ExternalContainer != null)
            {
                float ewinX = winX + winW + 24f;
                DrawExternalWindow(ewinX, winY, winW, winH);
            }

            DrawTooltip();
            DrawDragGhost();

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 &&
                _dragSlot >= 0 && (_hoverContainer == null || _hoverSlot < 0))
            {
                CancelDrag();
            }
        }

        private void DrawPlayerWindow(float winX, float winY, float winW, float winH)
        {
            GUI.color = new Color(0.10f, 0.10f, 0.10f, 0.95f);
            GUI.Box(new Rect(winX, winY, winW, winH), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(winX, winY + 8f, winW, 28f), "Inventory", _title);

            float y = winY + 44f;
            float equipW = PlayerInventory.EquipmentCapacity * SlotSize +
                           (PlayerInventory.EquipmentCapacity - 1) * SlotGap;
            float equipX = winX + (winW - equipW) * 0.5f;
            DrawRow(_inv.Equipment, equipX, y, PlayerInventory.EquipmentCapacity);
            y += SlotSize + 16f;
            DrawGrid(_inv.Main, winX + 12f, y, MainCols, MainRows);
            y += MainRows * SlotSize + (MainRows - 1) * SlotGap + 16f;
            DrawRow(_inv.Hotbar, winX + 12f, y, PlayerInventory.HotbarCapacity);
        }

        private void DrawExternalWindow(float winX, float winY, float winW, float winH)
        {
            GUI.color = new Color(0.10f, 0.12f, 0.10f, 0.95f);
            GUI.Box(new Rect(winX, winY, winW, winH), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(winX, winY + 8f, winW, 28f), ExternalTitle ?? "Storage", _title);

            int cols = MainCols;
            int rows = Mathf.CeilToInt(ExternalContainer.Capacity / (float)cols);
            DrawGrid(ExternalContainer, winX + 12f, winY + 44f, cols, rows);
        }

        private void DrawRow(Container c, float x, float y, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var rect = new Rect(x + i * (SlotSize + SlotGap), y, SlotSize, SlotSize);
                DrawSlot(c, i, rect);
            }
        }

        private void DrawGrid(Container c, float x, float y, int cols, int rows)
        {
            int idx = 0;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                if (idx >= c.Capacity) return;
                var rect = new Rect(x + col * (SlotSize + SlotGap),
                                    y + row * (SlotSize + SlotGap),
                                    SlotSize, SlotSize);
                DrawSlot(c, idx, rect);
                idx++;
            }
        }

        private void DrawSlot(Container c, int slotIdx, Rect rect)
        {
            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            var stack = c[slotIdx];
            bool mouseOver = rect.Contains(Event.current.mousePosition);
            if (mouseOver) { _hoverContainer = c; _hoverSlot = slotIdx; }

            bool isSource = (_dragContainer == c && _dragSlot == slotIdx);
            if (!stack.IsEmpty && !isSource)
            {
                var def = _inv.Registry.GetById(stack.ItemId);
                if (def != null && def.Icon != null && def.Icon.texture != null)
                {
                    var inner = new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12);
                    GUI.DrawTexture(inner, def.Icon.texture, ScaleMode.ScaleToFit);
                }
                if (stack.Amount > 1)
                    GUI.Label(new Rect(rect.x, rect.y, rect.width - 4, rect.height - 2),
                              "x" + stack.Amount, _stackCount);
            }

            if (mouseOver)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.20f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = Color.white;
            }

            var e = Event.current;
            if (mouseOver)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && !stack.IsEmpty && _dragSlot < 0)
                {
                    _dragContainer = c; _dragSlot = slotIdx; e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0 && _dragSlot >= 0)
                {
                    DropOnSlot(c, slotIdx); e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 1 && !stack.IsEmpty)
                {
                    SplitInHalf(c, slotIdx); e.Use();
                }
            }
        }

        private void DrawTooltip()
        {
            if (_dragSlot >= 0) return;
            if (_hoverContainer == null || _hoverSlot < 0) return;
            var stack = _hoverContainer[_hoverSlot];
            if (stack.IsEmpty) return;
            var def = _inv.Registry.GetById(stack.ItemId);
            if (def == null) return;

            string text = def.DisplayName;
            if ((def.Flags & ItemFlags.HasDurability) != 0 && def.MaxDurability > 0)
                text += "\nDurability " + stack.Durability + "/" + def.MaxDurability;
            else if (def.IsStackable)
                text += "\nStack " + stack.Amount + "/" + def.MaxStack;

            Vector2 size = _tooltip.CalcSize(new GUIContent(text));
            float w = Mathf.Min(220f, Mathf.Max(size.x + 16f, 120f));
            float h = size.y + 12f;
            float x = Mathf.Min(Event.current.mousePosition.x + 16f, Screen.width - w - 4f);
            float y = Mathf.Min(Event.current.mousePosition.y + 16f, Screen.height - h - 4f);
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 8f, y + 4f, w - 16f, h - 8f), text, _tooltip);
        }

        private void DrawDragGhost()
        {
            if (_dragSlot < 0 || _dragContainer == null) return;
            var stack = _dragContainer[_dragSlot];
            if (stack.IsEmpty) { CancelDrag(); return; }
            var def = _inv.Registry.GetById(stack.ItemId);
            if (def == null || def.Icon == null || def.Icon.texture == null) return;

            float s = SlotSize - 12f;
            var p = Event.current.mousePosition;
            var rect = new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s);
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(rect, def.Icon.texture, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
            if (stack.Amount > 1)
                GUI.Label(new Rect(rect.x - 6, rect.y - 4, rect.width + 12, rect.height + 8),
                          "x" + stack.Amount, _stackCount);
        }

        private void DropOnSlot(Container dst, int dstSlot)
        {
            var src = _dragContainer;
            int srcSlot = _dragSlot;
            CancelDrag();
            if (src == null) return;

            if (src == dst) src.MoveSlot(srcSlot, dstSlot);
            else            CrossContainerSwap(src, srcSlot, dst, dstSlot);

            _inv.NotifySlotChanged(src, srcSlot);
            _inv.NotifySlotChanged(dst, dstSlot);
        }

        private void CrossContainerSwap(Container src, int srcSlot, Container dst, int dstSlot)
        {
            var a = src[srcSlot];
            var b = dst[dstSlot];
            if (b.IsEmpty)
            {
                ushort placed = dst.TryAdd(a.ItemId, a.Amount);
                ushort consumed = (ushort)(a.Amount - placed);
                if (consumed > 0) src.TryRemove(a.ItemId, consumed);
                return;
            }
            if (a.ItemId == b.ItemId)
            {
                var def = _inv.Registry.GetById(a.ItemId);
                if (def != null && def.IsStackable)
                {
                    int free = def.MaxStack - b.Amount;
                    if (free > 0)
                    {
                        int take = Mathf.Min(free, a.Amount);
                        ushort leftover = dst.TryAdd(a.ItemId, (ushort)take);
                        ushort moved = (ushort)(take - leftover);
                        if (moved > 0) src.TryRemove(a.ItemId, moved);
                        return;
                    }
                }
            }
            // Plain swap (loses durability/instance — acceptable for demo)
            src.TryRemove(a.ItemId, a.Amount);
            dst.TryRemove(b.ItemId, b.Amount);
            src.TryAdd(b.ItemId, b.Amount);
            dst.TryAdd(a.ItemId, a.Amount);
        }

        private void SplitInHalf(Container c, int slot)
        {
            var s = c[slot];
            if (s.IsEmpty || s.Amount < 2) return;
            ushort half = (ushort)(s.Amount / 2);
            for (int i = 0; i < c.Capacity; i++)
            {
                if (i == slot) continue;
                if (c[i].IsEmpty) { c.SplitSlot(slot, i, half); return; }
            }
        }

        private void CancelDrag()
        {
            _dragContainer = null;
            _dragSlot = -1;
        }
    }
}
