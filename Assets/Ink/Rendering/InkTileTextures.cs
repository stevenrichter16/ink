using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Embedded tile textures for the ink simulation.
    /// Creates procedural textures that match the visual style of the tile set.
    /// </summary>
    public static class InkTileTextures
    {
        private static Sprite _metalSprite;

        /// <summary>
        /// Get the metal tile sprite (procedural 4-panel horizontal bands).
        /// </summary>
        public static Sprite MetalSprite
        {
            get
            {
                if (_metalSprite == null)
                {
                    Texture2D tex = CreateMetalPanelTexture();
                    _metalSprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        tex.width
                    );
                }
                return _metalSprite;
            }
        }

        /// <summary>
        /// Creates a metal panel texture matching the rpgTile095 style:
        /// 4 horizontal panel bands with grooves and highlights.
        /// </summary>
        private static Texture2D CreateMetalPanelTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Colors matching the uploaded tile (gray-green metal panels)
            Color panelBase = new Color(0.58f, 0.62f, 0.60f, 1f);
            Color panelDark = new Color(0.50f, 0.54f, 0.52f, 1f);
            Color panelLight = new Color(0.68f, 0.72f, 0.70f, 1f);
            Color grooveColor = new Color(0.40f, 0.44f, 0.42f, 1f);
            Color highlightColor = new Color(0.75f, 0.78f, 0.76f, 1f);

            // 4 horizontal panel bands, each 16 pixels tall
            int bandHeight = size / 4;
            
            for (int y = 0; y < size; y++)
            {
                int yInBand = y % bandHeight;
                
                for (int x = 0; x < size; x++)
                {
                    Color c;
                    
                    // Groove at bottom of each band (dark line)
                    if (yInBand == 0)
                        c = grooveColor;
                    // Highlight at top of each band
                    else if (yInBand == bandHeight - 1)
                        c = highlightColor;
                    // Second row from bottom - slightly darker
                    else if (yInBand == 1)
                        c = panelDark;
                    // Second row from top - slightly lighter  
                    else if (yInBand == bandHeight - 2)
                        c = panelLight;
                    // Main panel body
                    else
                        c = panelBase;
                    
                    tex.SetPixel(x, y, c);
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }
    }
}
