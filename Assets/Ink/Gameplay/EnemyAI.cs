using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Enemy AI - chases and attacks the player.
    /// </summary>
    public class EnemyAI : GridEntity
    {
        [Header("Enemy Stats")]
        public int currentHealth = 3;
        
        [Header("Leveling")]
        public Levelable levelable;
        
        // Computed stats from Levelable (or fallback)
        public int maxHealth => levelable != null ? levelable.MaxHp : 20;
        public int attackDamage => levelable != null ? levelable.Atk : 5;
        public int speed
        {
            get
            {
                int spd = levelable != null ? levelable.Spd : 5;
                var fm = GetComponent<FactionMember>();
                if (fm != null) spd += fm.RankSpeedBonus;
                return spd;
            }
        }

        public string lootTableId;   // Links to LootDatabase
        public string enemyId;       // ID from EnemyDatabase for XP lookup
        [Header("Defense")]
        public int baseDefense = 0;
        
        private GridEntity _lastAttacker; // Track who killed us for XP award

        [Header("AI Behavior")]
        public int aggroRange = 6;      // Distance to start chasing
        public int attackRange = 1;     // Distance to attack (1 = adjacent)
        public AIState state = AIState.Idle;

        [Header("Targeting")]
        public bool targetFactionMembers = true;
        private GridEntity _retaliationTarget;

        public enum AIState
        {
            Idle,       // Stand still
            Chase,      // Move toward player
            Attack      // Attack player
        }

        private PlayerController _player;
        private TurnManager _turnManager;

        protected override void Awake()
        {
            base.Awake();
            entityType = EntityType.Enemy;
        }

        /// <summary>
        /// Initialize health to max. Called by FactionMember after stats are finalized.
        /// </summary>
        public void InitializeHealth()
        {
            currentHealth = maxHealth;
        }

        protected override void Start()
        {
            base.Start();
            // Only initialize health if at default value (not restored from save)
            if (currentHealth <= 0 || currentHealth == 3)
                currentHealth = maxHealth;
            
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            _turnManager = TurnManager.Instance;
            
            if (_turnManager != null)
                _turnManager.RegisterEnemy(this);
        }

        /// <summary>
        /// Called by TurnManager when it's this enemy's turn.
        /// </summary>
        public override void TakeTurn()
        {
            var fm = GetComponent<FactionMember>();
            fm?.TickAlert();
            fm?.TickAggroCooldown();

            if (_world == null) return;

            GridEntity target = FindTarget();
            if (target == null)
            {
                state = AIState.Idle;
                // Idle enemies may converse with nearby faction mates
                ConversationManager.Instance?.TryInitiateConversation(this);
                return;
            }

            int distToTarget = GridWorld.Distance(gridX, gridY, target.gridX, target.gridY);

            if (distToTarget <= attackRange)
            {
                state = AIState.Attack;
            }
            else if (distToTarget <= aggroRange)
            {
                state = AIState.Chase;
            }
            else
            {
                state = AIState.Idle;
            }

            switch (state)
            {
                case AIState.Idle:
                    break;

                case AIState.Chase:
                    ChaseTarget(target);
                    break;

                case AIState.Attack:
                    Attack(target);
                    break;
            }
        }

        private void ChaseTarget(GridEntity target)
        {
            if (target == null) return;

            // Log chase behavior
            var fm = GetComponent<FactionMember>();
            string factionInfo = fm != null && fm.faction != null ? $" ({fm.faction.displayName})" : "";
            string targetName = target is PlayerController ? "Player" : target.name;
            Debug.Log($"[Chase]{factionInfo} {name} is chasing {targetName} (distance: {GridWorld.Distance(gridX, gridY, target.gridX, target.gridY)})");

            Vector2Int dir = GridWorld.DirectionToward(gridX, gridY, target.gridX, target.gridY);
            
            // Try to move in that direction
            if (!TryMove(dir))
            {
                // If blocked, try perpendicular directions
                Vector2Int alt1 = new Vector2Int(dir.y, dir.x);
                Vector2Int alt2 = new Vector2Int(-dir.y, -dir.x);
                
                if (!TryMove(alt1))
                    TryMove(alt2);
            }
        }

        /// <summary>
        /// Public wrapper for testing.
        /// </summary>
        public GridEntity FindTargetPublic() => FindTarget();

        private GridEntity FindTarget()
        {
            GridEntity bestTarget = null;
            int bestDistance = int.MaxValue;

            // Prefer explicit retaliation target if valid AND still hostile
            if (_retaliationTarget != null && _retaliationTarget.gameObject.activeInHierarchy)
            {
                // Clear retaliation if target is no longer hostile (e.g., reputation improved)
                if (!HostilityService.IsHostile(this, _retaliationTarget))
                {
                    Debug.Log($"[{name}] Clearing retaliation target - no longer hostile");
                    _retaliationTarget = null;
                }
                else
                {
                    int dist = GridWorld.Distance(gridX, gridY, _retaliationTarget.gridX, _retaliationTarget.gridY);
                    if (dist <= aggroRange)
                    {
                        bestTarget = _retaliationTarget;
                        bestDistance = dist;
                    }
                    else
                    {
                        _retaliationTarget = null;
                    }
                }
            }

            if (_player != null && _player.gameObject.activeInHierarchy && HostilityService.IsHostile(this, _player))
            {
                int detectionRange = aggroRange;
                var fm = GetComponent<FactionMember>();
                if (fm != null && fm.faction != null)
                {
                    int rep = ReputationSystem.GetRep(fm.faction.id);
                    if (rep <= HostilityService.HostileThreshold)
                        detectionRange = HostilityService.HostileAggroRadius;
                }

                int dist = GridWorld.Distance(gridX, gridY, _player.gridX, _player.gridY);
                if (dist <= detectionRange && dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTarget = _player;
                }
            }

            if (targetFactionMembers)
            {
                var members = FactionMember.ActiveMembers;
                for (int i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member == null || !member.isActiveAndEnabled) continue;

                    var entity = member.GetComponent<GridEntity>();
                    if (entity == null || entity == this || !entity.gameObject.activeInHierarchy) continue;
                    if (!HostilityService.IsHostile(this, entity)) continue;

                    int dist = GridWorld.Distance(gridX, gridY, entity.gridX, entity.gridY);
                    if (dist <= aggroRange && dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestTarget = entity;
                    }
                }
            }

            return bestTarget;
        }

        public override bool CanAttack(GridEntity target)
        {
            if (target == null) return false;
            if (target == _retaliationTarget) return true;
            return HostilityService.IsHostile(this, target);
        }

        public void SetRetaliationTarget(GridEntity target)
        {
            _retaliationTarget = target;
        }

        public override int GetDefenseValue()
        {
            int def = levelable != null ? levelable.Def : baseDefense;
            var fm = GetComponent<FactionMember>();
            if (fm != null)
                def += fm.RankDefenseBonus;
            return def;
        }

        public override void Attack(GridEntity target)
        {
            if (!CanAttack(target)) return;
            
            Debug.Log($"[{name}] Attacking {target.name} for {attackDamage} damage!");
            target.ReceiveHit(this, attackDamage, "melee");
        }

