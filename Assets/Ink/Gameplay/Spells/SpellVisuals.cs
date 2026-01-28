using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Static utility class for generating procedural spell visuals.
    /// Uses texture/sprite CACHING to avoid memory leaks.
    /// </summary>
    public static class SpellVisuals
    {
        // === CACHED ASSETS (created once, reused forever) ===
        private static Texture2D _cachedFireballTex16;
        private static Texture2D _cachedFireballTex8;
        private static Sprite _cachedFireballSprite16;
        private static Sprite _cachedFireballSprite8;
        private static Material _cachedSpriteMaterial;
        
        /// <summary>
        /// Gets the shared sprite material (created once)
        /// </summary>
        public static Material GetSpriteMaterial()
        {
            if (_cachedSpriteMaterial == null)
            {
                _cachedSpriteMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            return _cachedSpriteMaterial;
        }
        
        /// <summary>
        /// Gets a cached fireball texture. Creates once, reuses forever.
        /// </summary>
        public static Texture2D GetFireballTexture(int size = 16)
        {
            if (size <= 8)
            {
                if (_cachedFireballTex8 == null)
                    _cachedFireballTex8 = CreateFireballTextureInternal(8);
                return _cachedFireballTex8;
            }
            else
            {
                if (_cachedFireballTex16 == null)
                    _cachedFireballTex16 = CreateFireballTextureInternal(16);
                return _cachedFireballTex16;
            }
        }
        
        /// <summary>
        /// Gets a cached fireball sprite. Creates once, reuses forever.
        /// </summary>
        public static Sprite GetFireballSprite(int size = 16)
        {
            if (size <= 8)
            {
                if (_cachedFireballSprite8 == null)
                    _cachedFireballSprite8 = CreateSpriteFromTexture(GetFireballTexture(8));
                return _cachedFireballSprite8;
            }
            else
            {
                if (_cachedFireballSprite16 == null)
                    _cachedFireballSprite16 = CreateSpriteFromTexture(GetFireballTexture(16));
                return _cachedFireballSprite16;
            }
        }
        
        /// <summary>
        /// Internal texture creation - only called once per size
        /// </summary>
        private static Texture2D CreateFireballTextureInternal(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = Vector2.one * (size / 2f);
            float radius = size / 2f;
            
            // Default colors (orange/yellow fire)
            Color coreColor = new Color(1f, 0.9f, 0.3f);
            Color edgeColor = new Color(1f, 0.4f, 0.1f);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    
                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        Color c = Color.Lerp(coreColor, edgeColor, dist);
                        c.a = 1f - (dist * dist);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        
        /// <summary>
        /// Creates a sprite from texture (internal use)
        /// </summary>
        private static Sprite CreateSpriteFromTexture(Texture2D texture, int pixelsPerUnit = 16)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.one * 0.5f,
                pixelsPerUnit
            );
        }
        
        // === LEGACY API (for compatibility - now uses caching internally) ===
        
        public static Texture2D CreateFireballTexture(int size = 16, Color? coreColor = null, Color? edgeColor = null)
        {
            // Ignore custom colors for caching - use standard texture
            return GetFireballTexture(size);
        }
        
        public static Sprite CreateSprite(Texture2D texture, int pixelsPerUnit = 16)
        {
            return CreateSpriteFromTexture(texture, pixelsPerUnit);
        }
        
        public static Sprite CreateFireballSprite(int size = 16, Color? coreColor = null, Color? edgeColor = null)
        {
            // Ignore custom colors for caching - use standard sprite
            return GetFireballSprite(size);
        }
        
        /// <summary>
        /// Configures a TrailRenderer for a fire trail effect.
        /// Uses shared material.
        /// </summary>
        public static void SetupFireTrail(TrailRenderer trail, Color startColor, Color endColor, float width = 0.15f, float time = 0.12f)
        {
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.numCapVertices = 2;
            trail.numCornerVertices = 2;
            
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = gradient;
            
            // Use shared material!
            trail.material = GetSpriteMaterial();
        }
        
        /// <summary>
        /// Creates impact burst - REDUCED particle count for performance
        /// </summary>
        public static void CreateImpactBurst(Vector3 position, Color color, int particleCount = 6, float speed = 3f)
        {
            // Cap particle count for performance
            particleCount = Mathf.Min(particleCount, 6);
            
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (i / (float)particleCount) * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                
                CreateImpactParticle(position, dir * speed, color);
            }
        }
        
        private static void CreateImpactParticle(Vector3 position, Vector2 velocity, Color color)
        {
            ImpactParticle.Spawn(
                "ImpactParticle",
                position,
                GetFireballSprite(8),
                color,
                velocity,
                0.3f,
                0.3f,
                15,
                true
            );
        }
    }
    
    /// <summary>
    /// Simple particle that moves and fades out
    /// </summary>
    public class ImpactParticle : MonoBehaviour
    {
        private const int MaxPoolSize = 128;
        private static readonly Queue<ImpactParticle> _pool = new Queue<ImpactParticle>();
        private static Transform _poolRoot;

        public Vector2 velocity;
        public float lifetime = 0.5f;
        public bool fadeOut = true;
        
        private float _timer;
        private SpriteRenderer _sr;
        private Color _baseColor;

public static ImpactParticle Spawn(string name, Vector3 position, Sprite sprite, Color color, Vector2 velocity, float lifetime, float scale, int sortingOrder, bool fadeOut)
        {
            if (sprite == null) return null;

            var particle = GetPooled();
            particle.gameObject.name = name;
            particle.velocity = velocity;
            particle.lifetime = lifetime;
            particle.fadeOut = fadeOut;
            particle._timer = 0f;
            particle._baseColor = color;

            particle.transform.SetParent(GetPoolRoot(), false);
            particle.transform.position = position;
            particle.transform.localScale = Vector3.one * scale;

            // Always re-fetch SpriteRenderer to handle Unity object lifecycle
            // (stale references can occur when pool overflows and objects are destroyed)
            particle._sr = particle.GetComponent<SpriteRenderer>();
            if (particle._sr == null)
                particle._sr = particle.gameObject.AddComponent<SpriteRenderer>();

            particle._sr.sprite = sprite;
            particle._sr.color = color;
            particle._sr.sortingOrder = sortingOrder;

            particle.gameObject.SetActive(true);
            return particle;
        }

        private static ImpactParticle GetPooled()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var go = new GameObject("ImpactParticle");
            return go.AddComponent<ImpactParticle>();
        }

        private static Transform GetPoolRoot()
        {
            if (_poolRoot != null) return _poolRoot;
            _poolRoot = new GameObject("PooledParticles").transform;
            return _poolRoot;
        }
        
        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
        }
        
        void Update()
        {
            _timer += Time.deltaTime;
            
            transform.position += (Vector3)velocity * Time.deltaTime;
            velocity *= 0.95f;
            
            if (fadeOut && _sr != null)
            {
                float alpha = 1f - (_timer / lifetime);
                _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            }
            
            if (_timer >= lifetime)
            {
                Recycle();
            }
        }

        private void Recycle()
        {
            if (_pool.Count >= MaxPoolSize)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
            _pool.Enqueue(this);
        }
    }
}
