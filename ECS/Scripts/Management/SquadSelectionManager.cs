// ECS/Scripts/Management/SquadSelectionManager.cs
using System.Collections.Generic;
using Core.ECS;
using Debug_Tool;
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
        [SerializeField] private LayerMask selectableLayers; // Nên chọn layer Troop và Squad
        [SerializeField] private LayerMask groundLayers;     // Nên chọn layer Ground
        [SerializeField] private float selectionDistance = 1000f;
        [SerializeField] private GameObject selectionCirclePrefab;
    
        [Header("Visual Settings")]
        [SerializeField] private Color allySquadColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color enemySquadColor = new Color(1, 0, 0, 0.3f);
        [SerializeField] private float selectionCircleHeight = 0.05f;
        [SerializeField] private float selectionCircleScale = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true; // Bật chế độ debug
    
        // Currently selected squad
        private Entity selectedSquad = null;
        private GameObject selectionCircle = null;
    
        // Reference to world manager
        private WorldManager worldManager;
        
        // Reference to grid manager
        private GridManager gridManager;
    
        // Squad GameObject references (for selection)
        private Dictionary<int, GameObject> squadGameObjects = new Dictionary<int, GameObject>();
        
        // Track previously highlighted cell
        private Vector2Int? previousHighlightedCell = null;
        
        // FIX: Thêm biến để theo dõi vị trí target thực sự của squad
        private Vector3 currentTargetPosition = Vector3.zero;
    
        private void Start()
        {
            worldManager = WorldManager.Instance;
            gridManager = GridManager.Instance;
            
            if (worldManager == null)
            {
                Debug.LogError("WorldManager không tìm thấy!");
                enabled = false;
                return;
            }
        
            // Tạo selection circle nếu prefab được gán
            if (selectionCirclePrefab != null)
            {
                selectionCircle = Instantiate(selectionCirclePrefab, Vector3.zero, Quaternion.Euler(90, 0, 0));
                selectionCircle.transform.localScale = Vector3.one * selectionCircleScale;
                
                // FIX: Đảm bảo selection circle có layer riêng và không ảnh hưởng đến logic game
                selectionCircle.layer = LayerMask.NameToLayer("UI");
                
                // FIX: Đảm bảo selection circle không có collider
                Collider[] colliders = selectionCircle.GetComponents<Collider>();
                foreach (var collider in colliders)
                {
                    Destroy(collider);
                }
                
                selectionCircle.SetActive(false);
            }
            
            // Kiểm tra và cảnh báo nếu layer mask chưa được thiết lập
            if (selectableLayers.value == 0)
            {
                Debug.LogError("Selectable layers mask chưa được thiết lập! Vui lòng thiết lập Troop và Squad layer.");
                // Thiết lập mặc định cho Troop (Layer 9) và Squad (Layer 10)
                selectableLayers = (1 << 9) | (1 << 10);
            }
            
            if (groundLayers.value == 0)
            {
                Debug.LogError("Ground layers mask chưa được thiết lập! Vui lòng thiết lập Ground layer.");
                // Thiết lập mặc định cho Ground (Layer 8)
                groundLayers = 1 << 8;
            }
            
            Debug.Log($"SquadSelectionManager đã khởi tạo với layer mask: selectableLayers={LayerMaskToString(selectableLayers)}, groundLayers={LayerMaskToString(groundLayers)}");
        }
    
        private void Update()
        {
            // Xử lý chọn squad
            if (Input.GetMouseButtonDown(0))
            {
                HandleSelection();
            }
        
            // Xử lý di chuyển squad
            if (Input.GetMouseButtonDown(1) && selectedSquad != null)
            {
                HandleMovementCommand();
            }
        
            // Cập nhật vị trí selection circle
            UpdateSelectionCircle();
            
            // Xử lý highlight cell dưới chuột
            HandleCellHighlighting();
        
            // Xử lý phím tắt
            HandleHotkeys();
        }
        
        /// <summary>
        /// Highlights cell under mouse cursor
        /// </summary>
        private void HandleCellHighlighting()
        {
            if (gridManager == null)
                return;
                
            Vector2Int cellCoords;
            if (gridManager.TryGetCellUnderMouse(out cellCoords))
            {
                // Only update highlight if it's a different cell
                if (!previousHighlightedCell.HasValue || previousHighlightedCell.Value != cellCoords)
                {
                    gridManager.HighlightCell(cellCoords);
                    previousHighlightedCell = cellCoords;
                }
            }
            else if (previousHighlightedCell.HasValue)
            {
                // Clear previous highlight when mouse is not over any cell
                previousHighlightedCell = null;
            }
        }
    
        /// <summary>
        /// Xử lý chọn Squad bằng chuột
        /// </summary>
        private void HandleSelection()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, selectionDistance, selectableLayers);
            
            if (debugMode)
            {
                Debug.Log($"Raycast detected {hits.Length} objects with selectableLayers = {LayerMaskToString(selectableLayers)}");
                foreach (var hit in hits)
                {
                    Debug.Log($"Hit: {hit.collider.gameObject.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} | Distance: {hit.distance}");
                }
            }
            
            if (hits.Length == 0)
            {
                if (debugMode) Debug.Log("Không hit được đối tượng nào thuộc selectableLayers");
                if (!UnityEngine.EventSystems.EventSystem.current || 
                    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    DeselectCurrentSquad();
                }
                return;
            }
            RaycastHit closestHit = hits[0];
            foreach (var hit in hits)
            {
                if (hit.distance < closestHit.distance)
                {
                    closestHit = hit;
                }
            }
            
            GameObject hitObject = closestHit.collider.gameObject;
            if (debugMode) Debug.Log($"Xử lý hit object: {hitObject.name} với layer {LayerMask.LayerToName(hitObject.layer)}");
            EntityBehaviour entityBehaviour = hitObject.GetComponent<EntityBehaviour>();
            if (!entityBehaviour)
            {
                entityBehaviour = hitObject.GetComponentInParent<EntityBehaviour>();
            }
            
            if (entityBehaviour)
            {
                Entity entity = entityBehaviour.GetEntity();
                if (debugMode && entity != null) Debug.Log($"Tìm thấy entity ID: {entity.Id}");
                if (entity != null && entity.HasComponent<SquadMemberComponent>())
                {
                    SquadMemberComponent squadMember = entity.GetComponent<SquadMemberComponent>();
                    int squadId = squadMember.SquadEntityId;
                    Entity squadEntity = FindSquadById(squadId);
                    
                    if (squadEntity != null)
                    {
                        // Bỏ chọn squad hiện tại nếu có
                        DeselectCurrentSquad();
                        
                        // Chọn squad mới
                        SelectSquad(squadEntity);
                        
                        Debug.Log($"Đã chọn Squad ID: {squadEntity.Id}");
                    }
                    else if (debugMode)
                    {
                        Debug.LogWarning($"Không tìm thấy Squad với ID: {squadId}");
                    }
                }
                else if (entity != null && entity.HasComponent<SquadStateComponent>())
                {
                    DeselectCurrentSquad();
                    SelectSquad(entity);
                    Debug.Log($"Đã chọn Squad ID: {entity.Id}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("Không tìm thấy EntityBehaviour trên đối tượng được hit hoặc parent của nó");
            }
        }
        
        /// <summary>
        /// Tìm Squad entity theo ID
        /// </summary>
        private Entity FindSquadById(int squadId)
        {
            // Tìm tất cả squad entity
            var squadEntities = worldManager.GetWorld().GetEntitiesWith<SquadStateComponent>();
            
            foreach (var entity in squadEntities)
            {
                if (entity.Id == squadId)
                {
                    return entity;
                }
            }
            
            return null;
        }
    
        /// <summary>
        /// Xử lý lệnh di chuyển Squad
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
                    Debug.Log($"Hit point for movement: {targetPosition}");
                }
            
                // Kiểm tra nếu có GridManager
                if (gridManager != null)
                {
                    // Lấy tọa độ ô dưới con trỏ
                    Vector2Int cellCoordinates = gridManager.GetGridCoordinates(targetPosition);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Cell coordinates: {cellCoordinates}");
                        Debug.Log($"Cell occupied: {gridManager.IsCellOccupied(cellCoordinates)}");
                    }
                
                    // Kiểm tra ô có hợp lệ và không bị chiếm
                    if (gridManager.IsWithinGrid(cellCoordinates))
                    {
                        bool canMoveToCell = true;
                        
                        if (gridManager.IsCellOccupied(cellCoordinates))
                        {
                            // Kiểm tra xem ô có bị chiếm bởi squad hiện tại không
                            if (selectedSquad.HasComponent<PositionComponent>())
                            {
                                Vector3 currentPos = selectedSquad.GetComponent<PositionComponent>().Position;
                                Vector2Int currentCell = gridManager.GetGridCoordinates(currentPos);
                                
                                // Cho phép di chuyển nếu đang ở chính ô đó hoặc ô liền kề
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
                            // Cập nhật vị trí đích thành tâm của ô
                            targetPosition = gridManager.GetCellCenter(cellCoordinates);
                            
                            // FIX: Lưu lại target position thực sự
                            currentTargetPosition = targetPosition;
                        
                            // Đánh dấu ô hiện tại là không bị chiếm
                            if (selectedSquad.HasComponent<PositionComponent>())
                            {
                                Vector3 currentPos = selectedSquad.GetComponent<PositionComponent>().Position;
                                Vector2Int currentCell = gridManager.GetGridCoordinates(currentPos);
                                
                                if (currentCell != cellCoordinates) // Chỉ khi di chuyển đến ô khác
                                {
                                    gridManager.SetCellOccupied(currentCell, false);
                                    gridManager.SetCellOccupied(cellCoordinates, true);
                                }
                            }
                        
                            // Chọn ô trong grid
                            gridManager.SelectCell(cellCoordinates);
                            
                            // Ra lệnh di chuyển
                            worldManager.CommandSquadMove(selectedSquad, targetPosition);
                            Debug.Log($"Di chuyển Squad {selectedSquad.Id} đến {targetPosition} (Cell: {cellCoordinates})");
                        }
                        else
                        {
                            Debug.Log("Ô đã bị chiếm bởi đội quân khác. Không thể di chuyển đến đó.");
                        }
                    }
                    else
                    {
                        Debug.Log($"Tọa độ ô {cellCoordinates} nằm ngoài grid. Không thể di chuyển đến đó.");
                    }
                }
                else
                {
                    // Không có GridManager, di chuyển trực tiếp đến hit point
                    // FIX: Lưu lại target position thực sự
                    currentTargetPosition = targetPosition;
                    
                    worldManager.CommandSquadMove(selectedSquad, targetPosition);
                    Debug.Log($"Di chuyển Squad {selectedSquad.Id} đến {targetPosition} (Không có grid)");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("Không hit được đối tượng nào thuộc groundLayers");
            }
        }
    
        /// <summary>
        /// Xử lý các phím tắt
        /// </summary>
        private void HandleHotkeys()
        {
            // Lệnh dừng lại
            if (Input.GetKeyDown(KeyCode.S) && selectedSquad != null)
            {
                worldManager.CommandSquadStop(selectedSquad);
                Debug.Log($"Ra lệnh Squad {selectedSquad.Id} dừng lại");
            }
            if (Input.GetKeyDown(KeyCode.D) && selectedSquad != null)
            {
                worldManager.CommandSquadDefend(selectedSquad);
                Debug.Log($"Ra lệnh Squad {selectedSquad.Id} phòng thủ tại vị trí hiện tại");
            }
            
            // Hiển thị debug info với F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                debugMode = !debugMode;
                Debug.Log($"Debug mode: {(debugMode ? "ON" : "OFF")}");
                
                var visualizers = FindObjectsOfType<TroopDebugVisualizer>();
                foreach (var viz in visualizers)
                {
                    viz.enabled = debugMode;
                }
            }
            
            // Thêm phím tắt F5 để refresh grid
            if (Input.GetKeyDown(KeyCode.F5) && gridManager != null)
            {
                gridManager.RefreshAllCellVisuals();
                Debug.Log("Refresh all grid cells");
            }
        }
    
        /// <summary>
        /// Chọn một Squad và hiển thị indicator
        /// </summary>
        private void SelectSquad(Entity squad)
        {
            selectedSquad = squad;
        
            // Hiển thị selection circle
            if (selectionCircle != null)
            {
                selectionCircle.SetActive(true);
            }
        
            // Cập nhật vị trí selection circle
            UpdateSelectionCircle();
        
            // Nếu có GridManager, cũng chọn ô
            if (gridManager != null && squad.HasComponent<PositionComponent>())
            {
                Vector3 squadPos = squad.GetComponent<PositionComponent>().Position;
                Vector2Int cellCoordinates = gridManager.GetGridCoordinates(squadPos);
                // Check if coordinates are valid before selecting
                if (gridManager.IsWithinGrid(cellCoordinates))
                {
                    gridManager.SelectCell(cellCoordinates);
                }
            }
            
            // FIX: Lấy vị trí mục tiêu của squad nếu có
            if (squad.HasComponent<SquadStateComponent>())
            {
                SquadStateComponent stateComponent = squad.GetComponent<SquadStateComponent>();
                if (stateComponent.CurrentState == SquadState.Moving)
                {
                    currentTargetPosition = stateComponent.TargetPosition;
                }
                else
                {
                    // Nếu squad không di chuyển, set target position là vị trí hiện tại
                    if (squad.HasComponent<PositionComponent>())
                    {
                        currentTargetPosition = squad.GetComponent<PositionComponent>().Position;
                    }
                }
            }
        }
    
        /// <summary>
        /// Bỏ chọn Squad hiện tại
        /// </summary>
        private void DeselectCurrentSquad()
        {
            if (selectedSquad == null) return;
        
            selectedSquad = null;
        
            // Ẩn selection circle
            if (selectionCircle != null)
            {
                selectionCircle.SetActive(false);
            }
            
            // Clear cell selection in grid
            if (gridManager != null)
            {
                gridManager.ClearCellSelections();
            }
            
            // FIX: Reset current target position
            currentTargetPosition = Vector3.zero;
        }
    
        /// <summary>
        /// Cập nhật vị trí của selection circle
        /// </summary>
        private void UpdateSelectionCircle()
        {
            if (selectedSquad == null || selectionCircle == null) return;
        
            if (selectedSquad.HasComponent<PositionComponent>())
            {
                // FIX: Đảm bảo selection circle luôn ở vị trí của squad, không phải ở vị trí mục tiêu
                Vector3 squadPos = selectedSquad.GetComponent<PositionComponent>().Position;
                selectionCircle.transform.position = new Vector3(
                    squadPos.x,
                    squadPos.y + selectionCircleHeight,
                    squadPos.z
                );
            }
        }
    
        /// <summary>
        /// Lấy squad đang được chọn
        /// </summary>
        public Entity GetSelectedSquad()
        {
            return selectedSquad;
        }
        
        /// <summary>
        /// Lấy vị trí mục tiêu thực sự của squad đã chọn
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            return currentTargetPosition;
        }
    
        /// <summary>
        /// Đăng ký GameObject đại diện cho một Squad
        /// </summary>
        public void RegisterSquadGameObject(int squadId, GameObject squadObject)
        {
            if (!squadGameObjects.ContainsKey(squadId))
            {
                squadGameObjects.Add(squadId, squadObject);
                
                // Đảm bảo đối tượng có đúng layer
                squadObject.layer = LayerMask.NameToLayer("Squad");
                
                // Đảm bảo đối tượng có collider
                Collider collider = squadObject.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider boxCollider = squadObject.AddComponent<BoxCollider>();
                    boxCollider.size = new Vector3(5f, 0.5f, 5f);
                    boxCollider.center = new Vector3(0, 0.25f, 0);
                    boxCollider.isTrigger = true;
                }
                
                if (debugMode) Debug.Log($"Đã đăng ký Squad GameObject {squadObject.name} với ID {squadId}");
            }
        }
        
        /// <summary>
        /// Chuyển đổi LayerMask thành chuỗi để debug
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