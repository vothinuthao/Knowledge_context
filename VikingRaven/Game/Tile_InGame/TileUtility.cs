using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Utility for generating and managing tiles in editor
    /// </summary>
    public class TileUtility : MonoBehaviour
    {
        [Header("Tile Creation")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private Transform _tileParent;
        [SerializeField] private float _tileSize = 4f;
        [SerializeField] private float _tileHeight = 0.5f;
        
        [Header("Grid Generation")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(5, 5);
        [SerializeField] private float _gridSpacing = 4f;
        [SerializeField] private bool _connectDiagonals = true;
        
        [Header("Custom Layout")]
        [SerializeField] private bool _overrideDefaultMesh = false;
        [SerializeField] private Mesh _customTileMesh;
        [SerializeField] private Material _tileMaterial;
        
        // Editor-only fields
        #if UNITY_EDITOR
        private int _selectedTileId = -1;
        private HashSet<TileComponent> _selectedTiles = new HashSet<TileComponent>();
        #endif
        
        /// <summary>
        /// Create a new tile at the specified position
        /// </summary>
        public TileComponent CreateTile(Vector3 position, int tileId = -1)
        {
            if (_tilePrefab == null)
            {
                Debug.LogError("TileUtility: Tile prefab not assigned!");
                return null;
            }
            
            // Create parent if needed
            if (_tileParent == null)
            {
                GameObject parent = new GameObject("Tiles");
                _tileParent = parent.transform;
            }
            
            // Create tile
            GameObject tileObject = Instantiate(_tilePrefab, position, Quaternion.identity, _tileParent);
            TileComponent tileComponent = tileObject.GetComponent<TileComponent>();
            
            if (tileComponent == null)
            {
                tileComponent = tileObject.AddComponent<TileComponent>();
            }
            
            // Set ID (if provided) or use current count as ID
            if (tileId < 0)
            {
                TileComponent[] existingTiles = FindObjectsOfType<TileComponent>();
                tileId = existingTiles.Length;
            }
            
            tileComponent.SetTileId(tileId);
            
            // Apply custom mesh if needed
            if (_overrideDefaultMesh && _customTileMesh != null)
            {
                MeshFilter meshFilter = tileObject.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.mesh = _customTileMesh;
                }
                
                if (_tileMaterial != null)
                {
                    MeshRenderer renderer = tileObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material = _tileMaterial;
                    }
                }
            }
            
            // Set size
            Transform tileTransform = tileObject.transform;
            tileTransform.localScale = new Vector3(_tileSize, _tileHeight, _tileSize);
            
            return tileComponent;
        }
        
        /// <summary>
        /// Generate a grid of tiles
        /// </summary>
        public void GenerateTileGrid()
        {
            // Create parent if needed
            if (_tileParent == null)
            {
                GameObject parent = new GameObject("Tiles");
                _tileParent = parent.transform;
            }
            
            // Create tiles
            List<TileComponent> createdTiles = new List<TileComponent>();
            
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int z = 0; z < _gridSize.y; z++)
                {
                    // Calculate position
                    Vector3 position = new Vector3(
                        x * _gridSpacing,
                        0,
                        z * _gridSpacing
                    );
                    
                    int tileId = x * _gridSize.y + z;
                    TileComponent tileComponent = CreateTile(position, tileId);
                    
                    if (tileComponent != null)
                    {
                        createdTiles.Add(tileComponent);
                    }
                }
            }
            
            // Connect neighbors
            ConnectTileNeighbors(createdTiles);
            
            Debug.Log($"TileUtility: Generated {createdTiles.Count} tiles in a {_gridSize.x}x{_gridSize.y} grid");
        }
        
        /// <summary>
        /// Connect tile neighbors in grid
        /// </summary>
        private void ConnectTileNeighbors(List<TileComponent> tiles)
        {
            // Create a lookup dictionary for faster neighbor finding
            Dictionary<Vector3, TileComponent> tilesByPosition = new Dictionary<Vector3, TileComponent>();
            foreach (var tile in tiles)
            {
                tilesByPosition[tile.transform.position] = tile;
            }
            
            // Connect neighbors
            foreach (var tile in tiles)
            {
                Vector3 tilePos = tile.transform.position;
                
                // Define neighbor offsets (4 or 8 directions)
                Vector3[] neighborOffsets = _connectDiagonals
                    ? new Vector3[] {
                        new Vector3(_gridSpacing, 0, 0),
                        new Vector3(-_gridSpacing, 0, 0),
                        new Vector3(0, 0, _gridSpacing),
                        new Vector3(0, 0, -_gridSpacing),
                        new Vector3(_gridSpacing, 0, _gridSpacing),
                        new Vector3(_gridSpacing, 0, -_gridSpacing),
                        new Vector3(-_gridSpacing, 0, _gridSpacing),
                        new Vector3(-_gridSpacing, 0, -_gridSpacing)
                    }
                    : new Vector3[] {
                        new Vector3(_gridSpacing, 0, 0),
                        new Vector3(-_gridSpacing, 0, 0),
                        new Vector3(0, 0, _gridSpacing),
                        new Vector3(0, 0, -_gridSpacing)
                    };
                
                // Find and connect neighbors
                foreach (var offset in neighborOffsets)
                {
                    Vector3 neighborPos = tilePos + offset;
                    
                    if (tilesByPosition.TryGetValue(neighborPos, out TileComponent neighbor))
                    {
                        tile.AddNeighbor(neighbor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Delete all tiles
        /// </summary>
        public void DeleteAllTiles()
        {
            TileComponent[] tiles = FindObjectsOfType<TileComponent>();
            
            foreach (var tile in tiles)
            {
                DestroyImmediate(tile.gameObject);
            }
            
            Debug.Log($"TileUtility: Deleted {tiles.Length} tiles");
        }
        
        // Editor-only methods for tile management
        #if UNITY_EDITOR
        /// <summary>
        /// Select a tile by ID
        /// </summary>
        public void SelectTile(int tileId)
        {
            _selectedTileId = tileId;
            
            // Highlight selected tile in Scene view
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Add a tile to selection
        /// </summary>
        public void AddTileToSelection(TileComponent tileComponent)
        {
            if (tileComponent != null)
            {
                _selectedTiles.Add(tileComponent);
                
                // Highlight selected tile in Scene view
                SceneView.RepaintAll();
            }
        }
        
        /// <summary>
        /// Clear tile selection
        /// </summary>
        public void ClearTileSelection()
        {
            _selectedTileId = -1;
            _selectedTiles.Clear();
            
            // Update Scene view
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Draw gizmos for editor visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_selectedTileId >= 0)
            {
                // Find and highlight the selected tile
                TileComponent[] tiles = FindObjectsOfType<TileComponent>();
                
                foreach (var tile in tiles)
                {
                    if (tile.TileId == _selectedTileId)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 size = new Vector3(_tileSize, _tileHeight * 2, _tileSize);
                        Gizmos.DrawWireCube(tile.transform.position + Vector3.up * 0.1f, size);
                        break;
                    }
                }
            }
            
            // Highlight multiple selected tiles
            foreach (var tile in _selectedTiles)
            {
                if (tile != null)
                {
                    Gizmos.color = Color.green;
                    Vector3 size = new Vector3(_tileSize, _tileHeight * 2, _tileSize);
                    Gizmos.DrawWireCube(tile.transform.position + Vector3.up * 0.1f, size);
                }
            }
        }
        
        /// <summary>
        /// Edit mode for tile grid layout
        /// </summary>
        [ContextMenu("Enable Tile Grid Edit Mode")]
        private void EnableEditMode()
        {
            // This would activate a custom Editor window for tile manipulation
            // Requires implementing a custom Editor script
            Debug.Log("TileUtility: Grid Edit Mode is not implemented yet");
        }
        #endif
    }
    
    // Editor-specific code for tile manipulation
    #if UNITY_EDITOR
    [CustomEditor(typeof(TileUtility))]
    public class TileUtilityEditor : Editor
    {
        private TileUtility _tileUtility;
        
        private void OnEnable()
        {
            _tileUtility = (TileUtility)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Generation Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Tile Grid"))
            {
                if (EditorUtility.DisplayDialog("Generate Tile Grid",
                    "This will generate a new tile grid. Any existing tiles might be affected. Continue?",
                    "Generate", "Cancel"))
                {
                    _tileUtility.GenerateTileGrid();
                }
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Delete All Tiles"))
            {
                if (EditorUtility.DisplayDialog("Delete All Tiles",
                    "This will delete all tiles in the scene. This cannot be undone. Continue?",
                    "Delete", "Cancel"))
                {
                    _tileUtility.DeleteAllTiles();
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Clear Selection"))
            {
                _tileUtility.ClearTileSelection();
            }
        }
    }
    #endif
}