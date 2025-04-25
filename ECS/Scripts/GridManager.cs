// ECS/Scripts/GridManager.cs
using UnityEngine;
using System.Collections.Generic;
using Core.Singleton;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// System to create and manage grid cells on the ground
/// </summary>
public class GridManager : ManualSingletonMono<GridManager>
{
    
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
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Dictionary to store tile game objects
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
    
    // Currently selected and highlighted cells
    private Vector2Int? selectedCell = null;
    private Vector2Int? highlightedCell = null;
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();

    protected override void Awake()
    {
        base.Awake();
        if (defaultCellMaterial == null)
        {
            Debug.LogWarning("Default cell material is not assigned. Creating a default material.");
            defaultCellMaterial = new Material(Shader.Find("Standard"));
            defaultCellMaterial.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        }
        
        if (selectedCellMaterial == null)
        {
            Debug.LogWarning("Selected cell material is not assigned. Creating a default material.");
            selectedCellMaterial = new Material(Shader.Find("Standard"));
            selectedCellMaterial.color = new Color(0.0f, 1.0f, 0.0f, 0.5f);
        }
        
        if (highlightedCellMaterial == null)
        {
            Debug.LogWarning("Highlighted cell material is not assigned. Creating a default material.");
            highlightedCellMaterial = new Material(Shader.Find("Standard"));
            highlightedCellMaterial.color = new Color(0.0f, 0.7f, 1.0f, 0.5f);
        }
        
        if (occupiedCellMaterial == null)
        {
            Debug.LogWarning("Occupied cell material is not assigned. Creating a default material.");
            occupiedCellMaterial = new Material(Shader.Find("Standard"));
            occupiedCellMaterial.color = new Color(1.0f, 0.5f, 0.0f, 0.5f);
        }
        
        // Create grid on awake in play mode
        if (Application.isPlaying)
        {
            CreateGrid();
        }
    }
    
    /// <summary>
    /// Creates all grid cells
    /// </summary>
    private void CreateGrid()
    {
        if (debugMode)
        {
            Debug.Log($"Creating grid: {gridWidth}x{gridLength}, Cell size: {cellSize}");
        }
        
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
        
        Debug.Log($"Grid created with {gridCells.Count} cells");
    }
    
