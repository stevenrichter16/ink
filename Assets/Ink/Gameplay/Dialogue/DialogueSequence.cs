using UnityEngine;

namespace InkSim
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea] public string text;
    }

    /// <summary>
    /// Simple authored dialogue with optional quest hooks.
    /// </summary>
    [CreateAssetMenu(menuName = "Ink/Dialogue Sequence", fileName = "DialogueSequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string id = "dialogue_id";
        public DialogueLine[] lines;

        [Header("Quest Hooks (optional)")]
        public QuestDefinition questToGive;
        public QuestDefinition questToTurnIn;
    }
}
