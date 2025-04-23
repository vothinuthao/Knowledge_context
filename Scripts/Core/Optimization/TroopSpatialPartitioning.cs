using System.Collections.Generic;
using UnityEngine;
using Troop;
using System.Linq;

namespace Core.Optimization
{
    /// <summary>
    /// Lớp chịu trách nhiệm tối ưu hóa tính toán cho các troop
    /// Sử dụng cấu trúc spatial partitioning để giảm số lượng kiểm tra
    /// </summary>
    public class TroopSpatialPartitioning : MonoBehaviour
    {
        // Singleton instance
        public static TroopSpatialPartitioning Instance { get; private set; }
        
        [Header("Grid Settings")]
        [Tooltip("Kích thước của mỗi cell trong grid")]
        public float cellSize = 5f;
        
        [Tooltip("Giới hạn của grid theo X (từ -boundX đến +boundX)")]
        public float boundX = 100f;
        
        [Tooltip("Giới hạn của grid theo Z (từ -boundZ đến +boundZ)")]
        public float boundZ = 100f;
        
        [Header("Debug")]
        public bool showDebugGrid = false;
        
        // Grid để lưu trữ các troop theo vị trí
        private Dictionary<Vector2Int, List<TroopController>> _grid = new Dictionary<Vector2Int, List<TroopController>>();
        
        // Map từ troop đến cell hiện tại của nó
        private Dictionary<TroopController, Vector2Int> _troopCells = new Dictionary<TroopController, Vector2Int>();
        
        // Số lượng cells theo mỗi chiều
        private int _cellCountX;
        private int _cellCountZ;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Tính toán số lượng cells
            _cellCountX = Mathf.CeilToInt(boundX * 2 / cellSize);
            _cellCountZ = Mathf.CeilToInt(boundZ * 2 / cellSize);
            
            // Khởi tạo grid
            InitializeGrid();
        }
        
        private void InitializeGrid()
        {
            _grid.Clear();
            
            for (int x = 0; x < _cellCountX; x++)
            {
                for (int z = 0; z < _cellCountZ; z++)
                {
                    Vector2Int cellCoord = new Vector2Int(x, z);
                    _grid[cellCoord] = new List<TroopController>();
                }
            }
        }
        
        /// <summary>
        /// Đăng ký một troop vào hệ thống spatial partitioning
        /// </summary>
        public void RegisterTroop(TroopController troop)
        {
            if (troop == null) return;
            
            // Tính toán cell cho troop
            Vector2Int cell = GetCellFromPosition(troop.GetPosition());
            
            // Thêm troop vào cell
            if (!_grid.ContainsKey(cell))
            {
                _grid[cell] = new List<TroopController>();
            }
            
            _grid[cell].Add(troop);
            _troopCells[troop] = cell;
        }
        
        /// <summary>
        /// Hủy đăng ký một troop khỏi hệ thống
        /// </summary>
        public void UnregisterTroop(TroopController troop)
        {
            if (troop == null) return;
            
            if (_troopCells.TryGetValue(troop, out Vector2Int cell))
            {
                if (_grid.ContainsKey(cell))
                {
                    _grid[cell].Remove(troop);
                }
                
                _troopCells.Remove(troop);
            }
        }
        
        /// <summary>
        /// Cập nhật vị trí của troop trong hệ thống
        /// </summary>
        public void UpdateTroopPosition(TroopController troop)
        {
            if (troop == null) return;
            
            // Lấy cell hiện tại
            if (!_troopCells.TryGetValue(troop, out Vector2Int currentCell))
            {
                // Nếu chưa có trong hệ thống, đăng ký mới
                RegisterTroop(troop);
                return;
            }
            
            // Tính toán cell mới
            Vector2Int newCell = GetCellFromPosition(troop.GetPosition());
            
            // Nếu cell không thay đổi, không cần làm gì
            if (newCell == currentCell) return;
            
            // Di chuyển troop sang cell mới
            if (_grid.ContainsKey(currentCell))
            {
                _grid[currentCell].Remove(troop);
            }
            
            if (!_grid.ContainsKey(newCell))
            {
                _grid[newCell] = new List<TroopController>();
            }
            
            _grid[newCell].Add(troop);
            _troopCells[troop] = newCell;
        }
        
        /// <summary>
        /// Lấy danh sách troop trong phạm vi radius từ position
        /// </summary>
        public List<TroopController> GetTroopsInRadius(Vector3 position, float radius, string[] excludeTags = null)
        {
            List<TroopController> troopsInRadius = new List<TroopController>();
            
            // Tính toán các cell cần kiểm tra
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            Vector2Int centerCell = GetCellFromPosition(position);
            
            // Duyệt qua các cell trong phạm vi
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + z);
                    
