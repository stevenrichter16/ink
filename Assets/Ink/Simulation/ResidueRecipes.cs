using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Holds runtime-created recipes for reaction residues.
    /// These are the visible products of ink reactions.
    /// </summary>
    public static class ResidueRecipes
    {
        private static InkRecipe _inertSludge;
        private static InkRecipe _lacquer;
        private static InkRecipe _corrosion;
        private static InkRecipe _bloom;

        /// <summary>
        /// Grey goop from Growth + Decay neutralization.
        /// Inert, doesn't spread much, slowly evaporates.
        /// </summary>
        public static InkRecipe InertSludge
        {
            get
            {
                if (_inertSludge == null)
                {
                    _inertSludge = ScriptableObject.CreateInstance<InkRecipe>();
                    _inertSludge.id = "residue_sludge";
                    _inertSludge.displayName = "Inert Sludge";
                    _inertSludge.domain = InkDomain.Binding; // inert, neutral
                    _inertSludge.carrier = InkCarrier.Gel;
                    _inertSludge.additives = InkAdditives.Thickener;
                    _inertSludge.viscosity = 3.0f;
                    _inertSludge.volatility = 0.03f;
                    _inertSludge.permanence = 0.4f;
                    _inertSludge.spreadRate = 0.2f;
                    _inertSludge.uiColor = new Color(0.4f, 0.4f, 0.35f, 1f); // grey-brown
                }
                return _inertSludge;
            }
        }

        /// <summary>
        /// Hard glossy coating from Resin + Solvent.
        /// Very permanent, doesn't spread, seals surfaces.
        /// </summary>
        public static InkRecipe Lacquer
        {
            get
            {
                if (_lacquer == null)
                {
                    _lacquer = ScriptableObject.CreateInstance<InkRecipe>();
                    _lacquer.id = "residue_lacquer";
                    _lacquer.displayName = "Lacquer";
                    _lacquer.domain = InkDomain.Binding;
                    _lacquer.carrier = InkCarrier.Gel;
                    _lacquer.additives = InkAdditives.Resin;
                    _lacquer.viscosity = 5.0f;
                    _lacquer.volatility = 0.01f;
                    _lacquer.permanence = 0.9f;
                    _lacquer.spreadRate = 0.05f;
                    _lacquer.uiColor = new Color(0.85f, 0.75f, 0.5f, 1f); // amber/honey
                }
                return _lacquer;
            }
        }

        /// <summary>
        /// Rusty corrosive residue from Binding + Decay.
        /// Spreads slowly, eats away at things.
        /// </summary>
        public static InkRecipe Corrosion
        {
            get
            {
                if (_corrosion == null)
                {
                    _corrosion = ScriptableObject.CreateInstance<InkRecipe>();
                    _corrosion.id = "residue_corrosion";
                    _corrosion.displayName = "Corrosion";
                    _corrosion.domain = InkDomain.Decay;
                    _corrosion.carrier = InkCarrier.Water;
                    _corrosion.additives = InkAdditives.Ash;
                    _corrosion.viscosity = 1.2f;
                    _corrosion.volatility = 0.08f;
                    _corrosion.permanence = 0.3f;
                    _corrosion.spreadRate = 0.4f;
                    _corrosion.uiColor = new Color(0.6f, 0.35f, 0.2f, 1f); // rust orange
                }
                return _corrosion;
            }
        }

        /// <summary>
        /// Vibrant growth burst from Growth + Energy.
        /// Spreads aggressively, high vitality.
        /// </summary>
        public static InkRecipe Bloom
        {
            get
            {
                if (_bloom == null)
                {
                    _bloom = ScriptableObject.CreateInstance<InkRecipe>();
                    _bloom.id = "residue_bloom";
                    _bloom.displayName = "Bloom";
                    _bloom.domain = InkDomain.Growth;
                    _bloom.carrier = InkCarrier.Water;
                    _bloom.additives = InkAdditives.Spores | InkAdditives.Surfactant;
                    _bloom.viscosity = 0.7f;
                    _bloom.volatility = 0.06f;
                    _bloom.permanence = 0.25f;
                    _bloom.spreadRate = 1.5f;
                    _bloom.uiColor = new Color(0.1f, 1f, 0.4f, 1f); // bright green
                }
                return _bloom;
            }
        }

        /// <summary>
        /// Get the recipe for a reaction product.
        /// </summary>
        public static InkRecipe GetRecipe(ReactionProduct product)
        {
            switch (product)
            {
                case ReactionProduct.InertSludge: return InertSludge;
                case ReactionProduct.Lacquer: return Lacquer;
                case ReactionProduct.Corrosion: return Corrosion;
                case ReactionProduct.Bloom: return Bloom;
                default: return null;
            }
        }
    }
}
