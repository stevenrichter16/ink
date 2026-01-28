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
    }
}
