using System.Collections.Generic;
using Core.ECS;
using Movement;
using Squad;
using UnityEngine;

namespace Management
{
    /// <summary>
    /// Manages squad selection and movement commands
    /// </summary>
    public class SquadSelectionManager : MonoBehaviour
    {
        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableLayers;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float selectionDistance = 1000f;
        [SerializeField] private GameObject selectionCirclePrefab;
    
        [Header("Visual Settings")]
        [SerializeField] private Color allySquadColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color enemySquadColor = new Color(1, 0, 0, 0.3f);
        [SerializeField] private float selectionCircleHeight = 0.05f;
        [SerializeField] private float selectionCircleScale = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
    
        // Currently selected squad
        private Entity selectedSquad = null;
        private GameObject selectionCircle = null;
    
        // Reference to world manager
        private WorldManager worldManager;
    
        // Squad GameObject references (for selection)
        private Dictionary<int, GameObject> squadGameObjects = new Dictionary<int, GameObject>();
    
        private void Start()
        {
            // Get WorldManager reference
            worldManager = WorldManager.Instance;
            if (worldManager == null)
            {
                Debug.LogError("WorldManager không tìm thấy!");
                enabled = false;
                return;
            }
        
            if (selectionCirclePrefab != null)
            {
                selectionCircle = Instantiate(selectionCirclePrefab, Vector3.zero, Quaternion.Euler(90, 0, 0));
                selectionCircle.transform.localScale = Vector3.one * selectionCircleScale;
                selectionCircle.SetActive(false);
            }
            
            if (selectableLayers.value == 0)
            {
                Debug.LogWarning("Selectable layers mask is not set! Setting to default 'Default' layer.");
                selectableLayers = LayerMask.GetMask("Default");
            }
            
            if (groundLayers.value == 0)
            {
                Debug.LogWarning("Ground layers mask is not set! Setting to default 'Ground' layer.");
                groundLayers = LayerMask.GetMask("Ground");
            }
        }
    
        private void Update()
        {
            // Handle squad selection
            if (Input.GetMouseButtonDown(0))
            {
                HandleSelection();
            }
        
            // Handle squad command to move
            if (Input.GetMouseButtonDown(1) && selectedSquad != null)
            {
                HandleMovementCommand();
            }
        
            // Update selection circle position
            UpdateSelectionCircle();
        
            // Handle hotkeys
            HandleHotkeys();
            
            // Debug logging
            if (debugMode && Input.GetMouseButtonDown(0))
            {
                DebugRaycastInfo();
            }
        }
    
        /// <summary>
        /// Handles selection of squads and troops with mouse click
        /// </summary>
        private void HandleSelection()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit, selectionDistance, selectableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                if (debugMode)
                {
                    Debug.Log($"Hit object: {hitObject.name} on layer {LayerMask.LayerToName(hitObject.layer)}");
                }
            
                // Try to find entity behaviour in hit object or its parent
                EntityBehaviour entityBehaviour = hitObject.GetComponent<EntityBehaviour>();
                if (entityBehaviour == null)
                {
                    // Try to get from parent if not found on direct hit
                    entityBehaviour = hitObject.GetComponentInParent<EntityBehaviour>();
                }
                
