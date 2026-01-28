using System;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Deterministic ink simulation implementing the 5 Immutable Ink Laws.
    /// Tick order: Age → Evaporate → React → Spread → Apply Deltas → Saturation Runoff → Cleanup
    /// </summary>
    public sealed class InkSimulator
    {
        private readonly InkGrid _grid;
        private readonly Dictionary<(int x, int y, InkRecipe r), float> _delta = new Dictionary<(int, int, InkRecipe), float>();
        
        // Reusable list to avoid allocations during reaction phase
        private readonly List<(int i, int j)> _reactionPairs = new List<(int, int)>(16);

        public InkSimulator(InkGrid grid)
        {
            _grid = grid;
        }

        public void ApplyInk(int x, int y, InkRecipe recipe, float amount)
        {
            if (recipe == null || amount <= 0f) return;
            if (!_grid.InBounds(x, y)) return;
            
            InkCell cell = _grid.Get(x, y);
            InkLayer layer = FindOrCreateLayer(cell, recipe);
            layer.amount += amount;
        }

        /// <summary>
        /// Advance simulation by one turn. Fully deterministic given same grid state.
        /// </summary>
public void Tick()
        {
            _delta.Clear();

            // Phase 1: Age all layers
            PhaseAge();

            // Phase 2: Reactions FIRST (before evaporation eats volatile inks)
            PhaseReact();

            // Phase 3: Evaporate (uses substrate rules)
            PhaseEvaporate();

            // Phase 4: Spread (uses substrate rules)
            PhaseSpread();

            // Phase 5: Apply accumulated deltas
            PhaseApplyDeltas();

            // Phase 6: Saturation runoff
            PhaseSaturationRunoff();

            // Phase 7: Cleanup dead layers
            PhaseCleanup();
        }

        #region Tick Phases

        private void PhaseAge()
        {
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    for (int i = 0; i < cell.layers.Count; i++)
                    {
                        cell.layers[i].ageTurns++;
                    }
                }
            }
        }

        private void PhaseEvaporate()
        {
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    SubstrateRules rules = SubstrateRules.Get(cell.substrate);

                    for (int i = 0; i < cell.layers.Count; i++)
                    {
                        InkLayer layer = cell.layers[i];
                        if (layer.amount <= 0.0001f) continue;

                        float evap = layer.recipe.volatility;

                        // Carrier modifiers (Law 1: Carrier Transport)
                        switch (layer.recipe.carrier)
                        {
                            case InkCarrier.Solvent: evap *= 1.25f; break;
                            case InkCarrier.Oil:    evap *= 0.50f; break;
                            case InkCarrier.Gel:    evap *= 0.35f; break;
                            case InkCarrier.Water:  evap *= 1.00f; break;
                        }

                        // Substrate modifier (Law 3: Substrate Binding)
                        evap *= rules.evapMult;

                        // Resin slows evaporation
                        if ((layer.recipe.additives & InkAdditives.Resin) != 0)
                            evap *= 0.7f;

                        // Ash increases evaporation (absorbs moisture)
                        if ((layer.recipe.additives & InkAdditives.Ash) != 0)
                            evap *= 1.15f;

                        layer.amount *= (1f - Clamp(evap, 0f, 0.95f));
                    }
                }
            }
        }

