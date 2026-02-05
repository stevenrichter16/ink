namespace InkSim
{
    [System.Serializable]
    public class DemandEvent
    {
        public string id;
        public string itemId;
        public float demandMultiplier;
        public int durationDays;
        public string districtId;  // null = global
        public string description;
    }
}
