using System.Collections.Generic;
using Core.ECS;
using Movement;
using Squad;
using UnityEngine;

namespace Management
{
    /// <summary>
    /// Quản lý việc chọn và điều khiển Squad di chuyển giữa các ô
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
    
        // Currently selected squad
        private Entity selectedSquad = null;
        private GameObject selectionCircle = null;
    
        // Reference to world manager
        private WorldManager worldManager;
    
        // Squad GameObject references (for selection)
        private Dictionary<int, GameObject> squadGameObjects = new Dictionary<int, GameObject>();
    
        private void Start()
        {
            worldManager = WorldManager.Instance;
            if (worldManager == null)
            {
                Debug.LogError("WorldManager không tìm thấy!");
                enabled = false;
                return;
            }
        
            // Create selection circle if prefab is assigned
            if (selectionCirclePrefab != null)
            {
                selectionCircle = Instantiate(selectionCirclePrefab, Vector3.zero, Quaternion.Euler(90, 0, 0));
                selectionCircle.transform.localScale = Vector3.one * selectionCircleScale;
                selectionCircle.SetActive(false);
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
        }
    
        /// <summary>
        /// Xử lý việc chọn Squad bằng mouse
        /// </summary>
        private void HandleSelection()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit, selectionDistance, selectableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
            
                // Try to find entity behaviour
                EntityBehaviour entityBehaviour = hitObject.GetComponent<EntityBehaviour>();
                if (entityBehaviour != null)
                {
                    Entity entity = entityBehaviour.GetEntity();
                
                    // Check if this is a troop entity
                    if (entity != null && entity.HasComponent<SquadMemberComponent>())
                    {
                        // Get the squadron this troop belongs to
                        SquadMemberComponent squadMember = entity.GetComponent<SquadMemberComponent>();
                        int squadId = squadMember.SquadEntityId;
                    
                        // Find the squad entity
                        Entity squadEntity = null;
                    
                        foreach (var candidateEntity in worldManager.GetWorld().GetEntitiesWith<SquadStateComponent>())
                        {
                            if (candidateEntity.Id == squadId)
                            {
                                squadEntity = candidateEntity;
                                break;
                            }
                        }
                    
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
            }
            else
            {
                // Clicked on empty space
                DeselectCurrentSquad();
            }
        }
    
        /// <summary>
        /// Xử lý lệnh di chuyển đến ô
        /// </summary>
        private void HandleMovementCommand()
        {
            if (selectedSquad == null) return;
        
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit, selectionDistance, groundLayers))
            {
                Vector3 targetPosition = hit.point;
            
                // Check if we have a grid manager
                if (GridManager.Instance != null)
                {
                    // Get the cell under the cursor
                    Vector2Int cellCoordinates = GridManager.Instance.GetGridCoordinates(targetPosition);
                
                    // Check if cell is valid and not occupied
                    if (GridManager.Instance.IsWithinGrid(cellCoordinates) && !GridManager.Instance.IsCellOccupied(cellCoordinates))
                    {
                        // Update target position to cell center
                        targetPosition = GridManager.Instance.GetCellCenter(cellCoordinates);
                    
                        // Mark current cell as unoccupied
                        if (selectedSquad.HasComponent<PositionComponent>())
                        {
                            Vector3 currentPos = selectedSquad.GetComponent<PositionComponent>().Position;
                            Vector2Int currentCell = GridManager.Instance.GetGridCoordinates(currentPos);
                            GridManager.Instance.SetCellOccupied(currentCell, false);
                        }
                    
                        // Mark target cell as occupied
                        GridManager.Instance.SetCellOccupied(cellCoordinates, true);
                    
                        // Select the cell in grid
                        GridManager.Instance.SelectCell(cellCoordinates);
                    }
                    else
                    {
                        Debug.Log("Cell is occupied or invalid. Cannot move there.");
                        return;
                    }
                }
            
                // Issue movement command
                worldManager.CommandSquadMove(selectedSquad, targetPosition);
                Debug.Log($"Moving Squad {selectedSquad.Id} to {targetPosition}");
            }
        }
    
        /// <summary>
        /// Xử lý các phím tắt trong game
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
        
            // Attack command example (would need target info)
            // if (Input.GetKeyDown(KeyCode.A) && selectedSquad != null && targetEntity != null)
            // {
            //     worldManager.CommandSquadAttack(selectedSquad, targetEntity);
            // }
        }
    
        /// <summary>
        /// Chọn một Squad
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
        
            // Additional selection visual cues could be added here
        }
    
        /// <summary>
        /// Huỷ chọn Squad hiện tại
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
        /// Cập nhật vị trí của vòng tròn selection
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
        /// Lấy Squad đang được chọn
        /// </summary>
        public Entity GetSelectedSquad()
        {
            return selectedSquad;
        }
    
        /// <summary>
        /// Đăng ký GameObject đại diện cho một Squad
        /// </summary>
        public void RegisterSquadGameObject(int squadId, GameObject squadObject)
        {
            if (!squadGameObjects.ContainsKey(squadId))
            {
                squadGameObjects.Add(squadId, squadObject);
            }
        }
    }

    
}