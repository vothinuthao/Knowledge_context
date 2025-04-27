// EnhancedQuickControl.cs
using UnityEngine;
using Core.ECS;
using Core.Grid;
using Managers;
using Components.Squad;
using Systems.Squad;
using Components;

namespace Helper_Tool
{
    /// <summary>
    /// Enhanced Quick Control Panel with better UI and debugging
    /// </summary>
    public class EnhancedQuickControl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private GridManager _gridManager;
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 _panelPosition = new Vector2(10, 10);
        [SerializeField] private float _panelWidth = 250f;
        [SerializeField] private GUISkin customSkin;
        
        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private bool _autoInitialize = true;
        
        private bool _isInitialized = false;
        private bool _showMenu = true;
        private Entity _selectedSquad;
        private string _statusMessage = "";
        
        // GUI Styles
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _successStyle;
        private GUIStyle _panelStyle;
        
        private void Start()
        {
            InitializeStyles();
            
            if (_autoInitialize)
            {
                AutoInitializeManagers();
            }
            
            ValidateReferences();
        }
        
        private void InitializeStyles()
        {
            // Header style
            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 16;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.normal.textColor = Color.white;
            
            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 14;
            _buttonStyle.padding = new RectOffset(10, 10, 5, 5);
            _buttonStyle.normal.textColor = Color.white;
            
            // Error style
            _errorStyle = new GUIStyle(GUI.skin.label);
            _errorStyle.normal.textColor = Color.red;
            _errorStyle.fontStyle = FontStyle.Bold;
            
            // Success style
            _successStyle = new GUIStyle(GUI.skin.label);
            _successStyle.normal.textColor = Color.green;
            
            // Panel style
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.padding = new RectOffset(10, 10, 10, 10);
        }
        
        private void AutoInitializeManagers()
        {
            Debug.Log("=== Auto Initializing Managers ===");
            
            // GameManager
            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
                if (_gameManager == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _gameManager = go.AddComponent<GameManager>();
                    Debug.LogWarning("Created new GameManager instance");
                }
            }
            
            // WorldManager
            if (_worldManager == null)
            {
                _worldManager = FindObjectOfType<WorldManager>();
                if (_worldManager == null)
                {
                    GameObject go = new GameObject("WorldManager");
                    _worldManager = go.AddComponent<WorldManager>();
                    _worldManager.InitializeWorld();
                    Debug.LogWarning("Created new WorldManager instance");
                }
            }
            
            // GridManager
            if (_gridManager == null)
            {
                _gridManager = GridManager.Instance;
                if (_gridManager == null)
                {
                    GameObject go = new GameObject("GridManager");
                    _gridManager = go.AddComponent<GridManager>();
                    _gridManager.InitializeGrid();
                    Debug.LogWarning("Created new GridManager instance");
                }
            }
            
            // Register essential systems
            if (_worldManager != null && _worldManager.World != null)
            {
                RegisterEssentialSystems();
            }
            
