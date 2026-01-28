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
        /// Applies percent-based defense reduction to raw damage.
        /// No soft caps; result is clamped at a minimum of 0.
        /// </summary>
        public static int ComputeDamageAfterDefense(int rawDamage, int defense, float perPoint = DefenseReductionPerPoint)
        {
            if (rawDamage <= 0) return 0;
            float reduction = defense * perPoint;
            float scaled = rawDamage * (1f - reduction);
            int actual = Mathf.RoundToInt(scaled);
            return Mathf.Max(0, actual);
        }
    }
}