    /// <summary>
    /// Creates visual representation of a grid cell
    /// </summary>
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
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1)
        {
            // Create Ground layer if it doesn't exist
            Debug.LogWarning("'Ground' layer not found. Please create it in your project settings.");
            cellObject.layer = 0; // Default layer
        }
        else
        {
            cellObject.layer = groundLayer;
        }
        
        // Add collider for mouse selection
        BoxCollider collider = cellObject.GetComponent<BoxCollider>();
        collider.size = new Vector3(1, 0.1f, 1); // Thin collider
        
        // Store reference
        cell.CellObject = cellObject;
    }
    
    /// <summary>
    /// Converts grid coordinates to world position
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
    /// Converts world position to grid coordinates
    /// </summary>
    public Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - gridOrigin;
        
        int x = Mathf.FloorToInt(localPosition.x / cellSize);
        int z = Mathf.FloorToInt(localPosition.z / cellSize);
        
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Checks if coordinates are within the grid
    /// </summary>
    public bool IsWithinGrid(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.x < gridWidth 
            && coordinates.y >= 0 && coordinates.y < gridLength;
    }
    
    /// <summary>
    /// Marks a cell as selected
    /// </summary>
    public void SelectCell(Vector2Int coordinates)
    {
        // FIX: Double check if coordinates are valid
        if (!IsWithinGrid(coordinates))
        {
            Debug.LogWarning($"Attempted to select cell outside grid: {coordinates}");
            return;
        }
        
        // Deselect previous cell
        if (selectedCell.HasValue && gridCells.ContainsKey(selectedCell.Value))
        {
            GridCell previousCell = gridCells[selectedCell.Value];
            if (previousCell.CellObject != null)
            {
                Renderer renderer = previousCell.CellObject.GetComponent<Renderer>();
                if (occupiedCells.Contains(selectedCell.Value))
                {
                    renderer.material = occupiedCellMaterial;
                }
                else
                {
                    renderer.material = defaultCellMaterial;
                }
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
        
        if (debugMode)
        {
            Debug.Log($"Selected cell: {coordinates}");
        }
    }
    
    /// <summary>
    /// Marks a cell as highlighted (on hover)
    /// </summary>
    public void HighlightCell(Vector2Int coordinates)
    {
        // FIX: Check if coordinates are valid
        if (!IsWithinGrid(coordinates))
        {
            return;
        }
        
        // Check if this is the same as current highlight
        if (highlightedCell.HasValue && highlightedCell.Value == coordinates)
        {
            return; // Already highlighted
        }
        
        // Remove highlight from previous cell
        if (highlightedCell.HasValue && gridCells.ContainsKey(highlightedCell.Value) 
            && highlightedCell.Value != selectedCell)
        {
            GridCell previousCell = gridCells[highlightedCell.Value];
            if (previousCell.CellObject != null)
            {
                Renderer renderer = previousCell.CellObject.GetComponent<Renderer>();
                if (occupiedCells.Contains(highlightedCell.Value))
                {
                    renderer.material = occupiedCellMaterial;
                }
                else
                {
                    renderer.material = defaultCellMaterial;
                }
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
    /// Sets a cell as occupied
    /// </summary>
    public void SetCellOccupied(Vector2Int coordinates, bool occupied)
    {
        if (!IsWithinGrid(coordinates))
        {
            if (debugMode)
            {
                Debug.LogWarning($"Tried to set occupancy of cell {coordinates}, which is outside the grid.");
            }
            return;
        }
        
        if (occupied)
        {
            if (!occupiedCells.Contains(coordinates))
            {
                occupiedCells.Add(coordinates);
                if (debugMode)
                {
                    Debug.Log($"Cell {coordinates} marked as occupied");
                }
            }
        }
        else
        {
            if (occupiedCells.Contains(coordinates))
            {
                occupiedCells.Remove(coordinates);
                if (debugMode)
                {
                    Debug.Log($"Cell {coordinates} marked as unoccupied");
                }
            }
        }
        
        // FIX: Update visual based on current selection/highlight state
        if (gridCells.ContainsKey(coordinates))
        {
            GridCell cell = gridCells[coordinates];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                
                // Apply correct material based on cell state
                if (selectedCell.HasValue && selectedCell.Value == coordinates)
                {
                    renderer.material = selectedCellMaterial;
                }
                else if (highlightedCell.HasValue && highlightedCell.Value == coordinates)
                {
                    renderer.material = highlightedCellMaterial;
                }
                else
                {
                    renderer.material = occupied ? occupiedCellMaterial : defaultCellMaterial;
                }
            }
        }
        
        // FIX: Update the cell's IsOccupied property
        if (gridCells.ContainsKey(coordinates))
        {
            gridCells[coordinates].IsOccupied = occupied;
        }
    }
    
    /// <summary>
    /// Checks if a cell is occupied
    /// </summary>
    public bool IsCellOccupied(Vector2Int coordinates)
    {
        return occupiedCells.Contains(coordinates);
    }
    
    /// <summary>
    /// Tries to get the cell under the mouse cursor
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
    /// Gets a cell by grid coordinates
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
    /// Gets a cell at world position
    /// </summary>
    public GridCell GetCellAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        return GetCell(coordinates);
    }
    
    /// <summary>
    /// Gets the center position of a cell
    /// </summary>
    public Vector3 GetCellCenter(Vector2Int coordinates)
    {
        return GetWorldPosition(coordinates) + new Vector3(cellSize / 2, 0, cellSize / 2);
    }
    
    // FIX: Added method to clear cell selection and highlighting
    /// <summary>
    /// Clears all current cell selections and highlights
    /// </summary>
    public void ClearCellSelections()
    {
        // Clear highlighted cell
        if (highlightedCell.HasValue && gridCells.ContainsKey(highlightedCell.Value))
        {
            GridCell cell = gridCells[highlightedCell.Value];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                if (occupiedCells.Contains(highlightedCell.Value))
                {
                    renderer.material = occupiedCellMaterial;
                }
                else
                {
                    renderer.material = defaultCellMaterial;
                }
            }
            highlightedCell = null;
        }
        
        // Clear selected cell
        if (selectedCell.HasValue && gridCells.ContainsKey(selectedCell.Value))
        {
            GridCell cell = gridCells[selectedCell.Value];
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                if (occupiedCells.Contains(selectedCell.Value))
                {
                    renderer.material = occupiedCellMaterial;
                }
                else
                {
                    renderer.material = defaultCellMaterial;
                }
            }
            selectedCell = null;
        }
        
        if (debugMode)
        {
            Debug.Log("Cleared all cell selections");
        }
    }
    
    /// <summary>
    /// Draws debug gizmos in editor
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
    
    /// <summary>
    /// Clears all occupied cell markers
    /// </summary>
    public void ClearAllOccupiedCells()
    {
        foreach (var coords in occupiedCells.ToArray())
        {
            SetCellOccupied(coords, false);
        }
        occupiedCells.Clear();
        
        // FIX: Reset all cells' IsOccupied property
        foreach (var cell in gridCells.Values)
        {
            cell.IsOccupied = false;
            
            // Reset material to default
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                
                // Don't reset if it's selected or highlighted
                if ((selectedCell.HasValue && selectedCell.Value == cell.Coordinates) ||
                    (highlightedCell.HasValue && highlightedCell.Value == cell.Coordinates))
                {
                    continue;
                }
                
                renderer.material = defaultCellMaterial;
            }
        }
    }
    
    /// <summary>
    /// Updates occupancy based on actual squad positions 
    /// </summary>
    public void RefreshOccupancyFromSquads()
    {
        ClearAllOccupiedCells();
        
        // This would need to be implemented according to your game's needs
        // For example, you could iterate over all squad entities and mark their positions
    }
    
    // FIX: Added method to refresh all cell visuals
    /// <summary>
    /// Refreshes the visual state of all cells
    /// </summary>
    public void RefreshAllCellVisuals()
    {
        foreach (var kvp in gridCells)
        {
            Vector2Int coordinates = kvp.Key;
            GridCell cell = kvp.Value;
            
            if (cell.CellObject != null)
            {
                Renderer renderer = cell.CellObject.GetComponent<Renderer>();
                
                // Determine which material to use based on cell state
                if (selectedCell.HasValue && selectedCell.Value == coordinates)
                {
                    renderer.material = selectedCellMaterial;
                }
                else if (highlightedCell.HasValue && highlightedCell.Value == coordinates)
                {
                    renderer.material = highlightedCellMaterial;
                }
                else if (cell.IsOccupied || occupiedCells.Contains(coordinates))
                {
                    renderer.material = occupiedCellMaterial;
                }
                else
                {
                    renderer.material = defaultCellMaterial;
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log("Refreshed all cell visuals");
        }
    }
}

/// <summary>
/// Represents a cell in the grid
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