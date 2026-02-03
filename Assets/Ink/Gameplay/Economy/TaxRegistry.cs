using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Simple per-district tax registry. Returns 0 by default.
    /// </summary>
    public static class TaxRegistry
    {
        private static readonly Dictionary<string, float> _taxByDistrict = new Dictionary<string, float>();

        public static void SetTax(string districtId, float taxRate)
        {
            if (string.IsNullOrEmpty(districtId)) return;
            _taxByDistrict[districtId] = taxRate;
        }

        public static float GetTax(string districtId)
        {
            if (string.IsNullOrEmpty(districtId)) return 0f;
            return _taxByDistrict.TryGetValue(districtId, out var t) ? t : 0f;
        }

        public static void Clear()
        {
            _taxByDistrict.Clear();
        }
    }
}
