using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Helper for creating economic inscriptions (taxes, demand spikes).
    /// </summary>
    public static class EconomicInscriptionService
    {
        private static int _nextId = 1;

        public static string InscribeTaxPolicy(string districtId, float rate, int durationTurns)
        {
            if (string.IsNullOrEmpty(districtId)) return null;

            var policy = new TaxPolicy
            {
                id = $"inscription_tax_{_nextId++}",
                type = TaxType.Sales,
                rate = rate,
                jurisdictionId = districtId,
                turnsRemaining = durationTurns,
                exemptFactions = new List<string>(),
                exemptItems = new List<string>(),
                targetItems = new List<string>()
            };

            TaxRegistry.AddPolicy(policy);
            return policy.id;
        }

        public static string InscribeDemandEvent(string itemId, float demandMultiplier, int durationDays, string districtId = null, string description = null)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            string id = $"inscription_demand_{_nextId++}";
            EconomicEventService.TriggerEvent(new DemandEvent
            {
                id = id,
                itemId = itemId,
                demandMultiplier = demandMultiplier,
                durationDays = durationDays,
                districtId = districtId,
                description = description
            });

            return id;
        }
    }
}
