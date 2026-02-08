using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Togglable color overlay that tints floor tiles to show faction territory control.
    /// Press 'O' to toggle. Each district's tiles are tinted with the controlling faction's color.
    /// </summary>
    public class TerritoryOverlay : MonoBehaviour
    {
        private Transform _overlayRoot;
        private bool _visible;
        private float _tileSize;
        private Sprite _whiteSprite;

        // Per-district list of overlay SpriteRenderers for batch color updates
        private List<DistrictOverlayGroup> _districtGroups = new List<DistrictOverlayGroup>();
        private int _totalTileCount;

        private struct DistrictOverlayGroup
        {
            public DistrictState State;
            public List<SpriteRenderer> Renderers;
        }

        /// <summary>Fired after overlay visibility changes. Parameter is new visibility state.</summary>
        public event Action<bool> OnVisibilityChanged;

        public Transform OverlayRoot => _overlayRoot;
        public bool IsVisible => _visible;
        public int OverlayTileCount => _totalTileCount;
        public int DistrictGroupCount => _districtGroups.Count;

        /// <summary>Returns the current tint color for a district group (for testing).</summary>
        public Color GetDistrictTint(int districtIndex)
        {
            if (districtIndex < 0 || districtIndex >= _districtGroups.Count) return Color.clear;
            var renderers = _districtGroups[districtIndex].Renderers;
            return renderers.Count > 0 ? renderers[0].color : Color.clear;
        }

        /// <summary>
        /// Build overlay tile pool for all district bounds. Call after DistrictControlService is ready.
        /// </summary>
        public void Initialize(float tileSize)
        {
            _tileSize = tileSize;
            _whiteSprite = CreateWhiteSprite(_tileSize);

            _overlayRoot = new GameObject("OverlayRoot").transform;
            _overlayRoot.SetParent(transform, false);
            _overlayRoot.gameObject.SetActive(false);
            _visible = false;
            _totalTileCount = 0;

            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            var states = dcs.States;
            var factions = dcs.Factions;

            for (int s = 0; s < states.Count; s++)
            {
                var state = states[s];
                var def = state.Definition;
                var group = new DistrictOverlayGroup
                {
                    State = state,
                    Renderers = new List<SpriteRenderer>()
                };

                for (int x = def.minX; x <= def.maxX; x++)
                {
                    for (int y = def.minY; y <= def.maxY; y++)
                    {
                        var go = new GameObject($"overlay_{x}_{y}");
                        go.transform.SetParent(_overlayRoot, false);
                        go.transform.localPosition = new Vector3(x * _tileSize, y * _tileSize, 0f);

                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = _whiteSprite;
                        sr.sortingOrder = 1;
                        sr.color = Color.clear;
                        group.Renderers.Add(sr);
                        _totalTileCount++;
                    }
                }

                _districtGroups.Add(group);
            }
        }

        /// <summary>Toggle overlay visibility. Refreshes colors when turning on.</summary>
        public void ToggleOverlay()
        {
            if (_overlayRoot == null) return;
            _visible = !_visible;
            _overlayRoot.gameObject.SetActive(_visible);
            if (_visible) RefreshColors();
            OnVisibilityChanged?.Invoke(_visible);
        }

        /// <summary>Update each district's overlay tiles to the controlling faction's color.</summary>
        public void RefreshColors()
        {
            var dcs = DistrictControlService.Instance;
            if (dcs == null) return;

            var factions = dcs.Factions;

            for (int g = 0; g < _districtGroups.Count; g++)
            {
                var group = _districtGroups[g];
                var dominant = DistrictControlService.GetDominantFaction(group.State, factions);
                Color tint = dominant != null ? dominant.color : Color.clear;

                for (int r = 0; r < group.Renderers.Count; r++)
                {
                    group.Renderers[r].color = tint;
                }
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.oKey.wasPressedThisFrame)
            {
                ToggleOverlay();
            }
        }

        private static Sprite CreateWhiteSprite(float tileSize)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            float ppu = 1f / tileSize; // 1px / tileSize = sprite renders at exactly tileSize world units
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
