using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    public class FormationDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _showFormationSlots = true;
        [SerializeField] private bool _showUnitMovements = true;
        [SerializeField] private Color _slotColor = Color.yellow;
        [SerializeField] private Color _unitPathColor = Color.green;
        [SerializeField] private Color _squadCenterColor = Color.red;
        [SerializeField] private float _slotSize = 0.5f;
        [SerializeField] private bool _showSlotIndices = true;
        [SerializeField] private int _selectedSquadId = -1; // -1 means show all squads

        // Thông tin runtime - các cấu trúc dữ liệu để theo dõi vị trí
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        private List<FormationSlotInfo> _formationSlots = new List<FormationSlotInfo>();
        private List<UnitMovementInfo> _unitMovements = new List<UnitMovementInfo>();

        private void Update()
        {
            if (_showFormationSlots || _showUnitMovements)
            {
                RefreshData();
            }
        }

        private void RefreshData()
        {
            // Xóa dữ liệu hiện tại
            _formationSlots.Clear();
            _unitMovements.Clear();
            _squadCenters.Clear();
            _squadRotations.Clear();

            // Tính toán trung tâm và hướng của squad
            CalculateSquadCentersAndRotations();

            // Thu thập thông tin về các slot và movement
            CollectFormationData();
        }

        private void CalculateSquadCentersAndRotations()
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
                if (_selectedSquadId != -1 && squadId != _selectedSquadId) continue;
                
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
                    _squadCenters[squadId] = center;
                    
                    // Tính hướng trung bình
                    Vector3 averageForward = Vector3.zero;
                    foreach (var fwd in forwards)
                    {
                        averageForward += fwd;
                    }
                    
                    if (averageForward.magnitude > 0.01f)
                    {
                        averageForward.Normalize();
                        _squadRotations[squadId] = Quaternion.LookRotation(averageForward);
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }

        private void CollectFormationData()
        {
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            foreach (var formationComponent in formationComponents)
            {
                int squadId = formationComponent.SquadId;
                
                // Bỏ qua nếu chỉ quan tâm đến squad cụ thể
                if (_selectedSquadId != -1 && squadId != _selectedSquadId) continue;
                
                if (_squadCenters.TryGetValue(squadId, out Vector3 center) &&
                    _squadRotations.TryGetValue(squadId, out Quaternion rotation))
                {
                    // Tính vị trí slot dựa trên offset và rotation của squad
                    Vector3 formationOffset = formationComponent.FormationOffset;
                    Vector3 rotatedOffset = rotation * formationOffset;
                    Vector3 slotPosition = center + rotatedOffset;
                    
                    // Thêm vào danh sách slots
                    _formationSlots.Add(new FormationSlotInfo
                    {
                        SquadId = squadId,
                        SlotIndex = formationComponent.FormationSlotIndex,
                        Position = slotPosition,
                        FormationType = formationComponent.CurrentFormationType
                    });
                    
                    // Lấy vị trí hiện tại của unit và thêm vào danh sách unit movements
                    var transformComponent = formationComponent.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        _unitMovements.Add(new UnitMovementInfo
                        {
                            UnitId = formationComponent.GetInstanceID(),
                            CurrentPosition = transformComponent.Position,
                            TargetSlotPosition = slotPosition,
                            SquadId = squadId,
                            SlotIndex = formationComponent.FormationSlotIndex
                        });
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Vẽ trung tâm của các squad
            foreach (var center in _squadCenters)
            {
                Gizmos.color = _squadCenterColor;
                Gizmos.DrawSphere(center.Value, _slotSize * 1.5f);
                
                // Vẽ hướng của squad
                if (_squadRotations.TryGetValue(center.Key, out Quaternion rotation))
                {
                    Gizmos.DrawRay(center.Value, rotation * Vector3.forward * _slotSize * 3);
                }
            }
            
            // Vẽ các formation slots
            if (_showFormationSlots)
            {
                foreach (var slot in _formationSlots)
                {
                    Gizmos.color = _slotColor;
                    Gizmos.DrawWireSphere(slot.Position, _slotSize);
                    
                    // Vẽ index của slot
                    if (_showSlotIndices)
                    {
                        #if UNITY_EDITOR
                        UnityEditor.Handles.Label(slot.Position + Vector3.up * _slotSize, 
                            $"{slot.SlotIndex} (S{slot.SquadId})");
                        #endif
                    }
                }
            }
            
            // Vẽ đường movement của unit
            if (_showUnitMovements)
            {
                foreach (var movement in _unitMovements)
                {
                    Gizmos.color = _unitPathColor;
                    Gizmos.DrawLine(movement.CurrentPosition, movement.TargetSlotPosition);
                    
                    // Vẽ mũi tên chỉ hướng
                    Vector3 direction = (movement.TargetSlotPosition - movement.CurrentPosition).normalized;
                    if (direction.magnitude > 0.01f)
                    {
                        Vector3 midPoint = Vector3.Lerp(movement.CurrentPosition, movement.TargetSlotPosition, 0.7f);
                        float arrowSize = _slotSize;
                        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * arrowSize;
                        
                        Gizmos.DrawRay(midPoint, -direction * arrowSize + right * 0.5f);
                        Gizmos.DrawRay(midPoint, -direction * arrowSize - right * 0.5f);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class FormationSlotInfo
    {
        public int SquadId;
        public int SlotIndex;
        public Vector3 Position;
        public FormationType FormationType;
    }

    [System.Serializable]
    public class UnitMovementInfo
    {
        public int UnitId;
        public Vector3 CurrentPosition;
        public Vector3 TargetSlotPosition;
        public int SquadId;
        public int SlotIndex;
    }
}