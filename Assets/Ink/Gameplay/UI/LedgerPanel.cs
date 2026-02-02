using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Runtime-built Ledger UI to view and edit player reputation with factions.
    /// </summary>
    public class LedgerPanel : MonoBehaviour
    {
        public bool IsVisible => _root != null && _root.activeSelf;

        private GameObject _root;
        private RectTransform _detailPane;
        private Text _titleText;
        private Text _factionName;
        private Text _repValue;
        private Slider _repSlider;
        private Action _onClose;

        private List<FactionDefinition> _factions = new List<FactionDefinition>();
        private int _selectedIndex = -1;
        private Dictionary<string, Text> _listLabels = new Dictionary<string, Text>();

        private Font _font;
        // Inter-faction editing
        private Dropdown _sourceDropdown;
        private Dropdown _targetDropdown;
        private int _interSourceIndex = -1;
        private int _interTargetIndex = -1;
        private bool _interFactionMode = false;


        public void Initialize(Action onClose, List<FactionDefinition> injectedFactions = null)
        {
            _onClose = onClose;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (injectedFactions != null)
            {
                _factions = new List<FactionDefinition>(injectedFactions);
            }
            else
            {
                LoadFactions();
            }
            BuildUI();
            if (_factions.Count > 0) SelectFaction(0);
            ReputationSystem.OnRepChanged += OnRepChanged;
            ReputationSystem.OnInterRepChanged += OnInterRepChanged;
        }

        private void OnDestroy()
        {
            ReputationSystem.OnRepChanged -= OnRepChanged;
        }

        private void LoadFactions()
        {
            _factions.Clear();
            _factions.AddRange(Resources.LoadAll<FactionDefinition>("Factions"));
        }

        private void BuildUI()
        {
            _root = new GameObject("LedgerRoot", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            var canvasRect = _root.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            // Backdrop
            var backdrop = CreateImage(_root.transform, new Color(0f, 0f, 0f, 0.7f));
            Stretch(backdrop.rectTransform);

            // Panel
            var panel = CreateImage(_root.transform, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            var panelRect = panel.rectTransform;
            panelRect.sizeDelta = new Vector2(1200f, 720f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            // Title bar
            _titleText = CreateText(panel.transform, "Ledger — Faction Reputation", 32, FontStyle.Bold);
            var titleRect = _titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.02f, 0.9f);
            titleRect.anchorMax = new Vector2(0.98f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Columns container
            var columns = new GameObject("Columns", typeof(RectTransform)).GetComponent<RectTransform>();
            columns.SetParent(panel.transform, false);
            columns.anchorMin = new Vector2(0.02f, 0.12f);
            columns.anchorMax = new Vector2(0.98f, 0.88f);
            columns.offsetMin = Vector2.zero;
            columns.offsetMax = Vector2.zero;
            var hLayout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;

            // Faction list
            var listGO = new GameObject("FactionList", typeof(RectTransform));
            listGO.transform.SetParent(columns, false);
            var listLayout = listGO.AddComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.spacing = 6;
            var listLE = listGO.AddComponent<LayoutElement>();
            listLE.flexibleWidth = 1f;

            var scroll = listGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(listGO.transform, false);
            var vpRect = viewport.GetComponent<RectTransform>();
            Stretch(vpRect);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = 4;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = vpRect;
            scroll.content = content;

            // Detail pane
            _detailPane = new GameObject("DetailPane", typeof(RectTransform)).GetComponent<RectTransform>();
            _detailPane.transform.SetParent(columns, false);
            var detailLE = _detailPane.gameObject.AddComponent<LayoutElement>();
            detailLE.flexibleWidth = 2f;
            var detailLayout = _detailPane.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.childForceExpandHeight = false;
            detailLayout.childForceExpandWidth = true;
            detailLayout.spacing = 10;
            detailLayout.padding = new RectOffset(10, 10, 10, 10);

            // Footer
            var footer = new GameObject("Footer", typeof(RectTransform)).GetComponent<RectTransform>();
            footer.SetParent(panel.transform, false);
            footer.anchorMin = new Vector2(0.02f, 0.02f);
            footer.anchorMax = new Vector2(0.98f, 0.08f);
            StretchHeight(footer, 0);
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandHeight = false;
            footerLayout.childForceExpandWidth = false;
            footerLayout.spacing = 12;

            var closeBtn = CreateButton(footer.transform, "Close (B/Esc)", OnCloseClicked);
            closeBtn.GetComponentInChildren<Text>().fontSize = 20;

            // Populate list buttons
            for (int i = 0; i < _factions.Count; i++)
            {
                int idx = i;
                var f = _factions[i];
                var btn = CreateButton(content.transform, $"{f.displayName}  ({ReputationSystem.GetRep(f.id)})", () => SelectFaction(idx));
                btn.GetComponent<LayoutElement>().minHeight = 44;
                var txt = btn.GetComponentInChildren<Text>();
                txt.fontSize = 20;
                txt.alignment = TextAnchor.MiddleLeft;
                _listLabels[f.id.ToLowerInvariant()] = txt;
            }

            BuildDetailPane();
        }

        private void BuildDetailPane()
        {
            foreach (Transform child in _detailPane) Destroy(child.gameObject);

            _factionName = CreateText(_detailPane, "Faction", 28, FontStyle.Bold);
            _repValue = CreateText(_detailPane, "Rep: 0 (Neutral)", 22, FontStyle.Normal);

            _repSlider = BuildSlider(_detailPane, -100, 100, OnSliderChanged);

            var buttonsRow = new GameObject("PresetButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            buttonsRow.transform.SetParent(_detailPane, false);
            var h = buttonsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            CreateButton(buttonsRow, "Friendly (+50)", () => SetRepValue(50));
            CreateButton(buttonsRow, "Neutral (0)", () => SetRepValue(0));
            CreateButton(buttonsRow, "Hostile (-50)", () => SetRepValue(-50));

            var applyRow = new GameObject("ApplyRow", typeof(RectTransform)).GetComponent<RectTransform>();
            applyRow.transform.SetParent(_detailPane, false);
            var h2 = applyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h2.spacing = 10;
            h2.childForceExpandWidth = false;
            h2.childForceExpandHeight = false;
            CreateButton(applyRow, "Apply", ApplyCurrent);
            CreateButton(applyRow, "Revert", RevertCurrent);
        }

        public void SelectFaction(int index)
        {
            if (index < 0 || index >= _factions.Count) return;
            _selectedIndex = index;
            var f = _factions[index];
            int rep = ReputationSystem.GetRep(f.id);
            UpdateDetail(f, rep);
        }

        private void UpdateDetail(FactionDefinition f, int rep)
        {
            _factionName.text = f.displayName;
            _repSlider.SetValueWithoutNotify(rep);
            _repValue.text = $"Rep: {rep} ({StatusWord(rep)})";
            _repValue.color = StatusColor(rep);
        }

        private void OnSliderChanged(float value)
        {
            if (_selectedIndex < 0) return;
            int rep = Mathf.RoundToInt(value);
            _repValue.text = $"Rep: {rep} ({StatusWord(rep)})";
            _repValue.color = StatusColor(rep);
        }

        public void SetRepValue(int value)
        {
            _repSlider.value = value;
            OnSliderChanged(value);
        }

        public void ApplyCurrent()
        {
            if (_selectedIndex < 0) return;
            var f = _factions[_selectedIndex];
            int oldRep = ReputationSystem.GetRep(f.id);
            int rep = Mathf.RoundToInt(_repSlider.value);
            Debug.Log($"[Ledger] Player reputation with '{f.displayName}' changed: {oldRep} → {rep} ({StatusWord(rep)})");
            ReputationSystem.SetRep(f.id, rep);
            UpdateDetail(f, rep);
            RefreshListLabel(f.id, rep);
        }

        public void RevertCurrent()
        {
            if (_selectedIndex < 0) return;
            var f = _factions[_selectedIndex];
            int rep = ReputationSystem.GetRep(f.id);
            UpdateDetail(f, rep);
        }

        private void OnCloseClicked()
        {
            _onClose?.Invoke();
        }

        public void Show()
        {
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        
        #region Inter-Faction Reputation Editing

        /// <summary>Select a source and target faction pair for inter-faction reputation editing.</summary>
        public void SelectInterFactionPair(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _factions.Count) return;
            if (targetIndex < 0 || targetIndex >= _factions.Count) return;
            
            _interSourceIndex = sourceIndex;
            _interTargetIndex = targetIndex;
            _interFactionMode = true;
            
            // Don't allow same source and target
            if (sourceIndex == targetIndex)
            {
                _interFactionMode = false;
                return;
            }
            
            var src = _factions[sourceIndex];
            var dst = _factions[targetIndex];
            int rep = ReputationSystem.GetInterRep(src.id, dst.id);
            UpdateInterDetail(src, dst, rep);
        }

        private void UpdateInterDetail(FactionDefinition src, FactionDefinition dst, int rep)
        {
            _factionName.text = $"{src.displayName} → {dst.displayName}";
            _repSlider.SetValueWithoutNotify(rep);
            _repValue.text = $"Rep: {rep} ({StatusWord(rep)})";
            _repValue.color = StatusColor(rep);
        }

        /// <summary>Apply the current slider value as inter-faction reputation.</summary>
        public void ApplyInterRep()
        {
            if (!_interFactionMode) return;
            if (_interSourceIndex < 0 || _interTargetIndex < 0) return;
            if (_interSourceIndex == _interTargetIndex) return; // Prevent self-targeting
            if (_interSourceIndex >= _factions.Count || _interTargetIndex >= _factions.Count) return;
            
            var src = _factions[_interSourceIndex];
            var dst = _factions[_interTargetIndex];
            int oldRep = ReputationSystem.GetInterRep(src.id, dst.id);
            int rep = Mathf.RoundToInt(_repSlider.value);
            Debug.Log($"[Ledger] Inter-faction reputation '{src.displayName}' → '{dst.displayName}' changed: {oldRep} → {rep} ({StatusWord(rep)})");
            ReputationSystem.SetInterRep(src.id, dst.id, rep);
            UpdateInterDetail(src, dst, rep);
        }

        /// <summary>Revert the slider to the current inter-faction reputation value.</summary>
        public void RevertInterRep()
        {
            if (!_interFactionMode) return;
            if (_interSourceIndex < 0 || _interTargetIndex < 0) return;
            if (_interSourceIndex >= _factions.Count || _interTargetIndex >= _factions.Count) return;
            
            var src = _factions[_interSourceIndex];
            var dst = _factions[_interTargetIndex];
            int rep = ReputationSystem.GetInterRep(src.id, dst.id);
            UpdateInterDetail(src, dst, rep);
        }

        private void OnInterRepChanged(string srcId, string dstId, int value)
        {
            if (!_interFactionMode) return;
            if (_interSourceIndex < 0 || _interTargetIndex < 0) return;
            if (_interSourceIndex >= _factions.Count || _interTargetIndex >= _factions.Count) return;
            
            var src = _factions[_interSourceIndex];
            var dst = _factions[_interTargetIndex];
            if (string.Equals(src.id, srcId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dst.id, dstId, StringComparison.OrdinalIgnoreCase))
            {
                UpdateInterDetail(src, dst, value);
            }
        }

        #endregion

// Helpers
        private Text CreateText(Transform parent, string content, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.font = _font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private Image CreateImage(Transform parent, Color color)
        {
            var go = new GameObject("Image", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Button CreateButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.28f, 0.9f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = CreateText(go.transform, label, 18, FontStyle.Normal);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 36;
            le.minWidth = 140;
            return btn;
        }

        private static string StatusWord(int rep)
        {
            if (rep <= HostilityService.HostileThreshold) return "Hostile";
            if (rep >= HostilityService.FriendlyThreshold) return "Friendly";
            return "Neutral";
        }

        private static Color StatusColor(int rep)
        {
            if (rep <= HostilityService.HostileThreshold) return new Color(0.85f, 0.3f, 0.3f);
            if (rep >= HostilityService.FriendlyThreshold) return new Color(0.35f, 0.8f, 0.45f);
            return new Color(0.92f, 0.74f, 0.35f);
        }

        private void OnRepChanged(string factionId, int value)
        {
            RefreshListLabel(factionId, value);
            if (_selectedIndex >= 0)
            {
                var f = _factions[_selectedIndex];
                if (string.Equals(f.id, factionId, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateDetail(f, value);
                }
            }
        }

        private void RefreshListLabel(string factionId, int rep)
        {
            if (string.IsNullOrEmpty(factionId)) return;
            var key = factionId.ToLowerInvariant();
            if (_listLabels.TryGetValue(key, out var txt))
            {
                // Keep original display name by re-reading factions dictionary
                var display = key;
                foreach (var f in _factions)
                {
                    if (f.id.Equals(factionId, StringComparison.OrdinalIgnoreCase))
                    {
                        display = f.displayName;
                        break;
                    }
                }
                txt.text = $"{display}  ({rep})";
            }
        }

        private Slider BuildSlider(Transform parent, float min, float max, Action<float> onChanged)
        {
            var go = new GameObject("RepSlider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;

            // Background
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            Stretch(bgRect);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f, 1f);

            // Fill Area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.25f);
            faRect.anchorMax = new Vector2(1f, 0.75f);
            faRect.offsetMin = new Vector2(10f, 0f);
            faRect.offsetMax = new Vector2(-20f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.45f, 0.8f, 0.45f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            Stretch(fillRect);
            slider.fillRect = fillRect;

            // Handle
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = new Vector2(0f, 0f);
            haRect.anchorMax = new Vector2(1f, 1f);
            haRect.offsetMin = new Vector2(10f, 0f);
            haRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            var sliderLE = go.AddComponent<LayoutElement>();
            sliderLE.minHeight = 44;

            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return slider;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchHeight(RectTransform rt, float padding)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, padding);
            rt.offsetMax = new Vector2(rt.offsetMax.x, -padding);
        }
    }
}
