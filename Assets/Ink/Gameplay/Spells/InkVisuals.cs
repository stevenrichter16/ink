using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static utility class for generating procedural ink visuals.
    /// Uses CACHING to avoid memory leaks from repeated texture creation.
    /// </summary>
    public static class InkVisuals
    {
        // === CACHED ASSETS ===
        private static Texture2D _cachedBlobTex16;
        private static Texture2D _cachedBlobTex12;
        private static Texture2D _cachedBlobTex8;
        private static Texture2D _cachedBlobTex6;
        private static Texture2D _cachedPuddleTex;
        private static Sprite _cachedBlobSprite16;
        private static Sprite _cachedBlobSprite12;
        private static Sprite _cachedBlobSprite8;
        private static Sprite _cachedBlobSprite6;
        private static Sprite _cachedPuddleSprite;
        private static Gradient _cachedStreamGradient;
        private static AnimationCurve _cachedWidthCurve;
        
        /// <summary>
        /// Gets cached ink blob texture by size
        /// </summary>
        public static Texture2D GetInkBlobTexture(int size)
        {
            switch (size)
            {
                case 6:
                    if (_cachedBlobTex6 == null) _cachedBlobTex6 = CreateInkBlobTextureInternal(6);
                    return _cachedBlobTex6;
                case 8:
                    if (_cachedBlobTex8 == null) _cachedBlobTex8 = CreateInkBlobTextureInternal(8);
                    return _cachedBlobTex8;
                case 12:
                    if (_cachedBlobTex12 == null) _cachedBlobTex12 = CreateInkBlobTextureInternal(12);
                    return _cachedBlobTex12;
                default:
                    if (_cachedBlobTex16 == null) _cachedBlobTex16 = CreateInkBlobTextureInternal(16);
                    return _cachedBlobTex16;
            }
        }
        
        /// <summary>
        /// Gets cached ink blob sprite by size
        /// </summary>
        public static Sprite GetInkBlobSprite(int size)
        {
            switch (size)
            {
                case 6:
                    if (_cachedBlobSprite6 == null) _cachedBlobSprite6 = CreateSprite(GetInkBlobTexture(6));
                    return _cachedBlobSprite6;
                case 8:
                    if (_cachedBlobSprite8 == null) _cachedBlobSprite8 = CreateSprite(GetInkBlobTexture(8));
                    return _cachedBlobSprite8;
                case 12:
                    if (_cachedBlobSprite12 == null) _cachedBlobSprite12 = CreateSprite(GetInkBlobTexture(12));
                    return _cachedBlobSprite12;
                default:
                    if (_cachedBlobSprite16 == null) _cachedBlobSprite16 = CreateSprite(GetInkBlobTexture(16));
                    return _cachedBlobSprite16;
            }
        }
        
        /// <summary>
        /// Gets cached puddle sprite
        /// </summary>
        public static Sprite GetInkPuddleSprite()
        {
            if (_cachedPuddleSprite == null)
            {
                if (_cachedPuddleTex == null)
                    _cachedPuddleTex = CreateInkPuddleTextureInternal();
                _cachedPuddleSprite = CreateSprite(_cachedPuddleTex);
            }
            return _cachedPuddleSprite;
        }
        
        /// <summary>
        /// Gets cached stream gradient
        /// </summary>
        public static Gradient GetInkStreamGradient()
        {
            if (_cachedStreamGradient == null)
            {
                _cachedStreamGradient = new Gradient();
                _cachedStreamGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.12f, 0.06f, 0.18f), 0f),
                        new GradientColorKey(new Color(0.08f, 0.04f, 0.12f), 0.4f),
                        new GradientColorKey(new Color(0.15f, 0.08f, 0.22f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.7f, 0f),
                        new GradientAlphaKey(1f, 0.3f),
                        new GradientAlphaKey(0.9f, 1f)
                    }
                );
            }
            return _cachedStreamGradient;
        }
        
        /// <summary>
        /// Gets cached width curve for stream
        /// </summary>
        public static AnimationCurve GetStreamWidthCurve()
        {
            if (_cachedWidthCurve == null)
            {
                _cachedWidthCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.3f, 0.8f),
                    new Keyframe(0.7f, 0.5f),
                    new Keyframe(1f, 0.2f)
                );
            }
            return _cachedWidthCurve;
        }
        
        // === INTERNAL TEXTURE CREATION (called once per size) ===
        
        private static Texture2D CreateInkBlobTextureInternal(int size)
        {
            float seed = 42f; // Fixed seed for consistent cached texture
            
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = Vector2.one * (size / 2f);
            float radius = size / 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    
                    // Perturb with noise for blobby shape
                    float noise = Mathf.PerlinNoise(x * 0.4f + seed, y * 0.4f + seed);
                    dist += (noise - 0.5f) * 0.4f;
                    
                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        Color inkCore = new Color(0.08f, 0.04f, 0.12f);
                        Color inkEdge = new Color(0.18f, 0.08f, 0.22f);
                        
                        Color c = Color.Lerp(inkCore, inkEdge, dist * 0.7f);
                        c.a = 1f - Mathf.Pow(dist, 3f) * 0.4f;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        
        private static Texture2D CreateInkPuddleTextureInternal()
        {
            int width = 24;
            int height = 12;
            float seed = 123f; // Fixed seed
            
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(width / 2f, height / 2f);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - center.x) / (width / 2f);
                    float dy = (y + 0.5f - center.y) / (height / 2f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    float noise = Mathf.PerlinNoise(x * 0.3f + seed, y * 0.3f + seed);
                    dist += (noise - 0.5f) * 0.35f;
                    
                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        Color inkCore = new Color(0.06f, 0.03f, 0.1f);
                        Color inkEdge = new Color(0.15f, 0.08f, 0.2f);
                        
                        Color c = Color.Lerp(inkCore, inkEdge, dist * 0.6f);
                        c.a = 0.85f - dist * 0.3f;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        
        private static Sprite CreateSprite(Texture2D texture, int pixelsPerUnit = 16)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.one * 0.5f,
                pixelsPerUnit
            );
        }
        
        // === LEGACY API (now uses caching internally) ===
        
        public static Texture2D CreateInkBlobTexture(int size = 16, float seed = -1f)
        {
            return GetInkBlobTexture(size);
        }
        
        public static Texture2D CreateInkPuddleTexture(int width = 24, int height = 12, float seed = -1f)
        {
            if (_cachedPuddleTex == null)
                _cachedPuddleTex = CreateInkPuddleTextureInternal();
            return _cachedPuddleTex;
        }
        
        public static Sprite CreateInkBlobSprite(int size = 16)
        {
            return GetInkBlobSprite(size);
        }
        
        public static Sprite CreateInkPuddleSprite()
        {
            return GetInkPuddleSprite();
        }
        
        public static Gradient CreateInkStreamGradient()
        {
            return GetInkStreamGradient();
        }
        
        public static AnimationCurve CreateStreamWidthCurve()
        {
            return GetStreamWidthCurve();
        }
        
        /// <summary>
        /// Spawn ink splatter particles - REDUCED COUNT for performance
        /// </summary>
        public static void CreateInkSplatter(Vector3 position, int count = 4, float speed = 2f)
        {
            // Cap count for performance
            count = Mathf.Min(count, 5);
            
            for (int i = 0; i < count; i++)
            {
                float scale = Random.Range(0.15f, 0.35f);
                ImpactParticle.Spawn(
                    "InkSplatter",
                    position,
                    GetInkBlobSprite(8),
                    Color.white,
                    Random.insideUnitCircle * speed,
                    Random.Range(0.25f, 0.4f),
                    scale,
                    14,
                    true
                );
            }
        }
        
        /// <summary>
        /// Spawn drip particle - uses cached sprite
        /// </summary>
        public static void CreateDrip(Vector3 position)
        {
            Vector3 dripPos = position + (Vector3)(Random.insideUnitCircle * 0.05f);
            float scale = Random.Range(0.1f, 0.2f);
            ImpactParticle.Spawn(
                "InkDrip",
                dripPos,
                GetInkBlobSprite(6),
                Color.white,
                new Vector2(Random.Range(-0.3f, 0.3f), -1f),
                0.4f,
                scale,
                13,
                true
            );
        }
    }
}