                if (entityBehaviour != null)
                {
                    Entity entity = entityBehaviour.GetEntity();
                    
                    if (debugMode && entity != null)
                    {
                        Debug.Log($"Found entity with ID: {entity.Id}");
                    }
                
                    // Check if this is a troop entity
                    if (entity != null && entity.HasComponent<SquadMemberComponent>())
                    {
                        // Get the squadron this troop belongs to
                        SquadMemberComponent squadMember = entity.GetComponent<SquadMemberComponent>();
                        int squadId = squadMember.SquadEntityId;
                    
                        // Find the squad entity
                        Entity squadEntity = FindSquadById(squadId);
                    
                        if (squadEntity != null)
                        {
                            // Deselect current squad if any
                            DeselectCurrentSquad();
                        
                            // Select new squad
                            SelectSquad(squadEntity);
                        
                            Debug.Log($"Selected Squad ID: {squadEntity.Id}");
                        }
                    }
                    else if (entity != null && entity.HasComponent<SquadStateComponent>())
                    {
                        // Direct squadron selection
                        DeselectCurrentSquad();
                        SelectSquad(entity);
                    
                        Debug.Log($"Selected Squad ID: {entity.Id}");
                    }
                }
                else if (debugMode)
                {
                    Debug.LogWarning("No EntityBehaviour component found on hit object or its parents");
                }
            }
            else
            {
                // Clicked on empty space, only deselect if not hitting UI
                if (!UnityEngine.EventSystems.EventSystem.current || 
                    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    DeselectCurrentSquad();
                }
            }
        }
        
        /// <summary>
        /// Helper method to find a squad entity by ID
        /// </summary>
        private Entity FindSquadById(int squadId)
        {
            foreach (var candidateEntity in worldManager.GetWorld().GetEntitiesWith<SquadStateComponent>())
            {
                if (candidateEntity.Id == squadId)
                {
                    return candidateEntity;
                }
            }
            return null;
        }
    
        /// <summary>
        /// Handles movement commands for the selected squad
        /// </summary>
        private void HandleMovementCommand()
        {
            if (selectedSquad == null) return;
        
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit, selectionDistance, groundLayers))
            {
                Vector3 targetPosition = hit.point;
                
                if (debugMode)
                {
                    Debug.Log($"Movement target: {targetPosition}");
                }
            
                // Check if we have a grid manager
                if (GridManager.Instance != null)
                {
                    // Get the cell under the cursor
                    Vector2Int cellCoordinates = GridManager.Instance.GetGridCoordinates(targetPosition);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Target cell coordinates: {cellCoordinates}");
                        Debug.Log($"Cell occupied: {GridManager.Instance.IsCellOccupied(cellCoordinates)}");
                    }
                
                    // Check if cell is valid and not occupied (or is occupied by the current squad)
                    if (GridManager.Instance.IsWithinGrid(cellCoordinates))
                    {
                        bool canMoveToCell = true;
                        
                        if (GridManager.Instance.IsCellOccupied(cellCoordinates))
                        {
                            // Check if cell is occupied by current squad
                            if (selectedSquad.HasComponent<PositionComponent>())
                            {
                                Vector3 currentPos = selectedSquad.GetComponent<PositionComponent>().Position;
                                Vector2Int currentCell = GridManager.Instance.GetGridCoordinates(currentPos);
                                
                                // Allow movement if moving to the same cell or adjacent
                                canMoveToCell = (currentCell == cellCoordinates) || 
                                               (Mathf.Abs(currentCell.x - cellCoordinates.x) <= 1 && 
                                                Mathf.Abs(currentCell.y - cellCoordinates.y) <= 1);
                            }
                            else
                            {
                                canMoveToCell = false;
                            }
                        }
                        
                        if (canMoveToCell)
                        {
                            // Update target position to cell center
                            targetPosition = GridManager.Instance.GetCellCenter(cellCoordinates);
                        
                            // Mark current cell as unoccupied
                            if (selectedSquad.HasComponent<PositionComponent>())
                            {
                                Vector3 currentPos = selectedSquad.GetComponent<PositionComponent>().Position;
                                Vector2Int currentCell = GridManager.Instance.GetGridCoordinates(currentPos);
                                
                                if (currentCell != cellCoordinates) // Only if moving to a different cell
                                {
                                    GridManager.Instance.SetCellOccupied(currentCell, false);
                                    GridManager.Instance.SetCellOccupied(cellCoordinates, true);
                                }
                            }
                        
                            // Select the cell in grid
                            GridManager.Instance.SelectCell(cellCoordinates);
                            
                            // Issue movement command
                            worldManager.CommandSquadMove(selectedSquad, targetPosition);
                            Debug.Log($"Moving Squad {selectedSquad.Id} to {targetPosition} (Cell: {cellCoordinates})");
                        }
                        else
                        {
                            Debug.Log("Cell is occupied by another squad. Cannot move there.");
                        }
                    }
                    else
                    {
                        Debug.Log($"Cell coordinates {cellCoordinates} are outside the grid. Cannot move there.");
                    }
                }
                else
                {
                    // No grid manager, just move directly to hit point
                    worldManager.CommandSquadMove(selectedSquad, targetPosition);
                    Debug.Log($"Moving Squad {selectedSquad.Id} to {targetPosition} (No grid)");
                }
            }
        }
    
        /// <summary>
        /// Handles hotkeys for squad commands
        /// </summary>
        private void HandleHotkeys()
        {
            // Stop command
            if (Input.GetKeyDown(KeyCode.S) && selectedSquad != null)
            {
                worldManager.CommandSquadStop(selectedSquad);
                Debug.Log($"Commanding Squad {selectedSquad.Id} to stop");
            }
        
            // Defend command 
            if (Input.GetKeyDown(KeyCode.D) && selectedSquad != null)
            {
                worldManager.CommandSquadDefend(selectedSquad);
                Debug.Log($"Commanding Squad {selectedSquad.Id} to defend position");
            }
        }
    
        /// <summary>
        /// Selects a squad and shows visual indicator
        /// </summary>
        private void SelectSquad(Entity squad)
        {
            selectedSquad = squad;
        
            // Show selection circle
            if (selectionCircle != null)
            {
                selectionCircle.SetActive(true);
            }
        
            // Update selection circle position
            UpdateSelectionCircle();
        
            // If we have a grid, also select cell
            if (GridManager.Instance != null && squad.HasComponent<PositionComponent>())
            {
                Vector3 squadPos = squad.GetComponent<PositionComponent>().Position;
                Vector2Int cellCoordinates = GridManager.Instance.GetGridCoordinates(squadPos);
                GridManager.Instance.SelectCell(cellCoordinates);
            }
        }
    
        /// <summary>
        /// Deselects the current squad and hides indicators
        /// </summary>
        private void DeselectCurrentSquad()
        {
            if (selectedSquad == null) return;
        
            selectedSquad = null;
        
            // Hide selection circle
            if (selectionCircle != null)
            {
                selectionCircle.SetActive(false);
            }
        }
    
        /// <summary>
        /// Updates the position of the selection circle to follow the selected squad
        /// </summary>
        private void UpdateSelectionCircle()
        {
            if (selectedSquad == null || selectionCircle == null) return;
        
            if (selectedSquad.HasComponent<PositionComponent>())
            {
                Vector3 squadPos = selectedSquad.GetComponent<PositionComponent>().Position;
                selectionCircle.transform.position = new Vector3(
                    squadPos.x,
                    squadPos.y + selectionCircleHeight,
                    squadPos.z
                );
            }
        }
    
        /// <summary>
        /// Returns the currently selected squad
        /// </summary>
        public Entity GetSelectedSquad()
        {
            return selectedSquad;
        }
    
        /// <summary>
        /// Registers a GameObject that represents a squad
        /// </summary>
        public void RegisterSquadGameObject(int squadId, GameObject squadObject)
        {
            if (!squadGameObjects.ContainsKey(squadId))
            {
                squadGameObjects.Add(squadId, squadObject);
            }
        }
        
        /// <summary>
        /// Helper method to debug raycast issues
        /// </summary>
        private void DebugRaycastInfo()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, selectionDistance);
            
            Debug.Log($"Raycast detected {hits.Length} objects");
            
            foreach (var hit in hits)
            {
                Debug.Log($"Hit: {hit.collider.gameObject.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} | Distance: {hit.distance}");
            }
            
            Debug.Log($"Selectable layers: {LayerMaskToString(selectableLayers)}");
            Debug.Log($"Ground layers: {LayerMaskToString(groundLayers)}");
        }
        
        /// <summary>
        /// Helper method to convert layer mask to readable string
        /// </summary>
        private string LayerMaskToString(LayerMask mask)
        {
            var layers = "";
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    layers += LayerMask.LayerToName(i) + ", ";
                }
            }
            return layers.TrimEnd(',', ' ');
        }
    }
}