using NUnit.Framework;
using InkSim;

public class CombatFeedbackTests
{
    [Test]
    public void HitPauseDuration_IsPositive()
    {
        Assert.Greater(CombatFeedback.HitPauseDurationSec, 0f);
    }

    [Test]
    public void HitPauseDuration_IsSubtle_NotDisruptive()
    {
        // Hit-pause should be a quick freeze-frame, not a long stall
        Assert.That(CombatFeedback.HitPauseDurationSec, Is.LessThanOrEqualTo(0.15f));
    }

    [Test]
    public void ShakeIntensity_IsPositive()
    {
        Assert.Greater(CombatFeedback.ShakeIntensity, 0f);
    }

    [Test]
    public void ShakeIntensity_IsSubtle()
    {
        // Camera shake should be noticeable but not nauseating
        Assert.That(CombatFeedback.ShakeIntensity, Is.LessThanOrEqualTo(0.2f));
    }

    [Test]
    public void ShakeDuration_IsPositive()
    {
        Assert.Greater(CombatFeedback.ShakeDurationSec, 0f);
    }

    [Test]
    public void ShakeDuration_IsBrief()
    {
        // Shake should be a quick punch, not a prolonged rumble
        Assert.That(CombatFeedback.ShakeDurationSec, Is.LessThanOrEqualTo(0.2f));
    }
}
