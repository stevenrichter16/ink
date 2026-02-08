using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Bottom-left legend panel mapping faction colors to faction names.
    /// Visible only when the territory overlay is active.
    /// </summary>
    public class FactionLegendPanel : MonoBehaviour
    {
        private GameObject _canvasGO;
        private GameObject _panelGO;
        private Font _monoFont;
        private TerritoryOverlay _overlay;

        public bool IsVisible => _panelGO != null && _panelGO.activeSelf;

        /// <summary>
        /// Build the legend UI and subscribe to overlay toggle.
        /// Call after TerritoryOverlay is initialized and faction colors are assigned.
        /// </summary>
        public void Initialize(TerritoryOverlay overlay)
        {
            _overlay = overlay;
            BuildUI();
            _panelGO.SetActive(false);

            if (_overlay != null)
                _overlay.OnVisibilityChanged += OnOverlayToggled;
        }

        private void OnOverlayToggled(bool visible)
        {
            if (_panelGO != null)
                _panelGO.SetActive(visible);
        }

        public void Show()
        {
            if (_panelGO != null) _panelGO.SetActive(true);
        }

        public void Hide()
        {
            if (_panelGO != null) _panelGO.SetActive(false);
        }

        /// <summary>
        /// Build legend data from current district control state.
        /// Returns one entry per unique controlling faction across all districts.
        /// </summary>
        public static List<(string name, Color color)> BuildLegendData()
        {
            var result = new List<(string, Color)>();
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return result;

            var seen = new HashSet<string>();
            foreach (var state in dcs.States)
            {
                var faction = DistrictControlService.GetDominantFaction(state, dcs.Factions);
                if (faction != null && seen.Add(faction.id))
                {
                    result.Add((faction.displayName, faction.color));
                }
            }
            return result;
        }

        private void BuildUI()
        {
            _monoFont = GetMonoFont();

            _canvasGO = new GameObject("FactionLegendCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGO.transform.SetParent(transform, false);
            var canvas = _canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 175;
            var scaler = _canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _panelGO = new GameObject("Panel", typeof(Image));
            _panelGO.transform.SetParent(_canvasGO.transform, false);
            var img = _panelGO.GetComponent<Image>();
            img.color = new Color(0.06f, 0.08f, 0.10f, 0.85f);

            var panelRect = _panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(12f, 12f);
            panelRect.sizeDelta = new Vector2(220f, 0f);

            var fitter = _panelGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = _panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 3;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGO = new GameObject("Title", typeof(Text));
            titleGO.transform.SetParent(_panelGO.transform, false);
            var title = titleGO.GetComponent<Text>();
            title.font = _monoFont;
            title.fontSize = 16;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleLeft;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.text = "Factions";

            // Faction rows
            var data = BuildLegendData();
            foreach (var entry in data)
            {
                CreateLegendRow(_panelGO.transform, entry.name, entry.color);
            }
        }

        private void CreateLegendRow(Transform parent, string factionName, Color factionColor)
        {
            var rowGO = new GameObject("Row_" + factionName, typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(parent, false);
            var hLayout = rowGO.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandHeight = false;

            // Color swatch
            var swatchGO = new GameObject("Swatch", typeof(Image));
            swatchGO.transform.SetParent(rowGO.transform, false);
            var swatchImg = swatchGO.GetComponent<Image>();
            // Use faction color but with visible alpha for the swatch
            swatchImg.color = new Color(factionColor.r, factionColor.g, factionColor.b, 0.9f);
            var swatchLE = swatchGO.AddComponent<LayoutElement>();
            swatchLE.preferredWidth = 16;
            swatchLE.preferredHeight = 16;

            // Faction name
            var nameGO = new GameObject("Name", typeof(Text));
            nameGO.transform.SetParent(rowGO.transform, false);
            var nameText = nameGO.GetComponent<Text>();
            nameText.font = _monoFont;
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyle.Normal;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            nameText.text = factionName;
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 160;
            nameLE.preferredHeight = 16;
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

        private void OnDestroy()
        {
            if (_overlay != null)
                _overlay.OnVisibilityChanged -= OnOverlayToggled;
            if (_canvasGO != null)
                Destroy(_canvasGO);
        }
    }
}
