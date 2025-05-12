using UnityEngine;
using UnityEngine.AI;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class NavigationComponent : BaseComponent
    {
        [SerializeField] private NavMeshAgent _navMeshAgent;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private bool _isPathfindingActive = true;
        
        // Biến điều khiển ưu tiên
        [SerializeField] private NavigationCommandPriority _currentCommandPriority = NavigationCommandPriority.Normal;
        [SerializeField] private float _priorityDecayTime = 1.0f;
        private float _lastCommandTime = 0f;
        
        // Thông tin đội hình
        private Vector3 _squadCenter;
        private Vector3 _formationOffset;
        private bool _hasFormationInfo = false;
        private bool _useFormationPosition = false;
        
        // Biến kiểm soát vị trí chính xác
        private bool _hasReachedExactPosition = false;
        private Vector3 _exactPosition;
        
        // Thuộc tính truy cập
        public Vector3 Destination => _destination;
        public bool IsPathfindingActive => _isPathfindingActive;
        public bool HasReachedDestination => _hasReachedExactPosition;
        public NavigationCommandPriority CurrentCommandPriority => _currentCommandPriority;
        
        private Vector3 _destination;

        public override void Initialize()
        {
            if (_navMeshAgent == null)
            {
                _navMeshAgent = GetComponent<NavMeshAgent>();
                
                if (_navMeshAgent == null)
                {
                    _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
                }
            }
            
            // Cấu hình cơ bản
            _navMeshAgent.stoppingDistance = _stoppingDistance;
            
            // Cấu hình tránh chướng ngại vật
            ConfigureObstacleAvoidance();
        }
        
        /// <summary>
        /// Cấu hình tránh chướng ngại vật cho NavMeshAgent
        /// </summary>
        private void ConfigureObstacleAvoidance()
        {
            if (_navMeshAgent == null) return;
            
            // Sử dụng chất lượng tránh vật cản trung bình để cân bằng hiệu suất và hiệu quả
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            
            // Đặt bán kính phù hợp cho đơn vị
            _navMeshAgent.radius = 0.4f;
            
            // Bật tự động tìm đường đi mới khi gặp chướng ngại vật
            _navMeshAgent.autoRepath = true;
            
            // Điều chỉnh ưu tiên tránh chướng ngại vật (số thấp = ưu tiên cao hơn)
            // Thêm yếu tố ngẫu nhiên để tránh các agent cùng ưu tiên
            _navMeshAgent.avoidancePriority = 50 + Random.Range(0, 10);
        }

        /// <summary>
        /// Đặt điểm đến cho đơn vị
        /// </summary>
        public void SetDestination(Vector3 destination, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            bool priorityDecayed = timeSinceLastCommand > _priorityDecayTime;
            
            if (priority >= _currentCommandPriority || priorityDecayed)
            {
                // Reset trạng thái khóa vị trí
                _hasReachedExactPosition = false;
                
                // Kích hoạt lại NavMeshAgent
                if (_navMeshAgent != null && _navMeshAgent.enabled)
                {
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.updatePosition = true;
                    _navMeshAgent.updateRotation = true;
                }
                
                _destination = destination;
                _currentCommandPriority = priority;
                _lastCommandTime = Time.time;
                _useFormationPosition = false;
                
                if (_isPathfindingActive && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.stoppingDistance = _stoppingDistance;
                    _navMeshAgent.SetDestination(destination);
                }
                
                Debug.Log($"NavigationComponent: Đặt điểm đến {destination}, ưu tiên: {priority}");
            }
        }

        /// <summary>
        /// Đặt thông tin vị trí trong đội hình
        /// </summary>
        public void SetFormationInfo(Vector3 squadCenter, Vector3 formationOffset, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            if (priority >= _currentCommandPriority)
            {
                // Reset trạng thái khóa vị trí
                _hasReachedExactPosition = false;
                
                // Kích hoạt lại NavMeshAgent
                if (_navMeshAgent != null && _navMeshAgent.enabled)
                {
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.updatePosition = true;
                    _navMeshAgent.updateRotation = true;
                }
                
                _squadCenter = squadCenter;
                _formationOffset = formationOffset;
                _hasFormationInfo = true;
                _useFormationPosition = true;
                
                // Tính vị trí formation chính xác
                _exactPosition = _squadCenter + _formationOffset;
                
                // Đặt điểm đến là vị trí formation
                SetDestination(_exactPosition, priority);
                
                Debug.Log($"NavigationComponent: Đặt thông tin formation (trung tâm: {squadCenter}, offset: {formationOffset})");
            }
        }

        void Update()
        {
            UpdatePathfinding();
        }

        /// <summary>
        /// Cập nhật di chuyển và xử lý định vị chính xác
        /// </summary>
        public void UpdatePathfinding()
        {
            if (!IsActive || _navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
                return;
            
            // Nếu đã đến vị trí chính xác, không làm gì thêm
            if (_hasReachedExactPosition)
                return;
            
            // Giảm ưu tiên theo thời gian
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            if (timeSinceLastCommand > _priorityDecayTime && _currentCommandPriority > NavigationCommandPriority.Low)
            {
                _currentCommandPriority = (NavigationCommandPriority)((int)_currentCommandPriority - 1);
                _lastCommandTime = Time.time;
            }
            
            // Kiểm tra nếu đã gần đến đích
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance ||
                (_navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete && 
                 _navMeshAgent.remainingDistance == 0))
            {
                // Nếu có thông tin đội hình, sử dụng vị trí chính xác đó
                if (_useFormationPosition && _hasFormationInfo)
                {
                    _exactPosition = _squadCenter + _formationOffset;
                    
                    // Nếu rất gần vị trí chính xác, snap và khóa
                    if (Vector3.Distance(transform.position, _exactPosition) < 0.2f)
                    {
                        // Dừng hoàn toàn NavMeshAgent
                        _navMeshAgent.isStopped = true;
                        _navMeshAgent.updatePosition = false;
                        _navMeshAgent.updateRotation = false;
                        _navMeshAgent.velocity = Vector3.zero;
                        
                        // Snap đến vị trí chính xác
                        transform.position = _exactPosition;
                        
                        // Đánh dấu đã đến vị trí chính xác
                        _hasReachedExactPosition = true;
                        
                        Debug.Log($"Đơn vị đã khóa tại vị trí chính xác: {_exactPosition}");
                    }
                    else
                    {
                        // Di chuyển trực tiếp đến vị trí chính xác với bước nhỏ
                        transform.position = Vector3.MoveTowards(
                            transform.position, 
                            _exactPosition, 
                            Time.deltaTime * _navMeshAgent.speed * 0.5f);
                    }
                }
                else
                {
                    // Không có thông tin đội hình, chỉ dừng agent
                    _navMeshAgent.isStopped = true;
                    _hasReachedExactPosition = true;
                }
            }
        }

        public void EnablePathfinding()
        {
            _isPathfindingActive = true;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = false;
            }
        }

        public void DisablePathfinding()
        {
            _isPathfindingActive = false;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        public override void Cleanup()
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }
        
        /// <summary>
        /// Cập nhật offset trong đội hình
        /// </summary>
        public void UpdateFormationOffset(Vector3 newOffset)
        {
            if (_hasFormationInfo)
            {
                _formationOffset = newOffset;
                _exactPosition = _squadCenter + _formationOffset;
                
                // Nếu đã khóa vị trí, cập nhật ngay
                if (_hasReachedExactPosition)
                {
                    // Mở khóa, di chuyển đến vị trí mới
                    _hasReachedExactPosition = false;
                    SetDestination(_exactPosition, _currentCommandPriority);
                }
            }
        }
        
        /// <summary>
        /// Mở khóa vị trí để cho phép di chuyển
        /// </summary>
        public void UnlockPosition()
        {
            _hasReachedExactPosition = false;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.updatePosition = true;
                _navMeshAgent.updateRotation = true;
            }
        }
    }
    
    // Enum định nghĩa mức ưu tiên cho lệnh di chuyển
    public enum NavigationCommandPriority
    {
        Low = 0,         // Ưu tiên thấp - dùng cho movement tự động, behavior thông thường
        Normal = 1,      // Ưu tiên trung bình - dùng cho formation và di chuyển squad
        High = 2,        // Ưu tiên cao - dùng cho lệnh từ người chơi
        Critical = 3     // Ưu tiên tối đa - dùng cho state đặc biệt như stun, knockback
    }
}