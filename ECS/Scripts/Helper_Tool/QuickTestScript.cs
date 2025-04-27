// QuickTestScript.cs

using Core.ECS;
using Core.Grid;
using Managers;
using Systems.Squad;
using UnityEngine;

namespace Helper_Tool
{
    /// <summary>
    /// Script nhanh để test tính năng spawn squad
    /// Attach vào một GameObject trong scene
    /// </summary>
    public class QuickTestScript : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private GridManager _gridManager;
    
        [Header("Test Settings")]
        [SerializeField] private KeyCode _testSpawnKey = KeyCode.T;
        [SerializeField] private KeyCode _clearKey = KeyCode.C;
    
        private Entity _lastSpawnedSquad;
    
        private void Start()
        {
            if (_gameManager == null)
                _gameManager = GameManager.Instance;
            
            if (_worldManager == null)
                _worldManager = FindObjectOfType<WorldManager>();
            
            if (_gridManager == null)
                _gridManager = GridManager.Instance;
            
            Debug.Log("QuickTestScript initialized - Press T to spawn squad");
        }
    
        private void Update()
        {
            // Test spawn at center
            if (Input.GetKeyDown(_testSpawnKey))
            {
                SpawnTestSquad();
            }
        
            // Clear all squads
            if (Input.GetKeyDown(_clearKey))
            {
                ClearAllSquads();
            }
        
            // Move last spawned squad to mouse position
            if (Input.GetMouseButtonDown(1) && _lastSpawnedSquad != null)
            {
                MoveSquadToMouse();
            }
        }
    
        private void SpawnTestSquad()
        {
            if (_gameManager == null)
            {
                Debug.LogError("GameManager not found!");
                return;
            }
        
            Vector2Int centerPos = new Vector2Int(10, 10);
            Vector3 worldPos = _gridManager.GetCellCenter(centerPos);
        
            _lastSpawnedSquad = _gameManager.CreateSquad(null, worldPos, Faction.PLAYER);
        
            if (_lastSpawnedSquad != null)
            {
                Debug.Log($"Spawned squad at {worldPos} - ID: {_lastSpawnedSquad.Id}");
            }
            else
            {
                Debug.LogError("Failed to spawn squad!");
            }
        }
    
        private void MoveSquadToMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
                Vector3 targetPos = _gridManager.GetCellCenter(gridPos);
            
                // Use SquadCommandSystem to move squad
                var commandSystem = _worldManager.World.GetSystem<SquadCommandSystem>();
                if (commandSystem != null)
                {
                    commandSystem.CommandMove(_lastSpawnedSquad, targetPos);
                    Debug.Log($"Moving squad {_lastSpawnedSquad.Id} to {targetPos}");
                }
                else
                {
                    Debug.LogError("SquadCommandSystem not found!");
                }
            }
        }
    
        private void ClearAllSquads()
        {
            // Simple implementation to clear all squads
            Debug.Log("Clearing all squads...");
        
            // This would need proper implementation in GameManager
            // For now, just log the action
        }
    
        private void OnGUI()
        {
            // Simple UI instructions
            GUI.Box(new Rect(10, 10, 250, 100), "Quick Test Controls");
            GUI.Label(new Rect(20, 30, 230, 20), $"[{_testSpawnKey}] Spawn Squad at Center");
            GUI.Label(new Rect(20, 50, 230, 20), "[Right Click] Move Last Spawned Squad");
            GUI.Label(new Rect(20, 70, 230, 20), $"[{_clearKey}] Clear All Squads");
        
            if (_lastSpawnedSquad != null)
            {
                GUI.Label(new Rect(20, 90, 230, 20), $"Last Spawned: Squad {_lastSpawnedSquad.Id}");
            }
        }
    }
}