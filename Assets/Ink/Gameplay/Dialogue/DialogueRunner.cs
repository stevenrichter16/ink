using UnityEngine;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Minimal dialogue runner that steps through a DialogueSequence and triggers quest hooks.
    /// Attach to an NPC and call Begin(player) when interacting.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        [Header("Data")]
        public DialogueSequence defaultSequence;
        public DialogueSequence onQuestCompleteSequence;
        [Tooltip("Check this quest state to decide which sequence to play.")]
        public string questIdForStateSwitch;
        public QuestState questStateForAlternate = QuestState.Completed;

        [Header("Reputation")]
        public string factionId;
        public DialogueSequence friendlySequence;
        public DialogueSequence hostileSequence;
        public int friendlyThreshold = 25;
        public int hostileThreshold = -25;
        public KeyCode advanceKey = KeyCode.Space;

        private PlayerController _player;
        private QuestLog _questLog;
        private int _index;
        private bool _running;
        private DialogueSequence _activeSequence;

        public void Begin(PlayerController player)
        {
            if (IsOpen) return;

            _player = player;
            _questLog = player != null ? player.GetComponent<QuestLog>() : null;
            _activeSequence = SelectSequence();
            if (_activeSequence == null) return;

            _index = 0;
            _running = true;
            IsOpen = true;
        }

        private void Update()
        {
            if (!_running) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool advance =
                (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame);

            if (advance)
            {
                Advance();
            }
        }

        private void Advance()
        {
            _index++;
            if (_activeSequence.lines == null || _index >= _activeSequence.lines.Length)
            {
                Finish();
            }
        }

        private void Finish()
        {
            _running = false;
            IsOpen = false;

            if (_questLog != null)
            {
                if (_activeSequence.questToGive != null)
                    _questLog.AddQuest(_activeSequence.questToGive);

                if (_activeSequence.questToTurnIn != null)
                    _questLog.TurnIn(_activeSequence.questToTurnIn.id);
            }
        }

        private void OnGUI()
        {
            if (!_running || _activeSequence == null || _activeSequence.lines == null || _index >= _activeSequence.lines.Length) return;

            var line = _activeSequence.lines[_index];
            string speaker = string.IsNullOrEmpty(line.speaker) ? "NPC" : line.speaker;

            GUILayout.BeginArea(new Rect(40, Screen.height - 220, Screen.width - 80, 180), GUI.skin.box);
            GUILayout.Label($"<b>{speaker}</b>", GUI.skin.label);
            GUILayout.Label(line.text, GUI.skin.label);
            GUILayout.Space(8);
            GUILayout.Label("[Space]/Click to continue", GUI.skin.label);
            GUILayout.EndArea();
        }

        private DialogueSequence SelectSequence()
        {
            // Reputation-based override
            if (!string.IsNullOrEmpty(factionId) && (friendlySequence != null || hostileSequence != null))
            {
                int rep = ReputationSystem.GetRep(factionId);
                if (rep >= friendlyThreshold && friendlySequence != null)
                    return friendlySequence;
                else if (rep <= hostileThreshold && hostileSequence != null)
                    return hostileSequence;
            }

            DialogueSequence chosen = defaultSequence;

            if (_questLog != null && !string.IsNullOrEmpty(questIdForStateSwitch) && onQuestCompleteSequence != null)
            {
                var state = _questLog.GetQuestState(questIdForStateSwitch);
                if (state.HasValue && state.Value == questStateForAlternate)
                    chosen = onQuestCompleteSequence;
            }

            return chosen;
        }
    }
}
