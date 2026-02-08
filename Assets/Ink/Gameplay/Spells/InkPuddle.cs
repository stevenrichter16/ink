using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Ink puddle - a ground hazard that deals damage over time to enemies standing in it.
    /// Fades out over its lifetime.
    /// </summary>
    public class InkPuddle : MonoBehaviour
    {
        private const int MaxPoolSize = 32;
        private static readonly Queue<InkPuddle> _pool = new Queue<InkPuddle>();
        private static readonly Dictionary<Vector2Int, InkPuddle> _byCell = new Dictionary<Vector2Int, InkPuddle>();
        private static Transform _poolRoot;

        [Header("Properties")]
        public int gridX;
        public int gridY;
        public float lifetime = 4f;
        public float tickRate = 0.6f;
        public int damagePerTick = 1;
        
        [Header("Visuals")]
        public float fadeStartPercent = 0.6f;   // Start fading at 60% lifetime
        
        // State
        private float _timer;
        private float _tickTimer;
        private SpriteRenderer _spriteRenderer;
        private float _initialAlpha;
        private bool _registered;
        private Vector2Int _cell;

        /// <summary>Entity that created this puddle (for AuthorizeFight checks).</summary>
        [System.NonSerialized]
        public GridEntity caster;
        
        void Start()
        {
            if (_spriteRenderer == null)
                SetupVisuals();
        }
        
        void Update()
        {
            _timer += Time.deltaTime;
            
            // Apply damage tick
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickRate)
            {
                _tickTimer = 0;
                ApplyDamageTick();
            }
            
            // Fade out
            UpdateFade();
            
            // Destroy when expired
            if (_timer >= lifetime)
            {
                Recycle();
            }
        }
        
private void SetupVisuals()
        {
            // Always re-fetch SpriteRenderer to handle Unity object lifecycle
            // (stale references occur when pool overflows and objects are destroyed)
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                
            _spriteRenderer.sprite = InkVisuals.CreateInkPuddleSprite();
            _spriteRenderer.sortingOrder = 1; // Just above floor
            _spriteRenderer.color = Color.white;
            
            // Random rotation for variety
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Slight random scale
            float scale = Random.Range(0.3f, 0.45f);
            transform.localScale = new Vector3(scale, scale * 0.6f, 1f); // Flatter
            
            _initialAlpha = _spriteRenderer.color.a;
        }
        
        private void ApplyDamageTick()
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;
            
            var entity = gridWorld.GetEntityAt(gridX, gridY);
            if (entity == null) return;
            
            // Hostility pipeline gate: skip allies, truce members, same faction
            if (caster != null && !HostilityPipeline.AuthorizeFight(caster, entity).authorized)
                return;

            // Damage any entity standing in puddle via CombatResolver (dodge + defense)
            entity.ReceiveHit(caster, damagePerTick, "puddle");
            InkVisuals.CreateInkSplatter(transform.position, 2, 1f);
        }
        
private void UpdateFade()
        {
            if (_spriteRenderer == null) return;  // Safety check for stale references
            
            float lifePercent = _timer / lifetime;
            
            if (lifePercent > fadeStartPercent)
            {
                // Fade from fadeStartPercent to 1.0
                float fadeProgress = (lifePercent - fadeStartPercent) / (1f - fadeStartPercent);
                float alpha = Mathf.Lerp(_initialAlpha, 0f, fadeProgress);
                
                var c = _spriteRenderer.color;
                c.a = alpha;
                _spriteRenderer.color = c;
            }
        }
        
        /// <summary>
        /// Check if an entity is standing in any ink puddle
        /// </summary>
        public static bool IsInInkPuddle(int gridX, int gridY)
        {
            return _byCell.ContainsKey(new Vector2Int(gridX, gridY));
        }
        
        /// <summary>
        /// Factory method to create a puddle at grid position
        /// </summary>
        public static InkPuddle Create(int gridX, int gridY, Vector3 worldPos, float lifetime = 4f, int damagePerTick = 1, GridEntity casterEntity = null)
        {
            Vector2Int key = new Vector2Int(gridX, gridY);
            if (_byCell.TryGetValue(key, out var existing))
            {
                existing._timer = 0f;
                existing._tickTimer = 0f;
                existing.lifetime = Mathf.Max(existing.lifetime, lifetime);
                existing.damagePerTick = damagePerTick;
                if (casterEntity != null) existing.caster = casterEntity;
                existing.SetupVisuals();
                return existing;
            }

            var puddle = GetPooled();
            puddle.gameObject.SetActive(true);
            puddle.transform.SetParent(GetPoolRoot(), false);
            puddle.transform.position = worldPos;

            puddle.gridX = gridX;
            puddle.gridY = gridY;
            puddle.lifetime = lifetime;
            puddle.damagePerTick = damagePerTick;
            puddle.caster = casterEntity;
            puddle.InitializeForSpawn();

            return puddle;
        }

        private void InitializeForSpawn()
        {
            _timer = 0f;
            _tickTimer = 0f;
            SetupVisuals();
            Register();
            Debug.Log($"[InkPuddle] Created at ({gridX}, {gridY}), lifetime={lifetime}s");
        }

        private static InkPuddle GetPooled()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var go = new GameObject("InkPuddle");
            return go.AddComponent<InkPuddle>();
        }

        private static Transform GetPoolRoot()
        {
            if (_poolRoot != null) return _poolRoot;
            _poolRoot = new GameObject("PooledInkPuddles").transform;
            return _poolRoot;
        }

        private void Register()
        {
            if (_registered)
                Unregister();

            _cell = new Vector2Int(gridX, gridY);
            _byCell[_cell] = this;
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered) return;
            _byCell.Remove(_cell);
            _registered = false;
        }

        private void Recycle()
        {
            Unregister();
            if (_pool.Count >= MaxPoolSize)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
            _pool.Enqueue(this);
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }
    }
}
