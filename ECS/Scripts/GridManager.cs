using UnityEngine;
using System.Collections.Generic;
using Core.Singleton;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hệ thống grid tạo và quản lý các ô (tiles) trên mặt đất
/// </summary>
public class GridManager : ManualSingletonMono<GridManager>
{
    public static GridManager Instance { get; private set; }
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridLength = 20;
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float cellHeight = 0.1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    
    [Header("Cell Visuals")]
    [SerializeField] private bool visualizeGrid = true;
    [SerializeField] private Material defaultCellMaterial;
    [SerializeField] private Material selectedCellMaterial;
    [SerializeField] private Material highlightedCellMaterial;
    [SerializeField] private Material occupiedCellMaterial;
    
    // Dictionary to store tile game objects
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
    
    // Currently selected and highlighted cells
    private Vector2Int? selectedCell = null;
    private Vector2Int? highlightedCell = null;
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();

    public override void Awake()
    {
        base.Awake();
        if (Application.isPlaying)
        {
            CreateGrid();
        }
    }
    
    private void CreateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                Vector2Int coordinates = new Vector2Int(x, z);
                Vector3 position = GetWorldPosition(coordinates);
                GridCell cell = new GridCell(coordinates, position);
                if (visualizeGrid)
                {
                    CreateCellVisual(cell);
                }
                
