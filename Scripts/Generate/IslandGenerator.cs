using UnityEngine;
using System.Collections.Generic;

public class IslandGenerator : MonoBehaviour
{
    [Header("Island Settings")]
    [Tooltip("Kích thước đảo theo X")]
    public int islandWidth = 10;
    
    [Tooltip("Kích thước đảo theo Z")]
    public int islandLength = 10;
    
    [Tooltip("Tỷ lệ vị trí có thể đặt quân")]
    [Range(0.1f, 1.0f)]
    public float validPositionRatio = 0.7f;
    
    [Header("Visualization")]
    [Tooltip("Prefab cho vị trí có thể đặt quân")]
    public GameObject validPositionPrefab;
    
    [Tooltip("Prefab cho vị trí không thể đặt quân")]
    public GameObject invalidPositionPrefab;
    
    [Tooltip("Khoảng cách giữa các vị trí")]
    public float cellSize = 2.0f;
    
    public GameObject islandBase; // Reference to the main island object
    public LayerMask groundLayer; // Layer for ground raycast
    
    
    // Data lưu trữ vị trí trên đảo
    private bool[,] _validPositions;
    private List<Vector3> _validWorldPositions = new List<Vector3>();
    private List<GameObject> _positionMarkers = new List<GameObject>();
    private List<Transform> _squadSpawnPoints = new List<Transform>();
    
    private void Awake()
    {
        _validPositions = new bool[islandWidth, islandLength];
    }
    
    private void Start()
    {
        // Tạo đảo
        GenerateIsland();
        
        // Tạo marker cho các vị trí
        VisualizeIsland();
        
        // Tạo các spawn point cho squad
        CreateSquadSpawnPoints();
    }
    
    // Tạo đảo ngẫu nhiên
    private void GenerateIsland()
    {
        // Khởi tạo tất cả vị trí là invalid
        for (int x = 0; x < islandWidth; x++)
        {
            for (int z = 0; z < islandLength; z++)
            {
                _validPositions[x, z] = false;
            }
        }
        
        // Tạo một số vị trí valid ngẫu nhiên
        int totalCells = islandWidth * islandLength;
        int validCellCount = Mathf.FloorToInt(totalCells * validPositionRatio);
        
        // Tạo đảo ban đầu từ giữa
        int centerX = islandWidth / 2;
        int centerZ = islandLength / 2;
        
        // Đảm bảo vị trí trung tâm luôn valid
        _validPositions[centerX, centerZ] = true;
        validCellCount--;
        
        // Tạo các vị trí valid khác bằng thuật toán mở rộng
        while (validCellCount > 0)
        {
            // Tìm một vị trí valid ngẫu nhiên
            List<Vector2Int> validCells = FindAllValidCells();
            
            if (validCells.Count == 0) break;
            
            // Tìm các ô lân cận của các vị trí valid
            List<Vector2Int> candidates = new List<Vector2Int>();
            foreach (var cell in validCells)
            {
                // Thêm các ô lân cận
                CheckAndAddNeighbor(candidates, cell.x + 1, cell.y);
                CheckAndAddNeighbor(candidates, cell.x - 1, cell.y);
                CheckAndAddNeighbor(candidates, cell.x, cell.y + 1);
                CheckAndAddNeighbor(candidates, cell.x, cell.y - 1);
            }
            
            // Không còn ô nào để mở rộng
            if (candidates.Count == 0) break;
            
            // Chọn một ô ngẫu nhiên từ candidates và đánh dấu nó là valid
            int randomIndex = Random.Range(0, candidates.Count);
            Vector2Int newValidCell = candidates[randomIndex];
            
            _validPositions[newValidCell.x, newValidCell.y] = true;
            validCellCount--;
        }
        
        // Lưu lại các vị trí world
        for (int x = 0; x < islandWidth; x++)
        {
            for (int z = 0; z < islandLength; z++)
            {
                if (_validPositions[x, z])
                {
                    Vector3 worldPos = new Vector3(
                        x * cellSize - (islandWidth * cellSize / 2),
                        0,
                        z * cellSize - (islandLength * cellSize / 2)
                    );
                    
                    _validWorldPositions.Add(worldPos);
                }
            }
        }
    }
    
    // Tìm tất cả các ô valid
    private List<Vector2Int> FindAllValidCells()
    {
        List<Vector2Int> result = new List<Vector2Int>();
        
        for (int x = 0; x < islandWidth; x++)
        {
            for (int z = 0; z < islandLength; z++)
            {
                if (_validPositions[x, z])
                {
                    result.Add(new Vector2Int(x, z));
                }
            }
        }
        
        return result;
    }
    
    // Kiểm tra và thêm vị trí hàng xóm vào danh sách
    private void CheckAndAddNeighbor(List<Vector2Int> candidates, int x, int z)
    {
        if (x >= 0 && x < islandWidth && z >= 0 && z < islandLength && !_validPositions[x, z])
        {
            Vector2Int neighbor = new Vector2Int(x, z);
            if (!candidates.Contains(neighbor))
            {
                candidates.Add(neighbor);
            }
        }
    }
    
