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

        public static float GetDemandMultiplier(string itemId, string districtId = null)
        {
            if (string.IsNullOrEmpty(itemId)) return 1f;
            float mult = 1f;

            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events[i];
                if (ev == null) continue;
                if (!string.Equals(ev.itemId, itemId)) continue;

                if (string.IsNullOrEmpty(ev.districtId) || string.Equals(ev.districtId, districtId))
                {
                    mult *= ev.demandMultiplier <= 0f ? 1f : ev.demandMultiplier;
                }
            }

            return mult;
        }

        public static List<DemandEvent> GetAllEvents()
        {
            return new List<DemandEvent>(_events);
        }

        public static void Clear()
        {
            _events.Clear();
        }
    }
}
