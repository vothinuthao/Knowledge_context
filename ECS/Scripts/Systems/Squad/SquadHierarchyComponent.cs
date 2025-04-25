// ECS/Scripts/Components/Squad/SquadHierarchyComponent.cs
using Core.ECS;
using System.Collections.Generic;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Component quản lý cấu trúc phân cấp trong squad
    /// </summary>
    public class SquadHierarchyComponent : IComponent
    {
        // ID của leader (troop chỉ huy) trong squad
        public int LeaderEntityId { get; set; } = -1;
        
        // Danh sách ID của các troop trong squad theo thứ tự ưu tiên
        public List<int> MemberIds { get; private set; } = new List<int>();
        
        // Danh sách các vai trò đặc biệt trong squad
        public Dictionary<SpecialRole, int> RoleAssignments { get; private set; } = new Dictionary<SpecialRole, int>();
        
        // Thiết lập đội hình khi di chuyển (các offset từ vị trí leader)
        public Dictionary<int, Vector3> FormationOffsets { get; private set; } = new Dictionary<int, Vector3>();
        
        public SquadHierarchyComponent()
        {
        }
        
        /// <summary>
        /// Thêm một troop vào squad
        /// </summary>
        public void AddMember(int entityId)
        {
            if (!MemberIds.Contains(entityId))
            {
                MemberIds.Add(entityId);
                
                // Nếu chưa có leader, gán leader
                if (LeaderEntityId == -1)
                {
                    LeaderEntityId = entityId;
                }
            }
        }
        
        /// <summary>
        /// Xóa một troop khỏi squad
        /// </summary>
        public void RemoveMember(int entityId)
        {
            if (MemberIds.Contains(entityId))
            {
                MemberIds.Remove(entityId);
                
                // Nếu xóa leader, chọn leader mới
                if (LeaderEntityId == entityId && MemberIds.Count > 0)
                {
                    LeaderEntityId = MemberIds[0];
                }
                
                // Xóa khỏi danh sách vai trò nếu có
                foreach (var role in System.Enum.GetValues(typeof(SpecialRole)))
                {
                    SpecialRole specialRole = (SpecialRole)role;
                    if (RoleAssignments.ContainsKey(specialRole) && RoleAssignments[specialRole] == entityId)
                    {
                        RoleAssignments.Remove(specialRole);
                    }
                }
                
                // Xóa khỏi đội hình
                if (FormationOffsets.ContainsKey(entityId))
                {
                    FormationOffsets.Remove(entityId);
                }
            }
        }
        
        /// <summary>
        /// Gán một vai trò đặc biệt cho troop
        /// </summary>
        public void AssignRole(int entityId, SpecialRole role)
        {
            if (MemberIds.Contains(entityId))
            {
                RoleAssignments[role] = entityId;
            }
        }
        
        /// <summary>
        /// Đặt offset vị trí cho troop trong đội hình
        /// </summary>
        public void SetFormationOffset(int entityId, Vector3 offset)
        {
            if (MemberIds.Contains(entityId))
            {
                FormationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Lấy vai trò của troop
        /// </summary>
        public SpecialRole GetRole(int entityId)
        {
            foreach (var role in RoleAssignments)
            {
                if (role.Value == entityId)
                {
                    return role.Key;
                }
            }
            
            return SpecialRole.Regular;
        }
        
        /// <summary>
        /// Kiểm tra xem troop có phải là leader không
        /// </summary>
        public bool IsLeader(int entityId)
        {
            return LeaderEntityId == entityId;
        }
        
        /// <summary>
        /// Tìm troop với vai trò nhất định
        /// </summary>
        public int GetEntityWithRole(SpecialRole role)
        {
            if (RoleAssignments.ContainsKey(role))
            {
                return RoleAssignments[role];
            }
            
            return -1;
        }
        
        /// <summary>
        /// Tạo đội hình tự động dựa trên số lượng troops
        /// </summary>
        public void GenerateFormationOffsets(float spacing)
        {
            FormationOffsets.Clear();
            
            int count = MemberIds.Count;
            if (count == 0) return;
            
            // Tính toán số hàng và cột tối ưu
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);
            
            // Sinh offset cho mỗi troop
            for (int i = 0; i < count; i++)
            {
                int entityId = MemberIds[i];
                
                // Leader luôn ở vị trí trung tâm
                if (entityId == LeaderEntityId)
                {
                    FormationOffsets[entityId] = Vector3.zero;
                    continue;
                }
                
                // Tính vị trí hàng và cột
                int row = i / columns;
                int col = i % columns;
                
                // Tính offset tương đối
                float xOffset = (col - (columns - 1) / 2.0f) * spacing;
                float zOffset = (row - (rows - 1) / 2.0f) * spacing;
                
                FormationOffsets[entityId] = new Vector3(xOffset, 0, zOffset);
            }
        }
    }
    
    /// <summary>
    /// Các vai trò đặc biệt trong squad
    /// </summary>
    public enum SpecialRole
    {
        Regular,    // Binh thường
        Leader,     // Chỉ huy
        Scout,      // Trinh sát
        Protector,  // Bảo vệ
        Flanker,    // Tấn công cánh
        Healer      // Hồi máu
    }
}