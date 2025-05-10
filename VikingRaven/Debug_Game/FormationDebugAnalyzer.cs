using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Units;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Công cụ phân tích và kiểm tra lỗi trong hệ thống formation
    /// </summary>
    public class FormationDebugAnalyzer : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private Text _outputText;
        [SerializeField] private bool _createUIIfMissing = true;
        [SerializeField] private bool _logToConsole = true;

        [Header("Analysis Settings")]
        [SerializeField] private int _targetSquadId = -1; // -1 = tất cả squad
        [SerializeField] private bool _autoAnalyze = false;
        [SerializeField] private float _autoAnalyzeInterval = 2.0f;
        [SerializeField] private bool _showDetailedOffsets = true;

        private float _lastAnalyzeTime = 0f;
        private Canvas _debugCanvas;
        private Text _debugText;

        private void Start()
        {
            if (_createUIIfMissing && _outputText == null)
            {
                CreateDebugUI();
            }

            if (_autoAnalyze)
            {
                // Chạy phân tích đầu tiên
                AnalyzeFormations();
            }
        }

        private void Update()
        {
            if (_autoAnalyze && Time.time - _lastAnalyzeTime >= _autoAnalyzeInterval)
            {
                AnalyzeFormations();
                _lastAnalyzeTime = Time.time;
            }
        }

        /// <summary>
        /// Phân tích tất cả các squad hoặc một squad cụ thể
        /// </summary>
        [ContextMenu("Analyze Formations")]
        public void AnalyzeFormations()
        {
            if (_targetSquadId != -1)
            {
                AnalyzeSquad(_targetSquadId);
                return;
            }

            StringBuilder output = new StringBuilder();
            output.AppendLine($"=== FORMATION ANALYSIS ({System.DateTime.Now.ToString("HH:mm:ss")}) ===\n");

            // Lấy tất cả các formation component
            var formationComponents = FindObjectsOfType<FormationComponent>();

            // Nhóm theo squad
            Dictionary<int, List<FormationComponent>> squadFormations = new Dictionary<int, List<FormationComponent>>();
            foreach (var formation in formationComponents)
            {
                int squadId = formation.SquadId;
                if (!squadFormations.ContainsKey(squadId))
                {
                    squadFormations[squadId] = new List<FormationComponent>();
                }
                squadFormations[squadId].Add(formation);
            }

            // Phân tích từng squad
            foreach (var entry in squadFormations)
            {
                AnalyzeSquadFormation(entry.Key, entry.Value, output);
            }

            // Hiển thị kết quả
            DisplayOutput(output.ToString());
        }

        /// <summary>
        /// Phân tích một squad cụ thể
        /// </summary>
        public void AnalyzeSquad(int squadId)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine($"=== FORMATION ANALYSIS: SQUAD {squadId} ===\n");

            // Lấy tất cả các formation component thuộc squad
            var formationComponents = FindObjectsOfType<FormationComponent>();
            List<FormationComponent> squadFormations = new List<FormationComponent>();

            foreach (var formation in formationComponents)
            {
                if (formation.SquadId == squadId)
                {
                    squadFormations.Add(formation);
                }
            }

            if (squadFormations.Count == 0)
            {
                output.AppendLine($"Không tìm thấy unit nào trong Squad {squadId}!");
            }
            else
            {
                AnalyzeSquadFormation(squadId, squadFormations, output);
            }

            // Hiển thị kết quả
            DisplayOutput(output.ToString());
        }

        /// <summary>
        /// Phân tích chi tiết đội hình của một squad
        /// </summary>
        private void AnalyzeSquadFormation(int squadId, List<FormationComponent> formations, StringBuilder output)
        {
            output.AppendLine($"Squad {squadId}: {formations.Count} units");

            // Kiểm tra consistency của formation type
            Dictionary<FormationType, int> formationTypeCounts = new Dictionary<FormationType, int>();
            foreach (var formation in formations)
            {
                if (!formationTypeCounts.ContainsKey(formation.CurrentFormationType))
                {
                    formationTypeCounts[formation.CurrentFormationType] = 0;
                }
                formationTypeCounts[formation.CurrentFormationType]++;
            }

            // Hiển thị formation types
            output.AppendLine("  Formation Types:");
            foreach (var typeEntry in formationTypeCounts)
            {
                output.AppendLine($"    - {typeEntry.Key}: {typeEntry.Value} units");
            }

            // Báo lỗi nếu có nhiều loại formation khác nhau
            if (formationTypeCounts.Count > 1)
            {
                output.AppendLine("  [LỖI] Squad có nhiều loại formation khác nhau!");
            }

            // Lấy primary formation type
            FormationType primaryFormationType = FormationType.None;
            int maxCount = 0;
            foreach (var typeEntry in formationTypeCounts)
            {
                if (typeEntry.Value > maxCount)
                {
                    maxCount = typeEntry.Value;
                    primaryFormationType = typeEntry.Key;
                }
            }

            output.AppendLine($"  Primary Formation Type: {primaryFormationType}");

            // Kiểm tra các slot
            Dictionary<int, FormationComponent> slotMap = new Dictionary<int, FormationComponent>();
            List<int> duplicateSlots = new List<int>();
            foreach (var formation in formations)
            {
                int slotIndex = formation.FormationSlotIndex;
                
                if (slotMap.ContainsKey(slotIndex))
                {
                    if (!duplicateSlots.Contains(slotIndex))
                    {
                        duplicateSlots.Add(slotIndex);
                    }
                }
                else
                {
                    slotMap[slotIndex] = formation;
                }
            }

            // Báo lỗi nếu có slot trùng lặp
            if (duplicateSlots.Count > 0)
            {
                output.AppendLine("  [LỖI] Các slot trùng lặp:");
                foreach (int slot in duplicateSlots)
                {
                    output.AppendLine($"    - Slot {slot} xuất hiện nhiều lần");
                }
            }

            // Kiểm tra slot thiếu
            int expectedSlotCount = formations.Count;
            List<int> missingSlots = new List<int>();
            for (int i = 0; i < expectedSlotCount; i++)
            {
                if (!slotMap.ContainsKey(i))
                {
                    missingSlots.Add(i);
                }
            }

            // Báo lỗi nếu có slot thiếu
            if (missingSlots.Count > 0)
            {
                output.AppendLine("  [LỖI] Các slot bị thiếu:");
                foreach (int slot in missingSlots)
                {
                    output.AppendLine($"    - Thiếu slot {slot}");
                }
            }

            // Hiển thị chi tiết offset
            if (_showDetailedOffsets)
            {
                output.AppendLine("\n  Formation Offsets:");
                
                // Sort by slot index for better readability
                List<FormationComponent> sortedFormations = new List<FormationComponent>(formations);
                sortedFormations.Sort((a, b) => a.FormationSlotIndex.CompareTo(b.FormationSlotIndex));
                
                foreach (var formation in sortedFormations)
                {
                    Vector3 offset = formation.FormationOffset;
                    output.AppendLine($"    - Slot {formation.FormationSlotIndex}: ({offset.x:F2}, {offset.y:F2}, {offset.z:F2})");
                }
            }

            // Tính trung tâm và kiểm tra khoảng cách
            if (formations.Count > 0)
            {
                Vector3 squadCenter = CalculateSquadCenter(formations);
                output.AppendLine($"\n  Squad Center: ({squadCenter.x:F2}, {squadCenter.y:F2}, {squadCenter.z:F2})");
                
                // Kiểm tra khoảng cách từ unit đến vị trí slot
                CheckUnitDistances(formations, squadCenter, output);
            }

            output.AppendLine("\n");
        }

        /// <summary>
        /// Tính toán trung tâm của squad
        /// </summary>
        private Vector3 CalculateSquadCenter(List<FormationComponent> formations)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var formation in formations)
            {
                var transformComponent = formation.GetComponent<TransformComponent>();
                if (transformComponent != null)
                {
                    sum += transformComponent.Position;
                    count++;
                }
            }
            
            return count > 0 ? sum / count : Vector3.zero;
        }

        /// <summary>
        /// Kiểm tra khoảng cách từ unit đến vị trí slot theo đội hình
        /// </summary>
        private void CheckUnitDistances(List<FormationComponent> formations, Vector3 squadCenter, StringBuilder output)
        {
            output.AppendLine("  Unit-to-Slot Distances:");
            
            // Tính rotation trung bình của squad
            Quaternion squadRotation = CalculateSquadRotation(formations);
            
            foreach (var formation in formations)
            {
                var transformComponent = formation.GetComponent<TransformComponent>();
                if (transformComponent == null) continue;
                
                // Vị trí hiện tại
                Vector3 currentPosition = transformComponent.Position;
                
                // Tính vị trí slot dự kiến
                Vector3 slotOffset = formation.FormationOffset;
                Vector3 slotPosition = squadCenter + squadRotation * slotOffset;
                
                // Tính khoảng cách
                float distance = Vector3.Distance(currentPosition, slotPosition);
                
                string status = "OK";
                if (distance > 3.0f) status = "[XA]";
                else if (distance > 1.0f) status = "[ĐANG DI CHUYỂN]";
                
                output.AppendLine($"    - Unit {formation.GetInstanceID()}, Slot {formation.FormationSlotIndex}: {distance:F2}m {status}");
            }
        }

        /// <summary>
        /// Tính rotation trung bình của squad
        /// </summary>
        private Quaternion CalculateSquadRotation(List<FormationComponent> formations)
        {
            Vector3 averageForward = Vector3.zero;
            
            foreach (var formation in formations)
            {
                var transformComponent = formation.GetComponent<TransformComponent>();
                if (transformComponent != null)
                {
                    averageForward += transformComponent.Forward;
                }
            }
            
            if (averageForward.magnitude > 0.01f)
            {
                averageForward.Normalize();
                return Quaternion.LookRotation(averageForward);
            }
            
            return Quaternion.identity;
        }

        /// <summary>
        /// Hiển thị kết quả phân tích
        /// </summary>
        private void DisplayOutput(string text)
        {
            // Hiển thị trong UI
            if (_debugText != null)
            {
                _debugText.text = text;
            }
            else if (_outputText != null)
            {
                _outputText.text = text;
            }

            // Log ra console nếu cần
            if (_logToConsole)
            {
                Debug.Log(text);
            }
        }

        /// <summary>
        /// Tạo UI debug nếu chưa có
        /// </summary>
        private void CreateDebugUI()
        {
            // Tạo canvas
            GameObject canvasObj = new GameObject("DebugCanvas");
            _debugCanvas = canvasObj.AddComponent<Canvas>();
            _debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Tạo panel background
            GameObject panelObj = new GameObject("DebugPanel");
            panelObj.transform.SetParent(_debugCanvas.transform);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.7f, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Tạo text
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(panelObj.transform);
            _debugText = textObj.AddComponent<Text>();
            _debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _debugText.fontSize = 14;
            _debugText.color = Color.white;
            _debugText.supportRichText = true;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            _outputText = _debugText;
        }

        /// <summary>
        /// Đặt squad mục tiêu để phân tích
        /// </summary>
        public void SetTargetSquad(int squadId)
        {
            _targetSquadId = squadId;
            
            if (_autoAnalyze)
            {
                AnalyzeSquad(squadId);
            }
        }
    }
}