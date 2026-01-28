using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Shows reputation change toasts near the cursor using pooled UI.
    /// </summary>
    public class ReputationToastManager : MonoBehaviour
    {
        public static ReputationToastManager Instance { get; private set; }

        private readonly Dictionary<string, int> _lastReputation = new Dictionary<string, int>();
        private readonly Dictionary<string, FactionDefinition> _factionCache = new Dictionary<string, FactionDefinition>();
        private readonly Queue<ReputationToast> _pool = new Queue<ReputationToast>();

        private Transform _root;
        private TileCursor _cursor;
        private GridWorld _gridWorld;
        private Font _monoFont;
        private bool _cacheBuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null) return;

            var existing = FindObjectOfType<ReputationToastManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            GameObject go = new GameObject("ReputationToastManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ReputationToastManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _root = new GameObject("ReputationToasts").transform;
            _root.SetParent(transform, false);

            _monoFont = Font.CreateDynamicFontFromOSFont("Courier New", 24);
            if (_monoFont == null)
                _monoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnEnable()
        {
            ReputationSystem.OnRepChanged += HandleRepChanged;
        }

        private void OnDisable()
        {
            ReputationSystem.OnRepChanged -= HandleRepChanged;
        }

        private void HandleRepChanged(string factionId, int newValue)
        {
            if (string.IsNullOrEmpty(factionId)) return;

            int previous = 0;
            if (_lastReputation.TryGetValue(factionId, out var oldValue))
                previous = oldValue;

            _lastReputation[factionId] = newValue;

            int delta = newValue - previous;
            if (delta == 0) return;

            string factionName = GetFactionDisplayName(factionId);
            string deltaText = delta > 0 ? $"+{delta}" : delta.ToString();
            string message = $"{factionName} {deltaText}";
            Color color = delta < 0 ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.3f, 1f, 0.5f, 1f);

            Vector3 worldPos = GetToastWorldPosition();
            SpawnToast(message, color, worldPos);
        }

        private void SpawnToast(string message, Color color, Vector3 worldPos)
        {
            ReputationToast toast = GetToast();
            toast.Play(message, color, worldPos);
        }

        internal void Recycle(ReputationToast toast)
        {
            if (toast == null) return;
            toast.gameObject.SetActive(false);
            _pool.Enqueue(toast);
        }

        private ReputationToast GetToast()
        {
            if (_pool.Count > 0)
            {
                var toast = _pool.Dequeue();
                toast.transform.SetParent(_root, false);
                return toast;
            }

            GameObject go = new GameObject("ReputationToast");
            go.transform.SetParent(_root, false);
            var toastComponent = go.AddComponent<ReputationToast>();
            toastComponent.Initialize(this, _monoFont);
            return toastComponent;
        }

        private Vector3 GetToastWorldPosition()
        {
            if (_cursor == null)
                _cursor = FindObjectOfType<TileCursor>();
            if (_gridWorld == null)
                _gridWorld = FindObjectOfType<GridWorld>();

            if (_cursor != null && _gridWorld != null)
            {
                float tileSize = _gridWorld.tileSize;
                Vector3 basePos = _gridWorld.GridToWorld(_cursor.gridX, _cursor.gridY);
                return basePos + new Vector3(tileSize * 0.4f, tileSize * 0.8f, 0f);
            }

            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                return player.transform.position + Vector3.up * 0.4f;

            return Vector3.zero;
        }

        private string GetFactionDisplayName(string factionId)
        {
            if (!_cacheBuilt)
                BuildFactionCache();

            if (_factionCache.TryGetValue(factionId, out var faction) && faction != null)
                return faction.displayName;

            return factionId;
        }

        private void BuildFactionCache()
        {
            _cacheBuilt = true;
            var factions = Resources.LoadAll<FactionDefinition>("Factions");
            for (int i = 0; i < factions.Length; i++)
            {
                var faction = factions[i];
                if (faction == null || string.IsNullOrEmpty(faction.id)) continue;
                if (!_factionCache.ContainsKey(faction.id))
                    _factionCache.Add(faction.id, faction);
            }
        }
    }
}
