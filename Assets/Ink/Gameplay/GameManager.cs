using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace InkSim
{
    /// <summary>
    /// Manages game restart functionality.
    /// Press R to restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Spawn Data")]
        private Vector2Int _playerSpawn;
        private List<EnemySpawnData> _enemySpawns = new List<EnemySpawnData>();

        private PlayerController _player;
        private float _tileSize;

        private struct EnemySpawnData
        {
            public EnemyAI enemy;
            public Vector2Int position;
            public int maxHealth;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Find and register player
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _playerSpawn = new Vector2Int(_player.gridX, _player.gridY);
                if (_player.World != null)
                    _tileSize = _player.World.tileSize;
            }

            // Find and register all enemies
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                _enemySpawns.Add(new EnemySpawnData
                {
                    enemy = enemy,
                    position = new Vector2Int(enemy.gridX, enemy.gridY),
                    maxHealth = enemy.maxHealth
                });
            }

            Debug.Log($"[GameManager] Registered {_enemySpawns.Count} enemies for restart");
        }

        private void Update()
        {
            // R to restart
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                // Don't restart if inventory is open
                if (!InventoryUI.IsOpen)
                {
                    Restart();
                }
            }
        }

        public void Restart()
        {
            Debug.Log("[GameManager] Restarting game...");

            GridWorld world = _player?.World;

            // Reset player
            if (_player != null)
            {
                // Clear old position
                world?.ClearOccupant(_player.gridX, _player.gridY);

                _player.enabled = true;
                _player.currentHealth = _player.MaxHealth;
                _player.gridX = _playerSpawn.x;
                _player.gridY = _playerSpawn.y;
                _player.transform.localPosition = new Vector3(
                    _playerSpawn.x * _tileSize,
                    _playerSpawn.y * _tileSize,
                    0
                );

                // Set new position
                world?.SetOccupant(_playerSpawn.x, _playerSpawn.y, _player);
            }

            // Reset enemies
            foreach (var spawnData in _enemySpawns)
            {
                if (spawnData.enemy == null)
                {
                    // Enemy was destroyed, skip for now
                    continue;
                }

                // Clear old position
                world?.ClearOccupant(spawnData.enemy.gridX, spawnData.enemy.gridY);

                spawnData.enemy.gameObject.SetActive(true);
                spawnData.enemy.enabled = true;
                spawnData.enemy.currentHealth = spawnData.maxHealth;
                spawnData.enemy.gridX = spawnData.position.x;
                spawnData.enemy.gridY = spawnData.position.y;
                spawnData.enemy.transform.localPosition = new Vector3(
                    spawnData.position.x * _tileSize,
                    spawnData.position.y * _tileSize,
                    0
                );

                // Set new position
                world?.SetOccupant(spawnData.position.x, spawnData.position.y, spawnData.enemy);

                // Re-register with turn manager
                TurnManager.Instance?.RegisterEnemy(spawnData.enemy);
            }

            // Reset turn state
            TurnManager.Instance?.ResetTurn();

            Debug.Log("[GameManager] Restart complete!");
        }
    }
}
