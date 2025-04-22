using UnityEngine;
using System.Collections.Generic;
using Troop;

public class SquadSystem : MonoBehaviour
{
    [Tooltip("Prefab để hiển thị vị trí trong squad")]
    public GameObject positionMarkerPrefab;
    
    [Tooltip("Khoảng cách giữa các vị trí trong squad")]
    public float spacing = 1.5f;
    
    [Tooltip("Số lượng hàng trong đội hình")]
    public int rows = 3;
    
    [Tooltip("Số lượng cột trong đội hình")]
    public int columns = 3;
    
    public LayerMask groundLayer;
    
    // Lưu trữ vị trí của squad trên map
    private Vector3 _squadPosition;
    private Quaternion _squadRotation;
    
    // Danh sách troop trong squad
    private List<TroopController> _troops = new List<TroopController>();
    
    // Array 2D lưu trữ việc các vị trí đã được chiếm chưa
    private bool[,] _occupiedPositions;
    
    // Danh sách các marker hiển thị vị trí
    private List<GameObject> _positionMarkers = new List<GameObject>();
    
    // Cache các vị trí trong world space
    private Vector3[,] _worldPositions;
    
    private void Awake()
    {
        _occupiedPositions = new bool[rows, columns];
        _worldPositions = new Vector3[rows, columns];
        
        _squadPosition = transform.position;
        _squadRotation = transform.rotation;
        
        CreatePositionMarkers();
        
        UpdateWorldPositions();
    }
    
