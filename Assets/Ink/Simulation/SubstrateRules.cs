namespace InkSim
{
    /// <summary>
    /// Substrate rule table (Law 3: Substrate Binding).
    /// Each substrate modifies how ink spreads, evaporates, and binds.
    /// </summary>
    public readonly struct SubstrateRules
    {
        public readonly float spreadMult;
        public readonly float evapMult;
        public readonly float bindingMult;

        public SubstrateRules(float spread, float evap, float binding)
        {
            spreadMult = spread;
            evapMult = evap;
            bindingMult = binding;
        }

        // Static lookup (no allocations, pure data)
        private static readonly SubstrateRules _stone = new SubstrateRules(0.70f, 0.60f, 1.30f);
        private static readonly SubstrateRules _soil  = new SubstrateRules(1.20f, 1.00f, 0.80f);
        private static readonly SubstrateRules _flesh = new SubstrateRules(0.90f, 0.70f, 1.00f);
        private static readonly SubstrateRules _metal = new SubstrateRules(0.50f, 1.30f, 1.50f);

        public static SubstrateRules Get(InkSubstrate substrate)
        {
            switch (substrate)
            {
                case InkSubstrate.Stone: return _stone;
                case InkSubstrate.Soil:  return _soil;
                case InkSubstrate.Flesh: return _flesh;
                case InkSubstrate.Metal: return _metal;
                default: return _soil; // fallback
            }
        }
    }
}
