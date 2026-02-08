using NUnit.Framework;
using InkSim;

public class InkPoolTests
{
    [Test]
    public void DefaultMaxInk_IsPositive()
    {
        Assert.Greater(InkPool.DefaultMaxInk, 0);
    }

    [Test]
    public void HasInk_TrueWhenSufficient()
    {
        Assert.IsTrue(InkPool.HasInk(50, 30));
    }

    [Test]
    public void HasInk_FalseWhenInsufficient()
    {
        Assert.IsFalse(InkPool.HasInk(10, 30));
    }

    [Test]
    public void HasInk_TrueWhenExact()
    {
        Assert.IsTrue(InkPool.HasInk(30, 30));
    }

    [Test]
    public void HasInk_TrueForZeroCost()
    {
        // Spells with 0 cost are always castable
        Assert.IsTrue(InkPool.HasInk(0, 0));
        Assert.IsTrue(InkPool.HasInk(50, 0));
    }

    [Test]
    public void SpendInk_ReducesCurrentInk()
    {
        int current = 100;
        int result = InkPool.SpendInk(current, 30);
        Assert.AreEqual(70, result);
    }

    [Test]
    public void SpendInk_ClampsAtZero()
    {
        int current = 10;
        int result = InkPool.SpendInk(current, 30);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void SpendInk_ZeroCostReturnsUnchanged()
    {
        int current = 50;
        int result = InkPool.SpendInk(current, 0);
        Assert.AreEqual(50, result);
    }

    [Test]
    public void RegenInk_IncreasesCurrentInk()
    {
        int current = 50;
        int maxInk = 100;
        int result = InkPool.RegenInk(current, maxInk, InkPool.RegenPerTurn);
        Assert.Greater(result, current);
    }

    [Test]
    public void RegenInk_ClampsAtMax()
    {
        int current = 98;
        int maxInk = 100;
        int result = InkPool.RegenInk(current, maxInk, 10);
        Assert.AreEqual(100, result);
    }

    [Test]
    public void RegenPerTurn_IsPositive()
    {
        Assert.Greater(InkPool.RegenPerTurn, 0);
    }
}
