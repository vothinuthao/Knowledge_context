using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class SquadCoordinationSystem : BaseSystem
    {
        // Set higher priority than FormationSystem
        [SerializeField] private int _systemPriority = 200;
        
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private FormationSystem _formationSystem;
        
        // Track squad movement targets
        private Dictionary<int, Vector3> _squadTargetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadTargetRotations = new Dictionary<int, Quaternion>();
        
        [SerializeField] private bool _debugLog = true;
        [SerializeField] private bool _useTwoPhaseMovement = true;
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Set priority for this system to ensure correct execution order
            Priority = _systemPriority;
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogError("SquadCoordinationSystem: FormationSystem not found!");
            }
            
            Debug.Log($"SquadCoordinationSystem initialized with priority {Priority}");
        }
        
        public override void Execute()
        {
            // Get all entities with formation components
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Update formation types for entities in each squad
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // If a new formation type has been set for this squad, update the entity
                    if (_squadFormationTypes.TryGetValue(squadId, out FormationType formationType) &&
                        formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                        
                        // Also notify the FormationSystem about the change
                        if (_formationSystem != null)
                        {
                            _formationSystem.ChangeFormation(squadId, formationType);
                        }
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Updated formation type for squad {squadId} to {formationType}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Đặt loại formation cho một squad
        /// </summary>
        public void SetSquadFormation(int squadId, FormationType formationType)
        {
            _squadFormationTypes[squadId] = formationType;
            
            // Ngay lập tức thông báo cho FormationSystem
            if (_formationSystem != null)
            {
                _formationSystem.ChangeFormation(squadId, formationType);
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Set formation type for squad {squadId} to {formationType}");
            }
        }
        
        /// <summary>
        /// Di chuyển một squad đến vị trí mới
        /// </summary>
        public void MoveSquadToPosition(int squadId, Vector3 targetPosition)
        {
            // Store target position for this squad
            _squadTargetPositions[squadId] = targetPosition;
            
            // By default, maintain current rotation or face forward if we don't have a stored rotation
            Quaternion targetRotation = Quaternion.identity;
            if (_squadTargetRotations.TryGetValue(squadId, out Quaternion currentRotation))
            {
                targetRotation = currentRotation;
            }
            _squadTargetRotations[squadId] = targetRotation;
            
            // Get the formation type for this squad
            FormationType formationType = FormationType.Line;
            if (_squadFormationTypes.TryGetValue(squadId, out var storedFormationType))
            {
                formationType = storedFormationType;
            }
            else if (_formationSystem != null)
            {
                formationType = _formationSystem.GetCurrentFormationType(squadId);
            }
            
            // Notify FormationSystem about manual movement
            if (_formationSystem != null)
            {
                _formationSystem.SetSquadManualMovement(squadId, true);
            }
            
            if (_useTwoPhaseMovement)
            {
                MoveWithTwoPhases(squadId, targetPosition, targetRotation, formationType);
            }
            else
            {
                // Traditional movement - all units move to the same point
                MoveWithoutFormation(squadId, targetPosition);
            }
        }
        
        /// <summary>
        /// Di chuyển squad với hai giai đoạn: tiếp cận và xếp đội hình
        /// </summary>
        private void MoveWithTwoPhases(int squadId, Vector3 targetPosition, Quaternion targetRotation, FormationType formationType)
        {
            // Find all entities in this squad
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            var squadMembers = new List<IEntity>();
            
            // Group squad members
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    squadMembers.Add(entity);
                }
            }
            
            if (squadMembers.Count == 0)
            {
                if (_debugLog)
                {
                    Debug.LogWarning($"SquadCoordinationSystem: Failed to move squad {squadId}, no entities found");
                }
                return;
            }
            
            // Get or generate formation template
            Vector3[] formationOffsets = GenerateFormationTemplate(formationType, squadMembers.Count);
            
            // Move each unit with the two-phase approach
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent != null && navigationComponent != null)
                {
                    // Make sure formation slot is updated
                    formationComponent.SetFormationSlot(i);
                    
                    // Update formation offset
                    Vector3 offset = formationOffsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Set destination to the central target
                    navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.High);
                    
                    // Also provide formation info for the second phase
                    navigationComponent.SetFormationInfo(targetPosition, offset, NavigationCommandPriority.High);
                    
                    if (_debugLog)
                    {
                        Debug.Log($"SquadCoordinationSystem: Moving entity {entity.Id} to {targetPosition} with formation offset {offset}");
                    }
                }
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Successfully moved squad {squadId} to {targetPosition} with two-phase movement");
            }
        }
        
        /// <summary>
        /// Di chuyển squad mà không duy trì đội hình (cách cũ)
        /// </summary>
        private void MoveWithoutFormation(int squadId, Vector3 targetPosition)
        {
            // Find all entities in this squad
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            bool anyUnitMoved = false;
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    // Get NavigationComponent
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    
                    if (navigationComponent != null)
                    {
                        // Use High priority for player commands
                        navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.High);
                        anyUnitMoved = true;
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Moving entity {entity.Id} to {targetPosition} with High priority");
                        }
                    }
                }
            }
            
            if (_debugLog)
            {
                if (anyUnitMoved)
                {
                    Debug.Log($"SquadCoordinationSystem: Successfully moved squad {squadId} to {targetPosition}");
                }
                else
                {
                    Debug.LogWarning($"SquadCoordinationSystem: Failed to move squad {squadId}, no entities found or no navigation components");
                }
            }
        }
        
        /// <summary>
        /// Tạo mảng offset vị trí dựa trên loại formation
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int count)
        {
            Vector3[] positions = new Vector3[count];
            
            switch (formationType)
            {
                case FormationType.Line:
                    // Đội hình Line: đơn vị xếp ngang hàng
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f; // Đảm bảo căn giữa
                        positions[i] = new Vector3(xOffset * 1.5f, 0, 0);
                    }
                    break;
                
                case FormationType.Column:
                    // Đội hình Column: đơn vị xếp dọc hàng
                    for (int i = 0; i < count; i++)
                    {
                        float zOffset = i - (count - 1) / 2.0f; // Đảm bảo căn giữa
                        positions[i] = new Vector3(0, 0, zOffset * 1.5f);
                    }
                    break;
                
                case FormationType.Phalanx:
                    // Đội hình Phalanx: lưới vuông/chữ nhật
                    int rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Căn giữa formation
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        positions[i] = new Vector3(xOffset * 1.0f, 0, zOffset * 1.0f);
                    }
                    break;
                
                case FormationType.Testudo:
                    // Đội hình Testudo: lưới chặt chẽ hơn
                    rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Căn giữa formation
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        positions[i] = new Vector3(xOffset * 0.7f, 0, zOffset * 0.7f);
                    }
                    break;
                
                case FormationType.Circle:
                    // Đội hình Circle: vòng tròn
                    float radius = Mathf.Max(1.5f, count * 0.25f); // Bán kính tự động điều chỉnh
                    
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i * 2 * Mathf.PI) / count;
                        float x = Mathf.Sin(angle) * radius;
                        float z = Mathf.Cos(angle) * radius;
                        positions[i] = new Vector3(x, 0, z);
                    }
                    break;
                
                case FormationType.Normal:
                    // Đội hình Normal: lưới 3x3 cố định
                    for (int i = 0; i < count; i++)
                    {
                        // Modulo 3 để tính hàng/cột trong lưới 3x3
                        int row = i / 3;
                        int col = i % 3;
                        
                        // Căn giữa formation để slot 4 (ở giữa) nằm ở vị trí (0,0)
                        float xOffset = col - 1.0f; 
                        float zOffset = row - 1.0f;
                        
                        positions[i] = new Vector3(xOffset * 1.5f, 0, zOffset * 1.5f);
                    }
                    break;
                
                default:
                    // Mặc định sử dụng đội hình Line
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f;
                        positions[i] = new Vector3(xOffset * 1.5f, 0, 0);
                    }
                    break;
            }
            
            return positions;
        }
    }
}