// ProjectStartup.cs

using Core.Grid;
using Data;
using Managers;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using UnityEngine;

namespace Helper_Tool
{
    /// <summary>
    /// Startup script to initialize the project quickly
    /// </summary>
    public class ProjectStartup : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private UIManager _uiManager;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _troopPrefab;
        [SerializeField] private GameObject _squadPrefab;
        
        [Header("Configs")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private SquadConfig _defaultSquadConfig;
        [SerializeField] private TroopConfig _defaultTroopConfig;
        
        private void Awake()
        {
            InitializeManagers();
            // InitializeECS();
            InitializeSystems();
            SetupTestEnvironment();
        }
        
        private void InitializeManagers()
        {
            // Initialize Grid Manager
            if (_gridManager != null)
            {
                _gridManager.InitializeGrid();
                Debug.Log("GridManager initialized");
            }
            
            // Initialize World Manager
            if (_worldManager != null)
            {
                _worldManager.InitializeWorld();
                Debug.Log("WorldManager initialized");
            }
            
            // Set up Game Manager references
            if (_gameManager != null)
            {
                // Game manager will handle the rest of initialization
                Debug.Log("GameManager initialized");
            }
        }
        
        private void InitializeSystems()
        {
            if (_worldManager == null || _worldManager.World == null)
            {
                Debug.LogError("World not initialized!");
                return;
            }
            
            var world = _worldManager.World;
            
            // Register systems in priority order
            
            // Squad systems
            world.RegisterSystem(new SquadCommandSystem());
            world.RegisterSystem(new EntityDetectionSystem());
            
            // Steering systems
            world.RegisterSystem(new SteeringSystem());
            world.RegisterSystem(new SeekSystem());
            world.RegisterSystem(new SeparationSystem());
            world.RegisterSystem(new AlignmentSystem());
            world.RegisterSystem(new CohesionSystem());
            world.RegisterSystem(new ArrivalSystem());
            world.RegisterSystem(new FleeSystem());
            
            // Formation systems
            world.RegisterSystem(new SquadFormationSystem());
            
            // Movement systems
            world.RegisterSystem(new MovementSystem());
            world.RegisterSystem(new RotationSystem());
            
            Debug.Log($"Registered {world.GetRegisteredSystemCount()} systems");
        }
        
        private void SetupTestEnvironment()
        {
            if (_gameConfig != null && _gameConfig.CreateTestSquads)
            {
                CreateTestSquad();
            }
        }
        
        private void CreateTestSquad()
        {
            if (_gameManager == null) return;
            
            // Create player squad at center of grid
            Vector3 spawnPosition = _gridManager.GetCellCenter(new Vector2Int(10, 10));
            
            var squad = _gameManager.CreateSquad(_defaultSquadConfig, spawnPosition, Faction.PLAYER);
            
            Debug.Log($"Created test squad at {spawnPosition}");
        }
        
        private void Update()
        {
            // Debug commands
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleDebugMode();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SpawnSquadAtMousePosition();
            }
        }
        
        private void ToggleDebugMode()
        {
            // Implementation for debug mode toggle
        }
        
        private void SpawnSquadAtMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
                Vector3 spawnPos = _gridManager.GetCellCenter(gridPos);
                
                _gameManager.CreateSquad(_defaultSquadConfig, spawnPos, Faction.PLAYER);
            }
        }
        
        private void OnGUI()
        {
            // Quick debug info
            GUI.Box(new Rect(10, 10, 200, 100), "Viking Raven Debug");
            GUI.Label(new Rect(20, 30, 180, 20), $"FPS: {1.0f / Time.deltaTime:F0}");
            
            if (_worldManager != null && _worldManager.World != null)
            {
                GUI.Label(new Rect(20, 50, 180, 20), $"Entities: {_worldManager.World.GetEntityCount()}");
                GUI.Label(new Rect(20, 70, 180, 20), $"Systems: {_worldManager.World.GetRegisteredSystemCount()}");
            }
        }
    }
}