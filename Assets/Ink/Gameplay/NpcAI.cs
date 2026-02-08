using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// NPC AI - neutral characters that can wander or stay stationary.
    /// </summary>
    public class NpcAI : GridEntity
    {
        [Header("NPC Behavior")]
        public AIBehavior behavior = AIBehavior.Stationary;
        public float wanderChance = 0.3f;  // Chance to move each turn when wandering

        [Header("Combat")]
        public int baseAttack = 3;
        public int baseMaxHealth = 20;
        [Header("Defense")]
        public int baseDefense = 0;
        public int aggroRange = 6;
        public int attackRange = 1;
        public int pursuitRange = 10;
        public GridEntity hostileTarget;
        public int currentHealth;

        [Header("Spells")]
        public bool useFactionSpells = true;

        private SpellData _rangedSpell;
        private bool _rangedSpellChecked;

        public enum AIBehavior
        {
            Stationary,  // Never moves
            Wander,      // Random movement
            Patrol       // Follow waypoints (not implemented yet)
        }

        private TurnManager _turnManager;
        private FactionMember _factionMember;
        private Levelable _levelable;
        private GridEntity _lastAttacker;
        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            entityType = EntityType.NPC;
        }

        protected override void Start()
        {
            base.Start();
            
            _turnManager = TurnManager.Instance;
            _factionMember = GetComponent<FactionMember>();
            _levelable = GetComponent<Levelable>();
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            InitializeHealth();
            if (_turnManager != null)
                _turnManager.RegisterNPC(this);
        }

        /// <summary>
        /// Called by TurnManager when it's this NPC's turn.
        /// </summary>
        public override void TakeTurn()
        {
            var fm = GetComponent<FactionMember>();
            fm?.TickAlert();
            fm?.TickAggroCooldown();

            // Auto-aggro to player when faction rep is hostile and player is close
            if (hostileTarget == null && _player != null && _player.gameObject.activeInHierarchy && HostilityService.IsHostile(this, _player))
            {
                int detectionRange = HostilityService.HostileAggroRadius;
                int dist = GridWorld.Distance(gridX, gridY, _player.gridX, _player.gridY);
                if (dist <= detectionRange)
                {
                    hostileTarget = _player;
                    Debug.Log($"[{name}] Auto-aggro to player within {detectionRange} (rep hostile).");
                }
            }

            // Reactive aggro: if recently hit, promote attacker to hostile target
            // This ensures NPCs defend themselves even if ShouldRetaliate was false
            // (e.g. non-Aggressive faction before rep drops to hostile)
            if (hostileTarget == null && _lastAttacker != null
                && _lastAttacker.gameObject.activeInHierarchy
                && GridWorld.Distance(gridX, gridY, _lastAttacker.gridX, _lastAttacker.gridY) <= pursuitRange
                && HostilityPipeline.AuthorizeFight(this, _lastAttacker).authorized)
            {
                hostileTarget = _lastAttacker;
            }
            _lastAttacker = null; // Clear each turn regardless

            if (TryCombat())
                return;

            // Check NPC goal system before default behavior
            if (NpcGoalSystem.TryExecuteGoal(this))
                return;

            // Idle NPCs may attempt conversation with nearby entities
            if (hostileTarget == null && ConversationManager.Instance != null)
            {
                if (ConversationManager.Instance.TryInitiateConversation(this))
                    return;
            }

            switch (behavior)
            {
                case AIBehavior.Stationary:
                    // Do nothing
                    break;

                case AIBehavior.Wander:
                    if (Random.value < wanderChance)
                        WanderRandomly();
                    break;

                case AIBehavior.Patrol:
                    // Handled by NpcGoalSystem patrol goals
                    break;
            }
        }

        /// <summary>
        /// Attempt to engage hostileTarget (spell, melee, or chase).
        /// Returns true if this turn was consumed by combat.
        /// </summary>
        private bool TryCombat()
        {
            if (hostileTarget == null) return false;

            EnsureRangedSpell();
            if (!IsValidTarget(hostileTarget))
            {
                hostileTarget = null;
                return false;
            }

            int distToTarget = GridWorld.Distance(gridX, gridY, hostileTarget.gridX, hostileTarget.gridY);
            if (CanCastSpellAt(hostileTarget))
            {
                Attack(hostileTarget);
                return true;
            }

            if (distToTarget <= attackRange)
            {
                Attack(hostileTarget);
                return true;
            }

            if (distToTarget <= pursuitRange)
            {
                ChaseTarget(hostileTarget);
                return true;
            }

            hostileTarget = null;
            return false;
        }

        private void ChaseTarget(GridEntity target)
        {
            if (target == null) return;

            Vector2Int dir = GridPathfinder.GetNextStep(gridX, gridY, target.gridX, target.gridY, GridWorld.Instance);
            if (dir == Vector2Int.zero)
                dir = GridWorld.DirectionToward(gridX, gridY, target.gridX, target.gridY);

            if (!TryMove(dir))
            {
                Vector2Int alt1 = new Vector2Int(dir.y, dir.x);
                Vector2Int alt2 = new Vector2Int(-dir.y, -dir.x);

                if (!TryMove(alt1))
                    TryMove(alt2);
            }
        }

        private bool IsValidTarget(GridEntity target)
        {
            if (target == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;
            if (HostilityService.AreSameFaction(this, target)) return false;
            return true;
        }

        public int attackDamage
        {
            get
            {
                return _levelable != null ? _levelable.Atk : baseAttack;
            }
        }

        public int speed
        {
            get
            {
                int spd = _levelable != null ? _levelable.Spd : 5;
                var fm = GetComponent<FactionMember>();
                if (fm != null) spd += fm.RankSpeedBonus;
                return spd;
            }
        }


        public override int GetDefenseValue()
        {
            int def = _levelable != null ? _levelable.Def : baseDefense;
            var fm = GetComponent<FactionMember>();
            if (fm != null)
                def += fm.RankDefenseBonus;
            return def;
        }

        public int maxHealth => _levelable != null ? _levelable.MaxHp : baseMaxHealth;

        
        public override bool CanAttack(GridEntity target)
        {
            if (target == null || target == this) return false;
            return HostilityPipeline.AuthorizeFight(this, target).authorized;
        }

        public override void Attack(GridEntity target)
        {
            if (!CanAttack(target)) return;

            EnsureRangedSpell();
            if (CanCastSpellAt(target))
            {
                Debug.Log($"[{name}] Casting {_rangedSpell.spellName} at {target.name}!");
                CastSpellAt(target);
                return;
            }

            Debug.Log($"[{name}] Attacking {target.name} for {attackDamage} damage!");
            target.ReceiveHit(this, attackDamage, "melee");
        }

        private void WanderRandomly()
        {
            // Look up home district bounds (null = unrestricted, e.g. border patrols)
            DistrictDefinition homeDef = null;
            if (_factionMember != null && !string.IsNullOrEmpty(_factionMember.homeDistrictId))
            {
                var dcs = DistrictControlService.Instance;
                if (dcs != null)
                {
                    var state = dcs.GetStateById(_factionMember.homeDistrictId);
                    if (state != null)
                        homeDef = state.Definition;
                }
            }

            // Pick a random direction
            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            // Shuffle and try each direction
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = directions[i];
                directions[i] = directions[j];
                directions[j] = temp;
            }

            bool moved = false;
            bool anyFilteredByBounds = false;
            foreach (var dir in directions)
            {
                // Soft territorial bound: skip directions that leave home district
                if (homeDef != null && !homeDef.Contains(gridX + dir.x, gridY + dir.y))
                {
                    anyFilteredByBounds = true;
                    continue;
                }

                if (TryMove(dir))
                {
                    moved = true;
                    break;
                }
            }

            // Fallback: if blocked specifically by district bounds (not just occupancy),
            // allow one step of overshoot to prevent NPCs freezing on boundary tiles.
            // Next turn the main loop will pull them back inside.
            if (!moved && anyFilteredByBounds)
            {
                foreach (var dir in directions)
                {
                    if (TryMove(dir))
                        break;
                }
            }
        }

        public override void ApplyDamageInternal(int amount, GridEntity attacker)
        {
            // NPCs could react to being attacked
            Debug.Log($"[{name}] Hey! Don't hit me!");

            if (attacker != null)
                _lastAttacker = attacker;

            if (attacker is PlayerController pc)
            {
                FactionCombatService.OnPlayerHit(this, pc);
            }

            int actualDamage = amount;
            if (actualDamage <= 0)
            {
                DamageNumber.Spawn(transform.position, 0, Color.gray);
                return;
            }

            currentHealth -= actualDamage;

            DamageNumber.Spawn(transform.position, actualDamage);

            if (_spriteRenderer != null)
                StartCoroutine(DamageFlash());

            if (attacker != null && attacker != this && !HostilityService.AreSameFaction(this, attacker))
            {
                if (HostilityService.ShouldRetaliate(this, attacker))
                    hostileTarget = attacker;
            }

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            // Flee when health drops critically low
            if (currentHealth <= maxHealth * 0.35f)
            {
                NpcGoalSystem.AssignFleeGoal(this);
                hostileTarget = null; // Stop fighting, start running
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

        public void InitializeHealth()
        {
            if (_levelable == null)
                _levelable = GetComponent<Levelable>();

            if (currentHealth <= 0)
                currentHealth = maxHealth;
            else if (currentHealth > maxHealth)
                currentHealth = maxHealth;
        }

        private void Die()
        {
            Debug.Log($"[{name}] Died!");
            CombatEvents.RaiseEntityKilled(this, _lastAttacker);
            if (_turnManager != null)
                _turnManager.UnregisterNPC(this);

            if (_world != null)
                _world.ClearOccupant(gridX, gridY);

            gameObject.SetActive(false);
        }

        private void EnsureRangedSpell()
        {
            if (_rangedSpellChecked || !useFactionSpells) return;
            _rangedSpellChecked = true;

            if (_factionMember == null)
                _factionMember = GetComponent<FactionMember>();
            if (_factionMember == null || _factionMember.faction == null) return;

            var rank = _factionMember.faction.GetRank(_factionMember.rankId);
            if (rank == null || rank.defaultSpells == null) return;

            for (int i = 0; i < rank.defaultSpells.Count; i++)
            {
                var spell = rank.defaultSpells[i];
                if (spell == null) continue;
                if (spell.projectileType == ProjectileType.Fireball)
                {
                    _rangedSpell = spell;
                    break;
                }
            }
        }

        private bool CanCastSpellAt(GridEntity target)
        {
            if (_rangedSpell == null || target == null) return false;
            if (_rangedSpell.requiresLineOfSight && !SpellUtils.HasLineOfSight(gridX, gridY, target.gridX, target.gridY, _world))
                return false;
            return _rangedSpell.IsInRange(gridX, gridY, target.gridX, target.gridY);
        }

        private void CastSpellAt(GridEntity target)
        {
            if (_rangedSpell == null || _world == null || target == null) return;

            Vector2Int adjusted = SpellUtils.GetAdjustedTarget(gridX, gridY, target.gridX, target.gridY, _world);
            if (adjusted.x == gridX && adjusted.y == gridY) return;

            float tileSize = _world.tileSize;
            Vector3 startPos = new Vector3(gridX * tileSize, gridY * tileSize, 0f);
            Vector3 targetPos = new Vector3(adjusted.x * tileSize, adjusted.y * tileSize, 0f);
            Fireball.Create(_rangedSpell, startPos, targetPos, adjusted.x, adjusted.y, this);
        }

        protected override void OnDestroy()
        {
            if (_turnManager != null)
                _turnManager.UnregisterNPC(this);
            base.OnDestroy();
        }
    }
}
