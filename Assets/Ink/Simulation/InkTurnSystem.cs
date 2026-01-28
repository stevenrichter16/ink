using System;

namespace InkSim
{
    // Authoritative turn clock for all ink simulation. No frame-based behavior.
    public static class InkTurnSystem
    {
        public static int Turn { get; private set; }
        public static event Action<int> OnTurn;

        public static void Reset(int turn)
        {
            Turn = turn;
            if (OnTurn != null) OnTurn(Turn);
        }

        public static void Advance()
        {
            Turn++;
            if (OnTurn != null) OnTurn(Turn);
        }
    }
}
