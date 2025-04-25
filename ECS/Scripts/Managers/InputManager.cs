// File: Managers/InputManager.cs
using UnityEngine;
using System;
using Components;
using Core.ECS;
using Components.Squad;
using Core.Grid;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _squadLayer;
        
        [Header("Settings")]
        [SerializeField] private float _doubleClickTime = 0.3f;
        [SerializeField] private float _dragThreshold = 5f;
        
        #region Events
        public event Action<Entity> OnSquadSelected;
        public event Action<Command> OnCommandIssued;
        public event Action<Rect> OnAreaSelection;
        #endregion
        
        #region Private Fields
        private Entity _selectedSquad;
        private float _lastClickTime;
        private Vector3 _dragStartPosition;
        private bool _isDragging;
        private GameManager _gameManager;
        private GridManager _gridManager;
        #endregion
        
        private void Start()
        {
            _gameManager = GameManager.Instance;
            _gridManager = GridManager.Instance;
            
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (_gameManager == null || _gameManager.CurrentState != GameState.PLAYING)
                return;
            
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        private void HandleMouseInput()
        {
            // Left click - selection
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClickDown();
            }
            else if (Input.GetMouseButton(0))
            {
                HandleLeftClickDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleLeftClickUp();
            }
            
            // Right click - commands
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
            
            // Middle mouse - camera control (optional)
            if (Input.GetMouseButton(2))
            {
                HandleMiddleMouseDrag();
            }
            
            // Mouse wheel - zoom (optional)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                HandleMouseScroll(scroll);
            }
        }
        
        private void HandleLeftClickDown()
        {
            _dragStartPosition = Input.mousePosition;
            _isDragging = false;
            
            // Check for double click
            float timeSinceLastClick = Time.time - _lastClickTime;
            bool isDoubleClick = timeSinceLastClick < _doubleClickTime;
            _lastClickTime = Time.time;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _squadLayer))
            {
                // Hit a squad
                Entity clickedSquad = GetSquadFromHit(hit);
                if (clickedSquad != null)
                {
                    SelectSquad(clickedSquad);
                    
                    if (isDoubleClick)
                    {
                        // Double click - center camera on squad
                        CenterCameraOnSquad(clickedSquad);
                    }
                }
            }
            else
            {
                // Clicked empty space - deselect
                DeselectCurrentSquad();
            }
        }
        
        private void HandleLeftClickDrag()
        {
            float dragDistance = Vector3.Distance(_dragStartPosition, Input.mousePosition);
            
            if (dragDistance > _dragThreshold)
            {
                _isDragging = true;
                
                // Create selection rectangle
                Rect selectionRect = GetScreenRect(_dragStartPosition, Input.mousePosition);
                OnAreaSelection?.Invoke(selectionRect);
            }
        }
        
        private void HandleLeftClickUp()
        {
            if (_isDragging)
            {
                // Finish area selection
                Rect selectionRect = GetScreenRect(_dragStartPosition, Input.mousePosition);
                SelectSquadsInArea(selectionRect);
            }
            
            _isDragging = false;
        }
        
        private void HandleRightClick()
        {
            if (_selectedSquad == null) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Check if clicked on enemy squad
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _squadLayer))
            {
                Entity targetSquad = GetSquadFromHit(hit);
                if (targetSquad != null && IsEnemySquad(targetSquad))
                {
                    // Attack command
                    IssueAttackCommand(targetSquad);
                    return;
                }
            }
            
            // Check if clicked on ground
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
            {
                // Move command
                Vector2Int gridPosition = _gridManager.GetGridCoordinates(hit.point);
                IssueMoveCommand(gridPosition);
            }
        }
        
        private void HandleKeyboardInput()
        {
            // ESC - pause game
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_gameManager.CurrentState == GameState.PLAYING)
                    _gameManager.PauseGame();
                else if (_gameManager.CurrentState == GameState.PAUSED)
                    _gameManager.ResumeGame();
            }
            
            // Tab - cycle through squads
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CycleSquadSelection();
            }
            
            // Formation shortcuts
            if (_selectedSquad != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    ChangeFormation(FormationType.BASIC);
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    ChangeFormation(FormationType.PHALANX);
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    ChangeFormation(FormationType.TESTUDO);
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    ChangeFormation(FormationType.WEDGE);
            }
            
            // Special commands
            if (_selectedSquad != null)
            {
                if (Input.GetKeyDown(KeyCode.S)) // Stop
                    IssueStopCommand();
                else if (Input.GetKeyDown(KeyCode.D)) // Defend
                    IssueDefendCommand();
                else if (Input.GetKeyDown(KeyCode.R)) // Retreat
                    IssueRetreatCommand();
            }
        }
        
        private void SelectSquad(Entity squad)
        {
            _selectedSquad = squad;
            OnSquadSelected?.Invoke(squad);
            
            Debug.Log($"Selected squad {squad.Id}");
        }
        
        private void DeselectCurrentSquad()
        {
            _selectedSquad = null;
            OnSquadSelected?.Invoke(null);
        }
        
        private void IssueMoveCommand(Vector2Int gridPosition)
        {
            var command = new Command
            {
                Type = CommandType.MOVE,
                TargetSquad = _selectedSquad,
                TargetPosition = gridPosition
            };
            
            OnCommandIssued?.Invoke(command);
        }
        
        private void IssueAttackCommand(Entity targetSquad)
        {
            var command = new Command
            {
                Type = CommandType.ATTACK,
                TargetSquad = _selectedSquad,
                TargetEntity = targetSquad
            };
            
            OnCommandIssued?.Invoke(command);
        }
        
        private void IssueDefendCommand()
        {
            var command = new Command
            {
                Type = CommandType.DEFEND,
                TargetSquad = _selectedSquad
            };
            
            OnCommandIssued?.Invoke(command);
        }
        
        private void IssueStopCommand()
        {
            var command = new Command
            {
                Type = CommandType.MOVE,
                TargetSquad = _selectedSquad,
                TargetPosition = _gridManager.GetGridCoordinates(_selectedSquad.GetComponent<PositionComponent>().Position)
            };
            
            OnCommandIssued?.Invoke(command);
        }
        
        private void IssueRetreatCommand()
        {
            // TODO: Implement retreat logic
        }
        
        private void ChangeFormation(FormationType formation)
        {
            var command = new Command
            {
                Type = CommandType.FORMATION_CHANGE,
                TargetSquad = _selectedSquad,
                Formation = formation
            };
            
            OnCommandIssued?.Invoke(command);
        }
        
        private Entity GetSquadFromHit(RaycastHit hit)
        {
            // TODO: Implement proper squad detection
            // For now, return first squad found
            foreach (var squad in _gameManager.GetSquadsByFaction(Faction.PLAYER))
            {
                var position = squad.GetComponent<PositionComponent>().Position;
                if (Vector3.Distance(position, hit.point) < 2f)
                {
                    return squad;
                }
            }
            
            return null;
        }
        
        private bool IsEnemySquad(Entity squad)
        {
            if (squad.HasComponent<FactionComponent>())
            {
                return squad.GetComponent<FactionComponent>().Faction == Faction.ENEMY;
            }
            return false;
        }
        
        private Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
        {
            // Move origin from bottom left to top left
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;
            
            // Calculate corners
            var topLeft = Vector3.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector3.Max(screenPosition1, screenPosition2);
            
            // Create rect
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }
        
        private void SelectSquadsInArea(Rect selectionRect)
        {
            // TODO: Implement multi-selection
        }
        
        private void CycleSquadSelection()
        {
            var playerSquads = _gameManager.GetSquadsByFaction(Faction.PLAYER);
            if (playerSquads.Count == 0) return;
            
            int currentIndex = _selectedSquad != null ? playerSquads.IndexOf(_selectedSquad) : -1;
            int nextIndex = (currentIndex + 1) % playerSquads.Count;
            
            SelectSquad(playerSquads[nextIndex]);
        }
        
        private void CenterCameraOnSquad(Entity squad)
        {
            // TODO: Implement camera movement
        }
        
        private void HandleMiddleMouseDrag()
        {
            // TODO: Implement camera panning
        }
        
        private void HandleMouseScroll(float scroll)
        {
            // TODO: Implement camera zoom
        }
    }
}