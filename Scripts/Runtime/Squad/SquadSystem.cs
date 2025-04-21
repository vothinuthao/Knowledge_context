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
        Vector3 start = position + Vector3.up * 10;
    
        if (Physics.Raycast(start, Vector3.down, out hit, 20f, groundLayer))
        {
            return hit.point;
        }
    
        return position;
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
    
    public bool AddTroop(TroopController troop)
    {
        if (troop == null || _troops.Contains(troop))
            return false;
        Vector2Int position = FindEmptyPosition();
        if (position.x == -1 || position.y == -1)
            return false;
        _occupiedPositions[position.x, position.y] = true;
        TroopControllerSquadExtensions.Instance.SetSquadPosition(troop, this, position);
        
        _troops.Add(troop);
        
        Vector3 targetPosition = _worldPositions[position.x, position.y];
        troop.SetTargetPosition(targetPosition);
        
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