                gridCells.Add(coordinates, cell);
            }
        }
    }
    
    private void CreateCellVisual(GridCell cell)
    {
        GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cellObject.name = $"Cell_{cell.Coordinates.x}_{cell.Coordinates.y}";
        cellObject.transform.parent = transform;
        cellObject.transform.position = cell.WorldPosition + new Vector3(0, cellHeight / 2, 0);
        cellObject.transform.localScale = new Vector3(cellSize * 0.9f, cellHeight, cellSize * 0.9f);
        
        // Set material
        Renderer renderer = cellObject.GetComponent<Renderer>();
        renderer.material = defaultCellMaterial;
        
        // Set layer to ground
        cellObject.layer = LayerMask.NameToLayer("Ground");
        
        // Add collider for mouse selection
        BoxCollider collider = cellObject.GetComponent<BoxCollider>();
        collider.size = new Vector3(1, 0.1f, 1); // Thin collider
        
        // Store reference
        cell.CellObject = cellObject;
    }
    
    /// <summary>
    /// Chuyển đổi từ tọa độ grid sang tọa độ thế giới
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int coordinates)
    {
        return gridOrigin + new Vector3(
            coordinates.x * cellSize, 
            0, 
            coordinates.y * cellSize
        );
    }
    
    /// <summary>
    /// Chuyển đổi từ tọa độ thế giới sang tọa độ grid
    /// </summary>
    public Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - gridOrigin;
        
        int x = Mathf.FloorToInt(localPosition.x / cellSize);
        int z = Mathf.FloorToInt(localPosition.z / cellSize);
        
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Kiểm tra xem tọa độ có nằm trong grid không
    /// </summary>
    public bool IsWithinGrid(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.x < gridWidth 
            && coordinates.y >= 0 && coordinates.y < gridLength;
    }
    
    /// <summary>
    /// Đánh dấu ô đã được chọn
    /// </summary>
    public void SelectCell(Vector2Int coordinates)
    {
        // Deselect previous cell
        if (selectedCell.HasValue && gridCells.ContainsKey(selectedCell.Value))
        {
            GridCell previousCell = gridCells[selectedCell.Value];
            if (previousCell.CellObject != null)
            {
                Renderer renderer = previousCell.CellObject.GetComponent<Renderer>();
                renderer.material = defaultCellMaterial;
            }
        }
        
        selectedCell = coordinates;
        
        // Apply selection visual
        if (gridCells.ContainsKey(coordinates))
        {
            GridCell cell = gridCells[coordinates];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                renderer.material = selectedCellMaterial;
            }
        }
    }
    
    /// <summary>
    /// Đánh dấu ô đang được hover
    /// </summary>
    public void HighlightCell(Vector2Int coordinates)
    {
        // Remove highlight from previous cell
        if (highlightedCell.HasValue && gridCells.ContainsKey(highlightedCell.Value) 
            && highlightedCell.Value != selectedCell)
        {
            GridCell previousCell = gridCells[highlightedCell.Value];
            if (previousCell.CellObject != null)
            {
                Renderer renderer = previousCell.CellObject.GetComponent<Renderer>();
                renderer.material = occupiedCells.Contains(highlightedCell.Value) ? 
                    occupiedCellMaterial : defaultCellMaterial;
            }
        }
        
        highlightedCell = coordinates;
        
        // Apply highlight visual
        if (gridCells.ContainsKey(coordinates) && coordinates != selectedCell)
        {
            GridCell cell = gridCells[coordinates];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                renderer.material = highlightedCellMaterial;
            }
        }
    }
    
    /// <summary>
    /// Đánh dấu ô đã bị chiếm
    /// </summary>
    public void SetCellOccupied(Vector2Int coordinates, bool occupied)
    {
        if (occupied)
        {
            if (!occupiedCells.Contains(coordinates))
            {
                occupiedCells.Add(coordinates);
            }
        }
        else
        {
            occupiedCells.Remove(coordinates);
        }
        
        if (gridCells.ContainsKey(coordinates) && coordinates != selectedCell && coordinates != highlightedCell)
        {
            GridCell cell = gridCells[coordinates];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                renderer.material = occupied ? occupiedCellMaterial : defaultCellMaterial;
            }
        }
    }
    
    /// <summary>
    /// Kiểm tra xem ô có bị chiếm chưa
    /// </summary>
    public bool IsCellOccupied(Vector2Int coordinates)
    {
        return occupiedCells.Contains(coordinates);
    }
    
    /// <summary>
    /// Tìm ô mà mouse đang trỏ vào
    /// </summary>
    public bool TryGetCellUnderMouse(out Vector2Int coordinates)
    {
        coordinates = new Vector2Int(-1, -1);
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                coordinates = GetGridCoordinates(hit.point);
                return IsWithinGrid(coordinates);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Lấy ô ở tọa độ grid
    /// </summary>
    public GridCell GetCell(Vector2Int coordinates)
    {
        if (gridCells.ContainsKey(coordinates))
        {
            return gridCells[coordinates];
        }
        return null;
    }
    
    /// <summary>
    /// Lấy ô ở tọa độ world
    /// </summary>
    public GridCell GetCellAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        return GetCell(coordinates);
    }
    
    /// <summary>
    /// Lấy trung tâm của ô
    /// </summary>
    public Vector3 GetCellCenter(Vector2Int coordinates)
    {
        return GetWorldPosition(coordinates) + new Vector3(cellSize / 2, 0, cellSize / 2);
    }
    
    /// <summary>
    /// Vẽ debug gizmos trong editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!visualizeGrid)
            return;
            
        Gizmos.color = Color.white;
        
        for (int x = 0; x <= gridWidth; x++)
        {
            Gizmos.DrawLine(
                gridOrigin + new Vector3(x * cellSize, 0, 0),
                gridOrigin + new Vector3(x * cellSize, 0, gridLength * cellSize)
            );
        }
        
        for (int z = 0; z <= gridLength; z++)
        {
            Gizmos.DrawLine(
                gridOrigin + new Vector3(0, 0, z * cellSize),
                gridOrigin + new Vector3(gridWidth * cellSize, 0, z * cellSize)
            );
        }
    }
}

/// <summary>
/// Đại diện cho một ô trong grid
/// </summary>
public class GridCell
{
    public Vector2Int Coordinates { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public GameObject CellObject { get; set; }
    
    // Additional properties
    public bool IsOccupied { get; set; }
    public object Occupant { get; set; }
    
    public GridCell(Vector2Int coordinates, Vector3 worldPosition)
    {
        Coordinates = coordinates;
        WorldPosition = worldPosition;
        IsOccupied = false;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor script để tùy chỉnh GridManager trong Inspector
/// </summary>
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GridManager gridManager = (GridManager)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Preview Grid"))
        {
            // Force OnDrawGizmos to update
            SceneView.RepaintAll();
        }
    }
}
#endif