using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Grid;

public class GridDebugTool : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showGridDebug = true;
    [SerializeField] private bool showGridCoordinates = true;
    [SerializeField] private bool showOccupiedCells = true;
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField] private Color occupiedCellColor = Color.red;
    
    [Header("Test Functions")]
    [SerializeField] private bool randomizeGridOnStart = false;
    [SerializeField] private int randomObstacleCount = 5;
    
    private GridManager _gridManager;
    
    private void Start()
    {
        _gridManager = FindObjectOfType<GridManager>();
        
        if (randomizeGridOnStart && _gridManager != null)
        {
            RandomizeGrid();
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGridDebug || Application.isPlaying) return;
        
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) return;
        
        // Vẽ preview của grid trong Editor
        DrawGridPreview(gridManager._gridWidth, gridManager._gridHeight, gridManager._cellSize);
    }
    
    private void OnGUI()
    {
        if (!showGridDebug || _gridManager == null) return;
        
        // Vẽ GUI debug
        GUI.Box(new Rect(10, 10, 200, 180), "Grid Debug");
        
        int y = 40;
        GUI.Label(new Rect(20, y, 180, 20), $"Grid Size: {_gridManager._gridWidth}x{_gridManager._gridHeight}");
        y += 25;
        GUI.Label(new Rect(20, y, 180, 20), $"Cell Size: {_gridManager._cellSize}");
        y += 25;
        
        if (GUI.Button(new Rect(20, y, 180, 30), "Reinitialize Grid"))
        {
            _gridManager.InitializeGrid();
        }
        y += 35;
        
        if (GUI.Button(new Rect(20, y, 180, 30), "Randomize Obstacles"))
        {
            RandomizeGrid();
        }
    }
    
    private void DrawGridPreview(int width, int height, float cellSize)
    {
        Gizmos.color = gridLineColor;
        
        // Vẽ các đường grid
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                new Vector3(x * cellSize, 0, 0),
                new Vector3(x * cellSize, 0, height * cellSize)
            );
        }
        
        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(
                new Vector3(0, 0, z * cellSize),
                new Vector3(width * cellSize, 0, z * cellSize)
            );
        }
    }
    
    private void RandomizeGrid()
    {
        if (_gridManager == null) return;
        
        // Tạo một số ô ngẫu nhiên làm chướng ngại vật
        for (int i = 0; i < randomObstacleCount; i++)
        {
            int x = Random.Range(0, _gridManager._gridWidth);
            int z = Random.Range(0, _gridManager._gridHeight);
            
            Vector2Int pos = new Vector2Int(x, z);
            
            if (!_gridManager.IsCellOccupied(pos))
            {
                // Đánh dấu ô đã bị chiếm
                _gridManager.SetCellOccupied(pos, true);
                
                // Thay đổi màu của tile
                GridCell cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    // Thay đổi visual (màu, material, v.v.)
                    cell.IsWalkable = false;
                    
                    // Tìm gameobject của tile
                    Transform tileTransform = _gridManager.transform.Find($"GridParent/Tile_{x}_{z}");
                    if (tileTransform != null)
                    {
                        Renderer renderer = tileTransform.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = occupiedCellColor;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Randomized grid with {randomObstacleCount} obstacles");
    }
}