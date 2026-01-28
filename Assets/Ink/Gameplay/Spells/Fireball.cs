using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Fireball projectile with procedural visuals.
    /// Features: gradient circle sprite, fire trail, flickering, impact burst.
    /// </summary>
    public class Fireball : Projectile
    {
        private const int MaxPoolSize = 32;
        private static readonly Queue<Fireball> _pool = new Queue<Fireball>();

        [Header("Fireball Visuals")]
        public Color coreColor = Color.yellow;
        public Color edgeColor = new Color(1f, 0.3f, 0f);
        public float baseScale = 0.3f;
        public float flickerSpeed = 15f;
        public float flickerAmount = 0.15f;
        
        private float _flickerTimer;
        private Vector3 _baseLocalScale;
        private SpriteRenderer _glowRenderer;
        private Coroutine _recycleRoutine;
        
        // Color caching to avoid sprite recreation on reuse
        private Color _lastCoreColor;
        private Color _lastEdgeColor;
        
        protected override void Awake()
        {
            base.Awake();
            SetupVisuals();
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!hasReachedTarget)
            {
                UpdateFlicker();
                RotateTowardsTarget();
            }
        }
        
private void SetupVisuals()
        {
            // Create procedural fireball sprite
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // Only recreate sprites if colors changed (avoid allocation on reuse)
            if (_spriteRenderer.sprite == null || _lastCoreColor != coreColor || _lastEdgeColor != edgeColor)
            {
                _spriteRenderer.sprite = SpellVisuals.CreateFireballSprite(16, coreColor, edgeColor);
                _lastCoreColor = coreColor;
                _lastEdgeColor = edgeColor;
                
                // Update glow with new colors
                EnsureGlowLayer();
                
                // Update trail with new colors
                if (_trailRenderer != null)
                    SpellVisuals.SetupFireTrail(_trailRenderer, coreColor, edgeColor, 0.12f, 0.1f);
            }
            
            _spriteRenderer.sortingOrder = 12;
            
            // Set base scale
            transform.localScale = Vector3.one * baseScale;
            _baseLocalScale = transform.localScale;
            
            // Add trail renderer if needed
            if (_trailRenderer == null)
            {
                _trailRenderer = gameObject.AddComponent<TrailRenderer>();
                SpellVisuals.SetupFireTrail(_trailRenderer, coreColor, edgeColor, 0.12f, 0.1f);
            }
            
            // Ensure glow exists
            if (_glowRenderer == null)
                EnsureGlowLayer();
        }
        
        private void EnsureGlowLayer()
        {
            if (_glowRenderer == null)
            {
                var glow = new GameObject("Glow");
                glow.transform.SetParent(transform);
                _glowRenderer = glow.AddComponent<SpriteRenderer>();
            }

            var glowTransform = _glowRenderer.transform;
            glowTransform.localPosition = Vector3.zero;
            glowTransform.localScale = Vector3.one * 1.8f;

            Color glowColor = edgeColor;
            glowColor.a = 0.4f;
            _glowRenderer.sprite = SpellVisuals.CreateFireballSprite(16, glowColor, glowColor);
            _glowRenderer.sortingOrder = 11;
        }
        
        private void UpdateFlicker()
        {
            _flickerTimer += Time.deltaTime * flickerSpeed;
            
            // Oscillate scale for flicker effect
            float flicker = 1f + Mathf.Sin(_flickerTimer) * flickerAmount;
            transform.localScale = _baseLocalScale * flicker;
        }
        
        private void RotateTowardsTarget()
        {
            // Slight rotation towards movement direction
            Vector3 dir = (targetPosition - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        protected override void OnImpactEffects()
        {
            // Create impact burst
            SpellVisuals.CreateImpactBurst(transform.position, edgeColor, 8, 2.5f);
            
            // Screen shake could go here
            // CameraShake.Instance?.Shake(0.1f, 0.05f);
            
            Debug.Log($"[Fireball] Impact at ({targetGridX}, {targetGridY})");
        }

        protected override void Cleanup()
        {
            if (_recycleRoutine != null)
                StopCoroutine(_recycleRoutine);

            _recycleRoutine = StartCoroutine(RecycleAfter(0.1f));
        }
        
        public override void Initialize(SpellData spellData, Vector3 start, Vector3 target, int gridX, int gridY, GridEntity casterEntity)
        {
            if (_recycleRoutine != null)
            {
                StopCoroutine(_recycleRoutine);
                _recycleRoutine = null;
            }

            hasReachedTarget = false;
            _flickerTimer = 0f;
            transform.rotation = Quaternion.identity;

            // Set colors from spell data before base init
            coreColor = spellData.secondaryColor;
            edgeColor = spellData.primaryColor;
            baseScale = spellData.projectileSize;
            
            base.Initialize(spellData, start, target, gridX, gridY, casterEntity);
            
            // Refresh visuals with spell colors
            SetupVisuals();

            if (_trailRenderer != null)
                _trailRenderer.Clear();
        }
        
        /// <summary>
        /// Factory method to create a fireball
        /// </summary>
        public static Fireball Create(SpellData spellData, Vector3 start, Vector3 target, int gridX, int gridY, GridEntity caster)
        {
            var fireball = GetPooled();
            fireball.Initialize(spellData, start, target, gridX, gridY, caster);
            return fireball;
        }

        private static Fireball GetPooled()
        {
            if (_pool.Count > 0)
            {
                var fireball = _pool.Dequeue();
                fireball.gameObject.SetActive(true);
                return fireball;
            }

            var go = new GameObject("Fireball");
            return go.AddComponent<Fireball>();
        }

        private IEnumerator RecycleAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Recycle();
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
