using System;

namespace InkSim
{
    [Serializable]
    public sealed class InkLayer
    {
        public InkRecipe recipe;
        public float amount;
        public int ageTurns;

        public InkLayer(InkRecipe recipe, float amount)
        {
            this.recipe = recipe;
            this.amount = amount;
            this.ageTurns = 0;
        }
    }
}
