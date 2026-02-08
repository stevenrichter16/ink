using NUnit.Framework;
using InkSim;

public class ProsperitySpawnTests
{
    [Test]
    public void ReinforcementCap_AtBaselineProsperity()
    {
        // Prosperity 1.0 = baseline, standard cap
        int cap = SpawnCapScaler.GetReinforcementCap(1.0f);
        Assert.AreEqual(SpawnCapScaler.BaseReinforcementCap, cap);
    }

    [Test]
    public void ReinforcementCap_HighProsperity_IncreasesMax()
    {
        // High prosperity means more defenders can be supported
        int cap = SpawnCapScaler.GetReinforcementCap(1.8f);
        Assert.Greater(cap, SpawnCapScaler.BaseReinforcementCap);
    }

    [Test]
    public void ReinforcementCap_LowProsperity_DecreasesMax()
    {
        // Low prosperity can't sustain as many troops
        int cap = SpawnCapScaler.GetReinforcementCap(0.3f);
        Assert.Less(cap, SpawnCapScaler.BaseReinforcementCap);
    }

    [Test]
    public void ReinforcementCap_NeverBelowMinimum()
    {
        int cap = SpawnCapScaler.GetReinforcementCap(0.1f);
        Assert.That(cap, Is.GreaterThanOrEqualTo(SpawnCapScaler.MinCap));
    }

    [Test]
    public void ReinforcementCap_NeverAboveMaximum()
    {
        int cap = SpawnCapScaler.GetReinforcementCap(2.0f);
        Assert.That(cap, Is.LessThanOrEqualTo(SpawnCapScaler.MaxCap));
    }

    [Test]
    public void RaidSize_ScalesWithProsperityDeficit()
    {
        // Low prosperity districts are targets for larger raids
        int raidLow = SpawnCapScaler.GetRaidSize(2, 0.3f);
        int raidHigh = SpawnCapScaler.GetRaidSize(2, 1.5f);
        Assert.GreaterOrEqual(raidLow, raidHigh);
    }

    [Test]
    public void RaidSize_NeverBelowMinimum()
    {
        int raid = SpawnCapScaler.GetRaidSize(2, 2.0f);
        Assert.That(raid, Is.GreaterThanOrEqualTo(2));
    }
}
