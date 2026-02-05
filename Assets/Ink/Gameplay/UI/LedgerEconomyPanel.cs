using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Runtime-built Ledger UI to view and edit district economic data.
    /// </summary>
    public class LedgerEconomyPanel : MonoBehaviour
    {
        public bool IsVisible => _root != null && _root.activeSelf;

        private GameObject _root;
        private RectTransform _detailPane;
        private Text _titleText;
        private Dropdown _districtDropdown;
        private Action _onClose;

        private Font _font;
        private List<DistrictState> _districts = new List<DistrictState>();
        private int _selectedIndex = -1;

        public void Initialize(Action onClose)
        {
            _onClose = onClose;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            LoadDistricts();
            BuildUI();
            if (_districts.Count > 0) SelectDistrict(0);
        }

        private void LoadDistricts()
        {
            _districts.Clear();
            var dcs = DistrictControlService.Instance;
            if (dcs != null && dcs.States != null)
            {
                for (int i = 0; i < dcs.States.Count; i++)
                    _districts.Add(dcs.States[i]);
            }
        }

        private void BuildUI()
        {
            _root = new GameObject("LedgerEconomyRoot", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            var canvasRect = _root.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            var backdrop = CreateImage(_root.transform, new Color(0f, 0f, 0f, 0.7f));
            Stretch(backdrop.rectTransform);

            var panel = CreateImage(_root.transform, new Color(0.08f, 0.08f, 0.1f, 0.96f));
            var panelRect = panel.rectTransform;
            panelRect.sizeDelta = new Vector2(1200f, 720f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            _titleText = CreateText(panel.transform, "Ledger — Economy", 32, FontStyle.Bold);
            var titleRect = _titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.02f, 0.9f);
            titleRect.anchorMax = new Vector2(0.98f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var header = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
            header.SetParent(panel.transform, false);
            header.anchorMin = new Vector2(0.02f, 0.82f);
            header.anchorMax = new Vector2(0.98f, 0.9f);
            header.offsetMin = Vector2.zero;
            header.offsetMax = Vector2.zero;
            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 12;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;

            CreateText(header, "District", 20, FontStyle.Bold);
            _districtDropdown = CreateDropdown(header, OnDistrictChanged);

            _detailPane = new GameObject("DetailPane", typeof(RectTransform)).GetComponent<RectTransform>();
            _detailPane.transform.SetParent(panel.transform, false);
            _detailPane.anchorMin = new Vector2(0.02f, 0.12f);
            _detailPane.anchorMax = new Vector2(0.98f, 0.8f);
            _detailPane.offsetMin = Vector2.zero;
            _detailPane.offsetMax = Vector2.zero;
            var detailLayout = _detailPane.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.childForceExpandHeight = false;
            detailLayout.childForceExpandWidth = true;
            detailLayout.spacing = 8;
            detailLayout.padding = new RectOffset(10, 10, 10, 10);

            var footer = new GameObject("Footer", typeof(RectTransform)).GetComponent<RectTransform>();
            footer.SetParent(panel.transform, false);
            footer.anchorMin = new Vector2(0.02f, 0.02f);
            footer.anchorMax = new Vector2(0.98f, 0.08f);
            StretchHeight(footer, 0);
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandHeight = false;
            footerLayout.childForceExpandWidth = false;
            footerLayout.spacing = 12;

            var inscribeBtn = CreateButton(footer.transform, "Inscribe Tax", OnInscribeTax);
            inscribeBtn.GetComponentInChildren<Text>().fontSize = 20;

            var closeBtn = CreateButton(footer.transform, "Close (B/Esc)", OnCloseClicked);
            closeBtn.GetComponentInChildren<Text>().fontSize = 20;

            PopulateDistrictDropdown();
        }

        private void PopulateDistrictDropdown()
        {
            if (_districtDropdown == null) return;
            _districtDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < _districts.Count; i++)
            {
                var name = _districts[i]?.Definition != null ? _districts[i].Definition.displayName : "District";
                options.Add(new Dropdown.OptionData(name));
            }
            _districtDropdown.AddOptions(options);
        }

        private void BuildDetailPane()
        {
            foreach (Transform child in _detailPane) Destroy(child.gameObject);

            if (_selectedIndex < 0 || _selectedIndex >= _districts.Count)
            {
                CreateText(_detailPane, "No district selected.", 20, FontStyle.Italic);
                return;
            }

            var state = _districts[_selectedIndex];
            var districtName = state.Definition != null ? state.Definition.displayName : state.Id;
            CreateText(_detailPane, districtName, 26, FontStyle.Bold);

            CreateSectionHeader(_detailPane, "Taxes");
            var policies = TaxRegistry.GetPoliciesFor(state.Id);
            if (policies.Count == 0)
            {
                CreateText(_detailPane, "No active tax policies.", 18, FontStyle.Italic);
            }
            else
            {
                for (int i = 0; i < policies.Count; i++)
                {
                    var p = policies[i];
                    string duration = p.turnsRemaining < 0 ? "permanent" : $"{p.turnsRemaining} turns";
                    CreateText(_detailPane, $"{p.type} {p.rate:P0} ({duration})", 18, FontStyle.Normal);
                }
            }

            CreateSectionHeader(_detailPane, "Officials");
            var officials = OfficialRegistry.GetInDistrict(state.Id);
            if (officials.Count == 0)
            {
                CreateText(_detailPane, "No officials assigned.", 18, FontStyle.Italic);
            }
            else
            {
                for (int i = 0; i < officials.Count; i++)
                {
                    var o = officials[i];
                    var name = string.IsNullOrEmpty(o.displayName) ? o.title : o.displayName;
                    CreateText(_detailPane, name, 18, FontStyle.Normal);
                }
            }

            CreateSectionHeader(_detailPane, "Trade Relations");
            var relations = TradeRelationRegistry.GetForFaction(state.ControllingFactionIndex >= 0 &&
                DistrictControlService.Instance != null &&
                DistrictControlService.Instance.Factions != null &&
                state.ControllingFactionIndex < DistrictControlService.Instance.Factions.Count ?
                DistrictControlService.Instance.Factions[state.ControllingFactionIndex].id : null);
            if (relations.Count == 0)
            {
                CreateText(_detailPane, "No active trade relations.", 18, FontStyle.Italic);
            }
            else
            {
                for (int i = 0; i < relations.Count; i++)
                {
                    var r = relations[i];
                    CreateText(_detailPane, $"{r.sourceFactionId} → {r.targetFactionId}: {r.status} (Tariff {r.tariffRate:P0})", 18, FontStyle.Normal);
                }
            }

            CreateSectionHeader(_detailPane, "Active Economic Layers");
            var center = GetDistrictCenter(state);
            var rules = OverlayResolver.GetRulesAt(center.x, center.y);
            CreateText(_detailPane, $"Price {rules.priceMultiplier:0.00}x | Tax {rules.taxModifier*100:+0;-0;0}%", 18, FontStyle.Normal);
            CreateText(_detailPane, $"Supply {rules.supplyModifier:0.00}x | Demand {rules.demandModifier:0.00}x", 18, FontStyle.Normal);
        }

        public void SelectDistrict(int index)
        {
            if (index < 0 || index >= _districts.Count) return;
            _selectedIndex = index;
            if (_districtDropdown != null)
                _districtDropdown.SetValueWithoutNotify(index);
            BuildDetailPane();
        }

        private void OnDistrictChanged(int index)
        {
            SelectDistrict(index);
        }

        private void OnInscribeTax()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _districts.Count) return;
            var state = _districts[_selectedIndex];

            InscriptionDialog.Show(_root.transform, "Inscribe Tax", (rate, duration, radius) =>
            {
                var center = GetDistrictCenter(state);
                var layer = new PalimpsestLayer
                {
                    center = center,
                    radius = radius,
                    turnsRemaining = duration,
                    priority = 0,
                };
                var token = $"TAX:{rate.ToString("0.00", CultureInfo.InvariantCulture)}";
                layer.tokens.Add(token);
                OverlayResolver.RegisterLayer(layer);
                BuildDetailPane();
            });
        }

        private Vector2Int GetDistrictCenter(DistrictState state)
        {
            if (state?.Definition == null)
                return Vector2Int.zero;
            int cx = Mathf.RoundToInt((state.Definition.minX + state.Definition.maxX) * 0.5f);
            int cy = Mathf.RoundToInt((state.Definition.minY + state.Definition.maxY) * 0.5f);
            return new Vector2Int(cx, cy);
        }

        private void OnCloseClicked()
        {
            _onClose?.Invoke();
        }

        public void Show()
        {
            LoadDistricts();
            PopulateDistrictDropdown();
            if (_districts.Count > 0)
            {
                int idx = Mathf.Clamp(_selectedIndex, 0, _districts.Count - 1);
                SelectDistrict(idx);
            }
            else
            {
                _selectedIndex = -1;
                BuildDetailPane();
            }
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

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

        private void CreateSectionHeader(Transform parent, string title)
        {
            var header = CreateText(parent, title, 20, FontStyle.Bold);
            header.color = new Color(0.8f, 0.9f, 1f);
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
            le.minWidth = 160;
            return btn;
        }

        private Dropdown CreateDropdown(Transform parent, Action<int> onChanged)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            var dropdown = go.GetComponent<Dropdown>();
            dropdown.targetGraphic = img;

            var label = CreateText(go.transform, "", 18, FontStyle.Normal);
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.anchorMin = new Vector2(0.1f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.9f, 1f);
            dropdown.captionText = label;

            var arrow = CreateText(go.transform, "▼", 16, FontStyle.Normal);
            arrow.alignment = TextAnchor.MiddleRight;
            arrow.rectTransform.anchorMin = new Vector2(0.9f, 0f);
            arrow.rectTransform.anchorMax = new Vector2(0.98f, 1f);

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            template.SetActive(false);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 200f);
            template.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.98f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            Stretch(viewport.GetComponent<RectTransform>());

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            var vLayout = content.GetComponent<VerticalLayoutGroup>();
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 2;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemToggle = item.GetComponent<Toggle>();
            var itemBg = CreateImage(item.transform, new Color(0.22f, 0.22f, 0.25f, 1f));
            itemBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            itemBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            itemToggle.targetGraphic = itemBg;

            var itemCheck = CreateImage(item.transform, new Color(0.45f, 0.8f, 0.45f, 1f));
            itemCheck.rectTransform.anchorMin = new Vector2(0f, 0f);
            itemCheck.rectTransform.anchorMax = new Vector2(0.08f, 1f);
            itemToggle.graphic = itemCheck;

            var itemLabel = CreateText(item.transform, "Option", 18, FontStyle.Normal);
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.rectTransform.anchorMin = new Vector2(0.1f, 0f);
            itemLabel.rectTransform.anchorMax = new Vector2(0.98f, 1f);

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.captionText = label;

            dropdown.onValueChanged.AddListener(i => onChanged?.Invoke(i));
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 320;
            le.minHeight = 32;
            return dropdown;
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
