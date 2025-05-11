using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    public class FormationSlotVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject _slotMarkerPrefab;
        [SerializeField] private int _squadId = -1; // -1 hiển thị tất cả các squad
        [SerializeField] private bool _autoUpdate = true;
        [SerializeField] private float _updateInterval = 0.5f;
        [SerializeField] private Color _slotColor = Color.yellow;
        
        private Dictionary<int, Dictionary<int, GameObject>> _slotMarkers = new Dictionary<int, Dictionary<int, GameObject>>();
        private float _lastUpdateTime;
        
        private void Start()
        {
            if (_slotMarkerPrefab == null)
            {
                Debug.LogWarning("Chưa gán slot marker prefab. Sử dụng hình cầu mặc định.");
                CreateDefaultSlotMarkerPrefab();
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
        
        private void CreateDefaultSlotMarkerPrefab()
        {
            _slotMarkerPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _slotMarkerPrefab.transform.localScale = Vector3.one * 0.3f;
            _slotMarkerPrefab.SetActive(false);
            
            // Loại bỏ collider để tránh tương tác vật lý
            Destroy(_slotMarkerPrefab.GetComponent<Collider>());
            
            // Thiết lập material màu vàng
            Renderer renderer = _slotMarkerPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = _slotColor;
                renderer.material = mat;
            }
        }
        
        public void UpdateVisualization()
        {
            // Xóa các marker hiện có
            ClearMarkers();
            
            // Tính toán trung tâm và hướng của squad
            Dictionary<int, Vector3> squadCenters = new Dictionary<int, Vector3>();
            Dictionary<int, Quaternion> squadRotations = new Dictionary<int, Quaternion>();
            CalculateSquadCentersAndRotations(squadCenters, squadRotations);
            
            // Tạo marker cho các formation slot
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            foreach (var formationComponent in formationComponents)
            {
                int squadId = formationComponent.SquadId;
                
                // Bỏ qua nếu chỉ quan tâm đến squad cụ thể
                if (_squadId != -1 && squadId != _squadId) continue;
                
                if (squadCenters.TryGetValue(squadId, out Vector3 center) &&
                    squadRotations.TryGetValue(squadId, out Quaternion rotation))
                {
                    // Tính vị trí slot dựa trên offset và rotation của squad
                    Vector3 formationOffset = formationComponent.FormationOffset;
                    Vector3 rotatedOffset = rotation * formationOffset;
                    Vector3 slotPosition = center + rotatedOffset;
                    
                    // Tạo hoặc cập nhật marker
                    CreateSlotMarker(squadId, formationComponent.FormationSlotIndex, slotPosition);
                }
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
        
        private void CreateSlotMarker(int squadId, int slotIndex, Vector3 position)
        {
            // Đảm bảo dictionary của squad tồn tại
            if (!_slotMarkers.ContainsKey(squadId))
            {
                _slotMarkers[squadId] = new Dictionary<int, GameObject>();
            }
            
            // Tạo marker nếu chưa tồn tại
            if (!_slotMarkers[squadId].ContainsKey(slotIndex))
            {
                GameObject marker = Instantiate(_slotMarkerPrefab, position, Quaternion.identity);
                marker.name = $"Slot_S{squadId}_I{slotIndex}";
                marker.SetActive(true);
                
                // Thêm nhãn văn bản
                CreateTextLabel(marker, slotIndex);
                
                _slotMarkers[squadId][slotIndex] = marker;
            }
            else
            {
                // Cập nhật marker đã tồn tại
                _slotMarkers[squadId][slotIndex].transform.position = position;
            }
        }
        
        private void CreateTextLabel(GameObject parent, int slotIndex)
        {
            // Tạo TextMesh cho chỉ số slot
            GameObject textObj = new GameObject("SlotIndexLabel");
            textObj.transform.SetParent(parent.transform);
            textObj.transform.localPosition = Vector3.up * 0.5f;
            textObj.transform.localRotation = Quaternion.identity;
            
            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = slotIndex.ToString();
            textMesh.fontSize = 12;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.color = Color.white;
            
        }
        
        private void ClearMarkers()
        {
            foreach (var squadMarkers in _slotMarkers.Values)
            {
                foreach (var marker in squadMarkers.Values)
                {
                    if (marker != null)
                    {
                        Destroy(marker);
                    }
                }
                squadMarkers.Clear();
            }
            _slotMarkers.Clear();
        }
        
        private void OnDestroy()
        {
            ClearMarkers();
            
            if (_slotMarkerPrefab != null && _slotMarkerPrefab.name.StartsWith("Sphere"))
            {
                Destroy(_slotMarkerPrefab);
            }
        }
    }
}