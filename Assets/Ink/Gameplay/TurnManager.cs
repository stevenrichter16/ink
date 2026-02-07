using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace InkSim
{
    /// <summary>
    /// Manages turn order: Player → Enemies → NPCs → repeat.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Turn Settings")]
        public float enemyTurnDelay = 0.02f;  // Delay between enemy actions
        public float turnTransitionDelay = 0.01f;  // Delay between player/enemy phases

        [Header("Day Cycle")]
        [Tooltip("Number of turns per in-game day. Economic tick fires each day.")]
        public int turnsPerDay = 20;

        public bool IsPlayerTurn { get; private set; } = true;
        public int TurnNumber { get; private set; } = 0;

        private List<EnemyAI> _enemies = new List<EnemyAI>();
        private List<NpcAI> _npcs = new List<NpcAI>();
        private bool _processingEnemyTurns = false;

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Register an enemy with the turn manager.
        /// </summary>
        public void RegisterEnemy(EnemyAI enemy)
        {
            if (!_enemies.Contains(enemy))
                _enemies.Add(enemy);
        }

        /// <summary>
        /// Unregister an enemy (when it dies).
        /// </summary>
        public void UnregisterEnemy(EnemyAI enemy)
        {
            _enemies.Remove(enemy);
        }

/// <summary>
        /// Clear all registered enemies. Used when loading a saved game.
        /// </summary>
        public void ClearEnemies()
        {
            _enemies.Clear();
        }


        /// <summary>
        /// Register an NPC with the turn manager.
        /// </summary>
        public void RegisterNPC(NpcAI npc)
        {
            if (!_npcs.Contains(npc))
                _npcs.Add(npc);
        }

        /// <summary>
        /// Unregister an NPC.
        /// </summary>
        public void UnregisterNPC(NpcAI npc)
        {
            _npcs.Remove(npc);
        }

        /// <summary>
        /// Called by PlayerController when player completes their action.
        /// </summary>
        public void PlayerActed()
        {
            if (!IsPlayerTurn) return;
            
            IsPlayerTurn = false;
            StartCoroutine(ProcessEnemyTurns());
        }

private IEnumerator ProcessEnemyTurns()
        {
            _processingEnemyTurns = true;

            // All enemies act simultaneously
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (i < _enemies.Count)
                {
                    EnemyAI enemy = _enemies[i];
                    if (enemy != null && enemy.gameObject.activeInHierarchy)
                        enemy.TakeTurn();
                }
            }

            // All NPCs act simultaneously
            for (int i = 0; i < _npcs.Count; i++)
            {
                NpcAI npc = _npcs[i];
                if (npc != null && npc.gameObject.activeInHierarchy)
                    npc.TakeTurn();
            }

            // Tick autonomous NPC-to-NPC conversations
            if (ConversationManager.Instance != null)
                ConversationManager.Instance.TickConversations(TurnNumber);

            // Wait for all movement to complete (minimum 0.1s for consistent pacing)
            float minWait = 0.1f;
            float elapsed = 0f;
            
            bool anyMoving = true;
            while (anyMoving || elapsed < minWait)
            {
                anyMoving = false;
                foreach (var enemy in _enemies)
                    if (enemy != null && enemy.isMoving) anyMoving = true;
                foreach (var npc in _npcs)
                    if (npc != null && npc.isMoving) anyMoving = true;
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            _processingEnemyTurns = false;
            OverlayResolver.TickDecay();
            TurnNumber++;
            WorldSimulationService.Instance?.OnTurnComplete(TurnNumber);

            // Trigger economic day cycle
            if (turnsPerDay > 0 && TurnNumber > 0 && TurnNumber % turnsPerDay == 0)
            {
                EconomicTickService.AdvanceEconomicDay();
            }

            IsPlayerTurn = true;
        }

        /// <summary>
        /// Get count of living enemies.
        /// </summary>
        public int GetEnemyCount()
        {
            _enemies.RemoveAll(e => e == null);
            return _enemies.Count;
        }

/// <summary>
        /// Reset turn state for game restart.
        /// </summary>
        public void ResetTurn()
        {
            StopAllCoroutines();
            _processingEnemyTurns = false;
            IsPlayerTurn = true;
            TurnNumber = 0;
            _enemies.RemoveAll(e => e == null);
        }

    }
}
