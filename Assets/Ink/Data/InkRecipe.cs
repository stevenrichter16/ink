using UnityEngine;

namespace InkSim
{
    [CreateAssetMenu(menuName = "Ink/Ink Recipe", fileName = "InkRecipe")]
    public class InkRecipe : ScriptableObject
    {
        [Header("Identity")]
        public string id = "ink";
        public string displayName = "Ink";

        [Header("Core")]
        public InkDomain domain = InkDomain.Binding;
        public InkCarrier carrier = InkCarrier.Water;
        public InkAdditives additives = InkAdditives.None;

        [Header("Physics")]
        [Range(0.05f, 5f)] public float viscosity = 1f;    // higher = slower spread
        [Range(0f, 1f)]   public float volatility = 0.1f;  // higher = evaporates faster
        [Range(0f, 1f)]   public float permanence = 0.2f;  // later: binding/lasting
        [Range(0f, 2f)]   public float spreadRate = 1f;    // base per-turn spread

        [Header("Tuning")]
        [Range(0f, 2f)]   public float reactionStrength = 1f;
        public Color uiColor = Color.white;
    }
}
