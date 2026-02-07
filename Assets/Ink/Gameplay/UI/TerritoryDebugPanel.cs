using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Lightweight runtime panel to visualize district C/P/H and fire debug actions.
    /// </summary>
    public class TerritoryDebugPanel : MonoBehaviour
    {
        public Key toggleKey = Key.L;
        public float autoRefreshSeconds = 0.5f;

        private GameObject _root;
        private RectTransform _panelRect;
        private float _refreshTimer;
        private Font _monoFont;
        private Text _infoText;
        private Transform _listContent;

        private void Start()
        {
            Debug.Log("[TerritoryDebugPanel] Start - building UI");
            BuildUI();
            Refresh();
            _root.SetActive(true);
            _refreshTimer = autoRefreshSeconds;
            Debug.Log($"[TerritoryDebugPanel] Initialized with toggleKey={toggleKey}");
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[toggleKey].wasPressedThisFrame)
            {
                Debug.Log("[TerritoryDebugPanel] Toggle key pressed");
                _root.SetActive(!_root.activeSelf);
                if (_root.activeSelf) Refresh();
            }
            else if (kb == null)
            {
                Debug.LogWarning("[TerritoryDebugPanel] Keyboard.current is null (Input System inactive?)");
            }

            if (_root != null && _root.activeSelf)
            {
                _refreshTimer -= Time.deltaTime;
                if (_refreshTimer <= 0f)
                {
                    _refreshTimer = autoRefreshSeconds;
                    Refresh();
                }
            }
        }

        private void BuildUI()
        {
            _monoFont = GetMonoFont();

            var canvasGO = new GameObject("TerritoryDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _root = new GameObject("Panel", typeof(Image));
            _root.transform.SetParent(canvasGO.transform, false);
            var img = _root.GetComponent<Image>();
            img.color = new Color(0.06f, 0.08f, 0.10f, 0.9f);

            _panelRect = _root.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(0f, 1f);
            _panelRect.anchorMax = new Vector2(0f, 1f);
            _panelRect.pivot = new Vector2(0f, 1f);
            _panelRect.anchoredPosition = new Vector2(12f, -12f);
            _panelRect.sizeDelta = new Vector2(960f, 560f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGO = new GameObject("Title", typeof(Text));
            titleGO.transform.SetParent(_root.transform, false);
            var title = titleGO.GetComponent<Text>();
            title.font = _monoFont;
            title.fontSize = 22;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleLeft;
            title.text = "Territory Monitor — C: Control  P: Patrol  H: Heat   [F8 to toggle]";

            // Buttons container
            var buttonsGO = new GameObject("Buttons", typeof(HorizontalLayoutGroup));
            buttonsGO.transform.SetParent(_root.transform, false);
            var hLayout = buttonsGO.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childAlignment = TextAnchor.MiddleLeft;

            AddButton(buttonsGO.transform, "Next Day", () =>
            {
                DistrictControlService.Instance?.AdvanceDay();
                Refresh();
            });

            AddButton(buttonsGO.transform, "Apply Edit (cursor district)", () =>
            {
                var svc = DistrictControlService.Instance;
                var cursor = UnityEngine.Object.FindFirstObjectByType<TileCursor>();
                if (svc != null && cursor != null)
                {
                    var state = svc.GetStateByPosition(cursor.gridX, cursor.gridY);
                    if (state != null)
                    {
                        svc.ApplyPalimpsestEdit(state.Id, 1f);
                        Refresh();
                    }
                }
            });

            AddButton(buttonsGO.transform, "Cleanup (cursor district)", () =>
            {
                var svc = DistrictControlService.Instance;
                var cursor = UnityEngine.Object.FindFirstObjectByType<TileCursor>();
                if (svc != null && cursor != null)
                {
                    var state = svc.GetStateByPosition(cursor.gridX, cursor.gridY);
                    if (state != null)
                    {
                        svc.ApplyCleanup(state.Id, 1f);
                        Refresh();
                    }
                }
            });

            // Info label
            var infoGO = new GameObject("Info", typeof(Text));
            infoGO.transform.SetParent(_root.transform, false);
            _infoText = infoGO.GetComponent<Text>();
            _infoText.font = _monoFont;
            _infoText.fontSize = 16;
            _infoText.color = Color.white;
            _infoText.alignment = TextAnchor.MiddleLeft;

            // Scrollable list area
            var scrollGO = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect), typeof(Mask));
            scrollGO.transform.SetParent(_root.transform, false);
            var scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.35f);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.sizeDelta = new Vector2(0, 340f);

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _listContent = content.transform;
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.offsetMin = new Vector2(10, 0);
            contentRT.offsetMax = new Vector2(-10, 0);

            var vLayout = content.GetComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var layoutElem = scrollGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 340f;

            // Scroll behaviour
            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        private Font GetMonoFont()
        {
            string[] candidates = { "Menlo", "Consolas", "Courier New", "Lucida Console" };
            foreach (var name in candidates)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 14);
                    if (f != null) return f;
                }
                catch { }
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width = 80f, float height = 30f)
        {
            var btnGO = new GameObject(label, typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var img = btnGO.GetComponent<Image>();
            img.color = new Color(0.25f, 0.85f, 0.45f, 0.95f); // bright green for visibility
            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGO = new GameObject("Text", typeof(Text));
            txtGO.transform.SetParent(btnGO.transform, false);
            var t = txtGO.GetComponent<Text>();
            t.text = label;
            t.font = _monoFont;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.black;

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var le = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
        }

        private void Refresh()
        {
            var svc = DistrictControlService.Instance;
            if (svc == null || _listContent == null || _infoText == null) return;

            _infoText.text = $"Day {svc.CurrentDay} — Control / Patrol / Heat. Use ▲/▼ to tweak Patrol (affects Control on next day).";

            // clear list
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            var factions = svc.Factions;
            if (svc.States == null || svc.States.Count == 0)
            {
                AddLabel(_listContent, "No districts loaded (check Resources/Districts and script GUIDs).", 16, FontStyle.Normal);
            }
            else
            {
                int rowIndex = 0;
                foreach (var state in svc.States)
                {
                    AddDistrictBlock(state, factions, rowIndex++ % 2 == 0);
                }
            }

            var cursor = UnityEngine.Object.FindFirstObjectByType<TileCursor>();
            if (cursor != null)
            {
                var state = svc.GetStateByPosition(cursor.gridX, cursor.gridY);
                AddLabel(_listContent, $"Cursor: ({cursor.gridX},{cursor.gridY}) — {(state != null ? state.Definition.displayName : "No district")}", 15, FontStyle.Italic);
            }
        }

        private void RefreshAndForce()
        {
            Refresh();
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Build a one-line price summary for a district and item id (debug only).
        /// </summary>
        public static string BuildPriceLine(DistrictState state, string itemId, MerchantProfile profile = null)
        {
            if (state == null) return "No district";
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MerchantProfile>();
                profile.buyMultiplier = 1f;
                profile.sellMultiplier = 1f;
            }
            var center = new Vector2Int(state.Definition.minX, state.Definition.minY);
            var bd = EconomicPriceResolver.GetBuyBreakdown(itemId, profile, center);
            var text = EconomicPriceResolver.FormatBreakdown(bd);
            return $"{state.Definition.displayName}: {text}";
        }

        /// <summary>
        /// Build economy modifier summary (tax/supply/prosperity) for a district and item.
        /// </summary>
        public static string BuildEconomyLine(DistrictState state, string itemId)
        {
            if (state == null) return "No district";
            float tax = TaxRegistry.GetTax(state.Id, null, itemId);
            float supply = SupplyService.GetSupplyByDistrict(state.Id, itemId);
            float pros = state.prosperity;
            return $"Tax:{tax*100:+0;-0;0}%  Supply:{supply:0.00}x  Pros:{pros:0.00}x";
        }

        private void AddDistrictBlock(DistrictState state, IReadOnlyList<FactionDefinition> factions, bool evenRow)
        {
            var block = new GameObject("District_" + state.Definition.displayName, typeof(Image), typeof(VerticalLayoutGroup));
            block.transform.SetParent(_listContent, false);
            var img = block.GetComponent<Image>();
            img.color = evenRow ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.02f);

            var layout = block.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            AddLabel(block.transform, state.Definition.displayName, 18, FontStyle.Bold);
            // Price line for debug item (potion)
            AddLabel(block.transform, BuildPriceLine(state, "potion"), 14, FontStyle.Italic);
            AddLabel(block.transform, BuildEconomyLine(state, "potion"), 13, FontStyle.Italic);

            for (int f = 0; f < factions.Count; f++)
            {
                AddFactionRow(block.transform, state, factions[f], f);
            }
        }

        private void AddFactionRow(Transform parent, DistrictState state, FactionDefinition faction, int factionIndex)
        {
            var row = new GameObject($"Faction_{faction.displayName}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childForceExpandHeight = false;
            h.childControlWidth = true;

            AddLabel(row.transform, faction.displayName + ":", 16, FontStyle.Normal, 180);
            AddLabel(row.transform, $"C {state.control[factionIndex]:0.00}", 16, FontStyle.Normal, 70);

            // Patrol controls
            AddLabel(row.transform, $"P {state.patrol[factionIndex]:0.00}", 16, FontStyle.Normal, 70);
            AddButton(row.transform, "+", () =>
            {
                var svc = DistrictControlService.Instance;
                svc?.AdjustPatrol(state.Id, factionIndex, +0.05f);
                var newP = state.patrol[factionIndex];
                var newC = state.control[factionIndex];
                Debug.Log($"[TerritoryDebug] {state.Definition.displayName} {faction.displayName} P -> {newP:0.00}, C -> {newC:0.00}");
                RefreshAndForce();
            }, 40, 28);
            AddButton(row.transform, "-", () =>
            {
                var svc = DistrictControlService.Instance;
                svc?.AdjustPatrol(state.Id, factionIndex, -0.05f);
                var newP = state.patrol[factionIndex];
                var newC = state.control[factionIndex];
                Debug.Log($"[TerritoryDebug] {state.Definition.displayName} {faction.displayName} P -> {newP:0.00}, C -> {newC:0.00}");
                RefreshAndForce();
            }, 40, 28);

            AddLabel(row.transform, $"H {state.heat[factionIndex]:0.00}", 16, FontStyle.Normal, 70);
        }

        private void AddLabel(Transform parent, string text, int size, FontStyle style, float preferredWidth = -1)
        {
            var go = new GameObject("Label", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _monoFont;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = text;

            var le = go.AddComponent<LayoutElement>();
            if (preferredWidth > 0) le.preferredWidth = preferredWidth;
        }
    }
}
