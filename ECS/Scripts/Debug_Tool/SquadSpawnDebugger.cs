// SquadSpawnDebugger.cs

using System.Collections.Generic;
using Components;
using Components.Squad;
using Core.Grid;
using Managers;
using Systems.Squad;
using UnityEngine;

namespace Debug_Tool
{
    /// <summary>
    /// Debugger to find issues with squad spawning
    /// </summary>
    public class SquadSpawnDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private GridManager _gridManager;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _troopPrefab;
        [SerializeField] private SquadConfig _testSquadConfig;
        
        [Header("Debug Settings")]
        [SerializeField] private bool _autoDebugOnStart = true;
        [SerializeField] private KeyCode _debugKey = KeyCode.F12;
        
        private List<string> _debugLog = new List<string>();
        private Vector2 _scrollPosition;
        
        private void Start()
        {
            if (_autoDebugOnStart)
            {
                PerformFullDebug();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(_debugKey))
            {
                PerformFullDebug();
            }
        }
        
        private void PerformFullDebug()
        {
            _debugLog.Clear();
            
            Log("=== SQUAD SPAWN DEBUG START ===");
            
            // Step 1: Check Managers
            CheckManagers();
            
            // Step 2: Check World and Systems
            CheckWorldAndSystems();
            
            // Step 3: Check Prefabs and Configs
            CheckPrefabsAndConfigs();
            
            // Step 4: Test Spawn Process
            TestSpawnProcess();
            
            Log("=== SQUAD SPAWN DEBUG END ===");
        }
        
        private void CheckManagers()
        {
            Log("--- Checking Managers ---");
            
            // GameManager
            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
                if (_gameManager == null)
                {
                    LogError("GameManager not found in scene!");
                }
                else
                {
                    LogSuccess("GameManager found via Instance");
                }
            }
            else
            {
                LogSuccess("GameManager assigned in inspector");
            }
            
            // WorldManager
            if (_worldManager == null)
            {
                _worldManager = FindObjectOfType<WorldManager>();
                if (_worldManager == null)
                {
                    LogError("WorldManager not found in scene!");
                }
                else
                {
                    LogSuccess("WorldManager found via FindObjectOfType");
                }
            }
            else
            {
                LogSuccess("WorldManager assigned in inspector");
            }
            
            // GridManager
            if (_gridManager == null)
            {
                _gridManager = GridManager.Instance;
                if (_gridManager == null)
                {
                    LogError("GridManager not found in scene!");
                }
                else
                {
                    LogSuccess("GridManager found via Instance");
                }
            }
            else
            {
                LogSuccess("GridManager assigned in inspector");
            }
        }
        
        private void CheckWorldAndSystems()
        {
            Log("--- Checking World and Systems ---");
            
            if (_worldManager == null)
            {
                LogError("WorldManager is null, cannot check World");
                return;
            }
            
            if (_worldManager.World == null)
            {
                LogError("World is null! WorldManager may not be initialized");
                return;
            }
            
            LogSuccess($"World exists - Entity count: {_worldManager.World.GetEntityCount()}");
            LogSuccess($"System count: {_worldManager.World.GetRegisteredSystemCount()}");
            
            // Check specific systems
            var commandSystem = _worldManager.World.GetSystem<SquadCommandSystem>();
            if (commandSystem == null)
            {
                LogError("SquadCommandSystem not registered!");
            }
            else
            {
                LogSuccess("SquadCommandSystem found");
            }
            
            // Add checks for other critical systems
        }
        
        private void CheckPrefabsAndConfigs()
        {
            Log("--- Checking Prefabs and Configs ---");
            
            if (_troopPrefab == null)
            {
                LogError("Troop prefab not assigned!");
            }
            else
            {
                LogSuccess("Troop prefab assigned");
                
                // Check for EntityBehaviour component
                var entityBehaviour = _troopPrefab.GetComponent<EntityBehaviour>();
                if (entityBehaviour == null)
                {
                    LogError("Troop prefab missing EntityBehaviour component!");
                }
                else
                {
                    LogSuccess("Troop prefab has EntityBehaviour");
                }
            }
            
            if (_testSquadConfig == null)
            {
                LogWarning("Squad config not assigned, using default");
            }
            else
            {
                LogSuccess("Squad config assigned");
            }
        }
        
        private void TestSpawnProcess()
        {
            Log("--- Testing Spawn Process ---");
            
            if (_gameManager == null || _worldManager == null || _gridManager == null)
            {
                LogError("Cannot test spawn - managers not initialized");
                return;
            }
            
            // Test spawn at center
            try
            {
                Vector2Int centerGrid = new Vector2Int(10, 10);
                Vector3 spawnPos = _gridManager.GetCellCenter(centerGrid);
                Log($"Attempting spawn at {spawnPos}");
                
                var squad = _gameManager.CreateSquad(_testSquadConfig, spawnPos, Faction.PLAYER);
                
                if (squad != null)
                {
                    LogSuccess($"Squad spawned successfully - ID: {squad.Id}");
                    
                    // Check components
                    if (squad.HasComponent<SquadStateComponent>())
                    {
                        LogSuccess("Squad has StateComponent");
                    }
                    else
                    {
                        LogError("Squad missing StateComponent!");
                    }
                    
                    if (squad.HasComponent<PositionComponent>())
                    {
                        LogSuccess("Squad has PositionComponent");
                    }
                    else
                    {
                        LogError("Squad missing PositionComponent!");
                    }
                }
                else
                {
                    LogError("CreateSquad returned null!");
                }
            }
            catch (System.Exception e)
            {
                LogError($"Exception during spawn: {e.Message}");
                LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        // Logging helpers
        private void Log(string message)
        {
            _debugLog.Add(message);
            Debug.Log(message);
        }
        
        private void LogSuccess(string message)
        {
            string msg = $"[SUCCESS] {message}";
            _debugLog.Add(msg);
            Debug.Log($"<color=green>{msg}</color>");
        }
        
        private void LogError(string message)
        {
            string msg = $"[ERROR] {message}";
            _debugLog.Add(msg);
            Debug.LogError(msg);
        }
        
        private void LogWarning(string message)
        {
            string msg = $"[WARNING] {message}";
            _debugLog.Add(msg);
            Debug.LogWarning(msg);
        }
        
        // GUI for debug log
        private void OnGUI()
        {
            if (_debugLog.Count == 0) return;
            
            float width = 400f;
            float height = 300f;
            float x = Screen.width - width - 10;
            float y = Screen.height - height - 10;
            
            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUI.Box(new Rect(0, 0, width, height), "Debug Log");
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(width), GUILayout.Height(height - 30));
            
            foreach (string logEntry in _debugLog)
            {
                GUIStyle style = GUI.skin.label;
                
                if (logEntry.Contains("[ERROR]"))
                    style.normal.textColor = Color.red;
                else if (logEntry.Contains("[SUCCESS]"))
                    style.normal.textColor = Color.green;
                else if (logEntry.Contains("[WARNING]"))
                    style.normal.textColor = Color.yellow;
                else
                    style.normal.textColor = Color.white;
                
                GUILayout.Label(logEntry, style);
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}