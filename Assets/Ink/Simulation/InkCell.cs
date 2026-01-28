using System.Collections.Generic;

namespace InkSim
{
    public sealed class InkCell
    {
        public InkSubstrate substrate;
        public float saturationLimit;
        public readonly List<InkLayer> layers = new List<InkLayer>();

        public InkCell(InkSubstrate substrate, float saturationLimit)
        {
            this.substrate = substrate;
            this.saturationLimit = saturationLimit;
        }

        public float TotalInk()
        {
            float total = 0f;
            for (int i = 0; i < layers.Count; i++) total += layers[i].amount;
            return total;
        }
    }
}
