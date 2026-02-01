using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Attach to any surface or item that can hold an inscription.
    /// This is a thin helper to create/register palimpsest layers at runtime.
    /// </summary>
    public class InscribableSurface : MonoBehaviour
    {
        [Header("Layer Defaults")]
        public int radius = 5;
        public int priority = 0;
        public int turns = 10;
        public List<string> defaultTokens = new List<string> { "TRUCE" };
        [Tooltip("Optional district id for territorial heat integration")]
        public string districtId;

        [Header("Behaviour")]
        public bool registerOnStart = false;

        private int _activeLayerId = -1;

        private void Start()
        {
            if (registerOnStart)
            {
                RegisterLayer();
            }
        }

        public PalimpsestLayer CreateLayer()
        {
            return new PalimpsestLayer
            {
                center = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.y)),
                radius = radius,
                priority = priority,
                turnsRemaining = turns,
                tokens = new List<string>(defaultTokens)
            };
        }

        public int RegisterLayer()
        {
            var layer = CreateLayer();
            _activeLayerId = OverlayResolver.RegisterLayer(layer);
            if (!string.IsNullOrEmpty(districtId) && DistrictControlService.Instance != null)
            {
                DistrictControlService.Instance.ApplyPalimpsestEdit(districtId, 1f);
            }
            return _activeLayerId;
        }

        public void EraseLayer()
        {
            if (_activeLayerId > 0)
            {
                OverlayResolver.UnregisterLayer(_activeLayerId);
                _activeLayerId = -1;
            }
        }
    }
}
