using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// A collection of loot entries with roll logic.
    /// </summary>
    [System.Serializable]
    public class LootTable
    {
        public string id;
        public List<LootEntry> entries = new List<LootEntry>();
        public int guaranteedDrops;  // Always drop at least this many
        public int maxDrops;         // Cap total drops

        public LootTable(string id, int guaranteedDrops = 0, int maxDrops = 3)
        {
            this.id = id;
            this.guaranteedDrops = guaranteedDrops;
            this.maxDrops = maxDrops;
        }

        /// <summary>
        /// Add an entry to this loot table.
        /// </summary>
        public LootTable Add(string itemId, float chance, int minQty = 1, int maxQty = 1)
        {
            entries.Add(new LootEntry(itemId, chance, minQty, maxQty));
            return this; // Fluent API
        }

        /// <summary>
        /// Roll this loot table and return list of (itemId, quantity) drops.
        /// </summary>
        public List<(string itemId, int quantity)> Roll()
        {
            List<(string, int)> results = new List<(string, int)>();
            List<LootEntry> successfulRolls = new List<LootEntry>();

            // Roll each entry independently
            foreach (var entry in entries)
            {
                if (Random.value <= entry.dropChance)
                {
                    successfulRolls.Add(entry);
                }
            }

            // If we didn't meet guaranteed minimum, force some drops
            if (successfulRolls.Count < guaranteedDrops && entries.Count > 0)
            {
                // Shuffle entries and pick until we meet minimum
                List<LootEntry> shuffled = new List<LootEntry>(entries);
                ShuffleList(shuffled);

                foreach (var entry in shuffled)
                {
                    if (!successfulRolls.Contains(entry))
                    {
                        successfulRolls.Add(entry);
                        if (successfulRolls.Count >= guaranteedDrops)
                            break;
                    }
                }
            }

            // Shuffle successful rolls and cap at maxDrops
            ShuffleList(successfulRolls);
            int dropCount = Mathf.Min(successfulRolls.Count, maxDrops);

            // Generate final results with random quantities
            for (int i = 0; i < dropCount; i++)
            {
                var entry = successfulRolls[i];
                int qty = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                results.Add((entry.itemId, qty));
            }

            return results;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