                    if (_grid.TryGetValue(cell, out List<TroopController> troopsInCell))
                    {
                        foreach (var troop in troopsInCell)
                        {
                            // Skip nếu troop null hoặc đã chết
                            if (troop == null || !troop.IsAlive()) continue;
                            
                            // Skip nếu troop có tag cần loại trừ
                            if (excludeTags != null && excludeTags.Contains(troop.gameObject.tag)) continue;
                            
                            // Kiểm tra khoảng cách thực tế
                            float distance = Vector3.Distance(position, troop.GetPosition());
                            if (distance <= radius)
                            {
                                troopsInRadius.Add(troop);
                            }
                        }
                    }
                }
            }
            
            return troopsInRadius;
        }
        
        /// <summary>
        /// Lấy danh sách các troop đồng minh trong phạm vi radius
        /// </summary>
        public List<TroopController> GetAlliesInRadius(Vector3 position, float radius, string allyTag)
        {
            // Tìm tất cả troop trong phạm vi, loại trừ các tag khác với allyTag
            List<string> excludeTags = new List<string>();
            
            // Thêm tất cả tag ngoại trừ allyTag vào danh sách loại trừ
            string[] allTags = UnityEngine.Object.FindObjectsOfType<GameObject>()
                .Select(go => go.tag)
                .Distinct()
                .Where(tag => tag != allyTag)
                .ToArray();
            
            return GetTroopsInRadius(position, radius, allTags);
        }
        
        /// <summary>
        /// Lấy danh sách các troop kẻ địch trong phạm vi radius
        /// </summary>
        public List<TroopController> GetEnemiesInRadius(Vector3 position, float radius, string allyTag)
        {
            // Tìm tất cả troop trong phạm vi, loại trừ allyTag
            return GetTroopsInRadius(position, radius, new string[] { allyTag });
        }
        
        /// <summary>
        /// Lấy cell từ vị trí trong không gian 3D
        /// </summary>
        private Vector2Int GetCellFromPosition(Vector3 position)
        {
            // Chuyển đổi từ world space sang grid space
            float normalizedX = (position.x + boundX) / (boundX * 2);
            float normalizedZ = (position.z + boundZ) / (boundZ * 2);
            
            // Chuyển đổi sang cell index
            int cellX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * _cellCountX), 0, _cellCountX - 1);
            int cellZ = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * _cellCountZ), 0, _cellCountZ - 1);
            
            return new Vector2Int(cellX, cellZ);
        }
        
        /// <summary>
        /// Lấy vị trí world space từ cell
        /// </summary>
        private Vector3 GetPositionFromCell(Vector2Int cell)
        {
            // Chuyển đổi từ cell index sang normalized space
            float normalizedX = (cell.x + 0.5f) / _cellCountX;
            float normalizedZ = (cell.y + 0.5f) / _cellCountZ;
            
            // Chuyển đổi sang world space
            float worldX = normalizedX * (boundX * 2) - boundX;
            float worldZ = normalizedZ * (boundZ * 2) - boundZ;
            
            return new Vector3(worldX, 0, worldZ);
        }
        
        /// <summary>
        /// Update hệ thống mỗi frame
        /// </summary>
        private void Update()
        {
            // Cập nhật vị trí của tất cả troop đã đăng ký
            foreach (var troopCell in _troopCells.Keys.ToList())
            {
                UpdateTroopPosition(troopCell);
            }
        }
        
        /// <summary>
        /// Vẽ debug grid
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGrid) return;
            
            Gizmos.color = Color.yellow;
            
            // Tính toán số lượng cells nếu chưa có
            if (_cellCountX == 0 || _cellCountZ == 0)
            {
                _cellCountX = Mathf.CeilToInt(boundX * 2 / cellSize);
                _cellCountZ = Mathf.CeilToInt(boundZ * 2 / cellSize);
            }
            
            // Vẽ grid
            for (int x = 0; x < _cellCountX; x++)
            {
                for (int z = 0; z < _cellCountZ; z++)
                {
                    Vector2Int cell = new Vector2Int(x, z);
                    Vector3 cellCenter = GetPositionFromCell(cell);
                    
                    // Vẽ wireframe cube
                    Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                    
                    // Nếu có troop trong cell, đổi màu
                    if (_grid != null && _grid.TryGetValue(cell, out List<TroopController> troopsInCell) && troopsInCell.Count > 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f));
                        Gizmos.color = Color.yellow;
                    }
                }
            }
        }
    }
}