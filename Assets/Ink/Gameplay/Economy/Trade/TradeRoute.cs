using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    public enum TradeRouteStatus
    {
        Active,
        Blocked,
        Dangerous,
        Contested
    }

    [CreateAssetMenu(menuName = "Ink/Economy/Trade Route")]
    public class TradeRoute : ScriptableObject
    {
        public string id;
        public string sourceDistrictId;
        public string destinationDistrictId;
        public TradeRouteStatus status;    // Active, Blocked, Dangerous, Contested
        [Range(0f, 1f)] public float efficiency;
        public List<string> primaryGoods = new List<string>();
        public int travelTimeInDays;
        public string controllingFactionId;
        public float tollRate;
    }
}
