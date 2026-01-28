using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Tracks player reputation per faction (in-memory).
    /// </summary>
    public static class ReputationSystem
    {
        private static readonly Dictionary<string, int> _rep = new Dictionary<string, int>();

        public static event Action<string, int> OnRepChanged;

        public static int GetRep(string factionId)
        {
            if (string.IsNullOrEmpty(factionId)) return 0;
            return _rep.TryGetValue(factionId, out var value) ? value : 0;
        }

        public static void EnsureFaction(string factionId, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(factionId)) return;
            if (!_rep.ContainsKey(factionId))
                _rep[factionId] = defaultValue;
        }

        public static void SetRep(string factionId, int value)
        {
            if (string.IsNullOrEmpty(factionId)) return;
            _rep[factionId] = value;
            OnRepChanged?.Invoke(factionId, value);
        }

        public static void AddRep(string factionId, int delta)
        {
            SetRep(factionId, GetRep(factionId) + delta);
        }
    }
}
