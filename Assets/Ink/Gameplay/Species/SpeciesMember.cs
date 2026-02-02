using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Attaches species identity to an entity and provides default faction context.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpeciesMember : MonoBehaviour
    {
        public SpeciesDefinition species;

        void Awake()
        {
            EnsureDefaultFaction();
        }

        /// <summary>
        /// Ensures the species has a defaultFaction set.
        /// If null, finds or creates a faction matching the species name.
        /// </summary>
        public void EnsureDefaultFaction()
        {
            if (species == null) return;
            if (species.defaultFaction != null) return;

            // Use species displayName, fallback to id
            string factionName = !string.IsNullOrEmpty(species.displayName) 
                ? species.displayName 
                : species.id;

            species.defaultFaction = FactionRegistry.GetOrCreate(factionName);
            Debug.Log($"[SpeciesMember] Auto-assigned faction '{factionName}' to species '{species.displayName}'");
        }
    }
}
