using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class SquadCoordinationSystem : BaseSystem
    {
        // Thiết lập ưu tiên cao hơn FormationSystem
        [SerializeField] private int _systemPriority = 200;
        
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private FormationSystem _formationSystem;
        
        // Theo dõi vị trí mục tiêu của squad
        private Dictionary<int, Vector3> _squadTargetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadTargetRotations = new Dictionary<int, Quaternion>();
        
        // Tham số khoảng cách trong đội hình
        [Header("Tham số đội hình")]
        [SerializeField] private float _lineSpacing = 2.0f;      // Khoảng cách giữa các đơn vị trong Line
        [SerializeField] private float _columnSpacing = 2.0f;    // Khoảng cách giữa các đơn vị trong Column
        [SerializeField] private float _phalanxSpacing = 1.2f;   // Khoảng cách giữa các đơn vị trong Phalanx
        [SerializeField] private float _testudoSpacing = 0.8f;   // Khoảng cách giữa các đơn vị trong Testudo
        [SerializeField] private float _circleMultiplier = 0.3f; // Hệ số nhân bán kính cho Circle
        [SerializeField] private float _gridSpacing = 2.0f;      // Khoảng cách các ô trong Normal
        
        [SerializeField] private bool _debugLog = false;
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Thiết lập ưu tiên cho hệ thống này
            Priority = _systemPriority;
            
            // Tìm FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogError("SquadCoordinationSystem: Không tìm thấy FormationSystem!");
            }
            
            Debug.Log($"SquadCoordinationSystem khởi tạo với ưu tiên {Priority}");
        }
        
        public override void Execute()
        {
            // Lấy tất cả đơn vị có FormationComponent
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Cập nhật loại đội hình cho các đơn vị trong mỗi squad
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // Nếu loại đội hình mới đã được thiết lập cho squad này, cập nhật cho đơn vị
                    if (_squadFormationTypes.TryGetValue(squadId, out FormationType formationType) &&
                        formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                        
                        // Thông báo cho FormationSystem về thay đổi
                        if (_formationSystem != null)
                        {
                            _formationSystem.ChangeFormation(squadId, formationType);
                        }
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Cập nhật loại đội hình cho squad {squadId} thành {formationType}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Đặt loại đội hình cho một squad
        /// </summary>
        public void SetSquadFormation(int squadId, FormationType formationType)
        {
            _squadFormationTypes[squadId] = formationType;
            
            // Thông báo ngay cho FormationSystem
            if (_formationSystem != null)
            {
                _formationSystem.ChangeFormation(squadId, formationType);
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Đặt loại đội hình cho squad {squadId} thành {formationType}");
            }
            
            // Cập nhật đội hình cho tất cả đơn vị trong squad
            UpdateSquadFormation(squadId, formationType);
        }
        
        /// <summary>
        /// Di chuyển một squad đến vị trí mới
        /// </summary>
        public void MoveSquadToPosition(int squadId, Vector3 targetPosition)
        {
            // Lưu vị trí mục tiêu cho squad này
            _squadTargetPositions[squadId] = targetPosition;
            
            // Mặc định giữ nguyên hướng quay hiện tại hoặc hướng về phía trước nếu không có
            Quaternion targetRotation = Quaternion.identity;
            if (_squadTargetRotations.TryGetValue(squadId, out Quaternion currentRotation))
            {
                targetRotation = currentRotation;
            }
            _squadTargetRotations[squadId] = targetRotation;
            
            // Lấy loại đội hình cho squad này
            FormationType formationType = FormationType.Line;
            if (_squadFormationTypes.TryGetValue(squadId, out var storedFormationType))
            {
                formationType = storedFormationType;
            }
            else if (_formationSystem != null)
            {
                formationType = _formationSystem.GetCurrentFormationType(squadId);
            }
            
            // Thông báo cho FormationSystem về di chuyển thủ công
            if (_formationSystem != null)
            {
                _formationSystem.SetSquadManualMovement(squadId, true);
            }
            
            // Tìm tất cả đơn vị trong squad này
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            var squadMembers = new List<IEntity>();
            
            // Nhóm các thành viên squad
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
                    Debug.LogWarning($"SquadCoordinationSystem: Không thể di chuyển squad {squadId}, không tìm thấy đơn vị nào");
                }
                return;
            }
            
            // Tạo hoặc lấy template đội hình
            Vector3[] formationOffsets = GenerateFormationTemplate(formationType, squadMembers.Count);
            
            // Di chuyển từng đơn vị
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent != null && navigationComponent != null)
                {
                    // Mở khóa vị trí nếu đơn vị đã bị khóa
                    navigationComponent.UnlockPosition();
                    
                    // Cập nhật slot và offset trong đội hình
                    formationComponent.SetFormationSlot(i);
                    Vector3 offset = formationOffsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Thiết lập thông tin đội hình cho từng đơn vị
                    navigationComponent.SetFormationInfo(targetPosition, offset, NavigationCommandPriority.High);
                    
                    if (_debugLog)
                    {
                        Debug.Log($"SquadCoordinationSystem: Di chuyển đơn vị {entity.Id} đến vị trí đội hình với offset {offset}");
                    }
                }
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Di chuyển squad {squadId} đến {targetPosition}");
            }
        }
        
        /// <summary>
        /// Cập nhật đội hình cho tất cả đơn vị trong squad
        /// </summary>
        private void UpdateSquadFormation(int squadId, FormationType formationType)
        {
            // Kiểm tra xem squad đã có vị trí mục tiêu chưa
            if (!_squadTargetPositions.TryGetValue(squadId, out Vector3 targetPosition))
                return;
            
            // Tìm tất cả đơn vị trong squad
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            var squadMembers = new List<IEntity>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    squadMembers.Add(entity);
                }
            }
            
            if (squadMembers.Count == 0)
                return;
            
            // Tạo template đội hình mới
            Vector3[] formationOffsets = GenerateFormationTemplate(formationType, squadMembers.Count);
            
            // Cập nhật vị trí cho từng đơn vị
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent != null && navigationComponent != null)
                {
                    // Cập nhật slot và offset trong đội hình
                    formationComponent.SetFormationSlot(i);
                    Vector3 offset = formationOffsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Cập nhật thông tin đội hình
                    navigationComponent.SetFormationInfo(targetPosition, offset, NavigationCommandPriority.High);
                }
            }
        }
        
        /// <summary>
        /// Tạo mảng offset vị trí dựa trên loại đội hình
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
                        positions[i] = new Vector3(xOffset * _lineSpacing, 0, 0);
                    }
                    break;
                
                case FormationType.Column:
                    // Đội hình Column: đơn vị xếp dọc hàng
                    for (int i = 0; i < count; i++)
                    {
                        float zOffset = i - (count - 1) / 2.0f; // Đảm bảo căn giữa
                        positions[i] = new Vector3(0, 0, zOffset * _columnSpacing);
                    }
                    break;
                
                case FormationType.Phalanx:
                    // Đội hình Phalanx: lưới vuông/chữ nhật
                    int rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Căn giữa đội hình
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        positions[i] = new Vector3(xOffset * _phalanxSpacing, 0, zOffset * _phalanxSpacing);
                    }
                    break;
                
                case FormationType.Testudo:
                    // Đội hình Testudo: lưới chặt chẽ hơn
                    rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Căn giữa đội hình
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        positions[i] = new Vector3(xOffset * _testudoSpacing, 0, zOffset * _testudoSpacing);
                    }
                    break;
                
                case FormationType.Circle:
                    // Đội hình Circle: vòng tròn
                    float radius = Mathf.Max(1.5f, count * _circleMultiplier); // Bán kính tự động điều chỉnh
                    
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
                        
                        // Căn giữa đội hình để slot 4 (ở giữa) nằm ở vị trí (0,0)
                        float xOffset = col - 1.0f; 
                        float zOffset = row - 1.0f;
                        
                        positions[i] = new Vector3(xOffset * _gridSpacing, 0, zOffset * _gridSpacing);
                    }
                    break;
                
                default:
                    // Mặc định sử dụng đội hình Line
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f;
                        positions[i] = new Vector3(xOffset * _lineSpacing, 0, 0);
                    }
                    break;
            }
            
            return positions;
        }
    }
}