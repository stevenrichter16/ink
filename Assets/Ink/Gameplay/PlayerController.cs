using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Player controller with integrated inventory and equipment.
    /// </summary>
    public class PlayerController : GridEntity
    {
        [Header("Base Stats")]
        [Header("Base Stats (fallback if no Levelable)")]
        public int baseMaxHealth = 100;
        public int baseAttack = 10;
        public int baseDefense = 5;
        
        [Header("Leveling")]
        public Levelable levelable;

        [Header("Current State")]
        public int currentHealth;

        [Header("Components")]
        public Inventory inventory;
        public Equipment equipment;
        public QuestLog questLog;

        [Header("Input")]
        public float inputCooldown = 0.01f;

        private float _lastInputTime;
        private TurnManager _turnManager;
        private Vector2 _moveInput;

        // Computed stats (base + equipment)
        public int MaxHealth => (levelable != null ? levelable.MaxHp : baseMaxHealth) + (equipment?.TotalHealthBonus ?? 0);
        public int AttackDamage => (levelable != null ? levelable.Atk : baseAttack) + (equipment?.TotalAttackBonus ?? 0);
        public int Defense => (levelable != null ? levelable.Def : baseDefense) + (equipment?.TotalDefenseBonus ?? 0);

        // Shortcuts for common items
        public int Coins => inventory?.CountItem("coin") ?? 0;
        public int Keys => inventory?.CountItem("key") ?? 0;

        protected override void Awake()
        {
            base.Awake();
            entityType = EntityType.Player;

            // Add components if not present
            inventory = GetComponent<Inventory>();
            if (inventory == null)
                inventory = gameObject.AddComponent<Inventory>();

            equipment = GetComponent<Equipment>();
            if (equipment == null)
                equipment = gameObject.AddComponent<Equipment>();

            questLog = GetComponent<QuestLog>();
            if (questLog == null)
                questLog = gameObject.AddComponent<QuestLog>();
        }

        protected override void Start()
        {
            base.Start();
            
            // Initialize database
            ItemDatabase.Initialize();
            LootDatabase.Initialize();
            
            _turnManager = FindObjectOfType<TurnManager>();
            
            // Auto-find Levelable if not assigned
            if (levelable == null)
                levelable = GetComponent<Levelable>();
            
            // Subscribe to level-up for full heal
            if (levelable != null)
                levelable.OnLevelUp += OnLevelUp;
            
            currentHealth = MaxHealth;

            // Start with armor tiers for testing
            inventory.AddItem("leather_armor", 1);
            inventory.AddItem("iron_armor", 1);
            inventory.AddItem("steel_armor", 1);

            OnCollision += HandleCollision;
        }

        protected override void Update()
        {
            base.Update();

            ReadInput();

            if (!isMoving && Time.time > _lastInputTime + inputCooldown)
            {
                if (!InventoryUI.IsOpen && !DialogueRunner.IsOpen && (_turnManager == null || _turnManager.IsPlayerTurn))
                {
                    ProcessInput();
                }
            }
        }

        private void ReadInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            _moveInput = Vector2.zero;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                _moveInput.y = 1;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                _moveInput.y = -1;

            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                _moveInput.x = -1;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                _moveInput.x = 1;
        }

        private void ProcessInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                _lastInputTime = Time.time;
                EndTurn();
                return;
            }

            Vector2Int direction = Vector2Int.zero;

            if (Mathf.Abs(_moveInput.y) > 0.1f)
                direction = _moveInput.y > 0 ? Vector2Int.up : Vector2Int.down;
            else if (Mathf.Abs(_moveInput.x) > 0.1f)
                direction = _moveInput.x > 0 ? Vector2Int.right : Vector2Int.left;

            if (direction != Vector2Int.zero)
            {
                _lastInputTime = Time.time;

                int targetX = gridX + direction.x;
                int targetY = gridY + direction.y;
                GridEntity target = _world?.GetEntityAt(targetX, targetY);

                if (target != null && HostilityService.IsHostile(this, target))
                {
                    Attack(target);
                    EndTurn();
                }
                else if (TryMove(direction))
                {
                    OnMoveComplete += OnPlayerMoveComplete;
                }
            }
        }

        private void OnPlayerMoveComplete(GridEntity entity)
        {
            OnMoveComplete -= OnPlayerMoveComplete;
            CheckForItems();
            EndTurn();
        }

        private void CheckForItems()
        {
            if (_world == null) return;

            // Check for old Item system (for compatibility)
            Item oldItem = _world.GetItemAt(gridX, gridY);
            if (oldItem != null)
            {
                oldItem.Pickup(this);
                return;
            }

            // Check for new ItemPickup system
            ItemPickup pickup = FindPickupAt(gridX, gridY);
            if (pickup != null)
            {
                pickup.Pickup(this);
            }
        }

        private ItemPickup FindPickupAt(int x, int y)
        {
            // Find ItemPickup at grid position
            foreach (var pickup in FindObjectsOfType<ItemPickup>())
            {
                if (pickup.gridX == x && pickup.gridY == y)
                    return pickup;
            }
            return null;
        }

        private void EndTurn()
        {
            if (_turnManager != null)
                _turnManager.PlayerActed();
        }

        private void HandleCollision(GridEntity self, GridEntity other)
        {
            // Combat handled in ProcessInput
        }

        public override bool CanAttack(GridEntity target)
        {
            return target != null && HostilityService.IsHostile(this, target);
        }

        public override void Attack(GridEntity target)
        {
            if (target == null) return;
            if (!HostilityService.IsHostile(this, target)) return;

            Debug.Log($"[Player] Attacking {target.name} for {AttackDamage} damage!");
            target.TakeDamage(AttackDamage, this);
        }

        /// <summary>
        /// Heal the player.
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
            Debug.Log($"[Player] Healed {amount}! Health: {currentHealth}/{MaxHealth}");
        }

