using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Centralized hit resolution: dodge -> defense reduction -> apply damage.
    /// </summary>
    public static class CombatResolver
    {
        /// <returns>true if the attack was dodged (no damage applied).</returns>
        public static bool ApplyHit(GridEntity attacker, GridEntity defender, int rawDamage, string damageType = "melee")
        {
            if (defender == null) return false;

            // Dodge check first
            if (DamageUtils.TryDodge(attacker, defender, damageType))
                return true;

            int defense = defender.GetDefenseValue();
            int actual = DamageUtils.ComputeDamageAfterDefense(rawDamage, defense);

            defender.ApplyDamageInternal(actual, attacker);
            CombatFeedback.Play();
            return false;
        }
    }
}
