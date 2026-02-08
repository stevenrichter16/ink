using UnityEngine;
using System;

namespace InkSim
{
    /// <summary>
    /// Base class for all grid-based entities (player, enemies, NPCs).
    /// Handles grid position, movement, and visual interpolation.
    /// </summary>
    public class GridEntity : MonoBehaviour
    {
        [Header("Grid Position")]
        public int gridX;
        public int gridY;

        [Header("Leash")]
        [Tooltip("Spawn position for leash calculation (enemies only).")]
        public int spawnX;
        public int spawnY;
        [Tooltip("Max distance from spawn before giving up chase. 0 = unlimited.")]
        public int leashRange = 0;

        [Header("Movement")]
        public float moveSpeed = 50f;
        public bool isMoving { get; private set; }

        [Header("Entity Type")]
        public EntityType entityType = EntityType.None;

        // Events
        public event Action<GridEntity> OnMoveComplete;
        public event Action<GridEntity, GridEntity> OnCollision;

        protected GridWorld _world;
        public GridWorld World => _world;
        protected SpriteRenderer _spriteRenderer;
        private Vector3 _targetPosition;
        private Vector3 _startPosition;
        private float _moveProgress;

        public enum EntityType
        {
            None,
            Player,
            Enemy,
            NPC,
            Item
        }

        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureSpecies();
        }

        protected virtual void Start()
        {
            _world = GridWorld.Instance;
            
            // Register at current position
            if (_world != null)
            {
                _world.SetOccupant(gridX, gridY, this);
                transform.position = _world.GridToWorld(gridX, gridY);
            }
        }

        /// <summary>
        /// Entry point for resolving an incoming hit. Handles dodge/defense in CombatResolver.
        /// </summary>
        public virtual void ReceiveHit(GridEntity attacker, int rawDamage, string damageType = "melee")
        {
            CombatResolver.ApplyHit(attacker, this, rawDamage, damageType);
        }

        /// <summary>
        /// Override to provide this entity's defense value (used by CombatResolver).
        /// </summary>
        public virtual int GetDefenseValue() => 0;

        /// <summary>
        /// Applies already-mitigated damage (after dodge/defense). Do not run dodge/defense here.
        /// </summary>
        public virtual void ApplyDamageInternal(int amount, GridEntity attacker)
        {
            // Base has no health; concrete entities override.
        }

        protected virtual void Update()
        {
            if (isMoving)
            {
                _moveProgress += Time.deltaTime * moveSpeed;
                
                if (_moveProgress >= 1f)
                {
                    _moveProgress = 1f;
                    isMoving = false;
                    transform.position = _targetPosition;
                    OnMoveComplete?.Invoke(this);
                }
                else
                {
                    transform.position = Vector3.Lerp(_startPosition, _targetPosition, _moveProgress);
                }
            }
        }

        /// <summary>
        /// Attempt to move in a direction. Returns true if successful.
        /// </summary>
        public virtual bool TryMove(Vector2Int direction)
        {
            if (isMoving) return false;
            if (direction == Vector2Int.zero) return false;
            if (_world == null) return false;

            int newX = gridX + direction.x;
            int newY = gridY + direction.y;

            // Check for entity at target
            GridEntity other = _world.GetEntityAt(newX, newY);
            if (other != null)
            {
                OnCollision?.Invoke(this, other);
                return false;
            }

            // Check if we can enter
            if (!_world.CanEnter(newX, newY))
                return false;

            // Execute move
            ExecuteMove(newX, newY);
            return true;
        }

        /// <summary>
        /// Force move to position (for initialization or teleport).
        /// NOTE: This bypasses NPC district bounds and enemy leash checks.
        /// Only use for initial placement, restart resets, or intentional teleports.
        /// </summary>
        public void SetPosition(int x, int y, bool immediate = true)
        {
            if (_world != null)
            {
                _world.ClearOccupant(gridX, gridY);
                _world.SetOccupant(x, y, this);
            }

            gridX = x;
            gridY = y;

            if (immediate && _world != null)
            {
                transform.position = _world.GridToWorld(x, y);
            }
        }

        protected void ExecuteMove(int newX, int newY)
        {
            // Update grid
            _world.ClearOccupant(gridX, gridY);
            _world.SetOccupant(newX, newY, this);

            // Update position
            gridX = newX;
            gridY = newY;

            // Start visual movement
            _startPosition = transform.position;
            _targetPosition = _world.GridToWorld(newX, newY);
            _moveProgress = 0f;
            isMoving = true;
        }

        /// <summary>
        /// Check if this entity can attack (override in subclasses).
        /// </summary>
        public virtual bool CanAttack(GridEntity target)
        {
            return false;
        }

        /// <summary>
        /// Perform attack on target (override in subclasses).
        /// </summary>
        public virtual void Attack(GridEntity target)
        {
        }

        /// <summary>
        /// Take damage (override in subclasses).
        /// </summary>
        public virtual void TakeDamage(int amount, GridEntity attacker)
        {
        }

        /// <summary>
        /// Called when it's this entity's turn (for AI).
        /// </summary>
        public virtual void TakeTurn()
        {
        }

        protected virtual void OnDestroy()
        {
            if (_world != null)
                _world.ClearOccupant(gridX, gridY);
        }

        private void EnsureSpecies()
        {
            var speciesMember = GetComponent<SpeciesMember>();
            if (speciesMember == null)
                speciesMember = gameObject.AddComponent<SpeciesMember>();

            if (speciesMember.species == null)
            {
                speciesMember.species = Resources.Load<SpeciesDefinition>("Species/DefaultSpecies");
                if (speciesMember.species == null)
                {
                    Debug.LogWarning($"[GridEntity] No SpeciesDefinition found for {name}. Assign one to SpeciesMember.");
                }
            }
        }
    }
}
