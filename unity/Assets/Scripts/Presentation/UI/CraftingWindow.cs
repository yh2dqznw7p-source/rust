// SPDX-License-Identifier: MIT
// RustLike — crafting window (Phase 0.5).
//
// Toggled with Q. Shows a list of all known recipes, with materials, time,
// and a "Craft" button. Locked recipes are greyed out. The active queue is
// shown at the bottom; clicking a queued job cancels it.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Crafting;
using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Presentation.UI
{
    public sealed class CraftingWindow : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private PlayerInventory _inv;
        private CraftingService _craft;
        private Vector2 _scroll;
        private GUIStyle _title, _row, _btn, _green, _red;
        private bool _stylesReady;

        private void Awake()
        {
            ServiceLocator.TryGet(out _inv);
            ServiceLocator.TryGet(out _craft);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q)) Toggle();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
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
            _row = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, alignment = TextAnchor.MiddleLeft, wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f) },
            };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            _green = new GUIStyle(_row) { normal = { textColor = new Color(0.40f, 0.95f, 0.40f) } };
            _red   = new GUIStyle(_row) { normal = { textColor = new Color(0.95f, 0.50f, 0.50f) } };
            _stylesReady = true;
        }

        private void OnGUI()
        {
            if (!IsOpen || _inv == null || _craft == null) return;
            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;

            float winW = 520f;
            float winH = Mathf.Min(560f, Screen.height - 80f);
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            GUI.color = new Color(0.10f, 0.10f, 0.10f, 0.95f);
            GUI.Box(new Rect(winX, winY, winW, winH), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(winX, winY + 8f, winW, 28f), "Crafting (Q to close)", _title);

            // Active queue
            float queueY = winY + 44f;
            DrawQueue(winX + 12f, queueY, winW - 24f);

            // Recipe list
            float listY = queueY + 80f;
            float listH = winH - (listY - winY) - 16f;
            DrawRecipeList(winX + 12f, listY, winW - 24f, listH);
        }

        private void DrawQueue(float x, float y, float w)
        {
            GUI.Label(new Rect(x, y, w, 18), "Queue:", _row);
            y += 20f;
            if (_craft.Queue.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, 18), "  (empty)", _row);
                return;
            }
            for (int i = 0; i < _craft.Queue.Count && i < 3; i++)
            {
                var job = _craft.Queue[i];
                var def = _inv.Registry.GetById(job.Recipe.OutputItemId);
                string title = (def != null ? def.DisplayName : "?") + "  ×" + job.Count;
                if (i == 0)
                    title += "   " + Mathf.RoundToInt(job.Progress01 * 100f) + "%";
                GUI.Label(new Rect(x, y, w - 70f, 18), title, _row);
                if (GUI.Button(new Rect(x + w - 64f, y - 2f, 60f, 22f), "Cancel", _btn))
                    _craft.Cancel(i);
                y += 20f;
            }
        }

        private void DrawRecipeList(float x, float y, float w, float h)
        {
            float rowH = 56f;
            int n = CraftingRecipes.All.Count;
            var viewRect = new Rect(0, 0, w - 18f, n * (rowH + 4f));
            _scroll = GUI.BeginScrollView(new Rect(x, y, w, h), _scroll, viewRect);

            for (int i = 0; i < n; i++)
            {
                var r = CraftingRecipes.All[i];
                DrawRecipeRow(0, i * (rowH + 4f), w - 18f, rowH, r);
            }
            GUI.EndScrollView();
        }

        private void DrawRecipeRow(float x, float y, float w, float h, Recipe r)
        {
            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = Color.white;

            var def = _inv.Registry.GetById(r.OutputItemId);
            string name = def != null ? def.DisplayName : ("Item " + r.OutputItemId);
            if (r.OutputAmount > 1) name += " ×" + r.OutputAmount;
            GUI.Label(new Rect(x + 8, y + 4, w - 100f, 18), name, _row);

            // ingredients line
            string ing = "";
            for (int i = 0; i < r.Inputs.Length; i++)
            {
                var ig = r.Inputs[i];
                var idef = _inv.Registry.GetById(ig.ItemId);
                int have = _inv.CountAll(ig.ItemId);
                string n2 = idef != null ? idef.DisplayName : ("#" + ig.ItemId);
                string color = have >= ig.Amount ? "#7af07a" : "#f08080";
                ing += "<color=" + color + ">" + n2 + " " + have + "/" + ig.Amount + "</color>";
                if (i < r.Inputs.Length - 1) ing += "  ·  ";
            }
            var rich = new GUIStyle(_row) { richText = true, fontSize = 11 };
            GUI.Label(new Rect(x + 8, y + 22, w - 110f, 32), ing, rich);

            if (r.WorkbenchTier > 0)
                GUI.Label(new Rect(x + 8, y + h - 16, 200, 14),
                    "(workbench T" + r.WorkbenchTier + ")", _row);

            // craft button
            bool learned = _craft.IsLearned(r);
            bool ok = _craft.CanCraft(r, out _);
            if (!learned)
            {
                GUI.Label(new Rect(x + w - 96f, y + h * 0.5f - 9f, 90f, 18f), "🔒 Locked", _red);
            }
            else
            {
                GUI.enabled = ok;
                if (GUI.Button(new Rect(x + w - 96f, y + 8, 88, 22), "Craft", _btn))
                    _craft.TryEnqueue(r, 1);
                if (GUI.Button(new Rect(x + w - 96f, y + 32, 88, 20), "Craft x5", _btn))
                    _craft.TryEnqueue(r, 5);
                GUI.enabled = true;
            }
        }
    }
}
