using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Result of a reaction between two ink layers.
    /// </summary>
    public readonly struct ReactionResult
    {
        public readonly float consumeA;     // fraction of layer A consumed (0-1)
        public readonly float consumeB;     // fraction of layer B consumed (0-1)
        public readonly ReactionProduct product;
        public readonly float productAmount;

        public ReactionResult(float consumeA, float consumeB, ReactionProduct product, float productAmount)
        {
            this.consumeA = consumeA;
            this.consumeB = consumeB;
            this.product = product;
            this.productAmount = productAmount;
        }

        public static readonly ReactionResult None = new ReactionResult(0f, 0f, ReactionProduct.None, 0f);
    }

    public enum ReactionProduct
    {
        None,
        InertSludge,    // Growth + Decay → neutral residue
        Lacquer,        // Resin + Solvent → permanent, non-spreading
        Bloom,          // Growth + Energy → amplified growth
        Corrosion,      // Decay spread when binding weakened
        Activation      // Memory triggered by energy
    }

    /// <summary>
    /// Static reaction table: domain pairs + additive interactions.
    /// Deterministic: given same inputs, always same outputs.
    /// </summary>
    public static class InkReactionTable
    {
        // Domain pair key: smaller enum value first for canonical ordering
        private static (InkDomain, InkDomain) Key(InkDomain a, InkDomain b)
        {
            return (a <= b) ? (a, b) : (b, a);
        }

        /// <summary>
        /// Evaluate reaction between two layers. Returns consumption and product.
        /// Catalyst additive reduces consumption cost.
        /// </summary>
public static ReactionResult Evaluate(InkLayer layerA, InkLayer layerB, InkSubstrate substrate)
        {
            if (layerA?.recipe == null || layerB?.recipe == null) return ReactionResult.None;
            if (layerA.recipe == layerB.recipe) return ReactionResult.None; // same recipe, no reaction

            InkRecipe rA = layerA.recipe;
            InkRecipe rB = layerB.recipe;

            // Check for additive-triggered reactions first (carrier + additive combos)
            ReactionResult additiveReaction = CheckAdditiveReactions(rA, rB);
            if (additiveReaction.product != ReactionProduct.None) return additiveReaction;

            // Domain pair reactions
            // Key() returns (smaller, larger) by enum value
            // Binding=0, Growth=1, Memory=2, Decay=3, Energy=4
            var key = Key(rA.domain, rB.domain);
            
            // Catalyst present = reactions are MORE vigorous (consume more, produce more)
            bool hasCatalyst = ((rA.additives | rB.additives) & InkAdditives.Catalyst) != 0;
            float reactMult = hasCatalyst ? 1.5f : 1.0f;

            // Binding(0) + Decay(3) → Corrosion
            if (key == (InkDomain.Binding, InkDomain.Decay))
            {
                return new ReactionResult(0.30f * reactMult, 0.20f * reactMult, ReactionProduct.Corrosion, 0.15f);
            }
            // Binding(0) + Growth(1) → constrained growth (no visible product)
            if (key == (InkDomain.Binding, InkDomain.Growth))
            {
                return new ReactionResult(0.10f * reactMult, 0.10f * reactMult, ReactionProduct.None, 0f);
            }
            // Growth(1) + Decay(3) → Inert Sludge (mutual annihilation)
            if (key == (InkDomain.Growth, InkDomain.Decay))
            {
                return new ReactionResult(0.50f * reactMult, 0.50f * reactMult, ReactionProduct.InertSludge, 0.30f);
            }
            // Growth(1) + Energy(4) → Bloom
            if (key == (InkDomain.Growth, InkDomain.Energy))
            {
                return new ReactionResult(0.15f * reactMult, 0.40f * reactMult, ReactionProduct.Bloom, 0.25f);
            }
            // Memory(2) + Energy(4) → Activation
            if (key == (InkDomain.Memory, InkDomain.Energy))
            {
                return new ReactionResult(0.10f * reactMult, 0.50f * reactMult, ReactionProduct.Activation, 0f);
            }

            return ReactionResult.None;
        }

        private static ReactionResult CheckAdditiveReactions(InkRecipe rA, InkRecipe rB)
        {
            // Resin + Solvent carrier → Lacquer
            bool hasResin = ((rA.additives | rB.additives) & InkAdditives.Resin) != 0;
            bool hasSolvent = (rA.carrier == InkCarrier.Solvent || rB.carrier == InkCarrier.Solvent);

            if (hasResin && hasSolvent)
            {
                // Solvent dissolves resin into lacquer
                return new ReactionResult(0.10f, 0.15f, ReactionProduct.Lacquer, 0.08f);
            }

            // Water + Oil → repulsion (no product, but layers don't mix - handled elsewhere)
            // Spores + Growth → handled in sim as spread boost

            return ReactionResult.None;
        }

        /// <summary>
        /// Check if spores should trigger a growth burst.
        /// </summary>
        public static bool ShouldSporesBurst(InkRecipe recipe)
        {
            return recipe != null 
                && recipe.domain == InkDomain.Growth 
                && (recipe.additives & InkAdditives.Spores) != 0;
        }
    }
}
