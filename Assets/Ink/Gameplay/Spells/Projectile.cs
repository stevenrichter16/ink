using UnityEngine;
using System;

namespace InkSim
{
    /// <summary>
    /// Base class for spell projectiles.
    /// Handles movement to target and damage application.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Target")]
        public Vector3 targetPosition;
        public int targetGridX;
        public int targetGridY;
        
        [Header("Properties")]
        public float speed = 10f;
        public int damage = 5;
        public float impactRadius = 0f;
        public GridEntity caster; // Who fired this projectile
        
        [Header("State")]
        public bool hasReachedTarget = false;
        
        public event Action<Projectile> OnImpact;
        
        protected SpriteRenderer _spriteRenderer;
        protected TrailRenderer _trailRenderer;
        
        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _trailRenderer = GetComponent<TrailRenderer>();
        }
        
        protected virtual void Update()
        {
            if (hasReachedTarget) return;
            
            MoveTowardsTarget();
        }
        
        protected virtual void MoveTowardsTarget()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distanceThisFrame = speed * Time.deltaTime;
            float remainingDistance = Vector3.Distance(transform.position, targetPosition);
            
            if (distanceThisFrame >= remainingDistance)
            {
                // Reached target
                transform.position = targetPosition;
                ReachTarget();
            }
            else
            {
                transform.position += direction * distanceThisFrame;
            }
        }
        
        protected virtual void ReachTarget()
        {
            hasReachedTarget = true;
            
            // Apply damage
            ApplyDamage();
            
            // Fire event
            OnImpact?.Invoke(this);
            
            // Impact effects (override in subclass)
            OnImpactEffects();
            
            // Cleanup
            Cleanup();
        }

        protected virtual void Cleanup()
        {
            Destroy(gameObject, 0.1f);
        }
        
        protected virtual void ApplyDamage()
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;
            
            if (impactRadius <= 0)
            {
                // Single target
                DamageAtTile(targetGridX, targetGridY);
            }
            else
            {
                // AoE damage
                int radius = Mathf.CeilToInt(impactRadius);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            DamageAtTile(targetGridX + dx, targetGridY + dy);
                        }
                    }
                }
            }
        }
        
        protected virtual void DamageAtTile(int x, int y)
        {
            var gridWorld = GridWorld.Instance;
            if (gridWorld == null) return;
            
            // Bounds check
            if (x < 0 || x >= gridWorld.width || y < 0 || y >= gridWorld.height) return;
            
            var occupant = gridWorld.GetEntityAt(x, y);
            if (occupant == null) return;

            // Don't damage self
            if (occupant == caster) return;
            
            // Damage any entity (spells can hit neutral/friendly targets)
            occupant.TakeDamage(damage, caster);
            Debug.Log($"[Projectile] Hit {occupant.name} for {damage} damage");
        }
        
        /// <summary>
        /// Override in subclasses for custom impact effects
        /// </summary>
        protected virtual void OnImpactEffects()
        {
            // Base implementation does nothing
        }
        
        /// <summary>
        /// Initialize projectile with spell data
        /// </summary>
        public virtual void Initialize(SpellData spellData, Vector3 start, Vector3 target, int gridX, int gridY, GridEntity casterEntity)
        {
            transform.position = start;
            targetPosition = target;
            targetGridX = gridX;
            targetGridY = gridY;
            speed = spellData.projectileSpeed;
            damage = spellData.damage;
            impactRadius = spellData.impactRadius;
            caster = casterEntity;
        }
    }
}
