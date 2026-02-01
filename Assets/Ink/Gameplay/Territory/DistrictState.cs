using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Runtime per-district state for each faction.
    /// </summary>
    public class DistrictState
    {
        public DistrictDefinition Definition { get; private set; }
        public string Id => Definition != null ? Definition.id : "";

        public float[] control;
        public float[] patrol;
        public float[] heat;
        public int[] lossStreak;

        public DistrictState(DistrictDefinition def, int factionCount)
        {
            Definition = def;
            control = new float[factionCount];
            patrol = new float[factionCount];
            heat = new float[factionCount];
            lossStreak = new int[factionCount];
        }

        public int ControllingFactionIndex
        {
            get
            {
                int best = -1;
                float bestC = 0f;
                for (int i = 0; i < control.Length; i++)
                {
                    if (control[i] > bestC)
                    {
                        bestC = control[i];
                        best = i;
                    }
                }
                return best;
            }
        }

        public void Neutralize()
        {
            for (int i = 0; i < control.Length; i++)
            {
                control[i] = 0.05f;
                patrol[i] = 0.05f;
                lossStreak[i] = 0;
            }
        }
    }
}