            _isInitialized = true;
        }
        
        private void RegisterEssentialSystems()
        {
            var world = _worldManager.World;
            
            // Check and register SquadCommandSystem
            if (world.GetSystem<SquadCommandSystem>() == null)
            {
                world.RegisterSystem(new SquadCommandSystem());
                Debug.Log("Registered SquadCommandSystem");
            }
            
            // Add other essential systems here
            // ... (SquadFormationSystem, SeekSystem, etc.)
            
            Debug.Log($"Total systems registered: {world.GetRegisteredSystemCount()}");
        }
        
        private void ValidateReferences()
        {
            bool hasError = false;
            
            if (_gameManager == null)
            {
                _statusMessage = "ERROR: GameManager not found!";
                hasError = true;
            }
            else if (_worldManager == null)
            {
                _statusMessage = "ERROR: WorldManager not found!";
                hasError = true;
            }
            else if (_gridManager == null)
            {
                _statusMessage = "ERROR: GridManager not found!";
                hasError = true;
            }
            else if (_worldManager.World == null)
            {
                _statusMessage = "ERROR: World not initialized!";
                hasError = true;
            }
            
            if (!hasError)
            {
                _statusMessage = "All systems ready!";
                Debug.Log("All references validated successfully");
            }
            else
            {
                Debug.LogError(_statusMessage);
            }
        }
        
        private void OnGUI()
        {
            if (customSkin != null)
            {
                GUI.skin = customSkin;
            }
            
            DrawMainPanel();
            
            if (_showDebugInfo)
            {
                DrawDebugInfo();
            }
        }
        
        private void DrawMainPanel()
        {
            float panelHeight = _showMenu ? 500f : 50f;
            Rect panelRect = new Rect(_panelPosition.x, _panelPosition.y, _panelWidth, panelHeight);
            
            GUI.Box(panelRect, "", _panelStyle);
            
            // Toggle button
            if (GUI.Button(new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 20, 30),
                _showMenu ? "Hide Menu" : "Show Menu", _buttonStyle))
            {
                _showMenu = !_showMenu;
            }
            
            if (!_showMenu) return;
            
            float yOffset = panelRect.y + 50;
            float buttonHeight = 35f;
            float spacing = 5f;
            
            // Header
            GUI.Label(new Rect(panelRect.x, yOffset, panelRect.width, 30), "Quick Control Panel", _headerStyle);
            yOffset += 40;
            
            // Status message
            GUIStyle statusStyle = _statusMessage.Contains("ERROR") ? _errorStyle : _successStyle;
            GUI.Label(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, 25), _statusMessage, statusStyle);
            yOffset += 30;
            
            // Initialize button (if needed)
            if (!_isInitialized)
            {
                if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                    "Initialize Systems", _buttonStyle))
                {
                    AutoInitializeManagers();
                    ValidateReferences();
                }
                yOffset += buttonHeight + spacing;
            }
            
            // Spawn buttons
            GUI.enabled = _isInitialized;
            
            if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                "Spawn at Center", _buttonStyle))
            {
                SpawnSquadAtCenter();
            }
            yOffset += buttonHeight + spacing;
            
            if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                "Spawn at Mouse", _buttonStyle))
            {
                SpawnSquadAtMouse();
            }
            yOffset += buttonHeight + spacing;
            
            if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                "Spawn Enemy", _buttonStyle))
            {
                SpawnEnemySquad();
            }
            yOffset += buttonHeight + spacing;
            
            if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                "Spawn Formation", _buttonStyle))
            {
                SpawnSquadsInFormation();
            }
            yOffset += buttonHeight + spacing * 2;
            
            // Selected squad info
            if (_selectedSquad != null)
            {
                GUI.Label(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, 25),
                    $"Selected: Squad {_selectedSquad.Id}", _successStyle);
                yOffset += 30;
                
                if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                    "Stop Squad", _buttonStyle))
                {
                    StopSelectedSquad();
                }
                yOffset += buttonHeight + spacing;
            }
            else
            {
                GUI.Label(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, 25),
                    "No squad selected", _errorStyle);
                yOffset += 30;
            }
            
            // Debug buttons
            yOffset += 10;
            if (GUI.Button(new Rect(panelRect.x + 10, yOffset, panelRect.width - 20, buttonHeight),
                "Debug Systems", _buttonStyle))
            {
                DebugSystems();
            }
            
            GUI.enabled = true;
        }
        
        private void DrawDebugInfo()
        {
            float debugWidth = 250f;
            float debugHeight = 200f;
            float debugX = Screen.width - debugWidth - 10;
            float debugY = 10;
            
            Rect debugRect = new Rect(debugX, debugY, debugWidth, debugHeight);
            GUI.Box(debugRect, "Debug Info", _panelStyle);
            
            float yOffset = debugY + 30;
            float spacing = 20f;
            
            GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                $"FPS: {(int)(1.0f / Time.deltaTime)}", _headerStyle);
            yOffset += spacing;
            
            GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                $"GameManager: {(_gameManager != null ? "OK" : "NULL")}", _gameManager != null ? _successStyle : _errorStyle);
            yOffset += spacing;
            
            GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                $"WorldManager: {(_worldManager != null ? "OK" : "NULL")}", _worldManager != null ? _successStyle : _errorStyle);
            yOffset += spacing;
            
            GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                $"GridManager: {(_gridManager != null ? "OK" : "NULL")}", _gridManager != null ? _successStyle : _errorStyle);
            yOffset += spacing;
            
            if (_worldManager != null && _worldManager.World != null)
            {
                GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                    $"Entities: {_worldManager.World.GetEntityCount()}", _successStyle);
                yOffset += spacing;
                
                GUI.Label(new Rect(debugX + 10, yOffset, debugWidth - 20, 20),
                    $"Systems: {_worldManager.World.GetRegisteredSystemCount()}", _successStyle);
            }
        }
        
        // Spawn methods with detailed logging
        private void SpawnSquadAtCenter()
        {
            Debug.Log("=== SpawnSquadAtCenter Started ===");
            
            if (!ValidateManagersForSpawn()) return;
            
            Vector2Int centerGrid = new Vector2Int(10, 10);
            Vector3 spawnPos = _gridManager.GetCellCenter(centerGrid);
            Debug.Log($"Spawn position calculated: {spawnPos}");
            
            try
            {
                var squad = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
                if (squad != null)
                {
                    _selectedSquad = squad;
                    _statusMessage = $"Spawned Squad {squad.Id} at center";
                    Debug.Log($"Successfully spawned squad {squad.Id}");
                }
                else
                {
                    _statusMessage = "Failed to spawn squad!";
                    Debug.LogError("CreateSquad returned null");
                }
            }
            catch (System.Exception e)
            {
                _statusMessage = $"Error: {e.Message}";
                Debug.LogError($"Exception while spawning squad: {e}");
            }
        }
        
        private void SpawnSquadAtMouse()
        {
            if (!ValidateManagersForSpawn()) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
                Vector3 spawnPos = _gridManager.GetCellCenter(gridPos);
                
                var squad = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
                if (squad != null)
                {
                    _selectedSquad = squad;
                    _statusMessage = $"Spawned Squad {squad.Id} at mouse";
                }
            }
            else
            {
                _statusMessage = "No valid ground position!";
            }
        }
        
        private void SpawnEnemySquad()
        {
            if (!ValidateManagersForSpawn()) return;
            
            Vector2Int randomGrid = new Vector2Int(Random.Range(0, 20), Random.Range(0, 20));
            Vector3 spawnPos = _gridManager.GetCellCenter(randomGrid);
            
            var squad = _gameManager.CreateSquad(null, spawnPos, Faction.ENEMY);
            if (squad != null)
            {
                _statusMessage = $"Spawned Enemy Squad {squad.Id}";
            }
        }
        
        private void SpawnSquadsInFormation()
        {
            if (!ValidateManagersForSpawn()) return;
            
            int squadCount = 4;
            float radius = 9.0f;
            Vector2Int centerGrid = new Vector2Int(10, 10);
            Vector3 center = _gridManager.GetCellCenter(centerGrid);
            
            for (int i = 0; i < squadCount; i++)
            {
                float angle = i * (360f / squadCount) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 spawnPos = center + offset;
                
                _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
            }
            
            _statusMessage = $"Spawned {squadCount} squads in formation";
        }
        
        private void StopSelectedSquad()
        {
            if (_selectedSquad == null) return;
            
            var commandSystem = _worldManager?.World?.GetSystem<SquadCommandSystem>();
            if (commandSystem != null)
            {
                var position = _selectedSquad.GetComponent<PositionComponent>();
                if (position != null)
                {
                    commandSystem.CommandMove(_selectedSquad, position.Position);
                    _statusMessage = "Squad stopped";
                }
            }
        }
        
        // Helper methods
        private bool ValidateManagersForSpawn()
        {
            if (_gameManager == null)
            {
                _statusMessage = "ERROR: GameManager is null!";
                Debug.LogError(_statusMessage);
                return false;
            }
            
            if (_worldManager == null || _worldManager.World == null)
            {
                _statusMessage = "ERROR: WorldManager or World is null!";
                Debug.LogError(_statusMessage);
                return false;
            }
            
            if (_gridManager == null)
            {
                _statusMessage = "ERROR: GridManager is null!";
                Debug.LogError(_statusMessage);
                return false;
            }
            
            return true;
        }
        
        private void DebugSystems()
        {
            Debug.Log("=== System Debug Information ===");
            
            if (_worldManager?.World == null)
            {
                Debug.LogError("World is null!");
                return;
            }
            
            var world = _worldManager.World;
            Debug.Log($"Total Systems: {world.GetRegisteredSystemCount()}");
            
            // Check specific systems
            var commandSystem = world.GetSystem<SquadCommandSystem>();
            Debug.Log($"SquadCommandSystem: {(commandSystem != null ? "OK" : "MISSING")}");
            
            // Add checks for other critical systems
            
            Debug.Log("=== End System Debug ===");
        }
        
        // Input handling
        private void Update()
        {
            // Quick spawn shortcut
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SpawnSquadAtCenter();
            }
            
            // Select squad on click
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectSquad();
            }
            
            // Move squad on right click
            if (Input.GetMouseButtonDown(1) && _selectedSquad != null)
            {
                MoveSelectedSquad();
            }
        }
        
        private void TrySelectSquad()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check for EntityBehaviour component
                var entityBehaviour = hit.collider.GetComponent<EntityBehaviour>();
                if (entityBehaviour != null)
                {
                    var entity = entityBehaviour.GetEntity();
                    if (entity != null && entity.HasComponent<SquadStateComponent>())
                    {
                        _selectedSquad = entity;
                        _statusMessage = $"Selected Squad {entity.Id}";
                    }
                }
            }
        }
        
        private void MoveSelectedSquad()
        {
            if (_selectedSquad == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
                Vector3 targetPos = _gridManager.GetCellCenter(gridPos);
                
                var commandSystem = _worldManager?.World?.GetSystem<SquadCommandSystem>();
                if (commandSystem != null)
                {
                    commandSystem.CommandMove(_selectedSquad, targetPos);
                    _statusMessage = $"Moving to {targetPos}";
                }
            }
        }
    }
}