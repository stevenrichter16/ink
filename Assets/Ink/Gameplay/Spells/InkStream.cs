using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace InkSim
{
    /// <summary>
    /// Ink stream spurt - a short burst projectile using LineRenderer.
    /// Extends from origin to target, deals damage on impact, leaves puddle.
    /// </summary>
    public class InkStream : MonoBehaviour
    {
        private const int MaxPoolSize = 16;
        private static readonly Queue<InkStream> _pool = new Queue<InkStream>();

        [Header("Targeting")]
        public Vector3 origin;
        public Vector3 target;
        public int targetGridX;
        public int targetGridY;
        public GridEntity caster;
        
        [Header("Stream Properties")]
        public float travelTime = 0.25f;        // Time to reach target
        public float lingerTime = 0.15f;        // Time stream stays visible after impact
        public int damage = 6;
        public float puddleChance = 0.4f;
        
        [Header("Visuals")]
        public float streamWidth = 0.15f;
        public int waveSegments = 12;
        public float waveAmplitude = 0.08f;
        public float waveSpeed = 20f;
        public float dripRate = 0.05f;          // Spawn drip every X seconds
        
        // State
        private enum Phase { Extending, Lingering, Retracting, Done }
        private Phase _phase = Phase.Extending;
        private float _timer;
        private float _dripTimer;
        private float _currentLength;           // 0 to 1, how extended the stream is
        
        // Components
        private LineRenderer _lineRenderer;
        private GameObject _tipBlob;
        private SpriteRenderer _tipRenderer;
        
        // Cached visuals to restore after pooling
        private Gradient _cachedGradient;
        private AnimationCurve _cachedWidthCurve;
        private Gradient _activeGradient;  // Current gradient (from UpdateColors)
        private float[] _originalAlphas = new float[] { 0.7f, 1f, 0.9f };  // Base alpha values
        
        public event Action<InkStream> OnImpact;
        
        void Awake()
        {
            SetupLineRenderer();
            SetupTipBlob();
        }
        
        void Update()
        {
            _timer += Time.deltaTime;
            
            switch (_phase)
            {
                case Phase.Extending:
                    UpdateExtending();
                    break;
                case Phase.Lingering:
                    UpdateLingering();
                    break;
                case Phase.Retracting:
                    UpdateRetracting();
                    break;
                case Phase.Done:
                    Recycle();
                    break;
            }
            
            // Update wavy line visual
            UpdateWavyLine();
            
            // Update tip blob position
            UpdateTipBlob();
        }
        
private void SetupLineRenderer()
        {
            if (_lineRenderer == null)
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = waveSegments;
            _lineRenderer.useWorldSpace = true;
            
            // Material
            _lineRenderer.material = SpellVisuals.GetSpriteMaterial();
            
            // Cache gradient and width curve for reuse after pooling
            if (_cachedGradient == null)
                _cachedGradient = InkVisuals.CreateInkStreamGradient();
            if (_cachedWidthCurve == null)
                _cachedWidthCurve = InkVisuals.CreateStreamWidthCurve();
            
            // Colors
            _lineRenderer.colorGradient = _cachedGradient;
            
            // Width
            _lineRenderer.widthMultiplier = streamWidth;
            _lineRenderer.widthCurve = _cachedWidthCurve;
            
            // Sorting
            _lineRenderer.sortingOrder = 11;
        }
        
        private void SetupTipBlob()
        {
            if (_tipBlob == null)
            {
                _tipBlob = new GameObject("TipBlob");
                _tipBlob.transform.SetParent(transform);
                _tipRenderer = _tipBlob.AddComponent<SpriteRenderer>();
            }
            else if (_tipRenderer == null)
            {
                _tipRenderer = _tipBlob.GetComponent<SpriteRenderer>();
            }

            _tipRenderer.sprite = InkVisuals.CreateInkBlobSprite(12);
            _tipRenderer.sortingOrder = 12;
            
            _tipBlob.transform.localScale = Vector3.one * 0.25f;
        }
        
        private void UpdateExtending()
        {
            // Extend stream from 0 to 1 over travelTime
            _currentLength = Mathf.Clamp01(_timer / travelTime);
            
            // Spawn drips along the way
            _dripTimer += Time.deltaTime;
            if (_dripTimer >= dripRate && _currentLength > 0.2f)
            {
                _dripTimer = 0;
                Vector3 dripPos = Vector3.Lerp(origin, target, _currentLength * Random.Range(0.3f, 0.9f));
                InkVisuals.CreateDrip(dripPos);
            }
            
            // Check if reached target
            if (_currentLength >= 1f)
            {
                OnReachTarget();
                _phase = Phase.Lingering;
                _timer = 0;
            }
        }
        
        private void UpdateLingering()
        {
            // Stream stays fully extended briefly
            _currentLength = 1f;
            
            if (_timer >= lingerTime)
            {
                _phase = Phase.Retracting;
                _timer = 0;
            }
        }
        
private void UpdateRetracting()
        {
            // Retract from origin toward target (stream disappears from back)
            float retractProgress = Mathf.Clamp01(_timer / (travelTime * 0.5f));
            float alphaMultiplier = 1f - retractProgress;  // Linear fade from 1 to 0
            
            // Fade out line using ORIGINAL alpha values (not current, which causes exponential decay)
            if (_activeGradient != null)
            {
                var alphaKeys = _activeGradient.alphaKeys;
                for (int i = 0; i < alphaKeys.Length && i < _originalAlphas.Length; i++)
                {
                    alphaKeys[i].alpha = _originalAlphas[i] * alphaMultiplier;
                }
                _activeGradient.SetKeys(_activeGradient.colorKeys, alphaKeys);
                _lineRenderer.colorGradient = _activeGradient;
            }
            
            // Fade tip blob
            if (_tipRenderer != null)
            {
                var c = _tipRenderer.color;
                c.a = alphaMultiplier;
                _tipRenderer.color = c;
            }
            
            if (retractProgress >= 1f)
            {
                _phase = Phase.Done;
            }
        }
        
        private void UpdateWavyLine()
        {
            if (_currentLength <= 0) return;
            
            Vector3 dir = (target - origin).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;
            float totalDist = Vector3.Distance(origin, target) * _currentLength;
            
            for (int i = 0; i < waveSegments; i++)
            {
                float t = i / (float)(waveSegments - 1);
                
                // Base position along line
                Vector3 pos = Vector3.Lerp(origin, origin + dir * totalDist, t);
                
                // Add wave displacement (tapers at both ends)
                float waveStrength = t * (1f - t) * 4f; // Parabola: 0 at ends, 1 at middle
                float wave = Mathf.Sin(t * 10f + Time.time * waveSpeed) * waveAmplitude * waveStrength;
                pos += perp * wave;
                
                _lineRenderer.SetPosition(i, pos);
            }
        }
        
        private void UpdateTipBlob()
        {
            if (_tipBlob == null) return;
            
            // Position at the tip of the stream
            Vector3 tipPos = Vector3.Lerp(origin, target, _currentLength);
            _tipBlob.transform.position = tipPos;
            
            // Slight pulsing scale
            float pulse = 1f + Mathf.Sin(Time.time * 15f) * 0.1f;
            _tipBlob.transform.localScale = Vector3.one * 0.25f * pulse;
            
            // Hide during retract
            _tipBlob.SetActive(_phase != Phase.Retracting && _phase != Phase.Done);
        }
        
        private void OnReachTarget()
        {
            Debug.Log($"[InkStream] Impact at ({targetGridX}, {targetGridY})");
            
            // Deal damage
            ApplyDamage();
            
            // Splatter effect
            InkVisuals.CreateInkSplatter(target, Random.Range(4, 8), 2.5f);
            
            // Maybe spawn puddle
            if (Random.value < puddleChance)
            {
                SpawnPuddle();
            }
            
            // Fire event
            OnImpact?.Invoke(this);
        }
        
        private void ApplyDamage()
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;
            
            var entity = gridWorld.GetEntityAt(targetGridX, targetGridY);
            if (entity == null || entity == caster) return;
            
            int raw = damage;
            int casterAtk = DamageUtils.GetAttackDamage(caster);
            if (casterAtk > 0)
                raw += casterAtk;

            CombatResolver.ApplyHit(caster, entity, raw, "projectile");
            Debug.Log($"[InkStream] Hit {entity.name} for {raw} damage");
        }
        
        private void SpawnPuddle()
        {
            InkPuddle.Create(targetGridX, targetGridY, target);
        }
        
        /// <summary>
        /// Initialize the ink stream with spell data
        /// </summary>
        public void Initialize(SpellData spellData, Vector3 startPos, Vector3 targetPos, int gridX, int gridY, GridEntity casterEntity)
        {
            origin = startPos;
            target = targetPos;
            targetGridX = gridX;
            targetGridY = gridY;
            caster = casterEntity;

            _phase = Phase.Extending;
            _timer = 0f;
            _dripTimer = 0f;
            _currentLength = 0f;
            OnImpact = null;
            transform.rotation = Quaternion.identity;
            
            damage = spellData.damage;
            travelTime = 1f / spellData.projectileSpeed * Vector3.Distance(startPos, targetPos) * 5f;
            travelTime = Mathf.Clamp(travelTime, 0.15f, 0.4f); // Keep it snappy

            ResetVisuals();
            
            // Use spell colors for visuals
            UpdateColors(spellData.primaryColor, spellData.secondaryColor);
        }
        
private void UpdateColors(Color primary, Color secondary)
        {
            // Create and store the active gradient
            _activeGradient = new Gradient();
            _activeGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(primary * 0.7f, 0f),
                    new GradientColorKey(primary * 0.5f, 0.4f),
                    new GradientColorKey(secondary, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(_originalAlphas[0], 0f),
                    new GradientAlphaKey(_originalAlphas[1], 0.3f),
                    new GradientAlphaKey(_originalAlphas[2], 1f)
                }
            );
            _lineRenderer.colorGradient = _activeGradient;
        }
        
        /// <summary>
        /// Factory method to create an ink stream
        /// </summary>
        public static InkStream Create(SpellData spellData, Vector3 start, Vector3 target, int gridX, int gridY, GridEntity caster)
        {
            var stream = GetPooled();
            stream.Initialize(spellData, start, target, gridX, gridY, caster);
            return stream;
        }

        private static InkStream GetPooled()
        {
            if (_pool.Count > 0)
            {
                var stream = _pool.Dequeue();
                stream.gameObject.SetActive(true);
                return stream;
            }

            var go = new GameObject("InkStream");
            return go.AddComponent<InkStream>();
        }

private void ResetVisuals()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = waveSegments;
                _lineRenderer.widthMultiplier = streamWidth;
                
                // Restore cached visuals (critical: gradient alpha gets destroyed during retract)
                if (_cachedWidthCurve != null)
                    _lineRenderer.widthCurve = _cachedWidthCurve;
                if (_cachedGradient != null)
                    _lineRenderer.colorGradient = _cachedGradient;
                    
                _lineRenderer.enabled = true;
            }

            if (_tipBlob != null)
            {
                _tipBlob.SetActive(true);
                _tipBlob.transform.localScale = Vector3.one * 0.25f;
                if (_tipRenderer != null)
                    _tipRenderer.color = Color.white;
            }

            ResetLine();
        }

        private void ResetLine()
        {
            if (_lineRenderer == null) return;
            for (int i = 0; i < waveSegments; i++)
                _lineRenderer.SetPosition(i, origin);
        }

        private void Recycle()
        {
            OnImpact = null;
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