private void PhaseReact()
        {
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    if (cell.layers.Count < 2) continue;

                    // Build pairs to react (avoiding double-processing)
                    _reactionPairs.Clear();
                    for (int i = 0; i < cell.layers.Count - 1; i++)
                    {
                        for (int j = i + 1; j < cell.layers.Count; j++)
                        {
                            _reactionPairs.Add((i, j));
                        }
                    }

                    // Process reactions
                    foreach (var pair in _reactionPairs)
                    {
                        InkLayer layerA = cell.layers[pair.i];
                        InkLayer layerB = cell.layers[pair.j];

                        if (layerA.amount <= 0.0001f || layerB.amount <= 0.0001f) continue;

                        ReactionResult result = InkReactionTable.Evaluate(layerA, layerB, cell.substrate);
                        if (result.product == ReactionProduct.None && result.consumeA <= 0f) continue;

                        // Law 5: Reaction Cost - consume ink
                        float consumedA = layerA.amount * result.consumeA;
                        float consumedB = layerB.amount * result.consumeB;
                        layerA.amount -= consumedA;
                        layerB.amount -= consumedB;

                        // Spawn visible residue product
                        if (result.product != ReactionProduct.None && result.productAmount > 0f)
                        {
                            InkRecipe residueRecipe = ResidueRecipes.GetRecipe(result.product);
                            if (residueRecipe != null)
                            {
                                // Scale product by how much was consumed
                                float productAmt = (consumedA + consumedB) * 0.5f + result.productAmount;
                                InkLayer residue = FindOrCreateLayer(cell, residueRecipe);
                                residue.amount += productAmt;
                                
                                #if UNITY_EDITOR
                                UnityEngine.Debug.Log($"[Ink] Reaction at ({x},{y}): {layerA.recipe.displayName} + {layerB.recipe.displayName} → {residueRecipe.displayName} (+{productAmt:F2})");
                                #endif
                            }
                        }
                    }

                    // Spores burst: growth + spores → extra spread next phase
                    for (int i = 0; i < cell.layers.Count; i++)
                    {
                        InkLayer layer = cell.layers[i];
                        if (InkReactionTable.ShouldSporesBurst(layer.recipe) && layer.amount > 0.5f)
                        {
                            // Spores trigger a small self-replication boost every few turns
                            if (layer.ageTurns % 3 == 0)
                            {
                                layer.amount *= 1.08f; // 8% growth burst
                            }
                        }
                    }
                }
            }
        }

        private void PhaseSpread()
        {
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    if (cell.layers.Count == 0) continue;

                    SubstrateRules rules = SubstrateRules.Get(cell.substrate);

                    for (int i = 0; i < cell.layers.Count; i++)
                    {
                        InkLayer layer = cell.layers[i];
                        if (layer.amount <= 0.0001f) continue;

                        float mobility = Mobility(layer.recipe, rules);
                        float portion = layer.amount * mobility;
                        if (portion <= 0.0001f) continue;

                        // Check water/oil repulsion: oil doesn't spread into water-dominant cells
                        float each = portion / 4f;
                        AccumulateDelta(x, y, layer.recipe, -portion);

                        TrySpreadTo(x + 1, y, layer.recipe, each);
                        TrySpreadTo(x - 1, y, layer.recipe, each);
                        TrySpreadTo(x, y + 1, layer.recipe, each);
                        TrySpreadTo(x, y - 1, layer.recipe, each);
                    }
                }
            }
        }

        private void PhaseApplyDeltas()
        {
            foreach (var kv in _delta)
            {
                int x = kv.Key.x;
                int y = kv.Key.y;
                InkRecipe recipe = kv.Key.r;
                float d = kv.Value;

                if (!_grid.InBounds(x, y)) continue;
                InkCell cell = _grid.Get(x, y);
                InkLayer layer = FindOrCreateLayer(cell, recipe);
                layer.amount = Math.Max(0f, layer.amount + d);
            }
        }

        private void PhaseSaturationRunoff()
        {
            // Law 4: Saturation Threshold - excess ink must go somewhere
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    float total = cell.TotalInk();
                    if (total <= cell.saturationLimit) continue;

                    float excess = total - cell.saturationLimit;
                    if (excess <= 0f) continue;

                    for (int i = 0; i < cell.layers.Count; i++)
                    {
                        InkLayer layer = cell.layers[i];
                        float ratio = layer.amount / total;
                        float take = Math.Min(layer.amount, excess * ratio);
                        if (take <= 0f) continue;

                        layer.amount -= take;
                        float distribute = take / 4f;

                        TrySpreadTo(x + 1, y, layer.recipe, distribute);
                        TrySpreadTo(x - 1, y, layer.recipe, distribute);
                        TrySpreadTo(x, y + 1, layer.recipe, distribute);
                        TrySpreadTo(x, y - 1, layer.recipe, distribute);
                    }
                }
            }
        }

        private void PhaseCleanup()
        {
            for (int y = 0; y < _grid.height; y++)
            {
                for (int x = 0; x < _grid.width; x++)
                {
                    InkCell cell = _grid.Get(x, y);
                    for (int i = cell.layers.Count - 1; i >= 0; i--)
                    {
                        if (cell.layers[i].amount <= 0.0001f)
                            cell.layers.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private float Mobility(InkRecipe r, SubstrateRules rules)
        {
            float viscosity = Math.Max(0.05f, r.viscosity);
            float baseMobility = r.spreadRate / viscosity;

            // Carrier modifier (Law 1)
            switch (r.carrier)
            {
                case InkCarrier.Water:   baseMobility *= 1.10f; break;
                case InkCarrier.Oil:     baseMobility *= 0.70f; break;
                case InkCarrier.Gel:     baseMobility *= 0.35f; break;
                case InkCarrier.Solvent: baseMobility *= 0.90f; break;
            }

            // Substrate modifier (Law 3)
            baseMobility *= rules.spreadMult;

            // Additives
            if ((r.additives & InkAdditives.Thickener) != 0)  baseMobility *= 0.65f;
            if ((r.additives & InkAdditives.Surfactant) != 0) baseMobility *= 1.25f;

            return Clamp(baseMobility * 0.10f, 0f, 0.35f);
        }

        private void TrySpreadTo(int x, int y, InkRecipe r, float amount)
        {
            if (!_grid.InBounds(x, y)) return;

            // Water/Oil repulsion check
            if (r.carrier == InkCarrier.Oil)
            {
                InkCell target = _grid.Get(x, y);
                InkLayer dominant = InkDominance.GetDominant(target);
                if (dominant != null && dominant.recipe.carrier == InkCarrier.Water && dominant.amount > 0.3f)
                {
                    // Oil repelled by water-dominant cell—only 25% makes it through
                    amount *= 0.25f;
                }
            }

            AccumulateDelta(x, y, r, amount);
        }

        private void AccumulateDelta(int x, int y, InkRecipe r, float amount)
        {
            var key = (x, y, r);
            if (_delta.TryGetValue(key, out float existing))
                _delta[key] = existing + amount;
            else
                _delta[key] = amount;
        }

        private static InkLayer FindOrCreateLayer(InkCell cell, InkRecipe recipe)
        {
            for (int i = 0; i < cell.layers.Count; i++)
            {
                if (cell.layers[i].recipe == recipe)
                    return cell.layers[i];
            }

            InkLayer layer = new InkLayer(recipe, 0f);
            cell.layers.Add(layer);
            return layer;
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        #endregion
    }
}
