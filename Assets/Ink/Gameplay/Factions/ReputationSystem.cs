using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Tracks player reputation per faction and inter-faction reputation (in-memory).
    /// </summary>
    public static class ReputationSystem
    {
        private static readonly Dictionary<string, int> _rep = new Dictionary<string, int>();
        private static readonly Dictionary<(string src, string dst), int> _interRep = new Dictionary<(string src, string dst), int>();

        public static event Action<string, int> OnRepChanged;
        public static event Action<string, string, int> OnInterRepChanged;

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

        #region Inter-Faction Reputation

        /// <summary>Get reputation from source faction toward target faction.</summary>
        public static int GetInterRep(string srcFactionId, string dstFactionId)
        {
            if (string.IsNullOrEmpty(srcFactionId) || string.IsNullOrEmpty(dstFactionId)) return 0;
            return _interRep.TryGetValue((srcFactionId, dstFactionId), out var value) ? value : 0;
        }

        /// <summary>Set reputation from source faction toward target faction.</summary>
        public static void SetInterRep(string srcFactionId, string dstFactionId, int value)
        {
            if (string.IsNullOrEmpty(srcFactionId) || string.IsNullOrEmpty(dstFactionId)) return;
            _interRep[(srcFactionId, dstFactionId)] = value;
            OnInterRepChanged?.Invoke(srcFactionId, dstFactionId, value);
        }

        /// <summary>Add delta to reputation from source faction toward target faction.</summary>
        public static void AddInterRep(string srcFactionId, string dstFactionId, int delta)
        {
            SetInterRep(srcFactionId, dstFactionId, GetInterRep(srcFactionId, dstFactionId) + delta);
        }

        #endregion

        /// <summary>Test helper to clear all reputation data and listeners (editor/tests only).</summary>
        public static void ClearForTests()
        {
#if UNITY_EDITOR
            _rep.Clear();
            _interRep.Clear();
            OnRepChanged = null;
            OnInterRepChanged = null;
#endif
        }
    }
}