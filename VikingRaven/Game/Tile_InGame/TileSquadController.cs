using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;
using VikingRaven.Units.Systems;
using VikingRaven.Game.Tile_InGame;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Controls squad movement based on tile system
    /// Uses model-based architecture with improved organization and error handling
    /// </summary>
    public class TileSquadController : MonoBehaviour
    {
        #region Inspector Fields

        [TitleGroup("References")]
        [Tooltip("Main camera in the scene")]
        [SerializeField] private Camera _mainCamera;
        
        [TitleGroup("Layer Settings")]
        [Tooltip("Layer mask for tiles")]
        [SerializeField] 
        private LayerMask _tileLayer = -1;
        
        [Tooltip("Layer mask for units")]
        [SerializeField] 
        private LayerMask _unitLayer = -1;

        [TitleGroup("Reference to unit factory")]
        [SerializeField] private UnitFactory _unitFactory;

        [TitleGroup("Combat System Reference")]
        [SerializeField] private CombatSystemController _combatController;

        [TitleGroup("Squad Settings")]
        [Tooltip("Number of troops in each squad")]
        [SerializeField, Range(3, 16), ProgressBar(3, 16)] 
        private int _troopsPerSquad = 9;
        
        [Tooltip("Currently selected squad ID")]
        [SerializeField, ReadOnly] 
        private int _selectedSquadId = -1;
        
        [Tooltip("Current formation type")]
        [SerializeField, EnumToggleButtons] 
        private FormationType _currentFormation = FormationType.Line;
        
        [Tooltip("Prefab for the selection marker")]
        [SerializeField, PreviewField(50), Required] 
        private GameObject _selectionMarkerPrefab;
        
        [Tooltip("Height of the selection marker above the ground")]
        [SerializeField, Range(0.1f, 2f)] 
        private float _selectionMarkerHeight = 0.5f;
        
        [TitleGroup("Spawn Settings")]
        [Tooltip("Whether to spawn squads at start")]
        [SerializeField, ToggleLeft] 
        private bool _spawnAtStart = true;
        
        [Tooltip("Number of player squads to spawn at start")]
        [SerializeField, Range(0, 5), ShowIf("_spawnAtStart")] 
        private int _initialPlayerSquads = 1;
        
        [Tooltip("Number of enemy squads to spawn at start")]
        [SerializeField, Range(0, 5), ShowIf("_spawnAtStart")] 
        private int _initialEnemySquads = 2;
        
        [TitleGroup("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField, ToggleLeft] 
        private bool _debugMode = false;

        #endregion

        #region Private Fields

        // References to singleton managers
        private TileManager _tileManager;
        private SquadFactory _squadFactory;
        private FormationSystem _formationSystem;
        private SquadCoordinationSystem _squadCoordinationSystem;
        
        // UI elements
        private GameObject _selectionMarker;
        
        // Movement state
        private bool _isMoving = false;
        private TileComponent _currentTileComponent; // Current tile of selected squad
        private TileComponent _targetTileComponent; // Target tile
        
        // Dictionary to track formation settings per squad
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();

        #endregion

        #region Unity Lifecycle Methods

        private void Start()
        {
            // Get references
            InitializeReferences();
            
            // Create selection marker
            CreateSelectionMarker();
            
            // Spawn initial units if set
            if (_spawnAtStart)
            {
                SpawnInitialSquads();
            }
            
            if (_debugMode)
            {
                Debug.Log("TileSquadController: Initialized. Select a squad by clicking on their units, then click on any highlighted tile to move.");
            }
        }

        private void Update()
        {
            // Handle input for squad selection and movement
            HandleInput();
            
            // Update movement state for visuals if needed
            if (_isMoving)
            {
                UpdateMovementState();
            }
            
            // Debug info
            if (_debugMode && _selectedSquadId >= 0)
            {
                TileComponent squadTileComponent = _tileManager?.GetTileBySquadId(_selectedSquadId);
                string tileInfo = squadTileComponent != null ? $"on Tile {squadTileComponent.TileId}" : "not on any tile";
                
                Debug.Log($"Selected Squad: {_selectedSquadId}, Formation: {_currentFormation}, {tileInfo}");
            }
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initialize all required references
        /// </summary>
        private void InitializeReferences()
        {
            // Get main camera if not assigned
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null && _debugMode)
                {
                    Debug.LogWarning("TileSquadController: No main camera found!");
                }
            }
            
            // Get TileManager singleton
            _tileManager = TileManager.Instance;
            if (_tileManager == null && _debugMode)
            {
                Debug.LogError("TileSquadController: TileManager singleton not found!");
            }
            
            // Get SquadFactory singleton
            _squadFactory = SquadFactory.Instance;
            if (_squadFactory == null && _debugMode)
            {
                Debug.LogError("TileSquadController: SquadFactory singleton not found!");
            }
            
            // Find SquadCoordinationSystem if needed
            _squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
            if (_squadCoordinationSystem == null && _debugMode)
            {
                Debug.LogWarning("TileSquadController: SquadCoordinationSystem not found!");
            }
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null && _debugMode)
            {
                Debug.LogWarning("TileSquadController: FormationSystem not found!");
            }
            
            // Check UnitFactory
            if (_unitFactory == null)
            {
                _unitFactory = FindObjectOfType<UnitFactory>();
                if (_unitFactory == null && _debugMode)
                {
                    Debug.LogError("TileSquadController: UnitFactory not found!");
                }
            }
            
            // Check CombatController
            if (_combatController == null)
            {
                _combatController = FindObjectOfType<CombatSystemController>();
                if (_combatController == null && _debugMode)
                {
                    Debug.LogWarning("TileSquadController: CombatSystemController not found!");
                }
            }
            
            // Set up layer masks if not assigned
            if (_tileLayer == 0)
            {
                _tileLayer = LayerMask.GetMask("Tile", "Default");
                if (_debugMode)
                {
                    Debug.Log("TileSquadController: Tile layer not specified, using default layers");
                }
            }
            
            if (_unitLayer == 0)
            {
                _unitLayer = LayerMask.GetMask("Unit", "Enemy");
                if (_debugMode)
                {
                    Debug.Log("TileSquadController: Unit layer not specified, using default layers");
                }
            }
        }
        
        /// <summary>
        /// Create the selection marker for visual feedback
        /// </summary>
        private void CreateSelectionMarker()
        {
            if (_selectionMarkerPrefab != null)
            {
                _selectionMarker = Instantiate(_selectionMarkerPrefab);
                _selectionMarker.SetActive(false);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("TileSquadController: No selection marker prefab assigned");
            }
        }
        
        /// <summary>
        /// Spawn initial squads based on settings
        /// </summary>
        private void SpawnInitialSquads()
        {
            if (_squadFactory == null || _tileManager == null)
            {
                Debug.LogError("TileSquadController: Cannot spawn initial squads - required managers not available");
                return;
            }

            // Spawn player squads
            for (int i = 0; i < _initialPlayerSquads; i++)
            {
                UnitType unitType = (UnitType)(i % 3); // Cycle through Infantry, Archer, Pike
                SpawnSquad(unitType, false);
            }
            
            // Spawn enemy squads
            for (int i = 0; i < _initialEnemySquads; i++)
            {
                UnitType unitType = (UnitType)(i % 3); // Cycle through Infantry, Archer, Pike
                SpawnSquad(unitType, true);
            }
            
            if (_debugMode)
            {
                Debug.Log($"TileSquadController: Spawned {_initialPlayerSquads} player squads and {_initialEnemySquads} enemy squads");
            }
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle player input for squad selection and movement
        /// </summary>
        private void HandleInput()
        {
            // Left click to select squad or move squad to tile
            if (Input.GetMouseButtonDown(0))
            {
                // Check if clicking on UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;
                
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                
                // Try selecting a unit first
                if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, _unitLayer))
                {
                    SelectSquad(unitHit.collider.gameObject);
                }
                // Then try selecting a tile
                else if (Physics.Raycast(ray, out RaycastHit tileHit, 100f, _tileLayer))
                {
                    TileComponent tileComponent = tileHit.collider.gameObject.GetComponent<TileComponent>();
                    if (tileComponent != null)
                    {
                        HandleTileSelection(tileComponent);
                    }
                }
            }
            
            // Right click for alternative actions (cycles formations)
            if (Input.GetMouseButtonDown(1))
            {
                if (_selectedSquadId >= 0)
                {
                    CycleFormation();
                }
            }
            
            // Number keys to change formation
            HandleFormationKeyInput();
            
            // Spawn hotkeys (I, A, P keys)
            HandleSpawnKeyInput();
        }
        
        /// <summary>
        /// Handle numeric key input for formation changes
        /// </summary>
        private void HandleFormationKeyInput()
        {
            if (_selectedSquadId < 0) return;
            
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ChangeFormation(FormationType.Line);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                ChangeFormation(FormationType.Phalanx);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                ChangeFormation(FormationType.Circle);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                ChangeFormation(FormationType.Testudo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                ChangeFormation(FormationType.Column);
            }
        }
        
        /// <summary>
        /// Handle spawn hotkey input for debugging/testing
        /// </summary>
        private void HandleSpawnKeyInput()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                SpawnSquad(UnitType.Infantry, false);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                SpawnSquad(UnitType.Archer, false);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                SpawnSquad(UnitType.Pike, false);
            }
            
            // With Shift key to spawn enemy units
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.I))
                {
                    SpawnSquad(UnitType.Infantry, true);
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    SpawnSquad(UnitType.Archer, true);
                }
                else if (Input.GetKeyDown(KeyCode.P))
                {
                    SpawnSquad(UnitType.Pike, true);
                }
            }
        }

        #endregion

        #region Squad Selection and Movement

        /// <summary>
        /// Select a squad from a unit GameObject
        /// </summary>
        private void SelectSquad(GameObject unitObject)
        {
            if (unitObject == null || _squadFactory == null || _unitFactory == null) return;
            
            // Get the entity and unit model
            var baseEntity = unitObject.GetComponent<BaseEntity>();
            if (baseEntity == null) return;
            
            UnitModel unitModel = _unitFactory.GetUnitModel(baseEntity.Id);
            if (unitModel == null) return;
            
            // Get the squad ID
            int squadId = unitModel.SquadId;
            if (squadId < 0) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(squadId);
            if (squadModel == null) return;
            
            // Check if this is an enemy squad
            if (squadModel.Data != null && squadModel.Data.Faction == "Enemy")
            {
                if (_debugMode)
                {
                    Debug.Log($"Selected enemy Squad ID: {squadId}. Cannot control enemy squads.");
                }
                return;
            }
            
            // Get current tile
            TileComponent squadTileComponent = _tileManager?.GetTileBySquadId(squadId);
            
            // Process selection
            _selectedSquadId = squadId;
            _currentTileComponent = squadTileComponent;
            
            // Get current formation from dictionary or from squad model
            if (_squadFormationTypes.TryGetValue(squadId, out FormationType formation))
            {
                _currentFormation = formation;
            }
            else
            {
                _currentFormation = squadModel.CurrentFormation;
                _squadFormationTypes[squadId] = _currentFormation;
            }
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(true);
                Vector3 markerPosition = unitObject.transform.position;
                markerPosition.y += _selectionMarkerHeight;
                _selectionMarker.transform.position = markerPosition;
            }
            
            // Highlight all valid tiles for movement
            if (_tileManager != null)
            {
                _tileManager.HighlightAllValidTiles(_selectedSquadId);
            }
            
            if (_debugMode)
            {
                Debug.Log($"Selected Squad ID: {_selectedSquadId}, on Tile: {(_currentTileComponent != null ? _currentTileComponent.TileId.ToString() : "none")}");
            }
        }
        
        /// <summary>
        /// Handle tile selection for movement
        /// </summary>
        private void HandleTileSelection(TileComponent selectedTileComponent)
        {
            // If no squad selected, do nothing
            if (_selectedSquadId < 0)
            {
                if (_debugMode)
                {
                    Debug.Log("No squad selected. Select a squad first.");
                }
                return;
            }
            
            // If current tile is selected, just update formation
            if (_currentTileComponent != null && selectedTileComponent.TileId == _currentTileComponent.TileId)
            {
                UpdateFormationInPlace();
                return;
            }
            
            // Check if tile is valid for movement
            if (selectedTileComponent.IsValidDestination(_selectedSquadId))
            {
                MoveSquadToTile(selectedTileComponent);
            }
            else if (_debugMode)
            {
                Debug.Log($"Cannot move to Tile {selectedTileComponent.TileId}. It's occupied by another squad.");
            }
        }
        
        /// <summary>
        /// Move selected squad to a tile
        /// </summary>
        private void MoveSquadToTile(TileComponent destinationTileComponent)
        {
            if (_selectedSquadId < 0 || destinationTileComponent == null || _squadFactory == null) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Set target tile
            _targetTileComponent = destinationTileComponent;
            
            // Get squad size
            int squadSize = squadModel.UnitCount;
            
            // Get optimal formation for the destination tile
            FormationType optimalFormation = _targetTileComponent.GetOptimalFormation(squadSize);
            float formationScale = _targetTileComponent.GetFormationScale(squadSize);
            
            // Set up movement state
            _isMoving = true;
            
            // Register squad as moving to the new tile
            if (_tileManager != null)
            {
                _tileManager.RegisterSquadOnTile(_selectedSquadId, _targetTileComponent.TileId);
            }
            
            // Update formation tracking
            _currentFormation = optimalFormation;
            _squadFormationTypes[_selectedSquadId] = optimalFormation;
            
            // Apply to Squad Model
            squadModel.SetFormation(optimalFormation);
            squadModel.SetFormationSpacing(formationScale);
            squadModel.SetTargetPosition(_targetTileComponent.CenterPosition);
            
            // Use CombatController as backup if direct model update doesn't work
            if (_combatController != null)
            {
                _combatController.MoveSquad(_selectedSquadId, _targetTileComponent.CenterPosition);
                _combatController.SetSquadFormation(_selectedSquadId, optimalFormation);
            }
            else if (_squadCoordinationSystem != null)
            {
                // Fallback to old system
                _squadCoordinationSystem.MoveSquadToPosition(_selectedSquadId, _targetTileComponent.CenterPosition);
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, optimalFormation);
            }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.transform.position = _targetTileComponent.CenterPosition + Vector3.up * _selectionMarkerHeight;
            }
            
            // Clear tile highlights
            if (_tileManager != null)
            {
                _tileManager.ClearHighlights();
            }
            
            if (_debugMode)
            {
                Debug.Log($"Moving Squad {_selectedSquadId} to Tile {_targetTileComponent.TileId} with formation {optimalFormation}");
            }
        }
        
        /// <summary>
        /// Update the movement state during squad movement
        /// </summary>
        private void UpdateMovementState()
        {
            if (_selectedSquadId < 0 || _squadFactory == null) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Check if the squad is still moving
            if (!squadModel.IsMoving)
            {
                CompleteMovement();
            }
        }
        
        /// <summary>
        /// Complete the movement process
        /// </summary>
        private void CompleteMovement()
        {
            _isMoving = false;
            
            if (_selectedSquadId < 0 || _squadFactory == null) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Fine-tune final position if needed
            if (_targetTileComponent != null)
            {
                squadModel.SetTargetPosition(_targetTileComponent.CenterPosition);
                
                if (_debugMode)
                {
                    Debug.Log($"Squad {_selectedSquadId} movement to Tile {_targetTileComponent.TileId} completed");
                }
            }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Highlight all valid tiles for next movement
            if (_tileManager != null)
            {
                _tileManager.HighlightAllValidTiles(_selectedSquadId);
            }
        }

        #endregion

        #region Formation Management

        /// <summary>
        /// Change the formation of the selected squad
        /// </summary>
        private void ChangeFormation(FormationType formation)
        {
            if (_selectedSquadId < 0 || _squadFactory == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning("No squad selected to change formation");
                }
                return;
            }
            
            _currentFormation = formation;
            _squadFormationTypes[_selectedSquadId] = formation;
            
            // Set formation using squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel != null)
            {
                squadModel.SetFormation(formation);
                
                if (_debugMode)
                {
                    Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation}");
                }
            }
            else if (_combatController != null)
            {
                // Fallback to combat controller
                _combatController.SetSquadFormation(_selectedSquadId, formation);
            }
            else if (_squadCoordinationSystem != null)
            {
                // Second fallback to old system
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
            }
        }
        
        /// <summary>
        /// Cycle through available formations
        /// </summary>
        private void CycleFormation()
        {
            FormationType nextFormation;
            
            switch (_currentFormation)
            {
                case FormationType.Line:
                    nextFormation = FormationType.Phalanx;
                    break;
                case FormationType.Phalanx:
                    nextFormation = FormationType.Circle;
                    break;
                case FormationType.Circle:
                    nextFormation = FormationType.Testudo;
                    break;
                case FormationType.Testudo:
                    nextFormation = FormationType.Column;
                    break;
                default:
                    nextFormation = FormationType.Line;
                    break;
            }
            
            ChangeFormation(nextFormation);
        }
        
        /// <summary>
        /// Update formation in the current tile without moving
        /// </summary>
        private void UpdateFormationInPlace()
        {
            if (_selectedSquadId < 0 || _currentTileComponent == null || _squadFactory == null) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Get optimal formation for current tile
            FormationType optimalFormation = _currentTileComponent.GetOptimalFormation(squadModel.UnitCount);
            
            // Apply formation
            ChangeFormation(optimalFormation);
            
            if (_debugMode)
            {
                Debug.Log($"Updated Squad {_selectedSquadId} formation to {optimalFormation} in place");
            }
        }

        #endregion

        #region Squad Creation and Management

        /// <summary>
        /// Spawn a squad of the specified type
        /// </summary>
        [Button("Spawn Squad")]
        public int SpawnSquad(UnitType unitType, bool isEnemy = false)
        {
            if (_tileManager == null || _squadFactory == null)
            {
                Debug.LogError("TileSquadController: Cannot spawn squad - required managers not available");
                return -1;
            }
            
            // Find appropriate spawn tile
            TileComponent spawnTileComponent = _tileManager.GetSpawnTileByUnitType(unitType, isEnemy);
            
            if (spawnTileComponent == null)
            {
                Debug.LogError($"TileSquadController: No spawn tile available for {unitType}");
                return -1;
            }
            
            // Create squad using squad factory
            SquadModel squadModel = _squadFactory.CreateSquad(
                unitType, 
                _troopsPerSquad, 
                spawnTileComponent.CenterPosition, 
                Quaternion.identity, 
                isEnemy
            );
            
            if (squadModel == null || squadModel.UnitCount == 0)
            {
                Debug.LogError($"TileSquadController: Failed to create squad at {spawnTileComponent.CenterPosition}");
                return -1;
            }
            
            int squadId = squadModel.SquadId;
            
            // Register squad on tile
            _tileManager.RegisterSquadOnTile(squadId, spawnTileComponent.TileId);
            
            // Set initial formation based on tile
            FormationType initialFormation = spawnTileComponent.GetOptimalFormation(squadModel.UnitCount);
            squadModel.SetFormation(initialFormation);
            
            // Store in formation tracking
            _squadFormationTypes[squadId] = initialFormation;
            
            if (_debugMode)
            {
                Debug.Log($"TileSquadController: Spawned {(isEnemy ? "enemy" : "player")} {unitType} squad with ID {squadId} at Tile {spawnTileComponent.TileId}");
            }
            
            return squadId;
        }
        
        /// <summary>
        /// Create a mixed squad with different unit types
        /// </summary>
        [Button("Spawn Mixed Squad")]
        public int SpawnMixedSquad(bool isEnemy = false)
        {
            if (_tileManager == null || _squadFactory == null)
            {
                Debug.LogError("TileSquadController: Cannot spawn mixed squad - required managers not available");
                return -1;
            }
            
            // Find appropriate spawn tile
            TileComponent spawnTileComponent = isEnemy 
                ? _tileManager.GetFreeEnemySpawnTile() 
                : _tileManager.GetFreePlayerSpawnTile();
            
            if (spawnTileComponent == null)
            {
                Debug.LogError("TileSquadController: No spawn tile available for mixed squad");
                return -1;
            }
            
            // Define unit composition
            Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>
            {
                { UnitType.Infantry, 4 },
                { UnitType.Archer, 3 },
                { UnitType.Pike, 2 }
            };
            
            // Create squad using squad factory
            SquadModel squadModel = _squadFactory.CreateMixedSquad(
                unitCounts, 
                spawnTileComponent.CenterPosition, 
                Quaternion.identity, 
                isEnemy
            );
            
            if (squadModel == null || squadModel.UnitCount == 0)
            {
                Debug.LogError($"TileSquadController: Failed to create mixed squad at {spawnTileComponent.CenterPosition}");
                return -1;
            }
            
            int squadId = squadModel.SquadId;
            
            // Register squad on tile
            _tileManager.RegisterSquadOnTile(squadId, spawnTileComponent.TileId);
            
            // Set initial formation based on tile
            FormationType initialFormation = spawnTileComponent.GetOptimalFormation(squadModel.UnitCount);
            squadModel.SetFormation(initialFormation);
            
            // Store in formation tracking
            _squadFormationTypes[squadId] = initialFormation;
            
            if (_debugMode)
            {
                Debug.Log($"TileSquadController: Spawned {(isEnemy ? "enemy" : "player")} mixed squad with ID {squadId} at Tile {spawnTileComponent.TileId}");
            }
            
            return squadId;
        }
        
        /// <summary>
        /// Disband the currently selected squad
        /// </summary>
        [Button("Disband Selected Squad")]
        public void DisbandSelectedSquad()
        {
            if (_selectedSquadId < 0 || _squadFactory == null) return;
            
            // Disband the squad
            _squadFactory.DisbandSquad(_selectedSquadId);
            
            // Clear selection
            _selectionMarker?.SetActive(false);
            _selectedSquadId = -1;
            _currentTileComponent = null;
            
            // Clear highlights
            if (_tileManager != null)
            {
                _tileManager.ClearHighlights();
            }
            
            if (_debugMode)
            {
                Debug.Log("Disbanded selected squad");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Called from the UI to set formation
        /// </summary>
        public void SetFormationFromUI(int formationIndex)
        {
            FormationType formation = (FormationType)formationIndex;
            ChangeFormation(formation);
        }
        
        /// <summary>
        /// GUI option to spawn squads
        /// </summary>
        public void SpawnSquadFromUI(int unitTypeIndex, bool isEnemy = false)
        {
            UnitType unitType = (UnitType)unitTypeIndex;
            SpawnSquad(unitType, isEnemy);
        }
        
        /// <summary>
        /// Get all player squad IDs
        /// </summary>
        public List<int> GetPlayerSquadIds()
        {
            return _squadFactory != null ? _squadFactory.GetPlayerSquadIds() : new List<int>();
        }
        
        /// <summary>
        /// Get all enemy squad IDs
        /// </summary>
        public List<int> GetEnemySquadIds()
        {
            return _squadFactory != null ? _squadFactory.GetEnemySquadIds() : new List<int>();
        }
        
        /// <summary>
        /// Debug game state information
        /// </summary>
        [Button("Debug Game State")]
        private void DebugGameState()
        {
            if (_squadFactory == null || _tileManager == null) return;
            
            Debug.Log($"--- Game State Debug ---");
            
            List<int> playerSquadIds = _squadFactory.GetPlayerSquadIds();
            Debug.Log($"Player Squads: {playerSquadIds.Count}");
            
            foreach (var squadId in playerSquadIds)
            {
                SquadModel squad = _squadFactory.GetSquad(squadId);
                TileComponent tileComponent = _tileManager.GetTileBySquadId(squadId);
                string tileInfo = tileComponent != null ? $"on Tile {tileComponent.TileId}" : "not on any tile";
                
                if (squad != null)
                {
                    Debug.Log($"- Player Squad {squadId} (Units: {squad.UnitCount}, Formation: {squad.CurrentFormation}) {tileInfo}");
                }
            }
            
            List<int> enemySquadIds = _squadFactory.GetEnemySquadIds();
            Debug.Log($"Enemy Squads: {enemySquadIds.Count}");
            
            foreach (var squadId in enemySquadIds)
            {
                SquadModel squad = _squadFactory.GetSquad(squadId);
                TileComponent tileComponent = _tileManager.GetTileBySquadId(squadId);
                string tileInfo = tileComponent != null ? $"on Tile {tileComponent.TileId}" : "not on any tile";
                
                if (squad != null)
                {
                    Debug.Log($"- Enemy Squad {squadId} (Units: {squad.UnitCount}, Formation: {squad.CurrentFormation}) {tileInfo}");
                }
            }
        }

        #endregion
    }
}