// SPDX-License-Identifier: MIT
// RustLike — "look at thing, press E" interaction.
//
// On every frame we shoot a short ray from the camera. If it hits something
// interactable (StorageContainer / ResearchTable / etc.) we show a hint and
// listen for E. Hold-E researches an item from the equipped slot.

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Crafting;
using RustLike.Gameplay.Inventory;
using RustLike.Gameplay.Loot;
using RustLike.Presentation.UI;
using UnityEngine;

namespace RustLike.Presentation.Player
{
    public sealed class InteractController : MonoBehaviour
    {
        [SerializeField] private float _range = 3.5f;

        private Transform _camera;
        private PlayerInventory _inv;
        private CraftingService _craft;
        private InventoryWindow _inventoryWindow;
        private CraftingWindow _craftingWindow;

        // Latest hit cached for HUD prompt and OnGUI
        private Collider _lastHit;
        private string _prompt;
        private float _holdResearchUntil;

        private GUIStyle _promptStyle;
        private bool _stylesReady;

        public string CurrentPrompt => _prompt;

        private void Awake()
        {
            ServiceLocator.TryGet(out _inv);
            ServiceLocator.TryGet(out _craft);
            var cam = GetComponentInChildren<Camera>();
            _camera = cam != null ? cam.transform : transform;
            _inventoryWindow = FindObjectOfType<InventoryWindow>();
            _craftingWindow  = FindObjectOfType<CraftingWindow>();
        }

        private void Update()
        {
            _prompt = null;
            _lastHit = null;

            if (_inventoryWindow != null && _inventoryWindow.IsOpen) return;
            if (_craftingWindow  != null && _craftingWindow.IsOpen)  return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector3 origin = _camera.position;
            Vector3 dir = _camera.forward;
            if (!Physics.Raycast(origin, dir, out var hit, _range, ~0, QueryTriggerInteraction.Ignore))
                return;

            _lastHit = hit.collider;

            // Storage container?
            var sc = hit.collider.GetComponentInParent<StorageContainer>();
            if (sc != null)
            {
                _prompt = "[E] Open " + sc.DisplayName;
                if (Input.GetKeyDown(KeyCode.E) && _inventoryWindow != null && sc.Container != null)
                    _inventoryWindow.OpenWithExternal(sc.Container, sc.DisplayName);
                return;
            }

            // Research table — hold E to research equipped item
            var rt = hit.collider.GetComponentInParent<ResearchTable>();
            if (rt != null && _inv != null && _craft != null)
            {
                var eq = _inv.GetEquippedDef();
                if (eq != null && (eq.Flags & ItemFlags.Researchable) != 0)
                {
                    if (Input.GetKey(KeyCode.E))
                    {
                        if (_holdResearchUntil <= 0f) _holdResearchUntil = Time.time + 1.5f;
                        float left = Mathf.Max(0f, _holdResearchUntil - Time.time);
                        _prompt = "Researching " + eq.DisplayName + "  " + left.ToString("0.0") + "s";
                        if (Time.time >= _holdResearchUntil)
                        {
                            if (_craft.ResearchItem(eq.ItemId)) _prompt = "Learned: " + eq.DisplayName;
                            _holdResearchUntil = 0f;
                        }
                    }
                    else _holdResearchUntil = 0f;
                }
                else
                {
                    _prompt = "[Hold E with researchable item] Research Table";
                }
                return;
            }

            // ResourceNode (ore) — quick visual hint
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                _prompt = "Hit with tool to harvest " + node.Kind;
                return;
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_prompt)) return;
            if (!_stylesReady)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                };
                _stylesReady = true;
            }
            float w = 380f, h = 28f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.62f;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, h), _prompt, _promptStyle);
        }
    }

    /// <summary> Marker component on a research-table prop. </summary>
    public sealed class ResearchTable : MonoBehaviour { }
}
