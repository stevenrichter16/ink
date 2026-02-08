using NUnit.Framework;
using UnityEngine;
using InkSim;

public class EnemyRangedTests
{
    [Test]
    public void EnemyAttackRange_DefaultIsMelee()
    {
        // Verify default attack range is 1 (melee)
        var go = new GameObject("TestEnemy");
        var enemy = go.AddComponent<EnemyAI>();
        Assert.AreEqual(1, enemy.attackRange);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShouldUseRanged_TrueWhenHasSpellAndOutOfMelee()
    {
        // When an enemy has a ranged spell and target is beyond melee but in spell range,
        // it should prefer ranged attack
        Assert.IsTrue(EnemyRangedHelper.ShouldUseRanged(
            hasSpell: true, distToTarget: 4, attackRange: 1, spellRange: 8));
    }

    [Test]
    public void ShouldUseRanged_FalseWhenInMelee()
    {
        // When target is adjacent, prefer melee over spell
        Assert.IsFalse(EnemyRangedHelper.ShouldUseRanged(
            hasSpell: true, distToTarget: 1, attackRange: 1, spellRange: 8));
    }

    [Test]
    public void ShouldUseRanged_FalseWhenNoSpell()
    {
        Assert.IsFalse(EnemyRangedHelper.ShouldUseRanged(
            hasSpell: false, distToTarget: 4, attackRange: 1, spellRange: 8));
    }

    [Test]
    public void ShouldUseRanged_FalseWhenOutOfSpellRange()
    {
        Assert.IsFalse(EnemyRangedHelper.ShouldUseRanged(
            hasSpell: true, distToTarget: 12, attackRange: 1, spellRange: 8));
    }
}
