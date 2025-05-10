using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Units.Components;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Phân tích hệ thống formation và tìm các lỗi có thể xảy ra
    /// </summary>
    public class FormationAnalyzer : MonoBehaviour
    {
        [SerializeField] public Text _uiOutput; // Text UI để hiển thị kết quả
        [SerializeField] private bool _autoAnalyze = true;
        [SerializeField] private float _analyzeInterval = 1.0f;
        [SerializeField] private int _selectedSquadId = -1; // -1 phân tích tất cả squad
        [SerializeField] private bool _logToConsole = true;
        
        private float _lastAnalyzeTime;
        
        private void Update()
        {
            if (_autoAnalyze && Time.time - _lastAnalyzeTime > _analyzeInterval)
            {
                AnalyzeFormations();
                _lastAnalyzeTime = Time.time;
            }
        }
        
        [ContextMenu("Phân tích Formations")]
        public void AnalyzeFormations()
        {
            StringBuilder output = new StringBuilder();
            
            // Lấy tất cả các formation component
            var formationComponents = FindObjectsOfType<FormationComponent>();
            
            // Nhóm theo squad
            Dictionary<int, List<FormationComponent>> squadFormations = new Dictionary<int, List<FormationComponent>>();
            
            foreach (var formation in formationComponents)
            {
                int squadId = formation.SquadId;
                
                // Bỏ qua nếu chỉ quan tâm đến squad cụ thể
                if (_selectedSquadId != -1 && squadId != _selectedSquadId) continue;
                
                if (!squadFormations.ContainsKey(squadId))
                {
                    squadFormations[squadId] = new List<FormationComponent>();
                }
                
                squadFormations[squadId].Add(formation);
            }
            
            // Phân tích từng squad
            foreach (var squadEntry in squadFormations)
            {
                int squadId = squadEntry.Key;
                var formations = squadEntry.Value;
                
                output.AppendLine($"Squad {squadId}: {formations.Count} units");
                
                // Kiểm tra loại formation
                HashSet<FormationType> formationTypes = new HashSet<FormationType>();
                foreach (var formation in formations)
                {
                    formationTypes.Add(formation.CurrentFormationType);
                }
                
                if (formationTypes.Count > 1)
                {
                    output.AppendLine($"  CẢNH BÁO: Nhiều loại formation khác nhau trong squad {squadId}:");
                    foreach (var type in formationTypes)
                    {
                        output.AppendLine($"    - {type}");
                    }
                }
                else if (formationTypes.Count == 1)
                {
                    output.AppendLine($"  Formation Type: {formationTypes.First()}");
                }
                
                // Kiểm tra các chỉ số slot
                HashSet<int> slotIndices = new HashSet<int>();
                Dictionary<int, int> slotCounts = new Dictionary<int, int>();
                
                foreach (var formation in formations)
                {
                    int slotIndex = formation.FormationSlotIndex;
                    slotIndices.Add(slotIndex);
                    
                    if (!slotCounts.ContainsKey(slotIndex))
                    {
                        slotCounts[slotIndex] = 0;
                    }
                    
                    slotCounts[slotIndex]++;
                }
                
                output.AppendLine($"  Slot Indices: {string.Join(", ", slotIndices)}");
                
                // Kiểm tra các slotIndex trùng lặp
                bool hasDuplicates = false;
                foreach (var slotEntry in slotCounts)
                {
                    if (slotEntry.Value > 1)
                    {
                        if (!hasDuplicates)
                        {
                            output.AppendLine("  CẢNH BÁO: Các slot index trùng lặp:");
                            hasDuplicates = true;
                        }
                        
                        output.AppendLine($"    - Slot {slotEntry.Key}: {slotEntry.Value} units");
                    }
                }
                
                // Kiểm tra nếu có slotIndex nào bị thiếu
                int expectedSlots = formations.Count;
                bool hasMissingSlots = false;
                
                for (int i = 0; i < expectedSlots; i++)
                {
                    if (!slotIndices.Contains(i))
                    {
                        if (!hasMissingSlots)
                        {
                            output.AppendLine("  CẢNH BÁO: Các slot index bị thiếu:");
                            hasMissingSlots = true;
                        }
                        
                        output.AppendLine($"    - Thiếu slot {i}");
                    }
                }
                
                // Kiểm tra khoảng cách unit đến vị trí slot
                CalculateSlotPositions(squadId, formations, out Dictionary<int, Vector3> slotPositions);
                
                output.AppendLine("  Khoảng cách unit đến slot:");
                
                foreach (var formation in formations)
                {
                    var transformComponent = formation.GetComponent<TransformComponent>();
                    if (transformComponent == null) continue;
                    
                    int slotIndex = formation.FormationSlotIndex;
                    
                    if (slotPositions.TryGetValue(slotIndex, out Vector3 slotPos))
                    {
                        float distance = Vector3.Distance(transformComponent.Position, slotPos);
                        
                        string status = distance < 0.5f ? "Đúng vị trí" : (distance < 2.0f ? "Gần" : "Xa");
                        output.AppendLine($"    - Unit {formation.GetInstanceID()}, Slot {slotIndex}: {distance:F2} đơn vị ({status})");
                    }
                }
                
                output.AppendLine();
            }
            
            // Hiển thị kết quả phân tích
            if (_logToConsole)
            {
                Debug.Log(output.ToString());
            }
            
            if (_uiOutput != null)
            {
                _uiOutput.text = output.ToString();
            }
        }
        
        private void CalculateSlotPositions(
            int squadId, 
            List<FormationComponent> formations, 
            out Dictionary<int, Vector3> slotPositions)
        {
            slotPositions = new Dictionary<int, Vector3>();
            
            // Tính trung tâm và hướng của squad
            Vector3 squadCenter = Vector3.zero;
            Vector3 squadForward = Vector3.zero;
            int count = 0;
            
            foreach (var formation in formations)
            {
                var transformComponent = formation.GetComponent<TransformComponent>();
                if (transformComponent == null) continue;
                
                squadCenter += transformComponent.Position;
                squadForward += transformComponent.Forward;
                count++;
            }
            
            if (count == 0) return;
            
            squadCenter /= count;
            
            Quaternion squadRotation;
            if (squadForward.magnitude > 0.01f)
            {
                squadForward.Normalize();
                squadRotation = Quaternion.LookRotation(squadForward);
            }
            else
            {
                squadRotation = Quaternion.identity;
            }
            
            // Tính vị trí các slot
            foreach (var formation in formations)
            {
                int slotIndex = formation.FormationSlotIndex;
                Vector3 formationOffset = formation.FormationOffset;
                Vector3 rotatedOffset = squadRotation * formationOffset;
                Vector3 slotPosition = squadCenter + rotatedOffset;
                
                slotPositions[slotIndex] = slotPosition;
            }
        }
    }
}