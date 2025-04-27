// QuickControlPanel.cs

using Components;
using Components.Squad;
using Core.ECS;
using Core.Grid;
using Managers;
using UnityEngine;

namespace Helper_Tool
{
    public class QuickControlPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridManager _gridManager;
    
        [Header("UI Settings")]
        [SerializeField] private Vector2 _panelPosition = new Vector2(10, 10);
        [SerializeField] private float _panelWidth = 200f;
        [SerializeField] private GUIStyle _buttonStyle;
        [SerializeField] private GUIStyle _labelStyle;
    
        private bool _isExpanded = true;
        private Entity _selectedSquad;
    
        private void Start()
        {
            if (_gameManager == null)
                _gameManager = GameManager.Instance;
            
            if (_gridManager == null)
                _gridManager = GridManager.Instance;
        }
    
        private void OnGUI()
        {
            DrawQuickControlPanel();
        }
    
        private void DrawQuickControlPanel()
        {
            // Main panel button
            if (GUI.Button(new Rect(_panelPosition.x, _panelPosition.y, 120, 30), 
                    _isExpanded ? "Hide Controls" : "Show Controls"))
            {
                _isExpanded = !_isExpanded;
            }
        
            if (!_isExpanded) return;
        
            // Panel background
            float panelHeight = 400;
            GUI.Box(new Rect(_panelPosition.x, _panelPosition.y + 35, _panelWidth, panelHeight), "Quick Controls");
        
            float yOffset = _panelPosition.y + 60;
            float buttonHeight = 30;
            float spacing = 5;
        
            // Squad Spawn Section
            GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), "Squad Spawning", _labelStyle);
            yOffset += 25;
        
            if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Spawn at Center"))
            {
                SpawnSquadAtCenter();
            }
            yOffset += buttonHeight + spacing;
        
            if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Spawn at Mouse"))
            {
                SpawnSquadAtMouse();
            }
            yOffset += buttonHeight + spacing;
        
            if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Spawn Enemy Squad"))
            {
                SpawnEnemySquad();
            }
            yOffset += buttonHeight + spacing;
        
            if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Spawn Circle Formation"))
            {
                SpawnSquadsInCircle();
            }
            yOffset += buttonHeight + spacing * 2;
        
            // Squad Control Section
            GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), "Squad Control", _labelStyle);
            yOffset += 25;
        
            // Display selected squad info
            if (_selectedSquad != null)
            {
                GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), 
                    $"Selected: Squad {_selectedSquad.Id}");
                yOffset += 20;
            
                if (_selectedSquad.HasComponent<SquadStateComponent>())
                {
                    var state = _selectedSquad.GetComponent<SquadStateComponent>();
                    GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), 
                        $"State: {state.CurrentState}");
                    yOffset += 25;
                }
            
                // Control buttons
                if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Stop Squad"))
                {
                    StopSelectedSquad();
                }
                yOffset += buttonHeight + spacing;
            
                if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Defend Position"))
                {
                    DefendSelectedSquad();
                }
                yOffset += buttonHeight + spacing;
            
                // Formation buttons
                if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, (_panelWidth - 30) / 2, buttonHeight), "Basic"))
                {
                    ChangeFormation(FormationType.BASIC);
                }
                if (GUI.Button(new Rect(_panelPosition.x + 10 + (_panelWidth - 30) / 2 + 10, yOffset, (_panelWidth - 30) / 2, buttonHeight), "Phalanx"))
                {
                    ChangeFormation(FormationType.PHALANX);
                }
                yOffset += buttonHeight + spacing;
            }
            else
            {
                GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), "No Squad Selected");
                yOffset += 25;
            }
        
            // Debug Section
            yOffset += 10;
            GUI.Label(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, 20), "Debug", _labelStyle);
            yOffset += 25;
        
            if (GUI.Button(new Rect(_panelPosition.x + 10, yOffset, _panelWidth - 20, buttonHeight), "Clear All Squads"))
            {
                ClearAllSquads();
            }
        }
    
        private void SpawnSquadAtCenter()
        {
            Vector2Int centerGrid = new Vector2Int(10, 10);
            Vector3 spawnPos = _gridManager.GetCellCenter(centerGrid);
        
            var squad = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
            _selectedSquad = squad;
        
            Debug.Log($"Spawned squad at center position {spawnPos}");
        }
    
        private void SpawnSquadAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
                Vector3 spawnPos = _gridManager.GetCellCenter(gridPos);
            
                var squad = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
                _selectedSquad = squad;
            
                Debug.Log($"Spawned squad at mouse position {spawnPos}");
            }
        }
    
        private void SpawnEnemySquad()
        {
            Vector2Int randomGrid = new Vector2Int(
                Random.Range(0, 20), 
                Random.Range(0, 20)
            );
            Vector3 spawnPos = _gridManager.GetCellCenter(randomGrid);
        
            _gameManager.CreateSquad(null, spawnPos, Faction.ENEMY);
        
            Debug.Log($"Spawned enemy squad at {spawnPos}");
        }
    
        private void SpawnSquadsInCircle()
        {
            int squadCount = 4;
            float radius = 9.0f;
            Vector2Int centerGrid = new Vector2Int(10, 10);
            Vector3 center = _gridManager.GetCellCenter(centerGrid);
        
            for (int i = 0; i < squadCount; i++)
            {
                float angle = i * (360f / squadCount) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 spawnPos = center + offset;
            
                // Snap to grid
                Vector2Int gridPos = _gridManager.GetGridCoordinates(spawnPos);
                spawnPos = _gridManager.GetCellCenter(gridPos);
            
                _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
            }
        
            Debug.Log($"Spawned {squadCount} squads in circle formation");
        }
    
        private void StopSelectedSquad()
        {
            if (_selectedSquad == null) return;
        
            var command = new Command
            {
                Type = CommandType.MOVE,
                TargetSquad = _selectedSquad,
                TargetPosition = _gridManager.GetGridCoordinates(_selectedSquad.GetComponent<PositionComponent>().Position)
            };
        
            // You might need to implement a direct command system
            // For now, we'll use the existing system
            Debug.Log("Stop command issued");
        }
    
        private void DefendSelectedSquad()
        {
            if (_selectedSquad == null) return;
        
            var command = new Command
            {
                Type = CommandType.DEFEND,
                TargetSquad = _selectedSquad
            };
        
            Debug.Log("Defend command issued");
        }
    
        private void ChangeFormation(FormationType formation)
        {
            if (_selectedSquad == null) return;
        
            var command = new Command
            {
                Type = CommandType.FORMATION_CHANGE,
                TargetSquad = _selectedSquad,
                Formation = formation
            };
        
            Debug.Log($"Formation change to {formation} issued");
        }
    
        private void ClearAllSquads()
        {
            // This would need to be implemented in GameManager
            Debug.Log("Clear all squads requested");
        }
    
        // Mouse selection handling
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectSquad();
            }
        }
    
        private void TrySelectSquad()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
        
            if (Physics.Raycast(ray, out hit))
            {
                // Try to find EntityBehaviour on hit object
                EntityBehaviour behaviour = hit.collider.GetComponent<EntityBehaviour>();
                if (behaviour != null && behaviour.GetEntity() != null)
                {
                    Entity entity = behaviour.GetEntity();
                
                    // Check if this is a squad entity
                    if (entity.HasComponent<SquadStateComponent>())
                    {
                        _selectedSquad = entity;
                        Debug.Log($"Selected squad {entity.Id}");
                    }
                }
            }
        }
    }
}