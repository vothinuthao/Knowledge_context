using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Controls squad movement and actions based on tile system
    /// </summary>
    public class TileBasedSquadController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _tileLayer;
        [SerializeField] private LayerMask _unitLayer;
        [SerializeField] private SquadCoordinationSystem _squadCoordinationSystem;
        
        [Header("Squad Selection")]
        [SerializeField] private int _selectedSquadId = -1;
        [SerializeField] private FormationType _currentFormation = FormationType.Line;
        [SerializeField] private GameObject _selectionMarkerPrefab;
        [SerializeField] private float _selectionMarkerHeight = 0.5f;
        
        [Header("Movement Settings")]
        [SerializeField] private float _formationScale = 0.8f; // Scale factor for formations to fit in tiles
        [SerializeField] private float _movementSpeed = 3.0f; // Standard movement speed
        [SerializeField] private float _rotationSpeed = 180.0f; // Degrees per second
        
        // References to managers
        private TileManager _tileManager;
        private FormationSystem _formationSystem;
        
        // UI and feedback elements
        private GameObject _selectionMarker;
        private Tile_InGame.TileComponent _currentTileComponent; // Current tile of selected squad
        private Tile_InGame.TileComponent _targetTileComponent; // Tile where squad is moving to
        
        // State tracking
        private bool _isMoving = false;
        private float _movementDuration = 0f;
        private float _currentMovementTime = 0f;
        
        // Debug settings
        [SerializeField] private bool _debugMode = true;
        
        private void Start()
        {
            // Get references
            InitializeReferences();
            
            // Create selection marker
            CreateSelectionMarker();
            
            Debug.Log("TileBasedSquadController: Initialized. Select a squad by clicking on their units, then click on a highlighted tile to move.");
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
                Debug.LogError("TileBasedSquadController: TileManager singleton not found!");
            }
            
            // Find SquadCoordinationSystem if not assigned
            if (_squadCoordinationSystem == null)
            {
                _squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
                if (_squadCoordinationSystem == null)
                {
                    Debug.LogWarning("TileBasedSquadController: SquadCoordinationSystem not found!");
                }
            }
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogWarning("TileBasedSquadController: FormationSystem not found!");
            }
            
            // Set up layer masks if not assigned
            if (_tileLayer == 0)
            {
                _tileLayer = LayerMask.GetMask("Tile", "Default");
                Debug.Log("TileBasedSquadController: Tile layer not specified, using default layers");
            }
            
            if (_unitLayer == 0)
            {
                _unitLayer = LayerMask.GetMask("Unit", "Enemy");
                Debug.Log("TileBasedSquadController: Unit layer not specified, using default layers");
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
                Debug.LogWarning("TileBasedSquadController: No selection marker prefab assigned");
            }
        }
        
        private void Update()
        {
            // Handle input for squad control
            HandleInput();
            
            // Update movement if squad is moving between tiles
            if (_isMoving)
            {
                UpdateMovement();
            }
            
            // Debug info
            if (_debugMode && _selectedSquadId >= 0)
            {
                Tile_InGame.TileComponent squadTileComponent = _tileManager.GetTileBySquadId(_selectedSquadId);
                string tileInfo = squadTileComponent != null ? $"on Tile {squadTileComponent.TileId}" : "not on any tile";
                
                Debug.Log($"Selected Squad: {_selectedSquadId}, Formation: {_currentFormation}, {tileInfo}");
            }
        }
        
        /// <summary>
        /// Handle player input for selecting and moving squads
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
                    Tile_InGame.TileComponent tileComponent = tileHit.collider.gameObject.GetComponent<Tile_InGame.TileComponent>();
                    if (tileComponent != null)
                    {
                        HandleTileSelection(tileComponent);
                    }
                }
            }
            
            // Right click for alternative actions (could be set different formations, attack, etc.)
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
        }
        
        /// <summary>
        /// Select a squad from a unit GameObject
        /// </summary>
        private void SelectSquad(GameObject unitObject)
        {
            if (unitObject == null) return;
            
            // Get formation component to determine squad
            var formationComponent = unitObject.GetComponent<FormationComponent>();
            if (formationComponent == null)
            {
                Debug.LogWarning("TileBasedSquadController: Selected object has no FormationComponent");
                return;
            }
            
            // Get squad ID and current tile
            int squadId = formationComponent.SquadId;
            Tile_InGame.TileComponent squadTileComponent = _tileManager.GetTileBySquadId(squadId);
            
            // Process selection
            _selectedSquadId = squadId;
            _currentTileComponent = squadTileComponent;
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(true);
                Vector3 markerPosition = unitObject.transform.position;
                markerPosition.y = _selectionMarkerHeight;
                _selectionMarker.transform.position = markerPosition;
            }
            
            // Highlight valid movement tiles
            if (_currentTileComponent != null && _tileManager != null)
            {
                _tileManager.SelectTile(_currentTileComponent, _selectedSquadId);
            }
            
            Debug.Log($"Selected Squad ID: {_selectedSquadId}, on Tile: {(_currentTileComponent != null ? _currentTileComponent.TileId.ToString() : "none")}");
        }
        
        /// <summary>
        /// Handle tile selection for movement
        /// </summary>
        private void HandleTileSelection(Tile_InGame.TileComponent selectedTileComponent)
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
            
            // Check if tile is valid for movement
            if (_tileManager.IsTileValidForMovement(selectedTileComponent))
            {
                MoveSquadToTile(selectedTileComponent);
            }
            else
            {
                Debug.Log($"Cannot move to Tile {selectedTileComponent.TileId}. It's not a valid movement destination.");
            }
        }
        
        /// <summary>
        /// Move selected squad to a tile
        /// </summary>
        private void MoveSquadToTile(Tile_InGame.TileComponent destinationTileComponent)
        {
            if (_selectedSquadId < 0 || destinationTileComponent == null)
            {
                return;
            }
            
            // Set target tile
            _targetTileComponent = destinationTileComponent;
            
            // Calculate movement parameters
            Vector3 startPosition = _currentTileComponent != null ? _currentTileComponent.CenterPosition : Vector3.zero;
            Vector3 endPosition = _targetTileComponent.CenterPosition;
            
            // Get squad size for formation scaling
            int squadSize = GetSquadSize(_selectedSquadId);
            
            // Get optimal formation for the destination tile
            FormationType optimalFormation = _targetTileComponent.GetOptimalFormation(squadSize);
            float formationScale = _targetTileComponent.GetFormationScale(squadSize);
            
            // Set up movement state
            _isMoving = true;
            _currentMovementTime = 0f;
            
            // Calculate movement duration based on distance
            float movementDistance = Vector3.Distance(startPosition, endPosition);
            _movementDuration = movementDistance / _movementSpeed;
            
            // Register squad as moving to the new tile
            _tileManager.RegisterSquadOnTile(_selectedSquadId, _targetTileComponent.TileId);
            
            // Set optimal formation for the destination
            ChangeFormation(optimalFormation);
            
            // Initiate squad movement using SquadCoordinationSystem
            if (_squadCoordinationSystem != null)
            {
                _squadCoordinationSystem.MoveSquadToPosition(_selectedSquadId, endPosition);
                
                // Apply formation with appropriately scaled offsets
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, optimalFormation);
                
                // Alternative: manually move each unit
                // MoveUnitsToFormation(_selectedSquadId, endPosition, optimalFormation, formationScale);
                
                Debug.Log($"Moving Squad {_selectedSquadId} to Tile {_targetTileComponent.TileId} with formation {optimalFormation}");
            }
            else
            {
                Debug.LogError("SquadCoordinationSystem not available, cannot move squad");
            }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Update selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.transform.position = endPosition + Vector3.up * _selectionMarkerHeight;
            }
            
            // Clear tile highlights
            _tileManager.ClearSelection();
        }
        
        /// <summary>
        /// Update squad's movement between tiles
        /// </summary>
        private void UpdateMovement()
        {
            // Update movement timer
            _currentMovementTime += Time.deltaTime;
            
            // Check if movement is complete
            if (_currentMovementTime >= _movementDuration)
            {
                CompleteMovement();
                return;
            }
            
            // Calculate movement progress
            float progress = _currentMovementTime / _movementDuration;
            progress = Mathf.SmoothStep(0, 1, progress); // Apply smoothing
            
            // Update position of selection marker to show progress
            if (_selectionMarker != null && _currentTileComponent != null && _targetTileComponent != null)
            {
                Vector3 startPos = _currentTileComponent.CenterPosition + Vector3.up * _selectionMarkerHeight;
                Vector3 endPos = _targetTileComponent.CenterPosition + Vector3.up * _selectionMarkerHeight;
                _selectionMarker.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }
        }
        
        /// <summary>
        /// Complete squad movement
        /// </summary>
        private void CompleteMovement()
        {
            _isMoving = false;
            
            // Ensure squad is properly positioned at the destination
            if (_squadCoordinationSystem != null && _targetTileComponent != null)
            {
                // Fine-tune final position
                _squadCoordinationSystem.MoveSquadToPosition(_selectedSquadId, _targetTileComponent.CenterPosition);
                
                // Re-apply formation to ensure it's correct
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, _currentFormation);
                
                Debug.Log($"Squad {_selectedSquadId} movement to Tile {_targetTileComponent.TileId} completed");
            }
            
            // Update current tile reference
            _currentTileComponent = _targetTileComponent;
            
            // Highlight valid movement options again
            if (_tileManager != null && _currentTileComponent != null)
            {
                _tileManager.SelectTile(_currentTileComponent, _selectedSquadId);
            }
        }
        
        /// <summary>
        /// Get the number of units in a squad
        /// </summary>
        private int GetSquadSize(int squadId)
        {
            // Find all units in squad
            var entities = FindObjectsOfType<FormationComponent>();
            int count = 0;
            
            foreach (var component in entities)
            {
                if (component.SquadId == squadId)
                {
                    count++;
                }
            }
            
            return count;
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
            
            // Apply formation using systems
            if (_squadCoordinationSystem != null)
            {
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation}");
            }
            else if (_formationSystem != null)
            {
                _formationSystem.ChangeFormation(_selectedSquadId, formation);
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation} via FormationSystem");
            }
            else
            {
                Debug.LogError("Neither SquadCoordinationSystem nor FormationSystem available");
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
            
            // Get squad size for optimal formation
            int squadSize = GetSquadSize(_selectedSquadId);
            
            // Get optimal formation for current tile
            FormationType optimalFormation = _currentTileComponent.GetOptimalFormation(squadSize);
            float formationScale = _currentTileComponent.GetFormationScale(squadSize);
            
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
        
        // Debug method to display squad information
        [ContextMenu("Debug Squad Info")]
        private void DebugSquadInfo()
        {
            if (_selectedSquadId < 0)
            {
                Debug.Log("No squad selected");
                return;
            }
            
            // Find all units in squad
            var formationComponents = FindObjectsOfType<FormationComponent>();
            List<FormationComponent> squadUnits = new List<FormationComponent>();
            
            foreach (var component in formationComponents)
            {
                if (component.SquadId == _selectedSquadId)
                {
                    squadUnits.Add(component);
                }
            }
            
            Debug.Log($"Squad {_selectedSquadId}: {squadUnits.Count} units, Formation: {_currentFormation}");
            
            // Find tile info
            Tile_InGame.TileComponent tileComponent = _tileManager?.GetTileBySquadId(_selectedSquadId);
            if (tileComponent != null)
            {
                Debug.Log($"On Tile {tileComponent.TileId} at {tileComponent.CenterPosition}");
            }
            else
            {
                Debug.Log("Not on any tile");
            }
        }
    }
}