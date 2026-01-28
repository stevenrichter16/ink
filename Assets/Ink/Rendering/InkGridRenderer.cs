using UnityEngine;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Renders the ink grid using actual GameObjects with SpriteRenderers.
    /// Replaces the Gizmos-based rendering for proper runtime visibility.
    /// </summary>
    public class InkGridRenderer : MonoBehaviour
    {
        [Header("Tile Sprites")]
        [Tooltip("Sprite for Stone substrate (leave null for solid color)")]
        public Sprite stoneSprite;
        [Tooltip("Sprite for Soil substrate (leave null for solid color)")]
        public Sprite soilSprite;
        [Tooltip("Sprite for Flesh substrate (leave null for solid color)")]
        public Sprite fleshSprite;
        [Tooltip("Sprite for Metal substrate")]
        public Sprite metalSprite;

        [Header("Fallback Colors (when no sprite assigned)")]
        public Color stoneColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        public Color soilColor = new Color(0.22f, 0.18f, 0.10f, 1f);
        public Color fleshColor = new Color(0.28f, 0.15f, 0.15f, 1f);
        public Color metalColor = new Color(0.18f, 0.20f, 0.24f, 1f);

        [Header("Settings")]
        public float cellSize = 0.5f;
        public int sortingOrder = 0;

        private InkGrid _grid;
        private Transform _tileParent;
        private SpriteRenderer[] _tileRenderers;
        private Sprite _whitePixel;
        private Sprite _proceduralMetal;
        private Sprite _proceduralStone;
        private Sprite _proceduralSoil;


public void Initialize(InkGrid grid, float cellSize)
        {
            _grid = grid;
            this.cellSize = cellSize;
            
            // Create fallback sprites
            _whitePixel = CreateWhitePixelSprite();
            _proceduralMetal = CreateProceduralMetalSprite();
            _proceduralStone = CreateProceduralStoneSprite();
            _proceduralSoil = CreateProceduralSoilSprite();
            
            // Create tile parent
            if (_tileParent != null) DestroyImmediate(_tileParent.gameObject);
            _tileParent = new GameObject("TileGrid").transform;
            _tileParent.SetParent(transform);
            _tileParent.localPosition = Vector3.zero;

            // Create tile renderers
            _tileRenderers = new SpriteRenderer[grid.width * grid.height];
            
            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    int idx = x + y * grid.width;
                    InkCell cell = grid.Get(x, y);

                    GameObject tileGO = new GameObject($"Tile_{x}_{y}");
                    tileGO.transform.SetParent(_tileParent);
                    tileGO.transform.localPosition = new Vector3(
                        (x + 0.5f) * cellSize,
                        (y + 0.5f) * cellSize,
                        0f
                    );
                    tileGO.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                    SpriteRenderer sr = tileGO.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = sortingOrder;
                    
                    // Assign sprite based on substrate (use procedural if none assigned)
                    sr.sprite = GetSubstrateSpriteWithFallback(cell.substrate);
                    sr.color = Color.white; // Sprites have their own colors baked in

                    _tileRenderers[idx] = sr;
                }
            }
        }

public void UpdateVisuals()
        {
            if (_grid == null || _tileRenderers == null) return;

            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    int idx = x + y * _grid.width;
                    InkCell cell = _grid.Get(x, y);
                    SpriteRenderer sr = _tileRenderers[idx];
                    if (sr == null) continue;

                    float total = cell.TotalInk();

                    // Get dominant ink color
                    Color inkColor = Color.white;
                    InkLayer dominant = InkDominance.GetDominant(cell);
                    if (dominant != null && dominant.recipe != null)
                        inkColor = dominant.recipe.uiColor;

                    // Blend based on saturation (tint the sprite)
                    float t = Mathf.Clamp01(total / Mathf.Max(0.0001f, cell.saturationLimit));
                    sr.color = Color.Lerp(Color.white, inkColor, t * 0.85f);
                }
            }
        }

        private Sprite GetSubstrateSprite(InkSubstrate substrate)
        {
            switch (substrate)
            {
                case InkSubstrate.Stone: return stoneSprite;
                case InkSubstrate.Soil: return soilSprite;
                case InkSubstrate.Flesh: return fleshSprite;
                case InkSubstrate.Metal: return metalSprite;
                default: return null;
            }
        }

        private Sprite GetSubstrateSpriteWithFallback(InkSubstrate substrate)
        {
            // First check if user assigned a sprite
            Sprite userSprite = GetSubstrateSprite(substrate);
            if (userSprite != null) return userSprite;
            
            // Otherwise use procedural fallback
            switch (substrate)
            {
                case InkSubstrate.Stone: return _proceduralStone;
                case InkSubstrate.Soil: return _proceduralSoil;
                case InkSubstrate.Metal: return _proceduralMetal;
                case InkSubstrate.Flesh: return _whitePixel; // No procedural flesh yet
                default: return _whitePixel;
            }
        }


        private Color GetSubstrateColor(InkSubstrate substrate)
        {
            switch (substrate)
            {
                case InkSubstrate.Stone: return stoneColor;
                case InkSubstrate.Soil: return soilColor;
                case InkSubstrate.Flesh: return fleshColor;
                case InkSubstrate.Metal: return metalColor;
                default: return Color.magenta;
            }
        }

private Sprite CreateWhitePixelSprite()
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>
        /// Creates a procedural metal panel texture (16x16) with horizontal bands
        /// </summary>
/// <summary>
        /// Gets the metal sprite from embedded tile data or creates a procedural fallback
        /// </summary>
private Sprite CreateProceduralMetalSprite()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Base metal color (blue-gray like the uploaded tile)
            Color baseColor = new Color(0.55f, 0.58f, 0.60f, 1f);
            Color darkLine = new Color(0.35f, 0.38f, 0.40f, 1f);
            Color lightLine = new Color(0.70f, 0.73f, 0.75f, 1f);
            Color rivetColor = new Color(0.45f, 0.48f, 0.50f, 1f);
            
            // Fill with base color
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, baseColor);
            
            // Horizontal panel lines (mimics the uploaded tile)
            int[] darkLines = { 0, 5, 10, 15 };
            int[] lightLines = { 1, 6, 11 };
            
            foreach (int y in darkLines)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, darkLine);
            
            foreach (int y in lightLines)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, lightLine);
            
            // Corner rivets
            tex.SetPixel(1, 13, rivetColor);
            tex.SetPixel(14, 13, rivetColor);
            tex.SetPixel(1, 2, rivetColor);
            tex.SetPixel(14, 2, rivetColor);
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Creates a procedural stone texture (16x16) with varied gray noise
        /// </summary>
        private Sprite CreateProceduralStoneSprite()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            System.Random rng = new System.Random(42); // Fixed seed for consistency
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = (float)rng.NextDouble() * 0.15f;
                    float v = 0.28f + noise;
                    tex.SetPixel(x, y, new Color(v * 0.95f, v * 0.95f, v, 1f));
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Creates a procedural soil texture (16x16) with brown earthy tones
        /// </summary>
        private Sprite CreateProceduralSoilSprite()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            System.Random rng = new System.Random(123);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = (float)rng.NextDouble() * 0.12f;
                    float r = 0.25f + noise;
                    float g = 0.18f + noise * 0.7f;
                    float b = 0.10f + noise * 0.4f;
                    tex.SetPixel(x, y, new Color(r, g, b, 1f));
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void OnDestroy()
        {
            if (_tileParent != null)
                DestroyImmediate(_tileParent.gameObject);
        }
    }
}
