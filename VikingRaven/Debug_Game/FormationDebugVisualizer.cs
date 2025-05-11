using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Hiển thị vị trí formation của các unit trong squad
    /// </summary>
    public class FormationDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [Tooltip("Bật/tắt hiển thị debug")]
        [SerializeField] private bool _showDebug = true;
        
        [Tooltip("Kích thước của ô hiển thị")]
        [SerializeField] private float _boxSize = 0.5f;
        
        [Tooltip("Hiện chỉ số trong đội hình")]
        [SerializeField] private bool _showIndexes = true;
        
        [Tooltip("Chiều cao chữ")]
        [SerializeField] private float _textHeight = 0.5f;
        
        [Tooltip("Hiện vùng chuyển giai đoạn")]
        [SerializeField] private bool _showPhaseRadius = true;
        
        [Header("Colors")]
        [Tooltip("Màu hiển thị cho các squad theo ID")]
        [SerializeField] private Color[] _squadColors = new Color[]
        {
            Color.blue,
            Color.red,
            Color.green,
            Color.yellow,
            Color.cyan,
            Color.magenta
        };
        
        [Tooltip("Màu cho ô tại vị trí đích")]
        [SerializeField] private Color _targetPositionColor = new Color(1, 1, 1, 0.5f);
        
        [Tooltip("Màu cho dòng kết nối từ unit đến vị trí đích")]
        [SerializeField] private Color _connectionLineColor = new Color(1, 1, 1, 0.3f);
        
        [Tooltip("Màu cho giai đoạn Approaching")]
        [SerializeField] private Color _approachingColor = new Color(1, 0.5f, 0, 0.3f);
        
        [Tooltip("Màu cho giai đoạn Forming")]
        [SerializeField] private Color _formingColor = new Color(0, 1, 0.5f, 0.3f);
        
        // Singleton instance
        private static FormationDebugVisualizer _instance;
        public static FormationDebugVisualizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FormationDebugVisualizer>();
                    
                    if (_instance == null)
                    {
                        GameObject visualizerObject = new GameObject("FormationDebugVisualizer");
                        _instance = visualizerObject.AddComponent<FormationDebugVisualizer>();
                    }
                }
                
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showDebug)
                return;
            
            if (!Application.isPlaying)
                return;
            
            DrawAllFormationPositions();
        }

        /// <summary>
        /// Vẽ tất cả vị trí đội hình cho tất cả squad
        /// </summary>
        private void DrawAllFormationPositions()
        {
            if (!EntityRegistry.HasInstance)
                return;
            
            var entityRegistry = EntityRegistry.Instance;
            var entities = entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Group by squads
            Dictionary<int, List<UnitFormationData>> squadFormations = new Dictionary<int, List<UnitFormationData>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent == null || transformComponent == null) 
                    continue;
                
                int squadId = formationComponent.SquadId;
                
                if (!squadFormations.ContainsKey(squadId))
                {
                    squadFormations[squadId] = new List<UnitFormationData>();
                }
                
                // Extract NavMeshAgent info if available
                float formationPhaseDistance = 5.0f;
                
                var navMeshAgent = navigationComponent.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navMeshAgent != null)
                {
                    // Try to get _formationPhaseDistance using reflection (since it's private)
                    var fieldInfo = navigationComponent.GetType().GetField("_formationPhaseDistance", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (fieldInfo != null)
                    {
                        formationPhaseDistance = (float)fieldInfo.GetValue(navigationComponent);
                    }
                }
                
                // Create formation data
                UnitFormationData data = new UnitFormationData
                {
                    EntityId = entity.Id,
                    FormationSlot = formationComponent.FormationSlotIndex,
                    FormationOffset = formationComponent.FormationOffset,
                    CurrentPosition = transformComponent.Position,
                    TargetPosition = navigationComponent != null ? navigationComponent.Destination : transformComponent.Position,
                    FormationPhaseDistance = formationPhaseDistance
                };
                
                squadFormations[squadId].Add(data);
            }
            
            // Draw each squad
            foreach (var squadEntry in squadFormations)
            {
                int squadId = squadEntry.Key;
                var formationData = squadEntry.Value;
                
                // Select color for this squad
                Color squadColor = GetSquadColor(squadId);
                
                // Calculate squad center
                Vector3 squadCenter = CalculateSquadCenter(formationData);
                
                // Calculate target center
                Vector3 targetCenter = CalculateTargetCenter(formationData);
                
                // Draw squad information
                DrawSquadFormation(squadId, squadCenter, targetCenter, formationData, squadColor);
            }
        }

        /// <summary>
        /// Tính toán trung tâm của squad
        /// </summary>
        private Vector3 CalculateSquadCenter(List<UnitFormationData> formationData)
        {
            if (formationData.Count == 0)
                return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var data in formationData)
            {
                sum += data.CurrentPosition;
            }
            
            return sum / formationData.Count;
        }

        /// <summary>
        /// Tính toán trung tâm của điểm đích
        /// </summary>
        private Vector3 CalculateTargetCenter(List<UnitFormationData> formationData)
        {
            if (formationData.Count == 0)
                return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var data in formationData)
            {
                sum += data.TargetPosition;
            }
            
            return sum / formationData.Count;
        }

        /// <summary>
        /// Vẽ thông tin đội hình cho một squad
        /// </summary>
        private void DrawSquadFormation(int squadId, Vector3 squadCenter, Vector3 targetCenter, List<UnitFormationData> formationData, Color squadColor)
        {
            // Draw squad center
            Gizmos.color = squadColor;
            Gizmos.DrawSphere(squadCenter, _boxSize * 0.5f);
            
            // Draw target center
            Gizmos.color = _targetPositionColor;
            Gizmos.DrawSphere(targetCenter, _boxSize * 0.5f);
            
            // Draw formation phase radius if we have data
            if (_showPhaseRadius && formationData.Count > 0)
            {
                float phaseRadius = formationData[0].FormationPhaseDistance;
                Gizmos.color = _approachingColor;
                Gizmos.DrawWireSphere(targetCenter, phaseRadius);
                
                // Draw inner circle for forming phase
                Gizmos.color = _formingColor;
                Gizmos.DrawWireSphere(targetCenter, phaseRadius * 0.5f);
            }
            
            // Draw each unit's position and target position
            foreach (var data in formationData)
            {
                // Draw current position
                Gizmos.color = squadColor;
                DrawBox(data.CurrentPosition, _boxSize);
                
                // Draw formation position (with offset from center)
                Gizmos.color = _targetPositionColor;
                Vector3 formationPos = targetCenter + data.FormationOffset;
                DrawBox(formationPos, _boxSize * 0.5f);
                
                // Draw connection line
                Gizmos.color = _connectionLineColor;
                Gizmos.DrawLine(data.CurrentPosition, formationPos);
                
                // Draw formation slot index
                if (_showIndexes)
                {
#if UNITY_EDITOR
                    // Draw formation slot index at current position
                    UnityEditor.Handles.color = squadColor;
                    Vector3 textPos = data.CurrentPosition + Vector3.up * _textHeight;
                    UnityEditor.Handles.Label(textPos, data.FormationSlot.ToString());
                    
                    // Draw entity ID at target position
                    UnityEditor.Handles.color = _targetPositionColor;
                    Vector3 idPos = formationPos + Vector3.up * _textHeight;
                    UnityEditor.Handles.Label(idPos, $"ID:{data.EntityId}");
#endif
                }
            }
        }

        /// <summary>
        /// Vẽ một ô vuông tại vị trí chỉ định
        /// </summary>
        private void DrawBox(Vector3 position, float size)
        {
            Vector3 halfSize = new Vector3(size, 0.1f, size) * 0.5f;
            Gizmos.DrawCube(position + Vector3.up * 0.05f, halfSize * 2);
            Gizmos.DrawWireCube(position + Vector3.up * 0.05f, halfSize * 2);
        }

        /// <summary>
        /// Lấy màu cho squad dựa trên ID
        /// </summary>
        private Color GetSquadColor(int squadId)
        {
            if (_squadColors.Length == 0)
                return Color.white;
            
            return _squadColors[squadId % _squadColors.Length];
        }
    }

    /// <summary>
    /// Dữ liệu về vị trí đội hình của một unit
    /// </summary>
    public class UnitFormationData
    {
        public int EntityId;
        public int FormationSlot;
        public Vector3 FormationOffset;
        public Vector3 CurrentPosition;
        public Vector3 TargetPosition;
        public float FormationPhaseDistance;
    }
}