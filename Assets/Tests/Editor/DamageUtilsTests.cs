using NUnit.Framework;
using InkSim;

public class DamageUtilsTests
{
    [Test]
    public void NoDefense_ReturnsRawDamage()
    {
        int actual = DamageUtils.ComputeDamageAfterDefense(20, 0);
        Assert.AreEqual(20, actual);
    }

    [Test]
    public void LowDefense_ReducesDamageByPercent()
    {
        // 20 damage, 2 DEF, 5% each → 20 * (1 - 0.10) = 18
        int actual = DamageUtils.ComputeDamageAfterDefense(20, 2);
        Assert.AreEqual(18, actual);
    }

    [Test]
    public void HighDefense_ClampsAtZero()
    {
        // 10 damage, 30 DEF → reduction > 100%, expect 0
        int actual = DamageUtils.ComputeDamageAfterDefense(10, 30);
        Assert.AreEqual(0, actual);
    }

    [Test]
    public void CustomPerPoint_UsesProvidedValue()
    {
        // 10 damage, 4 DEF, 10% each → 10 * (1 - 0.4) = 6
        int actual = DamageUtils.ComputeDamageAfterDefense(10, 4, 0.10f);
        Assert.AreEqual(6, actual);
    }

    [Test]
    public void NonPositiveRawDamage_ReturnsZero()
    {
        Assert.AreEqual(0, DamageUtils.ComputeDamageAfterDefense(0, 5));
        Assert.AreEqual(0, DamageUtils.ComputeDamageAfterDefense(-5, 5));
    }

    [Test]
    public void SpellDamage_IncludesCasterAttack()
    {
        // Spell base 5 + caster attack 7 = 12 raw; DEF 2 → 10% reduction → 10.8 rounds to 11
        int spellBase = 5;
        int casterAttack = 7;
        int raw = spellBase + casterAttack;
        int actual = DamageUtils.ComputeDamageAfterDefense(raw, 2);
        Assert.AreEqual(11, actual);
    }

    [Test]
    public void MeleeDamage_CombinesAttackStatAndWeaponBonus()
    {
        // Player attack stat 8 + weapon bonus 5 = 13 raw; DEF 3 → 15% reduction → 11.05 rounds to 11
        int attackerAttackStat = 8;
        int weaponAttackBonus = 5;
        int raw = attackerAttackStat + weaponAttackBonus;
        int actual = DamageUtils.ComputeDamageAfterDefense(raw, 3);
        Assert.AreEqual(11, actual);
    }
}
