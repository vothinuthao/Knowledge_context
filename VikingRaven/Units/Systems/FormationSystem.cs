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
        // Dictionary to track squad formations by squad ID
        private Dictionary<int, Dictionary<FormationType, Vector3[]>> _formationTemplates = 
            new Dictionary<int, Dictionary<FormationType, Vector3[]>>();
        
        private Dictionary<int, FormationType> _currentFormations = new Dictionary<int, FormationType>();
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        
        // Mapping từ squad ID tới center và rotation
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("FormationSystem initialized");
        }
        
        public override void Execute()
        {
            // Cập nhật danh sách unit theo squad
            UpdateSquadMembers();
            
            // Tính toán trung tâm và hướng mới cho mỗi squad
            CalculateSquadCentersAndRotations();
            
            // Cập nhật vị trí formation cho mỗi squad
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
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
        }
        
        /// <summary>
        /// Cập nhật danh sách đơn vị theo squad ID
        /// </summary>
        private void UpdateSquadMembers()
        {
            _squadMembers.Clear();
            
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null || !formationComponent.IsActive) continue;
                
                int squadId = formationComponent.SquadId;
                
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                
                _squadMembers[squadId].Add(entity);
                
                // Cập nhật loại formation hiện tại cho squad
                if (!_currentFormations.ContainsKey(squadId))
                {
                    _currentFormations[squadId] = formationComponent.CurrentFormationType;
                }
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
                        navigationComponent.SetDestination(targetPosition);
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