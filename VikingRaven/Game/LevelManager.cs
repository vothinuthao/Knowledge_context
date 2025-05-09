using Core.Utils;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game
{
     public class LevelManager : Singleton<LevelManager>
    {
        // Reference to GameManager now accessed through singleton
        private GameManager GameManager => GameManager.Instance;
        
        [SerializeField] private Transform[] _playerSpawnPoints;
        [SerializeField] private Transform[] _enemySpawnPoints;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("LevelManager initialized as singleton");
            
            // Check if GameManager is available
            if (GameManager == null)
            {
                Debug.LogError("LevelManager: GameManager singleton is not available");
            }
        }
        
        /// <summary>
        /// Starts the level by spawning all units
        /// </summary>
        public void StartLevel()
        {
            SpawnPlayerSquads();
            SpawnEnemySquads();
        }

        /// <summary>
        /// Spawns player squads at the specified spawn points
        /// </summary>
        private void SpawnPlayerSquads()
        {
            if (_playerSpawnPoints.Length == 0)
            {
                Debug.LogError("No player spawn points defined");
                return;
            }
            
            if (GameManager == null)
            {
                Debug.LogError("LevelManager: GameManager is null, cannot spawn player squads");
                return;
            }
            
            // Spawn an infantry squad at the first spawn point
            GameManager.CreateSquad(UnitType.Infantry, 8, _playerSpawnPoints[0].position);
            
            // Spawn a mixed squad at the second spawn point if available
            if (_playerSpawnPoints.Length > 1)
            {
                GameManager.CreateMixedSquad(_playerSpawnPoints[1].position);
            }
        }

        /// <summary>
        /// Spawns enemy squads at the specified spawn points
        /// </summary>
        private void SpawnEnemySquads()
        {
            if (_enemySpawnPoints.Length == 0)
            {
                Debug.LogError("No enemy spawn points defined");
                return;
            }
            
            if (GameManager == null)
            {
                Debug.LogError("LevelManager: GameManager is null, cannot spawn enemy squads");
                return;
            }
            
            // Spawn an archer squad at the first enemy spawn point
            GameManager.CreateSquad(UnitType.Archer, 6, _enemySpawnPoints[0].position);
            
            // Spawn a pike squad at the second enemy spawn point if available
            if (_enemySpawnPoints.Length > 1)
            {
                GameManager.CreateSquad(UnitType.Pike, 4, _enemySpawnPoints[1].position);
            }
        }
    }
}