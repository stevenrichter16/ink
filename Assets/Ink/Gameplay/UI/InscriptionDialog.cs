using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkSim
{
    /// <summary>
    /// Simple modal dialog for creating economic inscriptions.
    /// </summary>
    public class InscriptionDialog : MonoBehaviour
    {
        private Font _font;
        private Action<float, int, int> _onConfirm;
        private Action _onCancel;

        private Slider _rateSlider;
        private Slider _durationSlider;
        private Slider _radiusSlider;
        private Text _rateValue;
        private Text _durationValue;
        private Text _radiusValue;
        private Text _costText;
        private Text _costBreakdown;

        public static InscriptionDialog Show(
            Transform parent,
            string title,
            Action<float, int, int> onConfirm,
            Action onCancel = null,
            float defaultRate = 0.1f,
            int defaultDuration = 5,
            int defaultRadius = 5)
        {
            var go = new GameObject("InscriptionDialog", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var dialog = go.AddComponent<InscriptionDialog>();
            dialog.Initialize(title, onConfirm, onCancel, defaultRate, defaultDuration, defaultRadius);
            return dialog;
        }

        private void Initialize(
            string title,
            Action<float, int, int> onConfirm,
            Action onCancel,
            float defaultRate,
            int defaultDuration,
            int defaultRadius)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            BuildUI(title, defaultRate, defaultDuration, defaultRadius);
            UpdateCost();
        }

        private void BuildUI(string title, float defaultRate, int defaultDuration, int defaultRadius)
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

            float clampedRate = Mathf.Clamp(defaultRate, -0.5f, 0.5f);
            int clampedDuration = Mathf.Clamp(defaultDuration, 1, 20);
            int clampedRadius = Mathf.Clamp(defaultRadius, 1, 12);
            CreateLabeledSlider(content, "Rate", -0.5f, 0.5f, clampedRate, false, out _rateSlider, out _rateValue);
            CreateLabeledSlider(content, "Duration (days)", 1f, 20f, clampedDuration, true, out _durationSlider, out _durationValue);
            CreateLabeledSlider(content, "Radius", 1f, 12f, clampedRadius, true, out _radiusSlider, out _radiusValue);

            _costText = CreateText(content, "Ink Cost: 0", 20, FontStyle.Bold);
            _costText.alignment = TextAnchor.MiddleLeft;
            _costBreakdown = CreateText(content, "", 14, FontStyle.Normal);
            _costBreakdown.alignment = TextAnchor.UpperLeft;

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

            _rateSlider.onValueChanged.AddListener(_ => UpdateCost());
            _durationSlider.onValueChanged.AddListener(_ => UpdateCost());
            _radiusSlider.onValueChanged.AddListener(_ => UpdateCost());
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
            float rate = _rateSlider != null ? _rateSlider.value : 0f;
            int duration = _durationSlider != null ? Mathf.RoundToInt(_durationSlider.value) : 0;
            int radius = _radiusSlider != null ? Mathf.RoundToInt(_radiusSlider.value) : 0;

            if (_rateValue != null) _rateValue.text = rate.ToString("+0.00;-0.00;0.00");
            if (_durationValue != null) _durationValue.text = duration.ToString();
            if (_radiusValue != null) _radiusValue.text = radius.ToString();

            var cost = EconomicInkCostCalculator.CalculateTaxBreakdown(rate, duration, radius);
            if (_costText != null) _costText.text = $"Ink Cost: {cost.totalCost}";
            if (_costBreakdown != null) _costBreakdown.text = EconomicInkCostCalculator.FormatMultiline(cost);
        }

        private void OnConfirmClicked()
        {
            float rate = _rateSlider != null ? _rateSlider.value : 0f;
            int duration = _durationSlider != null ? Mathf.RoundToInt(_durationSlider.value) : 0;
            int radius = _radiusSlider != null ? Mathf.RoundToInt(_radiusSlider.value) : 0;

            _onConfirm?.Invoke(rate, duration, radius);
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
