using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Hiển thị trực quan các formation và offset trong game
    /// </summary>
    public class FormationDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private int _selectedSquadId = -1; // -1 = hiển thị tất cả squad
        [SerializeField] private float _updateInterval = 0.25f;
        [SerializeField] private GameObject _slotPrefab;

        [Header("Display Options")]
        [SerializeField] private bool _showSquadCenter = true;
        [SerializeField] private bool _showFormationSlots = true;
        [SerializeField] private bool _showUnitLinks = true;
        [SerializeField] private bool _showFormationInfo = true;

        [Header("Colors")]
        [SerializeField] private Color _squadCenterColor = Color.red;
        [SerializeField] private Color _unitLinkColor = Color.green;

        // Color mapping for different formation types
        private Dictionary<FormationType, Color> _formationColors = new Dictionary<FormationType, Color>
        {
            { FormationType.Line, Color.yellow },
            { FormationType.Column, Color.blue },
            { FormationType.Phalanx, Color.magenta },
            { FormationType.Testudo, Color.cyan },
            { FormationType.Circle, new Color(1.0f, 0.5f, 0.0f) }, // Orange
            { FormationType.Normal, new Color(0.5f, 1.0f, 0.5f) }  // Light Green
        };

        // Runtime data
        private float _lastUpdateTime = 0f;
        private Dictionary<int, SquadDebugInfo> _squadDebugInfo = new Dictionary<int, SquadDebugInfo>();
        private Dictionary<int, Dictionary<int, GameObject>> _slotMarkers = new Dictionary<int, Dictionary<int, GameObject>>();

        private void Start()
        {
            if (_slotPrefab == null)
            {
                CreateDefaultSlotPrefab();
            }
        }

        private void Update()
        {
            if (!_enabled) return;

            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                RefreshDebugData();
                UpdateVisualizers();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_enabled) return;

            foreach (var squadInfo in _squadDebugInfo.Values)
            {
                // Draw squad center
                if (_showSquadCenter)
                {
                    Gizmos.color = _squadCenterColor;
                    Gizmos.DrawSphere(squadInfo.Center, 0.5f);
                    
                    // Draw forward direction
                    Gizmos.DrawRay(squadInfo.Center, squadInfo.Rotation * Vector3.forward * 2f);
                }

                // Draw unit links
                if (_showUnitLinks)
                {
                    Gizmos.color = _unitLinkColor;
                    foreach (var unitInfo in squadInfo.Units)
                    {
                        if (unitInfo.SlotPosition != Vector3.zero)
                        {
                            Gizmos.DrawLine(unitInfo.CurrentPosition, unitInfo.SlotPosition);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tạo lại toàn bộ dữ liệu debug
        /// </summary>
        private void RefreshDebugData()
        {
            _squadDebugInfo.Clear();

            // Lấy tất cả FormationComponent
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            // Nhóm thành các squad
            Dictionary<int, List<DebugUnitInfo>> squadUnits = new Dictionary<int, List<DebugUnitInfo>>();
            Dictionary<int, Vector3> squadCenters = new Dictionary<int, Vector3>();
            Dictionary<int, Vector3> squadForwards = new Dictionary<int, Vector3>();
            Dictionary<int, FormationType> squadFormationTypes = new Dictionary<int, FormationType>();

            foreach (var fc in formationComponents)
            {
                int squadId = fc.SquadId;
                
                // Bỏ qua nếu chỉ xem squad cụ thể
                if (_selectedSquadId != -1 && squadId != _selectedSquadId)
                    continue;

                var tc = fc.GetComponent<TransformComponent>();
                if (tc == null) continue;

                // Initialize lists if needed
                if (!squadUnits.ContainsKey(squadId))
                {
                    squadUnits[squadId] = new List<DebugUnitInfo>();
                    squadCenters[squadId] = Vector3.zero;
                    squadForwards[squadId] = Vector3.zero;
                }

                // Add unit info
                squadUnits[squadId].Add(new DebugUnitInfo
                {
                    Entity = fc.Entity,
                    SlotIndex = fc.FormationSlotIndex,
                    FormationOffset = fc.FormationOffset,
                    CurrentPosition = tc.Position,
                    Forward = tc.Forward,
                    FormationType = fc.CurrentFormationType
                });

                // Accumulate position and forward for center/rotation calculation
                squadCenters[squadId] += tc.Position;
                squadForwards[squadId] += tc.Forward;
                
                // Store formation type (should be same for all units in squad)
                squadFormationTypes[squadId] = fc.CurrentFormationType;
            }

            // Calculate squad centers and rotations
            foreach (var squadId in squadUnits.Keys)
            {
                var units = squadUnits[squadId];
                if (units.Count == 0) continue;

                Vector3 center = squadCenters[squadId] / units.Count;
                Vector3 forward = squadForwards[squadId].normalized;
                Quaternion rotation = forward != Vector3.zero ? 
                    Quaternion.LookRotation(forward) : Quaternion.identity;

                // Calculate slot positions
                foreach (var unit in units)
                {
                    unit.SlotPosition = center + (rotation * unit.FormationOffset);
                }

                // Create squad debug info
                _squadDebugInfo[squadId] = new SquadDebugInfo
                {
                    SquadId = squadId,
                    Center = center,
                    Rotation = rotation,
                    FormationType = squadFormationTypes[squadId],
                    Units = units
                };
            }
        }

        /// <summary>
        /// Cập nhật các hiển thị trực quan (markers, labels)
        /// </summary>
        private void UpdateVisualizers()
        {
            // Clear old markers
            ClearMarkers();

            // Skip if not showing slots
            if (!_showFormationSlots) return;

            // Create markers for each squad and slot
            foreach (var squadInfo in _squadDebugInfo.Values)
            {
                int squadId = squadInfo.SquadId;
                
                // Initialize squad dictionary if needed
                if (!_slotMarkers.ContainsKey(squadId))
                {
                    _slotMarkers[squadId] = new Dictionary<int, GameObject>();
                }

                // Get formation color
                Color formationColor = Color.white;
                if (_formationColors.TryGetValue(squadInfo.FormationType, out Color color))
                {
                    formationColor = color;
                }

                // Create/update markers for each unit
                foreach (var unitInfo in squadInfo.Units)
                {
                    int slotIndex = unitInfo.SlotIndex;
                    
                    // Create marker if it doesn't exist
                    if (!_slotMarkers[squadId].ContainsKey(slotIndex))
                    {
                        GameObject marker = Instantiate(_slotPrefab, unitInfo.SlotPosition, Quaternion.identity);
                        marker.name = $"Slot_{squadId}_{slotIndex}";
                        
                        // Set marker color
                        Renderer renderer = marker.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = formationColor;
                        }
                        
                        // Add label
                        if (_showFormationInfo)
                        {
                            AddInfoLabel(marker, unitInfo, squadInfo.FormationType);
                        }
                        
                        _slotMarkers[squadId][slotIndex] = marker;
                    }
                    else
                    {
                        // Update existing marker
                        GameObject marker = _slotMarkers[squadId][slotIndex];
                        marker.transform.position = unitInfo.SlotPosition;
                        
                        // Update marker color
                        Renderer renderer = marker.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = formationColor;
                        }
                        
                        // Update label
                        if (_showFormationInfo)
                        {
                            Transform labelTransform = marker.transform.Find("InfoLabel");
                            if (labelTransform != null)
                            {
                                TextMesh textMesh = labelTransform.GetComponent<TextMesh>();
                                if (textMesh != null)
                                {
                                    textMesh.text = $"Slot: {slotIndex}\n" +
                                                    $"Type: {squadInfo.FormationType}\n" +
                                                    $"Offset: {unitInfo.FormationOffset.ToString("F1")}";
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Thêm label thông tin cho marker
        /// </summary>
        private void AddInfoLabel(GameObject parent, DebugUnitInfo unitInfo, FormationType formationType)
        {
            GameObject label = new GameObject("InfoLabel");
            label.transform.parent = parent.transform;
            label.transform.localPosition = new Vector3(0, 0.5f, 0);
            
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.fontSize = 10;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.color = Color.white;
            textMesh.text = $"Slot: {unitInfo.SlotIndex}\n" +
                           $"Type: {formationType}\n" +
                           $"Offset: {unitInfo.FormationOffset.ToString("F1")}";
            
            // Add billboard script
            label.AddComponent<BillboardText>();
        }

        /// <summary>
        /// Tạo prefab marker mặc định nếu chưa được set
        /// </summary>
        private void CreateDefaultSlotPrefab()
        {
            _slotPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _slotPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            _slotPrefab.SetActive(false);
            
            // Remove collider
            Destroy(_slotPrefab.GetComponent<Collider>());
            
            // Set default material
            Renderer renderer = _slotPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                renderer.material = mat;
            }
        }

        /// <summary>
        /// Xóa tất cả markers
        /// </summary>
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
            
            if (_slotPrefab != null && _slotPrefab.name.StartsWith("Sphere"))
            {
                Destroy(_slotPrefab);
            }
        }
    }

    /// <summary>
    /// Script để text luôn hướng về camera
    /// </summary>
    public class BillboardText : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    /// <summary>
    /// Thông tin debug của một squad
    /// </summary>
    public class SquadDebugInfo
    {
        public int SquadId;
        public Vector3 Center;
        public Quaternion Rotation;
        public FormationType FormationType;
        public List<DebugUnitInfo> Units = new List<DebugUnitInfo>();
    }

    /// <summary>
    /// Thông tin debug của một unit
    /// </summary>
    public class DebugUnitInfo
    {
        public IEntity Entity;
        public int SlotIndex;
        public Vector3 FormationOffset;
        public Vector3 CurrentPosition;
        public Vector3 SlotPosition;
        public Vector3 Forward;
        public FormationType FormationType;
    }
}