    // Tạo visual cho đảo
    private void VisualizeIsland()
    {
        // Xóa markers cũ nếu có
        foreach (var marker in _positionMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        _positionMarkers.Clear();
        
        // Tạo markers mới
        for (int x = 0; x < islandWidth; x++)
        {
            for (int z = 0; z < islandLength; z++)
            {
                Vector3 worldPos = new Vector3(
                    x * cellSize - (islandWidth * cellSize / 2),
                    0,
                    z * cellSize - (islandLength * cellSize / 2)
                );
                
                GameObject prefabToUse = _validPositions[x, z] ? validPositionPrefab : invalidPositionPrefab;
                
                if (prefabToUse != null)
                {
                    GameObject marker = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
                    marker.name = "Marker_" + x + "_" + z;
                    _positionMarkers.Add(marker);
                }
            }
        }
        for (int x = 0; x < islandWidth; x++)
        {
            for (int z = 0; z < islandLength; z++)
            {
                Vector3 worldPos = GetWorldPositionOnIsland(x, z);
            
                GameObject prefabToUse = _validPositions[x, z] ? validPositionPrefab : invalidPositionPrefab;
            
                if (prefabToUse != null)
                {
                    GameObject marker = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
                    marker.name = "Marker_" + x + "_" + z;
                    _positionMarkers.Add(marker);
                }
            }
        }
    }
    private Vector3 GetWorldPositionOnIsland(int x, int z)
    {
        // Tính vị trí trên mặt phẳng XZ
        Vector3 worldPos = new Vector3(
            x * cellSize - (islandWidth * cellSize / 2),
            100, // Start high
            z * cellSize - (islandLength * cellSize / 2)
        );
    
        // Raycast xuống để tìm vị trí y chính xác
        RaycastHit hit;
        if (Physics.Raycast(worldPos, Vector3.down, out hit, 200f, groundLayer))
        {
            // Lấy y từ hit point
            worldPos.y = hit.point.y + 0.05f; // Offset nhỏ để tránh z-fighting
        }
        else
        {
            // Fallback nếu không hit được ground
            worldPos.y = 0;
        }
    
        return worldPos;
    }
    // Tạo các spawn point cho squad
    private void CreateSquadSpawnPoints()
    {
        // Xóa spawn points cũ
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("SpawnPoint_"))
            {
                Destroy(child.gameObject);
            }
        }
        
        _squadSpawnPoints.Clear();
        
        // Chọn 4 vị trí spawn (2 cho người chơi, 2 cho kẻ địch)
        int spawnCount = Mathf.Min(4, _validWorldPositions.Count);
        
        // Nếu không đủ vị trí valid thì return
        if (_validWorldPositions.Count < spawnCount)
        {
            Debug.LogError("IslandGenerator: Không đủ vị trí valid để tạo spawn points!");
            return;
        }
        
        // Tạo danh sách các vị trí có thể sử dụng
        List<Vector3> availablePositions = new List<Vector3>(_validWorldPositions);
        
        // Tạo spawn point cho người chơi (ở 1/4 đầu đảo)
        for (int i = 0; i < 2; i++)
        {
            // Chọn các vị trí ở phía Tây của đảo
            List<Vector3> westPositions = availablePositions.FindAll(pos => pos.x < -islandWidth * cellSize / 4);
            
            if (westPositions.Count > 0)
            {
                // Chọn ngẫu nhiên một vị trí
                int randomIndex = Random.Range(0, westPositions.Count);
                Vector3 spawnPos = westPositions[randomIndex];
                
                // Tạo spawn point
                GameObject spawnPoint = new GameObject("SpawnPoint_Player_" + i);
                spawnPoint.transform.position = spawnPos;
                spawnPoint.transform.SetParent(transform);
                
                // Thêm vào danh sách và xóa khỏi available
                _squadSpawnPoints.Add(spawnPoint.transform);
                availablePositions.Remove(spawnPos);
            }
        }
        
        // Tạo spawn point cho kẻ địch (ở 1/4 cuối đảo)
        for (int i = 0; i < 2; i++)
        {
            // Chọn các vị trí ở phía Đông của đảo
            List<Vector3> eastPositions = availablePositions.FindAll(pos => pos.x > islandWidth * cellSize / 4);
            
            if (eastPositions.Count > 0)
            {
                // Chọn ngẫu nhiên một vị trí
                int randomIndex = Random.Range(0, eastPositions.Count);
                Vector3 spawnPos = eastPositions[randomIndex];
                
                // Tạo spawn point
                GameObject spawnPoint = new GameObject("SpawnPoint_Enemy_" + i);
                spawnPoint.transform.position = spawnPos;
                spawnPoint.transform.SetParent(transform);
                
                // Thêm vào danh sách và xóa khỏi available
                _squadSpawnPoints.Add(spawnPoint.transform);
                availablePositions.Remove(spawnPos);
            }
        }
    }
    
    // Lấy tất cả spawn points
    public Transform[] GetSquadSpawnPoints()
    {
        return _squadSpawnPoints.ToArray();
    }
    
    // Lấy danh sách vị trí valid
    public List<Vector3> GetValidPositions()
    {
        return _validWorldPositions;
    }
    
    // Kiểm tra xem một vị trí world có valid không
    public bool IsValidPosition(Vector3 worldPosition)
    {
        // Chuyển từ world position sang grid position
        int gridX = Mathf.RoundToInt((worldPosition.x + (islandWidth * cellSize / 2)) / cellSize);
        int gridZ = Mathf.RoundToInt((worldPosition.z + (islandLength * cellSize / 2)) / cellSize);
        
        if (gridX < 0 || gridX >= islandWidth || gridZ < 0 || gridZ >= islandLength)
            return false;
        
        return _validPositions[gridX, gridZ];
    }
}