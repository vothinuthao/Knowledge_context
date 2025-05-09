using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    public class UnitMovementVisualizer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _linePrefab;
        [SerializeField] private int _squadId = -1; // -1 hiển thị tất cả các squad
        [SerializeField] private Color _lineColor = Color.green;
        [SerializeField] private float _lineWidth = 0.1f;
        [SerializeField] private bool _autoUpdate = true;
        [SerializeField] private float _updateInterval = 0.1f;
        
        private Dictionary<int, LineRenderer> _unitLines = new Dictionary<int, LineRenderer>();
        private float _lastUpdateTime;
        
        private void Start()
        {
            if (_linePrefab == null)
            {
                Debug.LogWarning("Chưa gán line prefab. Tạo mặc định.");
                CreateDefaultLinePrefab();
            }
            
            UpdateVisualization();
        }
        
        private void Update()
        {
            if (_autoUpdate && Time.time - _lastUpdateTime > _updateInterval)
            {
                UpdateVisualization();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void CreateDefaultLinePrefab()
        {
            GameObject lineObj = new GameObject("DefaultLine");
            _linePrefab = lineObj.AddComponent<LineRenderer>();
            _linePrefab.startWidth = _lineWidth;
            _linePrefab.endWidth = _lineWidth;
            _linePrefab.material = new Material(Shader.Find("Sprites/Default"));
            _linePrefab.startColor = _lineColor;
            _linePrefab.endColor = _lineColor;
            _linePrefab.positionCount = 2;
            lineObj.SetActive(false);
        }
        
        public void UpdateVisualization()
        {
            // Tính toán trung tâm và hướng của squad
            Dictionary<int, Vector3> squadCenters = new Dictionary<int, Vector3>();
            Dictionary<int, Quaternion> squadRotations = new Dictionary<int, Quaternion>();
            CalculateSquadCentersAndRotations(squadCenters, squadRotations);
            
            // Xóa các line không còn cần thiết
            HashSet<int> activeUnitIds = new HashSet<int>();
            
            // Cập nhật các đường di chuyển của unit
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            foreach (var formationComponent in formationComponents)
            {
                int squadId = formationComponent.SquadId;
                
                // Bỏ qua nếu chỉ quan tâm đến squad cụ thể
                if (_squadId != -1 && squadId != _squadId) continue;
                
                var transformComponent = formationComponent.GetComponent<TransformComponent>();
                if (transformComponent == null) continue;
                
                int unitId = formationComponent.GetInstanceID();
                activeUnitIds.Add(unitId);
                
                if (squadCenters.TryGetValue(squadId, out Vector3 center) &&
                    squadRotations.TryGetValue(squadId, out Quaternion rotation))
                {
                    // Tính vị trí slot dựa trên offset và rotation của squad
                    Vector3 formationOffset = formationComponent.FormationOffset;
                    Vector3 rotatedOffset = rotation * formationOffset;
                    Vector3 slotPosition = center + rotatedOffset;
                    
                    // Tạo hoặc cập nhật đường di chuyển
                    UpdateUnitMovementLine(unitId, transformComponent.Position, slotPosition);
                }
            }
            
            // Xóa các line cho unit không còn tồn tại
            List<int> unitsToRemove = new List<int>();
            foreach (var unitId in _unitLines.Keys)
            {
                if (!activeUnitIds.Contains(unitId))
                {
                    unitsToRemove.Add(unitId);
                }
            }
            
            foreach (var unitId in unitsToRemove)
            {
                if (_unitLines[unitId] != null)
                {
                    Destroy(_unitLines[unitId].gameObject);
                }
                _unitLines.Remove(unitId);
            }
        }
        
        private void CalculateSquadCentersAndRotations(
            Dictionary<int, Vector3> squadCenters, 
            Dictionary<int, Quaternion> squadRotations)
        {
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            // Nhóm các entity theo squad
            Dictionary<int, List<Vector3>> squadPositions = new Dictionary<int, List<Vector3>>();
            Dictionary<int, List<Vector3>> squadForwards = new Dictionary<int, List<Vector3>>();
            
            foreach (var formationComponent in formationComponents)
            {
                var transformComponent = formationComponent.GetComponent<TransformComponent>();
                if (transformComponent == null) continue;
                
                int squadId = formationComponent.SquadId;
                
                // Bỏ qua nếu chỉ quan tâm đến squad cụ thể
                if (_squadId != -1 && squadId != _squadId) continue;
                
                // Thêm vị trí vào danh sách vị trí của squad
                if (!squadPositions.ContainsKey(squadId))
                {
                    squadPositions[squadId] = new List<Vector3>();
                    squadForwards[squadId] = new List<Vector3>();
                }
                
                squadPositions[squadId].Add(transformComponent.Position);
                squadForwards[squadId].Add(transformComponent.Forward);
            }
            
            // Tính trung tâm và hướng trung bình cho mỗi squad
            foreach (var squadId in squadPositions.Keys)
            {
                var positions = squadPositions[squadId];
                var forwards = squadForwards[squadId];
                
                if (positions.Count > 0)
                {
                    // Tính vị trí trung bình
                    Vector3 sum = Vector3.zero;
                    foreach (var pos in positions)
                    {
                        sum += pos;
                    }
                    
                    Vector3 center = sum / positions.Count;
                    squadCenters[squadId] = center;
                    
                    // Tính hướng trung bình
                    Vector3 averageForward = Vector3.zero;
                    foreach (var fwd in forwards)
                    {
                        averageForward += fwd;
                    }
                    
                    if (averageForward.magnitude > 0.01f)
                    {
                        averageForward.Normalize();
                        squadRotations[squadId] = Quaternion.LookRotation(averageForward);
                    }
                    else
                    {
                        squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }
        
        private void UpdateUnitMovementLine(int unitId, Vector3 startPos, Vector3 endPos)
        {
            LineRenderer line;
            
            // Tạo line nếu chưa tồn tại
            if (!_unitLines.TryGetValue(unitId, out line) || line == null)
            {
                GameObject lineObj = Instantiate(_linePrefab.gameObject);
                lineObj.SetActive(true);
                lineObj.name = $"UnitMovementLine_{unitId}";
                
                line = lineObj.GetComponent<LineRenderer>();
                line.startColor = _lineColor;
                line.endColor = _lineColor;
                line.startWidth = _lineWidth;
                line.endWidth = _lineWidth;
                
                _unitLines[unitId] = line;
            }
            
            // Cập nhật vị trí của line
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
            
            // Thêm mũi tên chỉ hướng
            Vector3 direction = (endPos - startPos).normalized;
            if (direction.magnitude > 0.01f)
            {
                // Cần thêm điểm cho mũi tên
                line.positionCount = 4;
                
                Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.7f);
                float arrowSize = _lineWidth * 5;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * arrowSize;
                
                line.SetPosition(0, startPos);
                line.SetPosition(1, midPoint);
                line.SetPosition(2, midPoint - direction * arrowSize + right * 0.5f);
                line.SetPosition(3, midPoint - direction * arrowSize - right * 0.5f);
            }
            else
            {
                line.positionCount = 2;
            }
        }
        
        private void OnDestroy()
        {
            foreach (var line in _unitLines.Values)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            _unitLines.Clear();
            
            // Hủy prefab nếu chúng ta đã tạo nó
            if (_linePrefab != null && _linePrefab.gameObject.name == "DefaultLine")
            {
                Destroy(_linePrefab.gameObject);
            }
        }
    }
}