public override void ApplyDamageInternal(int amount, GridEntity attacker)
        {
            // Track attacker for XP award
            if (attacker != null)
                _lastAttacker = attacker;

            // Handle faction aggro rules when player hits
            if (attacker is PlayerController pc)
            {
                FactionCombatService.OnPlayerHit(this, pc);
            }

            // Dodge handled in CombatResolver
            int actualDamage = DamageUtils.ComputeDamageAfterDefense(amount, GetDefenseValue());

            if (actualDamage == 0)
            {
                DamageNumber.Spawn(transform.position, 0, Color.gray);
                return;
            }
            
            currentHealth -= actualDamage;
            Debug.Log($"[{name}] Took {actualDamage} damage! Health: {currentHealth}/{maxHealth}");
            
            // Spawn damage number (white for enemy damage)
            DamageNumber.Spawn(transform.position, actualDamage);
            
            // Flash red
            if (_spriteRenderer != null)
            {
                StartCoroutine(DamageFlash());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (attacker != null && !HostilityService.AreSameFaction(this, attacker) && HostilityService.ShouldRetaliate(this, attacker))
            {
                _retaliationTarget = attacker;
            }
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (_spriteRenderer != null)
                _spriteRenderer.color = original;
        }

        private void Die()
        {
            Debug.Log($"[{name}] Died!");
            
            // Award XP to killer
            AwardXpToKiller();
            
            // Notify quests of kill
            NotifyQuests();
            
            // Drop loot before cleanup
            DropLoot();

            CombatEvents.RaiseEntityKilled(this, _lastAttacker);
            
            if (_turnManager != null)
                _turnManager.UnregisterEnemy(this);

            // Clear from grid
            if (_world != null)
                _world.ClearOccupant(gridX, gridY);
            
            // Disable instead of destroy (for restart functionality)
            gameObject.SetActive(false);
        }

/// <summary>
        /// Award XP to whoever killed this enemy.
        /// </summary>
        private void AwardXpToKiller()
        {
            if (_lastAttacker == null) return;
            
            // Get XP value from database
            string id = !string.IsNullOrEmpty(enemyId) ? enemyId : lootTableId;
            var enemyData = EnemyDatabase.Get(id);
            int xp = enemyData?.xpOnKill ?? 10;
            
            // Find Levelable on the attacker
            var levelable = _lastAttacker.GetComponent<Levelable>();
            if (levelable != null)
            {
                levelable.AddXp(xp);
                Debug.Log($"[{name}] Awarded {xp} XP to {_lastAttacker.name}");
            }
        }

        private void NotifyQuests()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var questLog = player != null ? player.questLog : null;
            string id = !string.IsNullOrEmpty(enemyId) ? enemyId : lootTableId;
            questLog?.OnEnemyKilled(id);
        }


        protected override void OnDestroy()
        {
            if (_turnManager != null)
                _turnManager.UnregisterEnemy(this);
            base.OnDestroy();
        }
    

private void DropLoot()
        {
            var table = LootDatabase.Get(lootTableId);
            if (table == null) return;

            var drops = table.Roll();
            if (drops.Count == 0) return;

            float tileSize = _world?.tileSize ?? 0.16f;
            
            // Scatter positions around death location
            Vector2Int[] offsets = {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, -1)
            };

            for (int i = 0; i < drops.Count; i++)
            {
                var (itemId, qty) = drops[i];
                Vector2Int offset = offsets[i % offsets.Length];
                
                int dropX = gridX + offset.x;
                int dropY = gridY + offset.y;
                
                // Check if position is walkable
                if (_world != null && !_world.IsWalkable(dropX, dropY))
                {
                    dropX = gridX;
                    dropY = gridY;
                }
                
                ItemPickup.CreateFromLoot(itemId, dropX, dropY, qty, tileSize);
                Debug.Log($"[{name}] Dropped {qty}x {itemId} at ({dropX}, {dropY})");
            }
        }
}
}
