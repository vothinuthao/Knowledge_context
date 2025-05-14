using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Represents a single tile in the game world that can contain squad units
    /// </summary>
    public class TileComponent : MonoBehaviour
    {
        [Header("Tile Settings")]
        [SerializeField] private int _tileId;
        [SerializeField] private Color _defaultColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        [SerializeField] private Color _highlightColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
        [SerializeField] private Color _occupiedColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.2f, 0.8f, 0.7f);
        
        [Header("Tile Properties")]
        [SerializeField] private float _tileSize = 4f; // Size of the tile
        [SerializeField] private bool _isWalkable = true; // Whether units can walk on this tile
        [SerializeField] private bool _isOccupied = false; // Whether the tile is currently occupied
        [SerializeField] private int _occupyingSquadId = -1; // ID of the squad currently occupying the tile

        [Header("Neighbor Tiles")]
        [SerializeField] private List<TileComponent> _neighbors = new List<TileComponent>();

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
        public List<TileComponent> Neighbors => _neighbors;

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
                if (_isOccupied)
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
        public void SetWalkable(bool walkable)
        {
            _isWalkable = walkable;
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
        /// Check if the tile is a valid movement destination for a specific squad
        /// </summary>
        public bool IsValidDestination(int currentSquadId)
        {
            // A tile is a valid destination if:
            // 1. It's walkable
            // 2. It's either not occupied OR it's occupied by the current squad
            return _isWalkable && (!_isOccupied || _occupyingSquadId == currentSquadId);
        }

        /// <summary>
        /// Add a neighbor tile
        /// </summary>
        public void AddNeighbor(TileComponent neighbor)
        {
            if (!_neighbors.Contains(neighbor))
            {
                _neighbors.Add(neighbor);
            }
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
        /// Get all valid neighbor tiles for movement
        /// </summary>
        public List<TileComponent> GetValidNeighbors(int currentSquadId)
        {
            List<TileComponent> validNeighbors = new List<TileComponent>();
            
            foreach (var neighbor in _neighbors)
            {
                if (neighbor.IsValidDestination(currentSquadId))
                {
                    validNeighbors.Add(neighbor);
                }
            }
            
            return validNeighbors;
        }

        /// <summary>
        /// Draw gizmos for visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw connections to neighbors
            Gizmos.color = Color.yellow;
            foreach (var neighbor in _neighbors)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(
                        transform.position + Vector3.up * 0.1f,
                        neighbor.transform.position + Vector3.up * 0.1f
                    );
                }
            }
            
            // Draw tile boundary
            Gizmos.color = _isWalkable ? Color.green : Color.red;
            Vector3 size = new Vector3(_tileSize, 0.1f, _tileSize);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, size);
            
            // Draw tile ID
            Gizmos.color = Color.black;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, $"Tile {_tileId}");
            #endif
        }
    }
}