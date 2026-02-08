using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Centralized damage math helpers.
    /// </summary>
    public static class DamageUtils
    {
        // Percent reduction per point of defense (5%).
        public const float DefenseReductionPerPoint = 0.05f;

        // Dodge caps
        public const float DodgeMin = 0.05f;
        public const float DodgeMax = 0.5f;
        public const float DodgeBias = 1.0f; // scales attacker speed contribution
        public const float MeleeDodgeMultiplier = 1.1f;      // melee is easier to dodge (higher mult = more dodge chance)
        public const float ProjectileDodgeMultiplier = 0.9f;  // projectiles are harder to dodge

        /// <summary>
        /// Compute dodge chance based on defender and attacker speed.
        /// typeHint: "melee" or "projectile" to slightly bias dodge.
        /// </summary>
        public static float ComputeDodgeChance(int defenderSpeed, int attackerSpeed, string typeHint = "melee")
        {
            float mult = typeHint == "projectile" ? ProjectileDodgeMultiplier : MeleeDodgeMultiplier;
            float denom = defenderSpeed + Mathf.Max(1, attackerSpeed) * DodgeBias;
            float chance = denom > 0 ? (defenderSpeed / denom) * mult : 0f;
            return Mathf.Clamp(chance, DodgeMin, DodgeMax);
        }

        /// <summary>
        /// Roll for dodge. Returns true if the defender dodged the attack.
        /// If dodged, it triggers defender counter-attack (if possible) and consumes attacker turn.
        /// </summary>
        public static bool TryDodge(GridEntity attacker, GridEntity defender, string typeHint = "melee")
        {
            if (defender == null) return false;

            int defenderSpd = GetSpeed(defender);
            int attackerSpd = GetSpeed(attacker);
            float chance = ComputeDodgeChance(defenderSpd, attackerSpd, typeHint);

            if (Random.value <= chance)
            {
                // Show dodge feedback
                DamageNumber.Spawn(defender.transform.position, 0, Color.cyan, true);

                // Spend attacker turn implicitly by just returning true

                // Optional: immediate counter-attack if defender can
                if (defender is EnemyAI enemy)
                {
                    var target = attacker as GridEntity;
                    if (enemy.CanAttack(target))
                        enemy.Attack(target);
                }
                else if (defender is NpcAI npc)
                {
                    var target = attacker as GridEntity;
                    if (npc.CanAttack(target))
                        npc.Attack(target);
                }
                else if (defender is PlayerController pc)
                {
                    var target = attacker as GridEntity;
                    if (pc.CanAttack(target))
                        pc.Attack(target);
                }

                return true;
            }

            return false;
        }

        private static int GetSpeed(GridEntity entity)
        {
            switch (entity)
            {
                case PlayerController pc:
                    return pc.Speed;
                case EnemyAI e:
                    return e.speed;
                case NpcAI n:
                    return n.speed;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Returns the attack damage value for any GridEntity we know about.
        /// Falls back to 0 if the entity type does not expose an attack stat.
        /// </summary>
        public static int GetAttackDamage(GridEntity attacker)
        {
            switch (attacker)
            {
                case PlayerController pc:
                    return pc.AttackDamage;
                case EnemyAI enemy:
                    return enemy.attackDamage;
                case NpcAI npc:
                    return npc.attackDamage;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Max fraction of damage that defense can absorb (80%).
        /// </summary>
        public const float MaxDefenseReduction = 0.8f;

        /// <summary>
        /// Applies percent-based defense reduction to raw damage.
        /// Capped at 80% reduction so attacks always deal at least 20% of raw damage.
        /// </summary>
        public static int ComputeDamageAfterDefense(int rawDamage, int defense, float perPoint = DefenseReductionPerPoint)
        {
            if (rawDamage <= 0) return 0;
            float reduction = Mathf.Min(MaxDefenseReduction, defense * perPoint);
            float scaled = rawDamage * (1f - reduction);
            int actual = Mathf.RoundToInt(scaled);
            return Mathf.Max(1, actual); // Always deal at least 1 damage
        }
    }
}
