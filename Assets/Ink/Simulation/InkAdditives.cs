using System;

namespace InkSim
{
    [Flags]
    public enum InkAdditives
    {
        None         = 0,
        Resin        = 1 << 0,
        Surfactant   = 1 << 1,
        Thickener    = 1 << 2,
        Catalyst     = 1 << 3,
        Spores       = 1 << 4,
        BoneDust     = 1 << 5,
        MetalFilings = 1 << 6,
        Ash          = 1 << 7
    }
}
