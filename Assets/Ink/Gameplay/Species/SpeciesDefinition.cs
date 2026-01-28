using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Defines a species and its default faction context.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeciesDefinition", menuName = "Ink/Species Definition")]
    public class SpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "species_id";
        public string displayName = "Species";

        [Header("Defaults")]
        public FactionDefinition defaultFaction;
        public string defaultRankId = "low";
        public int baseAggression = 0;

        [Header("Visual")]
        public int spriteIndex = -1;
    }
}
