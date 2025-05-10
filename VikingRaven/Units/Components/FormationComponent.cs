using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component xử lý vị trí của đơn vị trong đội hình
    /// </summary>
    public class FormationComponent : BaseComponent
    {
        [Tooltip("Chỉ số slot của đơn vị trong đội hình")]
        [SerializeField] private int _formationSlotIndex;
        
        [Tooltip("Vị trí offset của đơn vị so với trung tâm squad")]
        [SerializeField] private Vector3 _formationOffset;
        
        [Tooltip("ID của squad mà đơn vị thuộc về")]
        [SerializeField] private int _squadId;
        
        [Tooltip("Loại đội hình hiện tại")]
        [SerializeField] private FormationType _currentFormationType = FormationType.Line;
        
        // Properties để truy cập từ bên ngoài
        public int FormationSlotIndex => _formationSlotIndex;
        public Vector3 FormationOffset => _formationOffset;
        public int SquadId => _squadId;
        public FormationType CurrentFormationType => _currentFormationType;

        /// <summary>
        /// Đặt chỉ số slot trong đội hình
        /// </summary>
        public void SetFormationSlot(int slotIndex)
        {
            _formationSlotIndex = slotIndex;
        }

        /// <summary>
        /// Đặt vị trí offset trong đội hình
        /// </summary>
        public void SetFormationOffset(Vector3 offset)
        {
            _formationOffset = offset;
        }

        /// <summary>
        /// Đặt ID của squad
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }

        /// <summary>
        /// Đặt loại đội hình và cập nhật offset nếu cần
        /// </summary>
        public void SetFormationType(FormationType formationType)
        {
            // Chỉ cập nhật nếu đổi formation type
            if (_currentFormationType != formationType)
            {
                _currentFormationType = formationType;
                
                // Offset sẽ được cập nhật bởi FormationSystem
                // Vì FormationSystem quản lý offset cho tất cả đơn vị trong squad để đảm bảo tính nhất quán
            }
        }
        
        /// <summary>
        /// Được gọi khi component được khởi tạo
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // Các khởi tạo khác nếu cần
        }
        
        /// <summary>
        /// Được gọi khi component bị hủy
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            
            // Dọn dẹp nếu cần
        }
    }
}