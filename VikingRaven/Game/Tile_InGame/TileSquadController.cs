using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Components;
using VikingRaven.Units.Models;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Controls squad movement based on tile system
    /// Enhanced to work with the new factory and model system
    /// </summary>
    public class TileSquadController : MonoBehaviour
    {
        [TitleGroup("References")]
        [Tooltip("Main camera in the scene")]
        [SerializeField] private Camera _mainCamera;
        
        [Tooltip("Layer mask for tiles")]
        [SerializeField] private LayerMask _tileLayer;
        
        [Tooltip("Layer mask for units")]
        [SerializeField] private LayerMask _unitLayer;
        
        [Tooltip("Reference to squad coordination system")]
        [SerializeField] private SquadCoordinationSystem _squadCoordinationSystem;
        
        [Tooltip("Reference to unit factory")]
        [SerializeField] private UnitFactory _unitFactory;
        
        [Tooltip("Reference to combat system controller")]
        [SerializeField] private CombatSystemController _combatController;
        
        [TitleGroup("Squad Settings")]
        [Tooltip("Number of troops in each squad")]
        [SerializeField, Range(3, 12)] private int _troopsPerSquad = 9;
        
        [Tooltip("Currently selected squad ID")]
        [SerializeField, ReadOnly] private int _selectedSquadId = -1;
        
        [Tooltip("Current formation type")]
        [SerializeField] private FormationType _currentFormation = FormationType.Line;
        
        [Tooltip("Prefab for the selection marker")]
        [SerializeField] private GameObject _selectionMarkerPrefab;
        
        [Tooltip("Height of the selection marker above the ground")]
        [SerializeField, Range(0.1f, 2f)] private float _selectionMarkerHeight = 0.5f;
        
        [TitleGroup("Spawn Settings")]
        [Tooltip("Whether to spawn squads at start")]
        [SerializeField] private bool _spawnAtStart = true;
        
        [Tooltip("Number of player squads to spawn at start")]
        [SerializeField, Range(0, 5), ShowIf("_spawnAtStart")] private int _initialPlayerSquads = 1;
        
        [Tooltip("Number of enemy squads to spawn at start")]
        [SerializeField, Range(0, 5), ShowIf("_spawnAtStart")] private int _initialEnemySquads = 2;
        
        [TitleGroup("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool _debugMode = false;
        
        // References to other systems
        private TileManager _tileManager;
        private FormationSystem _formationSystem;
        private SquadFactory _squadFactory;
        
        // UI elements
        private GameObject _selectionMarker;
        
        // Movement state
        private bool _isMoving = false;
        private TileComponent _currentTileComponent; // Current tile of selected squad
        private TileComponent _targetTileComponent; // Target tile
        
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
            
            Debug.Log("EnhancedTileSquadController: Initialized. Select a squad by clicking on their units, then click on any highlighted tile to move.");
        }
        
        /// <summary>
        /// Initialize all required references
        /// </summary>
        private void InitializeReferences()
        {
            // Get main camera if not assigned
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            
            // Get TileManager
            _tileManager = TileManager.Instance;
            if (_tileManager == null)
            {
                Debug.LogError("EnhancedTileSquadController: TileManager singleton not found!");
            }
            
            // Find SquadCoordinationSystem if not assigned
            if (_squadCoordinationSystem == null)
            {
                _squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
                if (_squadCoordinationSystem == null)
                {
                    Debug.LogWarning("EnhancedTileSquadController: SquadCoordinationSystem not found!");
                }
            }
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogWarning("EnhancedTileSquadController: FormationSystem not found!");
            }
            
            // Check UnitFactory
            if (_unitFactory == null)
            {
                _unitFactory = FindObjectOfType<UnitFactory>();
                if (_unitFactory == null)
                {
                    Debug.LogError("EnhancedTileSquadController: EnhancedUnitFactory not found!");
                }
            }
            
            // Get SquadFactory
            _squadFactory = SquadFactory.Instance;
            if (_squadFactory == null)
            {
                Debug.LogError("EnhancedTileSquadController: EnhancedSquadFactory singleton not found!");
            }
            
            // Check CombatController
            if (_combatController == null)
            {
                _combatController = FindObjectOfType<CombatSystemController>();
                if (_combatController == null)
                {
                    Debug.LogWarning("EnhancedTileSquadController: CombatSystemController not found!");
                }
            }
            
            // Set up layer masks if not assigned
            if (_tileLayer == 0)
            {
                _tileLayer = LayerMask.GetMask("Tile", "Default");
                Debug.Log("EnhancedTileSquadController: Tile layer not specified, using default layers");
            }
            
            if (_unitLayer == 0)
            {
                _unitLayer = LayerMask.GetMask("Unit", "Enemy");
                Debug.Log("EnhancedTileSquadController: Unit layer not specified, using default layers");
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
            else
            {
                Debug.LogWarning("EnhancedTileSquadController: No selection marker prefab assigned");
            }
        }
        
        /// <summary>
        /// Spawn initial squads based on settings
        /// </summary>
        private void SpawnInitialSquads()
        {
            // Spawn player squads
            for (int i = 0; i < _initialPlayerSquads; i++)
            {
                UnitType unitType = (UnitType)(i % 3); // Cycle through Infantry, Archer, Pike
                SpawnSquad(unitType, false);
            }
            
            // Spawn enemy squads
            // for (int i = 0; i < _initialEnemySquads; i++)
            // {
            //     UnitType unitType = (UnitType)(i % 3); // Cycle through Infantry, Archer, Pike
            //     SpawnSquad(unitType, true);
            // }
            
            Debug.Log($"EnhancedTileSquadController: Spawned {_initialPlayerSquads} player squads and {_initialEnemySquads} enemy squads");
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
                TileComponent squadTileComponent = _tileManager.GetTileBySquadId(_selectedSquadId);
                string tileInfo = squadTileComponent != null ? $"on Tile {squadTileComponent.TileId}" : "not on any tile";
                
                Debug.Log($"Selected Squad: {_selectedSquadId}, Formation: {_currentFormation}, {tileInfo}");
            }
        }
        
        /// <summary>
        /// Handle player input
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
            
            // Right click for alternative actions (can be set to different formations)
            if (Input.GetMouseButtonDown(1))
            {
                // Toggle formations
                if (_selectedSquadId >= 0)
                {
                    CycleFormation();
                }
            }
            
            // Number keys to change formation
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
            
            // Spawn hotkeys (for testing)
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
        }
        
        /// <summary>
        /// Select a squad from a unit GameObject
        /// </summary>
        private void SelectSquad(GameObject unitObject)
        {
            if (unitObject == null) return;
            
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
            
            // Check if this is an enemy squad (skip for simplicity since we can check faction in the squad model)
            if (squadModel.Data != null && squadModel.Data.Faction == "Enemy")
            {
                Debug.Log($"Selected enemy Squad ID: {squadId}. Cannot control enemy squads.");
                return;
            }
            
            // Get current tile
            TileComponent squadTileComponent = _tileManager.GetTileBySquadId(squadId);
            
            // Process selection
            _selectedSquadId = squadId;
            _currentTileComponent = squadTileComponent;
            _currentFormation = squadModel.CurrentFormation;
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(true);
                Vector3 markerPosition = unitObject.transform.position;
                markerPosition.y = _selectionMarkerHeight;
                _selectionMarker.transform.position = markerPosition;
            }
            
            // Highlight all valid tiles for movement
            if (_tileManager != null)
            {
                _tileManager.HighlightAllValidTiles(_selectedSquadId);
            }
            
            Debug.Log($"Selected Squad ID: {_selectedSquadId}, on Tile: {(_currentTileComponent != null ? _currentTileComponent.TileId.ToString() : "none")}");
        }
        
        /// <summary>
        /// Handle tile selection for movement
        /// </summary>
        private void HandleTileSelection(TileComponent selectedTileComponent)
        {
            // If no squad selected, do nothing
            if (_selectedSquadId < 0)
            {
                Debug.Log("No squad selected. Select a squad first.");
                return;
            }
            
            // If current tile is selected, just update formation
            if (_currentTileComponent != null && selectedTileComponent.TileId == _currentTileComponent.TileId)
            {
                UpdateFormationInPlace();
                return;
            }
            
            // Check if tile is valid for movement (always allowed, no neighbor constraint)
            if (selectedTileComponent.IsValidDestination(_selectedSquadId))
            {
                MoveSquadToTile(selectedTileComponent);
            }
            else
            {
                Debug.Log($"Cannot move to Tile {selectedTileComponent.TileId}. It's occupied by another squad.");
            }
        }
        
        /// <summary>
        /// Move selected squad to a tile
        /// </summary>
        private void MoveSquadToTile(TileComponent destinationTileComponent)
        {
            if (_selectedSquadId < 0 || destinationTileComponent == null)
            {
                return;
            }
            
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
            _tileManager.RegisterSquadOnTile(_selectedSquadId, _targetTileComponent.TileId);
            
            // Set formation directly on the squad model
            squadModel.SetFormation(optimalFormation);
            
            // Set the target position
            squadModel.SetTargetPosition(_targetTileComponent.CenterPosition);
            
            // Use CombatController if available as a backup (uses squad model internally)
            // if (_combatController != null)
            // {
            //     _combatController.MoveSquad(_selectedSquadId, _targetTileComponent.CenterPosition);
            //     Debug.Log($"Moving Squad {_selectedSquadId} to Tile {_targetTileComponent.TileId} with formation {optimalFormation}");
            // }
            // else if (_squadCoordinationSystem != null)
            // {
            //     // Fallback to old system
            //     _squadCoordinationSystem.MoveSquadToPosition(_selectedSquadId, _targetTileComponent.CenterPosition);
            //     _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, optimalFormation);
            //     Debug.Log($"Moving Squad {_selectedSquadId} to Tile {_targetTileComponent.TileId} with formation {optimalFormation} via SquadCoordinationSystem");
            // }
            // else
            // {
            //     Debug.LogError("Neither CombatController nor SquadCoordinationSystem available, cannot move squad");
            // }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.transform.position = _targetTileComponent.CenterPosition + Vector3.up * _selectionMarkerHeight;
            }
            
            // Clear tile highlights
            _tileManager.ClearHighlights();
        }
        
        /// <summary>
        /// Update the movement state during squad movement
        /// </summary>
        private void UpdateMovementState()
        {
            if (_selectedSquadId < 0) return;
            
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
            
            if (_selectedSquadId < 0) return;
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Fine-tune final position if needed
            if (_targetTileComponent != null)
            {
                squadModel.SetTargetPosition(_targetTileComponent.CenterPosition);
                
                Debug.Log($"Squad {_selectedSquadId} movement to Tile {_targetTileComponent.TileId} completed");
            }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Highlight all valid tiles for next movement
            if (_tileManager != null)
            {
                _tileManager.HighlightAllValidTiles(_selectedSquadId);
            }
        }
        
        /// <summary>
        /// Spawn a new squad of the specified type
        /// </summary>
        [Button("Spawn Squad")]
        public int SpawnSquad(UnitType unitType, bool isEnemy = false)
        {
            // Find appropriate spawn tile
            TileComponent spawnTileComponent = _tileManager.GetSpawnTileByUnitType(unitType, isEnemy);
            
            if (spawnTileComponent == null)
            {
                Debug.LogError($"EnhancedTileSquadController: No spawn tile available for {unitType}");
                return -1;
            }
            
            // Create squad using enhanced squad factory
            SquadModel squadModel = _squadFactory.CreateSquad(unitType, _troopsPerSquad, spawnTileComponent.CenterPosition, Quaternion.identity, isEnemy);
            
            if (squadModel != null && squadModel.UnitCount > 0)
            {
                int squadId = squadModel.SquadId;
                
                // Register squad on tile
                _tileManager.RegisterSquadOnTile(squadId, spawnTileComponent.TileId);
                
                // Set initial formation based on tile
                FormationType initialFormation = spawnTileComponent.GetOptimalFormation(squadModel.UnitCount);
                squadModel.SetFormation(initialFormation);
                
                Debug.Log($"EnhancedTileSquadController: Spawned {(isEnemy ? "enemy" : "player")} {unitType} squad with ID {squadId} at Tile {spawnTileComponent.TileId}");
                
                return squadId;
            }
            else
            {
                Debug.LogError($"EnhancedTileSquadController: Failed to create squad at {spawnTileComponent.CenterPosition}");
                return -1;
            }
        }
        
        /// <summary>
        /// Change the formation of the selected squad
        /// </summary>
        private void ChangeFormation(FormationType formation)
        {
            if (_selectedSquadId < 0)
            {
                Debug.LogWarning("No squad selected to change formation");
                return;
            }
            
            _currentFormation = formation;
            
            // Set formation using squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel != null)
            {
                squadModel.SetFormation(formation);
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation}");
            }
            else if (_combatController != null)
            {
                // Fallback to combat controller
                _combatController.SetSquadFormation(_selectedSquadId, formation);
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation} via CombatController");
            }
            else if (_squadCoordinationSystem != null)
            {
                // Second fallback to old system
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation} via SquadCoordinationSystem");
            }
            else
            {
                Debug.LogError("No system available to change formation");
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
            if (_selectedSquadId < 0 || _currentTileComponent == null)
            {
                return;
            }
            
            // Get the squad model
            SquadModel squadModel = _squadFactory.GetSquad(_selectedSquadId);
            if (squadModel == null) return;
            
            // Get optimal formation for current tile
            FormationType optimalFormation = _currentTileComponent.GetOptimalFormation(squadModel.UnitCount);
            
            // Apply formation
            ChangeFormation(optimalFormation);
            
            Debug.Log($"Updated Squad {_selectedSquadId} formation to {optimalFormation} in place");
        }
        
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
            if (_squadFactory == null) return;
            
            Debug.Log($"--- Game State Debug ---");
            
            List<int> playerSquadIds = _squadFactory.GetPlayerSquadIds();
            Debug.Log($"Player Squads: {playerSquadIds.Count}");
            
            foreach (var squadId in playerSquadIds)
            {
                SquadModel squad = _squadFactory.GetSquad(squadId);
                TileComponent tileComponent = _tileManager?.GetTileBySquadId(squadId);
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
                TileComponent tileComponent = _tileManager?.GetTileBySquadId(squadId);
                string tileInfo = tileComponent != null ? $"on Tile {tileComponent.TileId}" : "not on any tile";
                
                if (squad != null)
                {
                    Debug.Log($"- Enemy Squad {squadId} (Units: {squad.UnitCount}, Formation: {squad.CurrentFormation}) {tileInfo}");
                }
            }
        }
    }
}