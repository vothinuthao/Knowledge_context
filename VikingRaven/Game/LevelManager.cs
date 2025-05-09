using UnityEngine;
using VikingRaven.Units.Components;
using Zenject;

namespace VikingRaven.Game
{
    public class LevelManager : MonoBehaviour
    {
        [Inject] private GameManager _gameManager;
        [SerializeField] private Transform[] _playerSpawnPoints;
        [SerializeField] private Transform[] _enemySpawnPoints;
        
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
            
            // Spawn an infantry squad at the first spawn point
            _gameManager.CreateSquad(UnitType.Infantry, 8, _playerSpawnPoints[0].position);
            
            // Spawn a mixed squad at the second spawn point if available
            if (_playerSpawnPoints.Length > 1)
            {
                _gameManager.CreateMixedSquad(_playerSpawnPoints[1].position);
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
            
            // Spawn an archer squad at the first enemy spawn point
            _gameManager.CreateSquad(UnitType.Archer, 6, _enemySpawnPoints[0].position);
            
            // Spawn a pike squad at the second enemy spawn point if available
            if (_enemySpawnPoints.Length > 1)
            {
                _gameManager.CreateSquad(UnitType.Pike, 4, _enemySpawnPoints[1].position);
            }
        }
    }
}