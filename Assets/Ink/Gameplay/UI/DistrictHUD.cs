using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Upper-left HUD showing the current district name and controlling faction.
    /// Updates when the player moves into a new district.
    /// </summary>
    public class DistrictHUD : MonoBehaviour
    {
        private GameObject _canvasGO;
        private GameObject _panelGO;
        private Text _districtLabel;
        private Text _factionLabel;
        private Font _monoFont;
        private string _lastDistrictId;
        private PlayerController _player;

        public string CurrentDistrictId => _lastDistrictId;
        public string DistrictLabelText => _districtLabel != null ? _districtLabel.text : "";
        public string FactionLabelText => _factionLabel != null ? _factionLabel.text : "";

        private void Start()
        {
            BuildUI();

            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _player.OnMoveComplete += OnPlayerMoved;
                UpdateForPosition(_player.gridX, _player.gridY);
            }
        }

        /// <summary>Build UI without player subscription (for editor tests).</summary>
        public void BuildUIForTests()
        {
            BuildUI();
        }

        private void OnPlayerMoved(GridEntity entity)
        {
            UpdateForPosition(entity.gridX, entity.gridY);
        }

        /// <summary>
        /// Update the HUD for a given grid position. Public for testability.
        /// </summary>
        public void UpdateForPosition(int x, int y)
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            var state = dcs.GetStateByPosition(x, y);
            string newId = state != null ? state.Id : "__wilderness__";

            if (newId == _lastDistrictId) return;
            _lastDistrictId = newId;

            if (state == null)
            {
                _districtLabel.text = "Wilderness";
                _factionLabel.text = "";
                _factionLabel.gameObject.SetActive(false);
            }
            else
            {
                _districtLabel.text = state.Definition.displayName;
                var faction = DistrictControlService.GetDominantFaction(state, dcs.Factions);
                if (faction != null)
                {
                    _factionLabel.text = faction.displayName;
                    _factionLabel.color = FactionColorOpaque(faction.color);
                    _factionLabel.gameObject.SetActive(true);
                }
                else
                {
                    _factionLabel.text = "Contested";
                    _factionLabel.color = Color.gray;
                    _factionLabel.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Convert faction overlay color (low alpha) to opaque for readable text.</summary>
        private static Color FactionColorOpaque(Color c)
        {
            return new Color(
                Mathf.Clamp01(c.r * 2f),
                Mathf.Clamp01(c.g * 2f),
                Mathf.Clamp01(c.b * 2f),
                1f
            );
        }

        private void BuildUI()
        {
            _monoFont = GetMonoFont();

            _canvasGO = new GameObject("DistrictHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGO.transform.SetParent(transform, false);
            var canvas = _canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 170;
            var scaler = _canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _panelGO = new GameObject("Panel", typeof(Image));
            _panelGO.transform.SetParent(_canvasGO.transform, false);
            var img = _panelGO.GetComponent<Image>();
            img.color = new Color(0.06f, 0.08f, 0.10f, 0.85f);

            var panelRect = _panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(12f, -12f);
            panelRect.sizeDelta = new Vector2(280f, 0f);

            var fitter = _panelGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = _panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // District name label
            var districtGO = new GameObject("DistrictLabel", typeof(Text));
            districtGO.transform.SetParent(_panelGO.transform, false);
            _districtLabel = districtGO.GetComponent<Text>();
            _districtLabel.font = _monoFont;
            _districtLabel.fontSize = 20;
            _districtLabel.fontStyle = FontStyle.Bold;
            _districtLabel.color = Color.white;
            _districtLabel.alignment = TextAnchor.MiddleLeft;
            _districtLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _districtLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _districtLabel.text = "";

            // Faction name label
            var factionGO = new GameObject("FactionLabel", typeof(Text));
            factionGO.transform.SetParent(_panelGO.transform, false);
            _factionLabel = factionGO.GetComponent<Text>();
            _factionLabel.font = _monoFont;
            _factionLabel.fontSize = 16;
            _factionLabel.fontStyle = FontStyle.Normal;
            _factionLabel.color = Color.white;
            _factionLabel.alignment = TextAnchor.MiddleLeft;
            _factionLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _factionLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _factionLabel.text = "";
            _factionLabel.gameObject.SetActive(false);
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
            if (_player != null)
                _player.OnMoveComplete -= OnPlayerMoved;
            if (_canvasGO != null)
                Destroy(_canvasGO);
        }
    }
}
