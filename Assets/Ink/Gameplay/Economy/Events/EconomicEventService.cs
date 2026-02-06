using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Simple runtime service for demand events.
    /// </summary>
    public static class EconomicEventService
    {
        private static readonly List<DemandEvent> _events = new List<DemandEvent>();

        public static void TriggerEvent(DemandEvent ev)
        {
            if (ev == null) return;
            _events.Add(ev);
        }

        public static void TickDay()
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                var ev = _events[i];
                if (ev == null)
                {
                    _events.RemoveAt(i);
                    continue;
                }
                if (ev.durationDays > 0)
                {
                    ev.durationDays--;
                    if (ev.durationDays == 0)
                        _events.RemoveAt(i);
                }
            }
        }

        /// <summary>Maximum combined demand multiplier to prevent exponential price spikes.</summary>
        private const float MaxDemandMultiplier = 5f;

        public static float GetDemandMultiplier(string itemId, string districtId = null)
        {
            if (string.IsNullOrEmpty(itemId)) return 1f;

            // Additive stacking: each event contributes (multiplier - 1) to avoid
            // exponential blowup. Two 2x events = 3x (not 4x).
            float bonus = 0f;

            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events[i];
                if (ev == null) continue;
                if (!string.Equals(ev.itemId, itemId)) continue;

                if (string.IsNullOrEmpty(ev.districtId) || string.Equals(ev.districtId, districtId))
                {
                    float m = ev.demandMultiplier <= 0f ? 1f : ev.demandMultiplier;
                    bonus += (m - 1f);
                }
            }

            // Clamp to [1/MaxDemandMultiplier, MaxDemandMultiplier] so demand events
            // never drive prices to zero or infinity.
            float result = 1f + bonus;
            return result < (1f / MaxDemandMultiplier) ? (1f / MaxDemandMultiplier)
                 : result > MaxDemandMultiplier ? MaxDemandMultiplier
                 : result;
        }

        public static List<DemandEvent> GetAllEvents()
        {
            return new List<DemandEvent>(_events);
        }

        public static bool RemoveEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            int removed = _events.RemoveAll(e => e != null && e.id == eventId);
            return removed > 0;
        }

        public static void Clear()
        {
            _events.Clear();
        }
    }
}
