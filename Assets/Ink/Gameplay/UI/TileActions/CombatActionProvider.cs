using System.Collections.Generic;
using static InkSim.TileInfoPanel;

namespace InkSim
{
    /// <summary>
    /// Provides combat-related tile actions (Kill, Damage, Heal, etc.)
    /// </summary>
    public class CombatActionProvider : ITileActionProvider
    {
        public IEnumerable<TileAction> GetActions(GridWorld world)
        {
            // Kill enemy
            yield return new TileAction(
                "Kill",
                ActionCategory.Combat,
                (x, y) => {
                    var enemy = world.GetEntityAt(x, y) as EnemyAI;
                    if (enemy != null) enemy.TakeDamage(9999, null);
                },
                (x, y) => world.GetEntityAt(x, y) is EnemyAI,
                priority: 0
            );

            // Damage enemy (10)
            yield return new TileAction(
                "Damage 10",
                ActionCategory.Combat,
                (x, y) => {
                    var enemy = world.GetEntityAt(x, y) as EnemyAI;
                    if (enemy != null) enemy.TakeDamage(10, null);
                },
                (x, y) => world.GetEntityAt(x, y) is EnemyAI,
                priority: 1
            );

            // Heal enemy to full
            yield return new TileAction(
                "Heal to Full",
                ActionCategory.Combat,
                (x, y) => {
                    var enemy = world.GetEntityAt(x, y) as EnemyAI;
                    if (enemy != null) enemy.currentHealth = enemy.maxHealth;
                },
                (x, y) => {
                    var enemy = world.GetEntityAt(x, y) as EnemyAI;
                    return enemy != null && enemy.currentHealth < enemy.maxHealth;
                },
                priority: 2
            );
        }
    }
}
