using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Training dummy that displays damage numbers when attacked.
    /// Does not move, retaliate, or die.
    /// </summary>
    public class AttackDummy : GridEntity
    {
        [Header("Dummy Settings")]
        public bool invincible = true;
        public int maxHealth = 9999;
        public int currentHealth = 9999;

        private SpriteRenderer _sr;

        protected override void Awake()
        {
            base.Awake();
            entityType = EntityType.Enemy; // So player can attack it
            _sr = GetComponent<SpriteRenderer>();
        }

        protected override void Start()
        {
            base.Start();
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Called when player attacks the dummy.
        /// </summary>
public override void TakeDamage(int amount, GridEntity attacker)
        {
            // Clamp damage to minimum 0
            int actualDamage = Mathf.Max(0, amount);
            
            if (actualDamage == 0)
            {
                DamageNumber.Spawn(transform.position, 0, Color.gray);
                return;
            }
            
            // Spawn damage number
            DamageNumber.Spawn(transform.position, actualDamage);
            
            // Flash red
            if (_spriteRenderer != null)
                StartCoroutine(DamageFlash());
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            Color original = _sr.color;
            _sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (_sr != null)
                _sr.color = original;
        }

        /// <summary>
        /// Dummy never moves.
        /// </summary>
        public override void TakeTurn()
        {
            // Do nothing - dummies don't act
        }
    }
}
