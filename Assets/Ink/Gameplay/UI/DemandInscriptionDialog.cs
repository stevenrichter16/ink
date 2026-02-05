using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Modal dialog for demand event inscriptions.
    /// </summary>
    public class DemandInscriptionDialog : MonoBehaviour
    {
        private Font _font;
        private Action<string, float, int> _onConfirm;
        private Action _onCancel;

        private Dropdown _itemDropdown;
        private List<string> _itemIds = new List<string>();
        private Slider _multiplierSlider;
        private Slider _durationSlider;
        private Text _multiplierValue;
        private Text _durationValue;
        private Text _costText;

        public static DemandInscriptionDialog Show(Transform parent, string title, Action<string, float, int> onConfirm, string defaultItemId = "potion", Action onCancel = null)
        {
            var go = new GameObject("DemandInscriptionDialog", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var dialog = go.AddComponent<DemandInscriptionDialog>();
            dialog.Initialize(title, onConfirm, onCancel, defaultItemId);
            return dialog;
        }

        public static List<string> GetItemOptions()
        {
            ItemDatabase.Initialize();
            var list = new List<string>(ItemDatabase.AllIds);
            list.Sort((a, b) =>
            {
                var aData = ItemDatabase.Get(a);
                var bData = ItemDatabase.Get(b);
                string aName = aData != null ? aData.name : a;
                string bName = bData != null ? bData.name : b;
                int cmp = string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        private void Initialize(string title, Action<string, float, int> onConfirm, Action onCancel, string defaultItemId)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            BuildUI(title, defaultItemId);
            UpdateCost();
        }

        private void BuildUI(string title, string defaultItemId)
        {
            var root = gameObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var backdrop = CreateImage(transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(backdrop.rectTransform);

            var panel = CreateImage(transform, new Color(0.1f, 0.1f, 0.12f, 0.98f));
            var panelRect = panel.rectTransform;
            panelRect.sizeDelta = new Vector2(520f, 420f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            var titleText = CreateText(panel.transform, title, 24, FontStyle.Bold);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.05f, 0.85f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(panel.transform, false);
            content.anchorMin = new Vector2(0.05f, 0.2f);
            content.anchorMax = new Vector2(0.95f, 0.82f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(8, 8, 8, 8);

            CreateLabeledDropdown(content, "Item", defaultItemId, out _itemDropdown, out _itemIds);
            CreateLabeledSlider(content, "Demand Multiplier", 1f, 3f, 2f, false, out _multiplierSlider, out _multiplierValue);
            CreateLabeledSlider(content, "Duration (days)", 1f, 20f, 5f, true, out _durationSlider, out _durationValue);

            _costText = CreateText(content, "Ink Cost: 0", 20, FontStyle.Bold);
            _costText.alignment = TextAnchor.MiddleLeft;

            var footer = new GameObject("Footer", typeof(RectTransform)).GetComponent<RectTransform>();
            footer.SetParent(panel.transform, false);
            footer.anchorMin = new Vector2(0.05f, 0.05f);
            footer.anchorMax = new Vector2(0.95f, 0.15f);
            footer.offsetMin = Vector2.zero;
            footer.offsetMax = Vector2.zero;
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandHeight = false;
            footerLayout.childForceExpandWidth = false;
            footerLayout.spacing = 12;

            CreateButton(footer.transform, "Confirm", OnConfirmClicked);
            CreateButton(footer.transform, "Cancel", OnCancelClicked);

            _multiplierSlider.onValueChanged.AddListener(_ => UpdateCost());
            _durationSlider.onValueChanged.AddListener(_ => UpdateCost());
        }

        private void CreateLabeledDropdown(Transform parent, string label, string defaultItemId, out Dropdown dropdown, out List<string> itemIds)
        {
            var row = new GameObject(label + "Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            CreateText(row, label, 18, FontStyle.Bold);

            var dropdownGO = new GameObject("ItemDropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownGO.transform.SetParent(row, false);
            var img = dropdownGO.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            dropdown = dropdownGO.GetComponent<Dropdown>();
            dropdown.targetGraphic = img;

            var labelText = CreateText(dropdownGO.transform, "", 18, FontStyle.Normal);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.rectTransform.anchorMin = new Vector2(0.1f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(0.9f, 1f);
            dropdown.captionText = labelText;

            var arrow = CreateText(dropdownGO.transform, "▼", 16, FontStyle.Normal);
            arrow.alignment = TextAnchor.MiddleRight;
            arrow.rectTransform.anchorMin = new Vector2(0.9f, 0f);
            arrow.rectTransform.anchorMax = new Vector2(0.98f, 1f);

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(dropdownGO.transform, false);
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
            dropdown.captionText = labelText;

            itemIds = GetItemOptions();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < itemIds.Count; i++)
            {
                var data = ItemDatabase.Get(itemIds[i]);
                string labelTextValue = data != null ? $"{data.name} ({itemIds[i]})" : itemIds[i];
                options.Add(new Dropdown.OptionData(labelTextValue));
            }
            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            int defaultIndex = 0;
            if (!string.IsNullOrEmpty(defaultItemId))
            {
                defaultIndex = itemIds.FindIndex(id => id == defaultItemId);
                if (defaultIndex < 0) defaultIndex = 0;
            }
            dropdown.SetValueWithoutNotify(defaultIndex);

            var le = dropdownGO.AddComponent<LayoutElement>();
            le.minHeight = 36;
        }

        private void CreateLabeledSlider(Transform parent, string label, float min, float max, float defaultValue, bool whole, out Slider slider, out Text valueText)
        {
            var row = new GameObject(label + "Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            CreateText(row, label, 18, FontStyle.Bold);
            slider = BuildSlider(row, min, max, whole);
            slider.SetValueWithoutNotify(defaultValue);
            valueText = CreateText(row, defaultValue.ToString("0.00"), 16, FontStyle.Normal);
        }

        private void UpdateCost()
        {
            float mult = _multiplierSlider != null ? _multiplierSlider.value : 1f;
            int duration = _durationSlider != null ? Mathf.RoundToInt(_durationSlider.value) : 0;

            if (_multiplierValue != null) _multiplierValue.text = mult.ToString("0.00");
            if (_durationValue != null) _durationValue.text = duration.ToString();

            int cost = Mathf.RoundToInt(Mathf.Max(0f, mult - 1f) * 50f + duration * 2f);
            if (_costText != null) _costText.text = $"Ink Cost: {cost}";
        }

        private void OnConfirmClicked()
        {
            string itemId = null;
            if (_itemDropdown != null && _itemIds != null && _itemDropdown.value >= 0 && _itemDropdown.value < _itemIds.Count)
                itemId = _itemIds[_itemDropdown.value];
            float mult = _multiplierSlider != null ? _multiplierSlider.value : 1f;
            int duration = _durationSlider != null ? Mathf.RoundToInt(_durationSlider.value) : 0;

            _onConfirm?.Invoke(itemId, mult, duration);
            Destroy(gameObject);
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Destroy(gameObject);
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
            le.minWidth = 120;
            return btn;
        }

        private Slider BuildSlider(Transform parent, float min, float max, bool wholeNumbers)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            Stretch(bgRect);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f, 1f);

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
            handleRect.sizeDelta = new Vector2(20f, 20f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            var sliderLE = go.AddComponent<LayoutElement>();
            sliderLE.minHeight = 36;

            return slider;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
