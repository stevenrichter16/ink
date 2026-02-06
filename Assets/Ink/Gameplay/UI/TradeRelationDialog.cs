using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Modal dialog for modifying trade status and tariff rate.
    /// </summary>
    public class TradeRelationDialog : MonoBehaviour
    {
        private Font _font;
        private Action<TradeStatus, float> _onConfirm;
        private Action _onCancel;

        private TradeStatus _status;
        private Text _statusValue;
        private Slider _tariffSlider;
        private Text _tariffValue;

        public static TradeRelationDialog Show(
            Transform parent,
            string title,
            TradeStatus defaultStatus,
            float defaultTariff,
            Action<TradeStatus, float> onConfirm,
            Action onCancel = null)
        {
            var go = new GameObject("TradeRelationDialog", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var dialog = go.AddComponent<TradeRelationDialog>();
            dialog.Initialize(title, defaultStatus, defaultTariff, onConfirm, onCancel);
            return dialog;
        }

        private void Initialize(
            string title,
            TradeStatus defaultStatus,
            float defaultTariff,
            Action<TradeStatus, float> onConfirm,
            Action onCancel)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _status = defaultStatus;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            BuildUI(title, Mathf.Clamp(defaultTariff, 0f, 1f));
            RefreshStatusLabel();
            UpdateTariffLabel();
        }

        private void BuildUI(string title, float defaultTariff)
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
            panelRect.sizeDelta = new Vector2(520f, 340f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            var titleText = CreateText(panel.transform, title, 24, FontStyle.Bold);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.05f, 0.84f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(panel.transform, false);
            content.anchorMin = new Vector2(0.05f, 0.22f);
            content.anchorMax = new Vector2(0.95f, 0.8f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(8, 8, 8, 8);

            CreateStatusSelector(content);
            CreateTariffSelector(content, defaultTariff);

            var footer = new GameObject("Footer", typeof(RectTransform)).GetComponent<RectTransform>();
            footer.SetParent(panel.transform, false);
            footer.anchorMin = new Vector2(0.05f, 0.05f);
            footer.anchorMax = new Vector2(0.95f, 0.17f);
            footer.offsetMin = Vector2.zero;
            footer.offsetMax = Vector2.zero;
            var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandHeight = false;
            footerLayout.childForceExpandWidth = false;
            footerLayout.spacing = 12;

            CreateButton(footer.transform, "Confirm", OnConfirmClicked);
            CreateButton(footer.transform, "Cancel", OnCancelClicked);
        }

        private void CreateStatusSelector(Transform parent)
        {
            var row = new GameObject("StatusRow", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            CreateText(row, "Trade Status", 18, FontStyle.Bold);

            var prev = CreateButton(row, "<", () => CycleStatus(-1));
            var prevLe = prev.GetComponent<LayoutElement>();
            if (prevLe != null)
            {
                prevLe.minWidth = 48;
                prevLe.minHeight = 30;
            }

            _statusValue = CreateText(row, "", 18, FontStyle.Normal);
            var statusLe = _statusValue.gameObject.AddComponent<LayoutElement>();
            statusLe.minWidth = 160;

            var next = CreateButton(row, ">", () => CycleStatus(1));
            var nextLe = next.GetComponent<LayoutElement>();
            if (nextLe != null)
            {
                nextLe.minWidth = 48;
                nextLe.minHeight = 30;
            }
        }

        private void CreateTariffSelector(Transform parent, float defaultTariff)
        {
            var row = new GameObject("TariffRow", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            CreateText(row, "Tariff Rate", 18, FontStyle.Bold);
            _tariffSlider = BuildSlider(row, 0f, 0.5f, false);
            _tariffSlider.SetValueWithoutNotify(defaultTariff);
            _tariffValue = CreateText(row, "", 16, FontStyle.Normal);
            _tariffSlider.onValueChanged.AddListener(_ => UpdateTariffLabel());
        }

        private void CycleStatus(int delta)
        {
            var values = (TradeStatus[])Enum.GetValues(typeof(TradeStatus));
            int index = Array.IndexOf(values, _status);
            if (index < 0) index = 0;

            index = (index + delta) % values.Length;
            if (index < 0) index += values.Length;

            _status = values[index];
            RefreshStatusLabel();
        }

        private void RefreshStatusLabel()
        {
            if (_statusValue != null)
                _statusValue.text = _status.ToString();
        }

        private void UpdateTariffLabel()
        {
            if (_tariffValue != null && _tariffSlider != null)
                _tariffValue.text = $"{_tariffSlider.value:P0}";
        }

        private void OnConfirmClicked()
        {
            float tariff = _tariffSlider != null ? _tariffSlider.value : 0f;
            _onConfirm?.Invoke(_status, tariff);
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
            var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(parent, false);

            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGO.transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            Stretch(bg.GetComponent<RectTransform>());

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(sliderGO.transform, false);
            fillArea.anchorMin = new Vector2(0f, 0f);
            fillArea.anchorMax = new Vector2(1f, 1f);
            fillArea.offsetMin = new Vector2(10, 10);
            fillArea.offsetMax = new Vector2(-10, -10);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea, false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.3f, 0.8f, 0.5f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(sliderGO.transform, false);
            handleArea.anchorMin = new Vector2(0f, 0f);
            handleArea.anchorMax = new Vector2(1f, 1f);
            handleArea.offsetMin = Vector2.zero;
            handleArea.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = Color.white;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16f, 24f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            var le = sliderGO.AddComponent<LayoutElement>();
            le.minHeight = 32;
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
