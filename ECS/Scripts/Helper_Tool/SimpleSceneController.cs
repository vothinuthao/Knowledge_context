// SimpleSceneController.cs
using UnityEngine;
using Managers;
using Core.Grid;
using Core.ECS;
using Components;
using Components.Squad;
using Squad;
using Systems.Steering;
using Systems.Movement;
using Systems.Squad;

public class SimpleSceneController : MonoBehaviour
{
    [Header("Managers - Auto-created if null")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private WorldManager _worldManager;
    [SerializeField] private GridManager _gridManager;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject _troopPrefab;
    [SerializeField] private GameObject _gridVisualPrefab;
    
    [Header("Quick Test Settings")]
    [SerializeField] private KeyCode _spawnSquadKey = KeyCode.Space;
    [SerializeField] private KeyCode _spawnAtMouseKey = KeyCode.S;
    [SerializeField] private KeyCode _spawnEnemyKey = KeyCode.E;
    
    private Entity _selectedSquad;
    
    private void Awake()
    {
        // Auto-create managers if they don't exist
        if (_gameManager == null)
        {
            GameObject go = new GameObject("GameManager");
            _gameManager = go.AddComponent<GameManager>();
        }
        
        if (_worldManager == null)
        {
            GameObject go = new GameObject("WorldManager");
            _worldManager = go.AddComponent<WorldManager>();
        }
        
        if (_gridManager == null)
        {
            GameObject go = new GameObject("GridManager");
            _gridManager = go.AddComponent<GridManager>();
        }
        
        // Initialize managers
        _gridManager.InitializeGrid();
        _worldManager.InitializeWorld();
        
        // Register systems
        RegisterSystems();
        
        // Create grid visual
        CreateGridVisual();
    }
    
    private void RegisterSystems()
    {
        var world = _worldManager.World;
        
        // Squad systems
        world.RegisterSystem(new SquadCommandSystem());
        world.RegisterSystem(new EntityDetectionSystem());
        world.RegisterSystem(new SquadFormationSystem());
        
        // Steering systems
        world.RegisterSystem(new SteeringSystem());
        world.RegisterSystem(new SeekSystem());
        world.RegisterSystem(new SeparationSystem());
        world.RegisterSystem(new ArrivalSystem());
        
        // Movement systems
        world.RegisterSystem(new MovementSystem());
        world.RegisterSystem(new RotationSystem());
        
        Debug.Log("All systems registered");
    }
    
    private void CreateGridVisual()
    {
        if (_gridVisualPrefab != null)
        {
            Instantiate(_gridVisualPrefab);
        }
        else
        {
            // Create simple grid lines
            GameObject gridObj = new GameObject("GridVisual");
            LineRenderer lineRenderer = gridObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineRenderer.endColor = new Color(1, 1, 1, 0.3f);
            lineRenderer.startWidth = lineRenderer.endWidth = 0.05f;
            
            // Simple grid visualization
            DrawGrid(lineRenderer);
        }
    }
    
    private void DrawGrid(LineRenderer lineRenderer)
    {
        int gridWidth = 20;
        int gridHeight = 20;
        float cellSize = 3.0f;
        
        int lineCount = (gridWidth + 1) * 2 + (gridHeight + 1) * 2;
        lineRenderer.positionCount = lineCount;
        
        int index = 0;
        
        // Vertical lines
        for (int x = 0; x <= gridWidth; x++)
        {
            lineRenderer.SetPosition(index++, new Vector3(x * cellSize, 0, 0));
            lineRenderer.SetPosition(index++, new Vector3(x * cellSize, 0, gridHeight * cellSize));
        }
        
        // Horizontal lines
        for (int y = 0; y <= gridHeight; y++)
        {
            lineRenderer.SetPosition(index++, new Vector3(0, 0, y * cellSize));
            lineRenderer.SetPosition(index++, new Vector3(gridWidth * cellSize, 0, y * cellSize));
        }
    }
    
    private void Update()
    {
        HandleInput();
        UpdateSelectedSquadVisual();
    }
    
    private void HandleInput()
    {
        // Spawn squad at center
        if (Input.GetKeyDown(_spawnSquadKey))
        {
            SpawnSquadAtCenter();
        }
        
        // Spawn squad at mouse position
        if (Input.GetKeyDown(_spawnAtMouseKey))
        {
            SpawnSquadAtMouse();
        }
        
        // Spawn enemy squad
        if (Input.GetKeyDown(_spawnEnemyKey))
        {
            SpawnEnemySquad();
        }
        
        // Select squad with left click
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectSquad();
        }
        
        // Move squad with right click
        if (Input.GetMouseButtonDown(1) && _selectedSquad != null)
        {
            MoveSelectedSquad();
        }
    }
    
    private void SpawnSquadAtCenter()
    {
        Vector2Int centerGrid = new Vector2Int(10, 10);
        Vector3 spawnPos = _gridManager.GetCellCenter(centerGrid);
        
        var squad = _gameManager.CreateSquad(null, spawnPos, Faction.PLAYER);
        _selectedSquad = squad;
        
        Debug.Log($"Spawned squad at center {spawnPos}");
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
    
    private void TrySelectSquad()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Find nearest squad
            float minDistance = float.MaxValue;
            Entity closestSquad = null;
            
            foreach (var squad in _worldManager.World.GetEntitiesWith<SquadStateComponent, PositionComponent>())
            {
                var pos = squad.GetComponent<PositionComponent>().Position;
                float distance = Vector3.Distance(hit.point, pos);
                
                if (distance < 3.0f && distance < minDistance)
                {
                    minDistance = distance;
                    closestSquad = squad;
                }
            }
            
            if (closestSquad != null)
            {
                _selectedSquad = closestSquad;
                Debug.Log($"Selected squad {_selectedSquad.Id}");
            }
        }
    }
    
    private void MoveSelectedSquad()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int gridPos = _gridManager.GetGridCoordinates(hit.point);
            Vector3 targetPos = _gridManager.GetCellCenter(gridPos);
            
            var commandSystem = _worldManager.World.GetSystem<SquadCommandSystem>();
            if (commandSystem != null)
            {
                commandSystem.CommandMove(_selectedSquad, targetPos);
                Debug.Log($"Moving squad {_selectedSquad.Id} to {targetPos}");
            }
        }
    }
    
    private void UpdateSelectedSquadVisual()
    {
        // Simple selection indicator (could be improved with visual effects)
        if (_selectedSquad != null && _selectedSquad.HasComponent<PositionComponent>())
        {
            var pos = _selectedSquad.GetComponent<PositionComponent>().Position;
            Debug.DrawLine(pos, pos + Vector3.up * 5, Color.green);
        }
    }
    
    private void OnGUI()
    {
        // Simple instruction panel
        GUI.Box(new Rect(10, 10, 200, 150), "Controls");
        GUI.Label(new Rect(20, 30, 180, 20), $"[{_spawnSquadKey}] Spawn at Center");
        GUI.Label(new Rect(20, 50, 180, 20), $"[{_spawnAtMouseKey}] Spawn at Mouse");
        GUI.Label(new Rect(20, 70, 180, 20), $"[{_spawnEnemyKey}] Spawn Enemy");
        GUI.Label(new Rect(20, 90, 180, 20), "[Left Click] Select Squad");
        GUI.Label(new Rect(20, 110, 180, 20), "[Right Click] Move Squad");
        
        if (_selectedSquad != null)
        {
            GUI.Label(new Rect(20, 130, 180, 20), $"Selected: Squad {_selectedSquad.Id}");
        }
    }
}