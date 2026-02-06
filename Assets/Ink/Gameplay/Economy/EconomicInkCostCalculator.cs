using UnityEngine;

namespace InkSim
{
    public struct InkCostBreakdown
    {
        public int baseCost;
        public int magnitudeCost;
        public int durationCost;
        public int radiusCost;
        public float complexityMultiplier;
        public int subtotal;
        public int totalCost;
    }

    /// <summary>
    /// Computes ink costs for economic inscriptions.
    /// </summary>
    public static class EconomicInkCostCalculator
    {
        public static InkCostBreakdown CalculateTaxBreakdown(float rateDelta, int durationTurns, int radius)
        {
            return Calculate(
                Mathf.Abs(rateDelta),
                durationTurns,
                radius,
                targetsFaction: false,
                targetsItem: false,
                contradictsExisting: false);
        }

        public static InkCostBreakdown CalculateDemandBreakdown(float demandMultiplier, int durationDays)
        {
            float magnitude = Mathf.Max(0f, demandMultiplier - 1f);
            return Calculate(
                magnitude,
                durationDays,
                radius: 0,
                targetsFaction: false,
                targetsItem: true,
                contradictsExisting: false);
        }

        public static InkCostBreakdown Calculate(
            float effectMagnitude,
            int duration,
            int radius,
            bool targetsFaction,
            bool targetsItem,
            bool contradictsExisting)
        {
            InkCostBreakdown breakdown = new InkCostBreakdown();
            breakdown.baseCost = 5;
            breakdown.magnitudeCost = Mathf.RoundToInt(Mathf.Abs(effectMagnitude) * 20f);
            breakdown.durationCost = Mathf.Max(0, duration) / 2;
            breakdown.radiusCost = Mathf.Max(0, radius) * 2;

            float multiplier = 1f;
            if (targetsFaction)
                multiplier *= 1.5f;
            if (targetsItem)
                multiplier *= 1.2f;
            if (contradictsExisting)
                multiplier *= 2f;

            breakdown.complexityMultiplier = multiplier;
            breakdown.subtotal = breakdown.baseCost + breakdown.magnitudeCost + breakdown.durationCost + breakdown.radiusCost;
            breakdown.totalCost = Mathf.RoundToInt(breakdown.subtotal * breakdown.complexityMultiplier);
            return breakdown;
        }

        public static string FormatMultiline(InkCostBreakdown cost)
        {
            string multiplier = cost.complexityMultiplier > 1f
                ? $"x{cost.complexityMultiplier:0.00}"
                : "x1.00";
            return
                $"Base: {cost.baseCost}\\n" +
                $"Magnitude: +{cost.magnitudeCost}\\n" +
                $"Duration: +{cost.durationCost}\\n" +
                $"Radius: +{cost.radiusCost}\\n" +
                $"Complexity: {multiplier}\\n" +
                $"Total: {cost.totalCost}";
        }
    }
}