/// <summary>
        /// Called when player levels up - heal to full.
        /// </summary>
        private void OnLevelUp(int newLevel)
        {
            currentHealth = MaxHealth;
            Debug.Log($"[Player] Level up! Now level {newLevel}. Full heal to {MaxHealth} HP.");
        }


        /// <summary>
        /// Use a consumable item from inventory.
        /// </summary>
        public bool UseItem(ItemInstance item)
        {
            if (item == null || !item.data.IsUsable) return false;

            // Apply effect
            if (item.data.healAmount > 0)
            {
                Heal(item.data.healAmount);
            }

            // Consume one
            inventory.RemoveItem(item.data.id, 1);
            Debug.Log($"[Player] Used {item.Name}");
            return true;
        }

        /// <summary>
        /// Use item by ID.
        /// </summary>
        public bool UseItem(string itemId)
        {
            var item = inventory.GetItem(itemId);
            return UseItem(item);
        }

public override void TakeDamage(int amount, GridEntity attacker)
        {
            // Apply defense (minimum 0 damage)
            int actualDamage = Mathf.Max(0, amount - Defense);
            
            if (actualDamage == 0)
            {
                Debug.Log($"[Player] BLOCKED! Took 0 damage");
                DamageNumber.Spawn(transform.position, 0, Color.gray);
                return;
            }
            
            currentHealth -= actualDamage;
            
            Debug.Log($"[Player] Took {actualDamage} damage! Health: {currentHealth}/{MaxHealth}");

            DamageNumber.Spawn(transform.position, actualDamage, DamageNumber.ColorPlayerHit);

            if (_spriteRenderer != null)
                StartCoroutine(DamageFlash());

            if (currentHealth <= 0)
                Die();
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            _spriteRenderer.color = original;
        }

        private void Die()
        {
            Debug.Log("[Player] Died! Game Over.");
            enabled = false;
        }

        protected override void OnDestroy()
        {
            OnCollision -= HandleCollision;
            if (levelable != null)
                levelable.OnLevelUp -= OnLevelUp;
            base.OnDestroy();
        }

        // Legacy methods for compatibility with old Item system
        public void AddCoins(int amount) => inventory?.AddItem("coin", amount);
        public void AddKeys(int amount) => inventory?.AddItem("key", amount);
        public void UpgradeAttack(int amount) { } // No longer used - equip weapons instead
    }
}
