using UnityEngine;
using UnityEngine.EventSystems;
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
        
        // Reference to visualize selected squad (optional)
        [SerializeField] private GameObject _selectionMarkerPrefab;
        private GameObject _selectionMarker;

        private void Start()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            
            if (_selectionMarkerPrefab != null)
            {
                _selectionMarker = Instantiate(_selectionMarkerPrefab);
                _selectionMarker.SetActive(false);
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Left click to select a squad or move selected squad
            if (Input.GetMouseButtonDown(0))
            {
                // Check if we're clicking on UI
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    
                    // Try to hit a unit first
                    if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, _unitLayer))
                    {
                        SelectSquad(unitHit.collider.gameObject);
                    }
                    // Then try to hit ground for movement
                    else if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, _groundLayer))
                    {
                        MoveSelectedSquad(groundHit.point);
                    }
                }
            }
            
            // Right click for alternate actions (could be attack, etc.)
            if (Input.GetMouseButtonDown(1))
            {
                // Can be used for attack, special abilities, etc.
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f, _unitLayer | _groundLayer))
                    {
                        AlternateAction(hit.point, hit.collider.gameObject);
                    }
                }
            }
            
            // Number keys to change formation
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeFormation(FormationType.Line);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeFormation(FormationType.Column);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeFormation(FormationType.Phalanx);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChangeFormation(FormationType.Testudo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ChangeFormation(FormationType.Circle);
            }
        }

        private void SelectSquad(GameObject unitObject)
        {
            // Get the formation component to identify squad
            var formationComponent = unitObject.GetComponent<FormationComponent>();
            if (formationComponent == null)
                return;
                
            _selectedSquadId = formationComponent.SquadId;
            
            // Update visualization (optional)
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
                return;
                
            // Find all units in selected squad and set their target position
            var entities = FindObjectsOfType<FormationComponent>();
            
            foreach (var formationComponent in entities)
            {
                if (formationComponent.SquadId == _selectedSquadId)
                {
                    var navigationComponent = formationComponent.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.SetDestination(targetPosition);
                    }
                }
            }
            
            // Move selection marker
            if (_selectionMarker != null)
            {
                _selectionMarker.transform.position = targetPosition;
            }
            
            Debug.Log($"Moving Squad {_selectedSquadId} to position {targetPosition}");
        }

        private void AlternateAction(Vector3 position, GameObject targetObject)
        {
            if (_selectedSquadId < 0)
                return;
                
            // Check if target is an enemy unit
            var targetUnitType = targetObject.GetComponent<UnitTypeComponent>();
            
            if (targetUnitType != null)
            {
                // Attack target if it's an enemy (simplified implementation)
                Debug.Log($"Squad {_selectedSquadId} attacking target at {position}");
                
                // In a real implementation, you would use squad battle commands
                // or trigger appropriate behaviors
                
                // For this example, let's change to circle formation (surround)
                ChangeFormation(FormationType.Circle);
                MoveSelectedSquad(position);
            }
            else
            {
                // Special movement or action on terrain
                Debug.Log($"Squad {_selectedSquadId} performing special action at {position}");
            }
        }

        private void ChangeFormation(FormationType formation)
        {
            if (_selectedSquadId < 0)
                return;
                
            // Use squad coordination system to change formation
            if (_squadCoordinationSystem != null)
            {
                _squadCoordinationSystem.SetSquadFormation(_selectedSquadId, formation);
                _currentFormation = formation;
                
                Debug.Log($"Changed Squad {_selectedSquadId} formation to {formation}");
            }
        }
    }
}