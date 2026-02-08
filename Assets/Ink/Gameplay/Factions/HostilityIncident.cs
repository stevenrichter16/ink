namespace InkSim
{
    /// <summary>
    /// Types of hostile incidents that feed into the tension pipeline.
    /// Each type carries a fixed tension delta.
    /// </summary>
    public enum IncidentType
    {
        VerbalInsult,       // +0.05
        ShoveMatch,         // +0.10
        TradeViolation,     // +0.08
        Trespassing,        // +0.10
        TheftAccusation,    // +0.15
        InscriptionDefaced, // +0.12
        PropertyDamage,     // +0.15
        DrawnWeapon,        // +0.20
        Assault,            // +0.25
        TerritorySeized,    // +0.30
        Murder,             // +0.50
    }

    /// <summary>
    /// Escalation stages derived from accumulated tension.
    /// Higher stages unlock progressively aggressive behaviors and dialogue.
    /// </summary>
    public enum EscalationStage
    {
        Calm,       // [0.0, 0.2)
        Uneasy,     // [0.2, 0.4)
        Tense,      // [0.4, 0.6)
        Volatile,   // [0.6, 0.8)
        Explosive,  // [0.8, 1.0]
    }

    /// <summary>
    /// Tracks accumulated tension between two factions in a specific district.
    /// Tension is a 0-1 float that maps to an EscalationStage.
    /// </summary>
    public struct TensionRecord
    {
        public string factionA;
        public string factionB;
        public string districtId;
        public float tension;
        public EscalationStage stage;
        public int lastIncidentTurn;
        public IncidentType lastIncidentType;
        public int incidentCount;
    }

    /// <summary>
    /// Result of an AuthorizeFight query. All combat paths must check this
    /// before dealing damage.
    /// </summary>
    public struct FightAuthorization
    {
        public bool authorized;
        public string reason;
        public EscalationStage stage;
        public float tension;

        public static FightAuthorization Denied(string reason = "default_deny")
        {
            return new FightAuthorization
            {
                authorized = false,
                reason = reason,
                stage = EscalationStage.Calm,
                tension = 0f
            };
        }

        public static FightAuthorization Authorized(string reason, EscalationStage stage, float tension)
        {
            return new FightAuthorization
            {
                authorized = true,
                reason = reason,
                stage = stage,
                tension = tension
            };
        }
    }

    /// <summary>
    /// Utility to map IncidentType → tension delta.
    /// </summary>
    public static class IncidentTensionDeltas
    {
        public static float GetDelta(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.VerbalInsult:       return 0.05f;
                case IncidentType.ShoveMatch:         return 0.10f;
                case IncidentType.TradeViolation:     return 0.08f;
                case IncidentType.Trespassing:        return 0.10f;
                case IncidentType.TheftAccusation:    return 0.15f;
                case IncidentType.InscriptionDefaced: return 0.12f;
                case IncidentType.PropertyDamage:     return 0.15f;
                case IncidentType.DrawnWeapon:        return 0.20f;
                case IncidentType.Assault:            return 0.25f;
                case IncidentType.TerritorySeized:    return 0.30f;
                case IncidentType.Murder:             return 0.50f;
                default:                              return 0.10f;
            }
        }

        /// <summary>
        /// Human-readable name for UI/dialogue tokens.
        /// </summary>
        public static string GetDisplayName(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.VerbalInsult:       return "insult";
                case IncidentType.ShoveMatch:         return "brawl";
                case IncidentType.TradeViolation:     return "trade violation";
                case IncidentType.Trespassing:        return "trespassing";
                case IncidentType.TheftAccusation:    return "theft";
                case IncidentType.InscriptionDefaced: return "defacement";
                case IncidentType.PropertyDamage:     return "property damage";
                case IncidentType.DrawnWeapon:        return "armed threat";
                case IncidentType.Assault:            return "assault";
                case IncidentType.TerritorySeized:    return "seizure of territory";
                case IncidentType.Murder:             return "murder";
                default:                              return "incident";
            }
        }
    }
}
