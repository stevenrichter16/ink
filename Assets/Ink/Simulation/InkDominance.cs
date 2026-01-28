namespace InkSim
{
    /// <summary>
    /// Determines which layer "controls meaning" on a tile.
    /// Used for rendering, interactions, and gameplay logic.
    /// </summary>
    public static class InkDominance
    {
        /// <summary>
        /// Calculate dominance score for a layer on a given substrate.
        /// Higher score = more dominant.
        /// </summary>
        public static float Score(InkLayer layer, InkSubstrate substrate)
        {
            if (layer == null || layer.recipe == null || layer.amount <= 0.0001f)
                return 0f;

            SubstrateRules rules = SubstrateRules.Get(substrate);
            
            // Base score: amount × permanence × substrate binding
            float score = layer.amount * layer.recipe.permanence * rules.bindingMult;
            
            // Age bonus: established ink gains slight dominance (capped)
            float ageBonus = 1f + System.Math.Min(layer.ageTurns * 0.02f, 0.5f);
            score *= ageBonus;

            // Resin additive boosts dominance (it's sticky!)
            if ((layer.recipe.additives & InkAdditives.Resin) != 0)
                score *= 1.15f;

            // BoneDust additive boosts dominance on Flesh substrate
            if ((layer.recipe.additives & InkAdditives.BoneDust) != 0 && substrate == InkSubstrate.Flesh)
                score *= 1.25f;

            // MetalFilings boost dominance on Metal substrate
            if ((layer.recipe.additives & InkAdditives.MetalFilings) != 0 && substrate == InkSubstrate.Metal)
                score *= 1.25f;

            return score;
        }

        /// <summary>
        /// Find the dominant layer in a cell. Returns null if no layers or all empty.
        /// </summary>
        public static InkLayer GetDominant(InkCell cell)
        {
            if (cell == null || cell.layers.Count == 0)
                return null;

            InkLayer dominant = null;
            float bestScore = 0f;
            int bestAge = -1;

            for (int i = 0; i < cell.layers.Count; i++)
            {
                InkLayer layer = cell.layers[i];
                float score = Score(layer, cell.substrate);

                if (score > bestScore || (score == bestScore && layer.ageTurns > bestAge))
                {
                    dominant = layer;
                    bestScore = score;
                    bestAge = layer.ageTurns;
                }
            }

            return dominant;
        }

        /// <summary>
        /// Get the dominant domain for a cell (for gameplay purposes).
        /// Returns null domain info if no dominant layer.
        /// </summary>
        public static InkDomain? GetDominantDomain(InkCell cell)
        {
            InkLayer dom = GetDominant(cell);
            return dom?.recipe?.domain;
        }
    }
}
