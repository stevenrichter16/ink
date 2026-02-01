using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InkSim
{
    /// <summary>
    /// Minimal runtime popup to pick a faction for a target FactionMember.
    /// </summary>
    public class FactionSelectionPopup : MonoBehaviour
    {
        private static FactionSelectionPopup _instance;
        private FactionMember _target;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        public static void Show(FactionMember target)
        {
            if (target == null) return;

            if (_instance == null)
            {
                var go = new GameObject("FactionSelectionPopup");
                _instance = go.AddComponent<FactionSelectionPopup>();
                DontDestroyOnLoad(go);
            }

            _instance.BuildUI(target);
        }

        private void BuildUI(FactionMember target)
        {
            ClearUI();
            _target = target;

            EnsureEventSystem();

            // Canvas
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _spawned.Add(canvasGO);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Panel
            var panel = new GameObject("Panel", typeof(Image), typeof(VerticalLayoutGroup));
            _spawned.Add(panel);
            panel.transform.SetParent(canvasGO.transform, false);
            var img = panel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.8f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rt = panel.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // Title
            AddLabel(panel.transform, $"Set Faction for {target.name}", 20, FontStyle.Bold);

            // Buttons per faction
            var factions = Resources.LoadAll<FactionDefinition>("Factions");
            foreach (var faction in factions)
            {
                AddButton(panel.transform, faction.displayName, () =>
                {
                    target.SetFaction(faction);
                    Close();
                });
            }

            // Cancel
            AddButton(panel.transform, "Cancel", Close);
        }

        private void AddLabel(Transform parent, string text, int size, FontStyle style)
        {
            var go = new GameObject("Label", typeof(Text));
            _spawned.Add(go);
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }

        private void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            _spawned.Add(go);
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.4f, 0.4f, 0.9f);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGO = new GameObject("Text", typeof(Text));
            _spawned.Add(txtGO);
            txtGO.transform.SetParent(go.transform, false);
            var t = txtGO.GetComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 16;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(260, 36);

            var textRT = txtGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void ClearUI()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }

        private void Close()
        {
            ClearUI();
            _target = null;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                _spawned.Add(es);
            }
        }
    }
}
