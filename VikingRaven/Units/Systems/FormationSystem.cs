using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Hệ thống quản lý formation cho các đơn vị
    /// </summary>
    public class FormationSystem : BaseSystem
    {
        // Set priority for this system - should execute before StateManagementSystem
        [SerializeField] private int _systemPriority = 300;
        
        // Dictionary to track squad formations by squad ID
        private Dictionary<int, Dictionary<FormationType, Vector3[]>> _formationTemplates = 
            new Dictionary<int, Dictionary<FormationType, Vector3[]>>();
        
        private Dictionary<int, FormationType> _currentFormations = new Dictionary<int, FormationType>();
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        
        // Tracking squad manual movement
        private Dictionary<int, bool> _squadManualMovement = new Dictionary<int, bool>();
        private Dictionary<int, float> _lastManualMoveTime = new Dictionary<int, float>();
        [SerializeField] private float _manualMoveTimeout = 5.0f; // How long manual move priority lasts
        
        // Mapping từ squad ID tới center và rotation
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        // Debug flags
        [SerializeField] private bool _logDebugInfo = true;
        
        public override void Initialize()
        {
            base.Initialize();
            // Set priority for this system
            Priority = _systemPriority;
            Debug.Log($"FormationSystem initialized with priority {Priority}");
            
            // Check EntityRegistry initialization
            if (EntityRegistry != null)
            {
                Debug.Log($"FormationSystem: EntityRegistry is available with {EntityRegistry.EntityCount} entities");
            }
            else
            {
                Debug.LogError("FormationSystem: EntityRegistry is null");
            }
        }
        
        public override void Execute()
        {
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.Execute: Starting execution with {(_squadMembers != null ? _squadMembers.Count : 0)} squads");
            }
            
            // Cập nhật danh sách unit theo squad
            UpdateSquadMembers();
            
            // Cập nhật trạng thái manual movement
            UpdateManualMovementStatus();
            
            // Tính toán trung tâm và hướng mới cho mỗi squad
            CalculateSquadCentersAndRotations();
            
            // Cập nhật vị trí formation cho mỗi squad
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
                // Kiểm tra xem squad này có đang di chuyển thủ công không
                bool isManuallyMoving = false;
                if (_squadManualMovement.TryGetValue(squadId, out isManuallyMoving) && isManuallyMoving)
                {
                    // Bỏ qua cập nhật vị trí formation nếu đang di chuyển thủ công
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Squad {squadId} is being manually moved, skipping formation update");
                    }
                    continue;
                }
                
                // Lấy formation type hiện tại
                FormationType formationType = FormationType.Line; // Mặc định
                if (_currentFormations.TryGetValue(squadId, out var currentFormation))
                {
                    formationType = currentFormation;
                }
                
                // Đảm bảo có template cho formation và số lượng unit
                EnsureFormationTemplate(squadId, formationType, members.Count);
                
                // Cập nhật vị trí formation
                UpdateFormationPositions(squadId, members, formationType);
            }
            
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.Execute: Finished execution, processed {_squadMembers.Count} squads");
            }
        }
        
        /// <summary>
        /// Cập nhật danh sách đơn vị theo squad ID
        /// </summary>
        private void UpdateSquadMembers()
        {
            _squadMembers.Clear();
            
            // Debug logging to check EntityRegistry state
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.UpdateSquadMembers: EntityRegistry has {EntityRegistry.EntityCount} entities");
            }
            
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.UpdateSquadMembers: Found {entities.Count} entities with FormationComponent");
            }
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null)
                {
                    Debug.LogWarning($"FormationSystem: Entity {entity.Id} has null FormationComponent");
                    continue;
                }
                
                if (!formationComponent.IsActive)
                {
                    Debug.LogWarning($"FormationSystem: Entity {entity.Id} has inactive FormationComponent");
                    continue;
                }
                
                int squadId = formationComponent.SquadId;
                
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                
                _squadMembers[squadId].Add(entity);
                
                if (_logDebugInfo)
                {
                    Debug.Log($"FormationSystem: Added entity {entity.Id} to squad {squadId}, slot {formationComponent.FormationSlotIndex}");
                }
                
                // Cập nhật loại formation hiện tại cho squad
                if (!_currentFormations.ContainsKey(squadId))
                {
                    _currentFormations[squadId] = formationComponent.CurrentFormationType;
                    
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Set formation type for squad {squadId} to {formationComponent.CurrentFormationType}");
                    }
                }
            }
            
            // Log squad member counts
            if (_logDebugInfo)
            {
                foreach (var squad in _squadMembers)
                {
                    Debug.Log($"FormationSystem: Squad {squad.Key} has {squad.Value.Count} members");
                }
            }
        }
        
        /// <summary>
        /// Cập nhật trạng thái di chuyển thủ công của các squad
        /// </summary>
        private void UpdateManualMovementStatus()
        {
            float currentTime = Time.time;
            List<int> expiredSquads = new List<int>();
            
            // Kiểm tra và cập nhật các squad hết thời gian di chuyển thủ công
            foreach (var squadId in _squadManualMovement.Keys)
            {
                if (_squadManualMovement[squadId])
                {
                    if (_lastManualMoveTime.TryGetValue(squadId, out float lastMoveTime))
                    {
                        if (currentTime - lastMoveTime > _manualMoveTimeout)
                        {
                            // Đã hết thời gian timeout cho manual movement
                            expiredSquads.Add(squadId);
                            if (_logDebugInfo)
                            {
                                Debug.Log($"FormationSystem: Squad {squadId} manual movement expired");
                            }
                        }
                    }
                }
            }
            
            // Reset trạng thái cho các squad hết hạn
            foreach (var squadId in expiredSquads)
            {
                _squadManualMovement[squadId] = false;
            }
        }
        
        /// <summary>
        /// Đánh dấu một squad là đang được di chuyển thủ công
        /// </summary>
        public void SetSquadManualMovement(int squadId, bool isManuallyMoving)
        {
            _squadManualMovement[squadId] = isManuallyMoving;
            _lastManualMoveTime[squadId] = Time.time;
            
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem: Squad {squadId} manual movement set to {isManuallyMoving}");
            }
        }
        
        /// <summary>
        /// Tính toán trung tâm và hướng của các squad
        /// </summary>
        private void CalculateSquadCentersAndRotations()
        {
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
                Vector3 centerSum = Vector3.zero;
                Vector3 forwardSum = Vector3.zero;
                int validCount = 0;
                
                foreach (var entity in members)
                {
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    if (transformComponent == null) continue;
                    
                    centerSum += transformComponent.Position;
                    forwardSum += transformComponent.Forward;
                    validCount++;
                }
                
                if (validCount > 0)
                {
                    // Tính trung tâm (vị trí trung bình của tất cả đơn vị)
                    Vector3 center = centerSum / validCount;
                    _squadCenters[squadId] = center;
                    
                    // Tính hướng trung bình
                    if (forwardSum.magnitude > 0.01f)
                    {
                        Quaternion rotation = Quaternion.LookRotation(forwardSum.normalized);
                        _squadRotations[squadId] = rotation;
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                    
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Calculated center for squad {squadId} at {center}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Đảm bảo có template cho loại formation và số lượng unit
        /// </summary>
        private void EnsureFormationTemplate(int squadId, FormationType formationType, int memberCount)
        {
            if (!_formationTemplates.ContainsKey(squadId))
            {
                _formationTemplates[squadId] = new Dictionary<FormationType, Vector3[]>();
            }
            
            if (!_formationTemplates[squadId].ContainsKey(formationType) || 
                _formationTemplates[squadId][formationType].Length != memberCount)
            {
                _formationTemplates[squadId][formationType] = GenerateFormationTemplate(formationType, memberCount);
                
                if (_logDebugInfo)
                {
                    Debug.Log($"FormationSystem: Generated new template for squad {squadId}, formation {formationType}, members {memberCount}");
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
        
        /// <summary>
        /// Cập nhật vị trí formation cho các đơn vị trong squad
        /// </summary>
        private void UpdateFormationPositions(int squadId, List<IEntity> members, FormationType formationType)
        {
            // Kiểm tra nếu có dữ liệu squad center và rotation
            if (!_squadCenters.TryGetValue(squadId, out Vector3 center) ||
                !_squadRotations.TryGetValue(squadId, out Quaternion rotation))
            {
                return;
            }
            
            // Lấy template đã tạo
            var formationTemplate = _formationTemplates[squadId][formationType];
            
            // Đảm bảo không vượt quá kích thước template
            int count = Mathf.Min(members.Count, formationTemplate.Length);
            
            // Cập nhật FormationComponent cho mỗi entity
            for (int i = 0; i < count; i++)
            {
                var entity = members[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    // Cập nhật formation slot và offset
                    formationComponent.SetFormationSlot(i);
                    formationComponent.SetFormationOffset(formationTemplate[i]);
                    
                    // Đảm bảo formation type đồng nhất
                    if (formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                    }
                    
                    // Cập nhật navigation target nếu cần
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null && navigationComponent.IsActive)
                    {
                        Vector3 targetPosition = center + (rotation * formationTemplate[i]);
                        
                        // Sử dụng ưu tiên Normal cho lệnh di chuyển formation
                        navigationComponent.SetDestination(targetPosition, NavigationCommandPriority.Normal);
                        
                        if (_logDebugInfo)
                        {
                            Debug.Log($"FormationSystem: Set destination for entity {entity.Id} to {targetPosition} with Normal priority");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Thay đổi loại formation cho một squad
        /// </summary>
        public void ChangeFormation(int squadId, FormationType formationType)
        {
            // Lưu trữ formation type mới
            _currentFormations[squadId] = formationType;
            
            Debug.Log($"FormationSystem: Đổi đội hình squad {squadId} thành {formationType}");
            
            // Xóa template hiện tại để tạo mới ở lần update tiếp theo
            if (_formationTemplates.ContainsKey(squadId) && 
                _formationTemplates[squadId].ContainsKey(formationType))
            {
                _formationTemplates[squadId].Remove(formationType);
            }
            
            // Cập nhật ngay lập tức nếu có thành viên
            if (_squadMembers.TryGetValue(squadId, out var members) && members.Count > 0)
            {
                // Đảm bảo có template
                EnsureFormationTemplate(squadId, formationType, members.Count);
                
                // Cập nhật vị trí
                if (_squadCenters.ContainsKey(squadId) && _squadRotations.ContainsKey(squadId))
                {
                    UpdateFormationPositions(squadId, members, formationType);
                }
            }
        }
        
        /// <summary>
        /// Lấy loại formation hiện tại của một squad
        /// </summary>
        public FormationType GetCurrentFormationType(int squadId)
        {
            if (_currentFormations.TryGetValue(squadId, out FormationType formationType))
            {
                return formationType;
            }
            
            return FormationType.Line; // Mặc định
        }
    }
}