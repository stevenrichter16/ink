using NUnit.Framework;
using UnityEngine;
using InkSim;

public class DodgeTests
{
    [Test]
    public void EqualSpeeds_AboutFiftyPercentBeforeCaps()
    {
        float chance = DamageUtils.ComputeDodgeChance(defenderSpeed: 10, attackerSpeed: 10, typeHint: "melee");
        // With melee multiplier 1.1, expected ~10/(10+10)=0.5 * 1.1 = 0.5 (capped at DodgeMax)
        Assert.That(chance, Is.EqualTo(DamageUtils.DodgeMax).Within(0.001f));
    }

    [Test]
    public void FasterDefender_ClampedAtMax()
    {
        float chance = DamageUtils.ComputeDodgeChance(defenderSpeed: 100, attackerSpeed: 1, typeHint: "projectile");
        // Should hit the cap (0.5)
        Assert.That(chance, Is.EqualTo(DamageUtils.DodgeMax).Within(0.0001f));
    }

    [Test]
    public void FasterAttacker_ApproachesMin()
    {
        float chance = DamageUtils.ComputeDodgeChance(defenderSpeed: 1, attackerSpeed: 100, typeHint: "melee");
        // Should be near the minimum cap (0.05)
        Assert.That(chance, Is.EqualTo(DamageUtils.DodgeMin).Within(0.0001f));
    }

    [Test]
    public void MeleeEasierToDodgeThanProjectile()
    {
        float melee = DamageUtils.ComputeDodgeChance(defenderSpeed: 10, attackerSpeed: 10, typeHint: "melee");
        float proj = DamageUtils.ComputeDodgeChance(defenderSpeed: 10, attackerSpeed: 10, typeHint: "projectile");
        // Melee multiplier (1.1) > Projectile multiplier (0.9) — melee attacks are easier to dodge
        Assert.Greater(melee, proj);
    }
}