    private void CreatePositionMarkers()
    {
        if (positionMarkerPrefab == null) return;
        
        foreach (var marker in _positionMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        _positionMarkers.Clear();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPos = CalculateLocalPosition(row, col);
                GameObject marker = Instantiate(positionMarkerPrefab, transform);
                marker.transform.localPosition = localPos;
                _positionMarkers.Add(marker);
            }
        }
    }
    private Vector3 GetPositionOnGround(Vector3 position)
    {
        RaycastHit hit;
        // Bắt đầu raycast từ cao hơn để đảm bảo hit được ground
        Vector3 start = position + Vector3.up * 10;
    
        Debug.DrawRay(start, Vector3.down * 20f, Color.red, 1f); // Visual debug
    
        if (Physics.Raycast(start, Vector3.down, out hit, 20f, groundLayer))
        {
            // Thêm offset nhỏ để troop không bị chìm
            return hit.point + Vector3.up * 0.1f;
        }
    
        // Nếu không hit được, return với y=0
        return new Vector3(position.x, 0.1f, position.z);
    }
    private Vector3 CalculateLocalPosition(int row, int col)
    {
        float xOffset = (col - (columns - 1) / 2.0f) * spacing;
        float zOffset = (row - (rows - 1) / 2.0f) * spacing;
        
        return new Vector3(xOffset, 0, zOffset);
    }
    
    private void UpdateWorldPositions()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPos = CalculateLocalPosition(row, col);
                Vector3 worldPos = transform.TransformPoint(localPos);
            
                worldPos = GetPositionOnGround(worldPos);
            
                _worldPositions[row, col] = worldPos;
            }
        }
    }
    
    private void Update()
    {
        if (transform.position != _squadPosition || transform.rotation != _squadRotation)
        {
            _squadPosition = transform.position;
            _squadRotation = transform.rotation;
            UpdateWorldPositions();
            
            UpdateTroopsPositions();
        }
    }
    
    // public bool AddTroop(TroopController troop)
    // {
    //     if (troop == null || _troops.Contains(troop))
    //         return false;
    //     Vector2Int position = FindEmptyPosition();
    //     if (position.x == -1 || position.y == -1)
    //         return false;
    //     _occupiedPositions[position.x, position.y] = true;
    //     TroopControllerSquadExtensions.Instance.SetSquadPosition(troop, this, position);
    //     
    //     _troops.Add(troop);
    //     
    //     Vector3 targetPosition = _worldPositions[position.x, position.y];
    //     troop.SetTargetPosition(targetPosition);
    //     
    //     return true;
    // }
    // Trong SquadSystem.cs, cập nhật AddTroop để đặt troop vào đúng vị trí ngay lập tức:
    public bool AddTroop(TroopController troop)
    {
        if (troop == null || _troops.Contains(troop))
            return false;
    
        // Tìm vị trí trống
        Vector2Int position = FindEmptyPosition();
        if (position.x == -1 || position.y == -1)
            return false;
    
        // Đánh dấu vị trí đã được chiếm
        _occupiedPositions[position.x, position.y] = true;
    
        // Lưu thông tin vị trí của troop trong squad
        TroopControllerSquadExtensions.Instance.SetSquadPosition(troop, this, position);
    
        // Thêm troop vào danh sách
        _troops.Add(troop);
    
        // Lấy vị trí world tương ứng
        Vector3 worldPosition = _worldPositions[position.x, position.y];
    
        // Set target position cho troop để nó di chuyển đến đó
        troop.SetTargetPosition(worldPosition);
    
        // *** THÊM DÒNG NÀY: Cập nhật vị trí troop ngay lập tức ***
        troop.GetModel().Position = worldPosition;
    
        Debug.Log($"Đã thêm troop {troop.name} vào squad tại vị trí {position}, world pos: {worldPosition}");
    
        return true;
    }
    
    public bool RemoveTroop(TroopController troop)
    {
        if (troop == null || !_troops.Contains(troop))
            return false;
        
        Vector2Int position = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
        
        if (position.x >= 0 && position.x < rows && position.y >= 0 && position.y < columns)
        {
            _occupiedPositions[position.x, position.y] = false;
        }
        _troops.Remove(troop);
        
        return true;
    }
    
    private Vector2Int FindEmptyPosition()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (!_occupiedPositions[row, col])
                {
                    return new Vector2Int(row, col);
                }
            }
        }
        
        return new Vector2Int(-1, -1);
    }
    private void UpdateTroopsPositions()
    {
        foreach (var troop in _troops)
        {
            if (troop == null) continue;
            
            Vector2Int position = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
            
            if (position.x >= 0 && position.x < rows && position.y >= 0 && position.y < columns)
            {
                Vector3 targetPosition = _worldPositions[position.x, position.y];
                troop.SetTargetPosition(targetPosition);
            }
        }
    }
    
    public bool SetTroopPosition(TroopController troop, int row, int col)
    {
        if (troop == null || !_troops.Contains(troop))
            return false;
        
        if (row < 0 || row >= rows || col < 0 || col >= columns)
            return false;
        
        if (_occupiedPositions[row, col])
            return false;
        
        Vector2Int oldPosition = TroopControllerSquadExtensions.Instance.GetSquadPosition(troop);
        
        if (oldPosition.x >= 0 && oldPosition.x < rows && oldPosition.y >= 0 && oldPosition.y < columns)
        {
            _occupiedPositions[oldPosition.x, oldPosition.y] = false;
        }
        
        _occupiedPositions[row, col] = true;
        TroopControllerSquadExtensions.Instance.SetSquadPosition(troop, this, new Vector2Int(row, col));
        
        Vector3 targetPosition = _worldPositions[row, col];
        troop.SetTargetPosition(targetPosition);
        
        return true;
    }
    
    public void MoveToPosition(Vector3 position)
    {
        transform.position = position;
        UpdateWorldPositions();
        UpdateTroopsPositions();
    }
    
    public void RotateToDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            UpdateWorldPositions();
            UpdateTroopsPositions();
        }
    }
    
    // Lấy số lượng troop trong squad
    public int GetTroopCount()
    {
        return _troops.Count;
    }
    
    // Kiểm tra xem squad đã đầy chưa
    public bool IsFull()
    {
        return _troops.Count >= rows * columns;
    }
    
    // Lấy danh sách troop
    public List<TroopController> GetTroops()
    {
        return new List<TroopController>(_troops);
    }
    
    public Vector3 GetPositionForTroop(SquadSystem squad, int row, int col)
    {
        if (row < 0 || col < 0)
            return squad.transform.position;
            
        float xOffset = (col - (squad.columns - 1) / 2.0f) * squad.spacing;
        float zOffset = (row - (squad.rows - 1) / 2.0f) * squad.spacing;
            
        Vector3 localPosition = new Vector3(xOffset, 0, zOffset);
        
        Vector3 worldPosition = squad.transform.TransformPoint(localPosition);
        
        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 10, Vector3.down, out hit, 20f, squad.groundLayer))
        {
            worldPosition.y = hit.point.y;
        }
            
        return worldPosition;
    }
}