using System.Collections.Generic;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Pooled manager for SpeechBubble instances. Attached alongside ConversationManager
    /// on the WorldSimulationService GameObject via TestMapBuilder.
    /// </summary>
    public class SpeechBubblePool : MonoBehaviour
    {
        public static SpeechBubblePool Instance { get; private set; }

        private readonly Queue<SpeechBubble> _pool = new Queue<SpeechBubble>();
        private readonly Dictionary<GridEntity, SpeechBubble> _activeBubbles = new Dictionary<GridEntity, SpeechBubble>();
        private Transform _root;
        private Font _font;
        private const int MaxPoolSize = 24; // Increased from 16 for turn-based lifetime

        // Color palette for conversation types
        public static readonly Color ColorSameFaction = Color.white;
        public static readonly Color ColorFriendlyCross = new Color(0.53f, 0.87f, 1f, 1f); // #88DDFF
        public static readonly Color ColorNeutralCross = new Color(1f, 1f, 0.67f, 1f);     // #FFFFAA
        public static readonly Color ColorHostileCross = new Color(1f, 0.53f, 0.53f, 1f);  // #FF8888

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _root = new GameObject("SpeechBubbles").transform;
            _root.SetParent(transform, false);

            _font = Font.CreateDynamicFontFromOSFont("Courier New", 20);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>
        /// Show a speech bubble above an entity.
        /// Recycles any existing bubble for this entity to prevent stacking.
        /// </summary>
        public static void Show(GridEntity entity, string text, Color color)
        {
            if (Instance == null || entity == null) return;

            // Recycle any existing bubble for this entity (prevents visual stacking)
            if (Instance._activeBubbles.TryGetValue(entity, out var oldBubble) && oldBubble != null)
            {
                Instance.Recycle(oldBubble);
            }

            var bubble = Instance.GetBubble();
            bubble.Show(entity, text, color);
            Instance._activeBubbles[entity] = bubble;
        }

        /// <summary>
        /// Determine the appropriate bubble color based on faction relationship.
        /// </summary>
        public static Color GetColorForRelationship(FactionMember a, FactionMember b)
        {
            if (a == null || b == null || a.faction == null || b.faction == null)
                return ColorSameFaction;

            if (a.faction.id == b.faction.id)
                return ColorSameFaction;

            int interRep = ReputationSystem.GetInterRep(a.faction.id, b.faction.id);
            if (interRep >= HostilityService.FriendlyThreshold)
                return ColorFriendlyCross;
            if (interRep <= HostilityService.HostileThreshold)
                return ColorHostileCross;

            return ColorNeutralCross;
        }

        /// <summary>
        /// Return a bubble to the pool.
        /// </summary>
        public void Recycle(SpeechBubble bubble)
        {
            if (bubble == null) return;
            bubble.gameObject.SetActive(false);

            // Remove from active tracking
            GridEntity toRemove = null;
            foreach (var kvp in _activeBubbles)
            {
                if (kvp.Value == bubble) { toRemove = kvp.Key; break; }
            }
            if (toRemove != null) _activeBubbles.Remove(toRemove);

            if (_pool.Count < MaxPoolSize)
                _pool.Enqueue(bubble);
            else
                Destroy(bubble.gameObject);
        }

        private SpeechBubble GetBubble()
        {
            if (_pool.Count > 0)
            {
                var bubble = _pool.Dequeue();
                bubble.transform.SetParent(_root, false);
                return bubble;
            }

            GameObject go = new GameObject("SpeechBubble");
            go.transform.SetParent(_root, false);
            var comp = go.AddComponent<SpeechBubble>();
            comp.Initialize(this, _font);
            return comp;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
