using Sirenix.OdinInspector;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Represents a single tile in the game world that can contain squad units
    /// </summary>
    public class TileComponent : MonoBehaviour
    {
        [TitleGroup("Tile Identification")]
        [Tooltip("Unique identifier for this tile")]
        [SerializeField] private int _tileId;

        [TitleGroup("Visual Settings")]
        [Tooltip("Color when the tile is in default state")]
        [SerializeField] private Color _defaultColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        
        [Tooltip("Color when the tile is highlighted as a valid move destination")]
        [SerializeField] private Color _highlightColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
        
        [Tooltip("Color when the tile is occupied by a squad")]
        [SerializeField] private Color _occupiedColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
        
        [Tooltip("Color when the tile is currently selected")]
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.2f, 0.8f, 0.7f);
        
        [Tooltip("Color when the tile is a spawn point")]
        [SerializeField] private Color _spawnPointColor = new Color(0.8f, 0.8f, 0.2f, 0.6f);
        
        [TitleGroup("Tile Properties")]
        [Tooltip("Size of the tile for formation calculations")]
        [SerializeField] private float _tileSize = 4f;
        
        [Tooltip("Whether units can walk on this tile")]
        [SerializeField] private bool _isWalkable = true;
        
        [Tooltip("Whether the tile is currently occupied by a squad")]
        [SerializeField, ReadOnly] private bool _isOccupied = false;
        
        [Tooltip("ID of the squad currently occupying the tile")]
        [SerializeField, ReadOnly] private int _occupyingSquadId = -1;
        
        [TitleGroup("Spawn Settings")]
        [Tooltip("Whether this tile is a spawn point for troops")]
        [SerializeField] private bool _isSpawnPoint = false;
        
        [Tooltip("Type of unit to spawn at this point")]
        [SerializeField, ShowIf("_isSpawnPoint")] private UnitType _spawnUnitType = UnitType.Infantry;
        
        [Tooltip("Whether this spawns enemy units")]
        [SerializeField, ShowIf("_isSpawnPoint")] private bool _isEnemySpawn = false;
        
        // References
        private MeshRenderer _meshRenderer;
        private Collider _collider;
        
        // Properties
        public int TileId => _tileId;
        public bool IsWalkable => _isWalkable;
        public bool IsOccupied => _isOccupied;
        public int OccupyingSquadId => _occupyingSquadId;
        public Vector3 CenterPosition => transform.position + Vector3.up * 0.1f; // Slightly above tile surface
        public float TileSize => _tileSize;
        public bool IsSpawnPoint => _isSpawnPoint;
        public UnitType SpawnUnitType => _spawnUnitType;
        public bool IsEnemySpawn => _isEnemySpawn;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            
            if (_meshRenderer == null)
            {
                Debug.LogError($"Tile {_tileId}: MeshRenderer component is missing!");
            }
            
            if (_collider == null)
            {
                Debug.LogError($"Tile {_tileId}: Collider component is missing!");
            }
            
            ResetTileAppearance();
        }

        /// <summary>
        /// Occupy this tile with a squad
        /// </summary>
        /// <param name="squadId">ID of the squad occupying the tile</param>
        public void OccupyTile(int squadId)
        {
            _isOccupied = true;
            _occupyingSquadId = squadId;
            UpdateTileAppearance();
            
            Debug.Log($"Tile {_tileId} is now occupied by squad {squadId}");
        }

        /// <summary>
        /// Release this tile (no longer occupied)
        /// </summary>
        public void ReleaseTile()
        {
            _isOccupied = false;
            _occupyingSquadId = -1;
            UpdateTileAppearance();
            
            Debug.Log($"Tile {_tileId} is now unoccupied");
        }

        /// <summary>
        /// Highlight this tile (for valid movement destination)
        /// </summary>
        public void HighlightTile()
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = _highlightColor;
            }
        }

        /// <summary>
        /// Select this tile (for currently selected squad)
        /// </summary>
        public void SelectTile()
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = _selectedColor;
            }
        }

        /// <summary>
        /// Reset tile appearance to default
        /// </summary>
        public void ResetTileAppearance()
        {
            UpdateTileAppearance();
        }

        /// <summary>
        /// Update the tile appearance based on its current state
        /// </summary>
        private void UpdateTileAppearance()
        {
            if (_meshRenderer != null)
            {
                if (_isSpawnPoint)
                {
                    _meshRenderer.material.color = _spawnPointColor;
                }
                else if (_isOccupied)
                {
                    _meshRenderer.material.color = _occupiedColor;
                }
                else
                {
                    _meshRenderer.material.color = _defaultColor;
                }
            }
        }

        /// <summary>
        /// Set the walkability of this tile
        /// </summary>
        [Button("Toggle Walkable")]
        public void ToggleWalkable()
        {
            _isWalkable = !_isWalkable;
            Debug.Log($"Tile {_tileId} walkable set to {_isWalkable}");
        }

        /// <summary>
        /// Set the ID of this tile
        /// </summary>
        public void SetTileId(int id)
        {
            _tileId = id;
            gameObject.name = $"Tile_{id}";
        }

        /// <summary>
        /// Toggle spawn point status
        /// </summary>
        [Button("Toggle Spawn Point")]
        public void ToggleSpawnPoint()
        {
            _isSpawnPoint = !_isSpawnPoint;
            UpdateTileAppearance();
            Debug.Log($"Tile {_tileId} spawn point set to {_isSpawnPoint}");
        }

        /// <summary>
        /// Toggle enemy spawn status
        /// </summary>
        [Button("Toggle Enemy Spawn"), ShowIf("_isSpawnPoint")]
        public void ToggleEnemySpawn()
        {
            _isEnemySpawn = !_isEnemySpawn;
            UpdateTileAppearance();
            Debug.Log($"Tile {_tileId} enemy spawn set to {_isEnemySpawn}");
        }

        /// <summary>
        /// Set this tile as a spawn point
        /// </summary>
        public void SetAsSpawnPoint(bool isSpawnPoint, UnitType unitType = UnitType.Infantry, bool isEnemy = false)
        {
            _isSpawnPoint = isSpawnPoint;
            _spawnUnitType = unitType;
            _isEnemySpawn = isEnemy;
            UpdateTileAppearance();
        }

        /// <summary>
        /// Check if the tile is a valid movement destination
        /// </summary>
        public bool IsValidDestination(int currentSquadId)
        {
            // A tile is a valid destination if:
            // 1. It's walkable
            // 2. It's either not occupied OR it's occupied by the current squad
            return _isWalkable && (!_isOccupied || _occupyingSquadId == currentSquadId);
        }

        /// <summary>
        /// Get the optimal squad formation for this tile
        /// </summary>
        public FormationType GetOptimalFormation(int squadSize)
        {
            // Default to Line formation for small tiles or small squads
            if (_tileSize < 3f || squadSize <= 3)
            {
                return FormationType.Line;
            }
            
            // For medium sized tiles or medium squads
            if (_tileSize < 5f || squadSize <= 6)
            {
                return FormationType.Phalanx;
            }
            
            // For large tiles or large squads
            return FormationType.Circle;
        }

        /// <summary>
        /// Calculate the scaled formation size to fit within the tile
        /// </summary>
        public float GetFormationScale(int squadSize)
        {
            // Scale factor based on tile size and squad size
            float baseTileArea = _tileSize * _tileSize;
            float unitArea = 0.7f * 0.7f; // Approximate area needed per unit
            float totalUnitArea = unitArea * squadSize;
            
            // Ensure units fit within 80% of the tile area
            float maxArea = baseTileArea * 0.8f;
            float scaleFactor = Mathf.Min(1.0f, maxArea / totalUnitArea);
            
            return scaleFactor;
        }

        /// <summary>
        /// Draw gizmos for visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw tile boundary
            Gizmos.color = _isWalkable ? Color.green : Color.red;
            Vector3 size = new Vector3(_tileSize, 0.1f, _tileSize);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, size);
            
            // Draw spawn point indicator
            if (_isSpawnPoint)
            {
                Gizmos.color = _isEnemySpawn ? Color.red : Color.yellow;
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            }
            
            // Draw tile ID
            Gizmos.color = Color.black;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, $"Tile {_tileId}");
            #endif
        }
    }
}