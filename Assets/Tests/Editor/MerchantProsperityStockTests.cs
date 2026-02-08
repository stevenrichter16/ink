using NUnit.Framework;
using InkSim;

public class MerchantProsperityStockTests
{
    [Test]
    public void StockMultiplier_AtBaselineProsperity_IsOne()
    {
        // Prosperity 1.0 = baseline, no stock change
        float mult = MerchantStockScaler.GetStockMultiplier(1.0f);
        Assert.That(mult, Is.EqualTo(1.0f).Within(0.01f));
    }

    [Test]
    public void StockMultiplier_HighProsperity_IncreasesStock()
    {
        // High prosperity should give more stock
        float mult = MerchantStockScaler.GetStockMultiplier(1.8f);
        Assert.Greater(mult, 1.0f);
    }

    [Test]
    public void StockMultiplier_LowProsperity_DecreasesStock()
    {
        // Low prosperity should reduce stock
        float mult = MerchantStockScaler.GetStockMultiplier(0.3f);
        Assert.Less(mult, 1.0f);
    }

    [Test]
    public void StockMultiplier_NeverBelowMinimum()
    {
        // Even at worst prosperity, merchant has at least minimum stock
        float mult = MerchantStockScaler.GetStockMultiplier(0.1f);
        Assert.That(mult, Is.GreaterThanOrEqualTo(MerchantStockScaler.MinStockMultiplier));
    }

    [Test]
    public void StockMultiplier_NeverAboveMaximum()
    {
        float mult = MerchantStockScaler.GetStockMultiplier(2.0f);
        Assert.That(mult, Is.LessThanOrEqualTo(MerchantStockScaler.MaxStockMultiplier));
    }

    [Test]
    public void ScaleQuantity_AtBaseline_ReturnsOriginal()
    {
        int scaled = MerchantStockScaler.ScaleQuantity(5, 1.0f);
        Assert.AreEqual(5, scaled);
    }

    [Test]
    public void ScaleQuantity_HighProsperity_ReturnsMore()
    {
        int scaled = MerchantStockScaler.ScaleQuantity(5, 1.8f);
        Assert.Greater(scaled, 5);
    }

    [Test]
    public void ScaleQuantity_LowProsperity_ReturnsLess()
    {
        int scaled = MerchantStockScaler.ScaleQuantity(10, 0.3f);
        Assert.Less(scaled, 10);
    }

    [Test]
    public void ScaleQuantity_AlwaysAtLeastOne()
    {
        // Even at terrible prosperity, at least 1 item is available
        int scaled = MerchantStockScaler.ScaleQuantity(1, 0.1f);
        Assert.That(scaled, Is.GreaterThanOrEqualTo(1));
    }
}
