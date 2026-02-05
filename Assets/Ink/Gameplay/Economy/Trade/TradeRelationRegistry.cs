using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// In-memory storage for faction trade relations.
    /// </summary>
    public static class TradeRelationRegistry
    {
        private static readonly Dictionary<string, FactionTradeRelation> _relations = new Dictionary<string, FactionTradeRelation>();

        private static string Key(string src, string dst)
        {
            return $"{src?.ToLowerInvariant()}->{dst?.ToLowerInvariant()}";
        }

        public static FactionTradeRelation GetRelation(string sourceFactionId, string targetFactionId)
        {
            if (string.IsNullOrEmpty(sourceFactionId) || string.IsNullOrEmpty(targetFactionId))
                return null;

            var key = Key(sourceFactionId, targetFactionId);
            if (_relations.TryGetValue(key, out var rel))
                return rel;

            // Default open relation
            rel = new FactionTradeRelation
            {
                sourceFactionId = sourceFactionId,
                targetFactionId = targetFactionId,
                status = TradeStatus.Open,
                tariffRate = 0f,
                bannedItems = new List<string>(),
                exclusiveItems = new List<string>()
            };
            _relations[key] = rel;
            return rel;
        }

        public static void SetRelation(FactionTradeRelation relation)
        {
            if (relation == null) return;
            if (string.IsNullOrEmpty(relation.sourceFactionId) || string.IsNullOrEmpty(relation.targetFactionId)) return;
            _relations[Key(relation.sourceFactionId, relation.targetFactionId)] = relation;
        }

        public static List<FactionTradeRelation> GetForFaction(string factionId)
        {
            var list = new List<FactionTradeRelation>();
            if (string.IsNullOrEmpty(factionId)) return list;
            var compare = factionId.ToLowerInvariant();

            foreach (var rel in _relations.Values)
            {
                if (rel == null) continue;
                if ((rel.sourceFactionId != null && rel.sourceFactionId.ToLowerInvariant() == compare) ||
                    (rel.targetFactionId != null && rel.targetFactionId.ToLowerInvariant() == compare))
                {
                    list.Add(rel);
                }
            }
            return list;
        }

        public static List<FactionTradeRelation> GetAll()
        {
            return new List<FactionTradeRelation>(_relations.Values);
        }

        public static void Clear()
        {
            _relations.Clear();
        }
    }
}
