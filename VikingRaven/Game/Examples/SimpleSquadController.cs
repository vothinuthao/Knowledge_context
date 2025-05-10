using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VikingRaven.Units;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Game.Examples
{
    public class SimpleSquadController : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _unitLayer;
        [SerializeField] private SquadCoordinationSystem _squadCoordinationSystem;
        
        [SerializeField] private int _selectedSquadId = -1;
        [SerializeField] private FormationType _currentFormation = FormationType.Line;
        
        // Reference để hiển thị squad được chọn
        [SerializeField] private GameObject _selectionMarkerPrefab;
        private GameObject _selectionMarker;
        
        // Debug options
        [SerializeField] private bool _debugMode = true;

        private void Start()
        {
            // Lấy main camera nếu chưa được gán
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            
            // Tạo selection marker nếu có prefab
            if (_selectionMarkerPrefab != null)
            {
                _selectionMarker = Instantiate(_selectionMarkerPrefab);
                _selectionMarker.SetActive(false);
            }
            
            // Thiết lập layer masks nếu chưa được gán
            if (_groundLayer == 0)
            {
                _groundLayer = LayerMask.GetMask("Ground", "Terrain", "Default");
                Debug.Log("SimpleSquadController: Ground layer not specified, using default layers");
            }
            
            if (_unitLayer == 0)
            {
                _unitLayer = LayerMask.GetMask("Unit", "Enemy");
                Debug.Log("SimpleSquadController: Unit layer not specified, using default layers");
            }
            
            // Tìm SquadCoordinationSystem nếu chưa được gán
            if (_squadCoordinationSystem == null)
            {
                _squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
                if (_squadCoordinationSystem == null)
                {
                    Debug.LogWarning("SimpleSquadController: SquadCoordinationSystem not found!");
                }
            }
            
            Debug.Log("SimpleSquadController initialized. Click on unit to select, then click on ground to move.");
        }

        private void Update()
        {
            HandleInput();
            
            // Hiển thị debug info
            if (_debugMode && _selectedSquadId >= 0)
            {
                Debug.Log($"Selected Squad: {_selectedSquadId}, Formation: {_currentFormation}");
            }
        }

        private void HandleInput()
        {
            // Left click để chọn squad hoặc di chuyển squad đã chọn
            if (Input.GetMouseButtonDown(0))
            {
                // Kiểm tra xem có đang click vào UI không
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;
                
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                
                // Thử hit vào unit trước
                if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, _unitLayer))
                {
                    SelectSquad(unitHit.collider.gameObject);
                }
                // Sau đó thử hit vào ground để di chuyển
                else if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, _groundLayer))
                {
                    MoveSelectedSquad(groundHit.point);
                }
            }
            
            // Right click cho hành động thay thế (có thể là tấn công, etc.)
            if (Input.GetMouseButtonDown(1))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;
                
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, _unitLayer | _groundLayer))
                {
                    AlternateAction(hit.point, hit.collider.gameObject);
                }
            }
            
            // Phím số để thay đổi formation
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ChangeFormation(FormationType.Line);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                ChangeFormation(FormationType.Column);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                ChangeFormation(FormationType.Phalanx);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                ChangeFormation(FormationType.Testudo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                ChangeFormation(FormationType.Circle);
            }
        }

        private void SelectSquad(GameObject unitObject)
        {
            if (unitObject == null)
                return;
                
            // Lấy FormationComponent để xác định squad
            var formationComponent = unitObject.GetComponent<FormationComponent>();
            if (formationComponent == null)
            {
                Debug.LogWarning("SimpleSquadController: Selected object has no FormationComponent");
                return;
            }
                
            _selectedSquadId = formationComponent.SquadId;
            
            // Cập nhật visualization
            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(true);
                _selectionMarker.transform.position = unitObject.transform.position;
            }
            
            Debug.Log($"Selected Squad ID: {_selectedSquadId}");
        }

        private void MoveSelectedSquad(Vector3 targetPosition)
        {
            if (_selectedSquadId < 0)
            {
                Debug.LogWarning("SimpleSquadController: No squad selected!");
                return;
            }
                
            // Tìm tất cả unit trong squad đã chọn
            var formationComponents = FindObjectsOfType<FormationComponent>();
            bool anyUnitMoved = false;
            
            foreach (var formationComponent in formationComponents)
            {
                if (formationComponent.SquadId == _selectedSquadId)
                {
                    var navigationComponent = formationComponent.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        // Đảm bảo pathfinding được bật
                        navigationComponent.EnablePathfinding();
                        navigationComponent.SetDestination(targetPosition);
                        anyUnitMoved = true;
                        
                        if (_debugMode)
                        {
                            Debug.Log($"Moving unit in squad {_selectedSquadId} to position {targetPosition}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unit in squad {_selectedSquadId} has no NavigationComponent!");
                    }
                }
            }
            
            // Di chuyển selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.transform.position = targetPosition;
            }
            
            if (!anyUnitMoved)
            {
                Debug.LogWarning($"No units found for squad ID: {_selectedSquadId}");
            }
            else
            {
                Debug.Log($"Moving Squad {_selectedSquadId} to position {targetPosition}");
            }
        }

        private void AlternateAction(Vector3 position, GameObject targetObject)
        {
            if (_selectedSquadId < 0)
            {
                Debug.LogWarning("SimpleSquadController: No squad selected for alternate action!");
                return;
            }
                
            var targetUnitType = targetObject.GetComponent<UnitTypeComponent>();
            
            if (targetUnitType != null)
            {
                Debug.Log($"Squad {_selectedSquadId} attacking target at {position}");
                ChangeFormation(FormationType.Circle);
                MoveSelectedSquad(position);
            }
            else
            {
                Debug.Log($"Squad {_selectedSquadId} performing special action at {position}");
                
            }
        }

        private void ChangeFormation(FormationType formation)
        {
            if (_selectedSquadId < 0)
            {
                Debug.LogWarning("SimpleSquadController: No squad selected for formation change!");
                return;
            }
            
            if (_squadCoordinationSystem != null)
            {
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
                _currentFormation = formation;
                
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation}");
            }
            else
            {
                Debug.LogError("SimpleSquadController: SquadCoordinationSystem is null, cannot change formation!");
                
                _squadCoordinationSystem = FindObjectOfType<SquadCoordinationSystem>();
                if (_squadCoordinationSystem != null)
                {
                    _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
                    _currentFormation = formation;
                    Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation} (after recovery)");
                }
            }
        }
        public void SetFormationFromUI(int formationIndex)
        {
            FormationType formation = (FormationType)formationIndex;
            ChangeFormation(formation);
        }
        
        // Phương thức debug - hiển thị trạng thái của tất cả các unit
        [ContextMenu("Debug All Units")]
        private void DebugAllUnits()
        {
            var formationComponents = FindObjectsOfType<FormationComponent>();
            Debug.Log($"Total units found: {formationComponents.Length}");
            
            // Nhóm theo squad ID
            Dictionary<int, List<FormationComponent>> squadUnits = new Dictionary<int, List<FormationComponent>>();
            
            foreach (var formation in formationComponents)
            {
                int squadId = formation.SquadId;
                
                if (!squadUnits.ContainsKey(squadId))
                {
                    squadUnits[squadId] = new List<FormationComponent>();
                }
                
                squadUnits[squadId].Add(formation);
            }
            
            // Hiển thị thông tin cho mỗi squad
            foreach (var squad in squadUnits)
            {
                Debug.Log($"Squad ID: {squad.Key}, Units: {squad.Value.Count}");
                
                foreach (var unit in squad.Value)
                {
                    var transform = unit.transform;
                    var navComponent = unit.GetComponent<NavigationComponent>();
                    var unitType = unit.GetComponent<UnitTypeComponent>();
                    
                    string typeStr = unitType != null ? unitType.UnitType.ToString() : "Unknown";
                    string posStr = transform != null ? transform.position.ToString("F1") : "Unknown";
                    string navStr = navComponent != null ? 
                        (navComponent.HasReachedDestination ? "At destination" : "Moving") : "No nav";
                    
                    Debug.Log($"  Unit ID: {unit.GetInstanceID()}, Type: {typeStr}, Pos: {posStr}, Nav: {navStr}");
                }
            }
        }
    }
}