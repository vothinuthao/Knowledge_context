using System.Collections.Generic;
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
        
        // Thêm các biến để kiểm soát ưu tiên của lệnh di chuyển
        [SerializeField] private NavigationCommandPriority _currentCommandPriority = NavigationCommandPriority.Normal;
        [SerializeField] private float _priorityDecayTime = 1.0f; // Thời gian để giảm ưu tiên lệnh
        private float _lastCommandTime = 0f;
        
        // Thêm các biến cho di chuyển hai giai đoạn
        [Header("Two-Phase Movement")]
        [SerializeField] private bool _useTwoPhaseMovement = true;
        [SerializeField] private float _formationPhaseDistance = 5.0f; // Khoảng cách bắt đầu xếp đội hình
        [SerializeField] private float _directMovementSpeed = 5.0f;
        [SerializeField] private float _rotationSpeed = 2.0f;
        
        // Trạng thái di chuyển
        private enum MovementPhase { 
            Approaching, // Đang di chuyển đến gần điểm đích
            Forming      // Đang di chuyển vào vị trí trong đội hình
        }
        
        private MovementPhase _currentPhase = MovementPhase.Approaching;
        
        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        
        private Vector3 _destination;
        private List<Vector3> _currentPath = new List<Vector3>();
        private int _currentWaypointIndex = 0;
        
        // Track original target and formation offset
        private Vector3 _squadCenter;
        private Vector3 _formationOffset;
        private bool _hasFormationInfo = false;
        
        public Vector3 Destination => _destination;
        public bool IsPathfindingActive => _isPathfindingActive;
        public bool HasReachedDestination => Vector3.Distance(transform.position, _destination) <= _stoppingDistance;
        public NavigationCommandPriority CurrentCommandPriority => _currentCommandPriority;

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
            
            // Configure NavMeshAgent
            _navMeshAgent.stoppingDistance = _stoppingDistance;
            _navMeshAgent.updateRotation = false; // We'll handle rotation separately
        }

        public void SetDestination(Vector3 destination, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            // Kiểm tra xem ưu tiên mới có cao hơn ưu tiên hiện tại hoặc ưu tiên hiện tại đã hết hạn chưa
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            bool priorityDecayed = timeSinceLastCommand > _priorityDecayTime;
            
            if (priority >= _currentCommandPriority || priorityDecayed)
            {
                _destination = destination;
                _currentPath.Clear();
                _currentWaypointIndex = 0;
                _currentCommandPriority = priority;
                _lastCommandTime = Time.time;
                
                // Reset to approaching phase
                _currentPhase = MovementPhase.Approaching;
                
                if (_isPathfindingActive && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.SetDestination(destination);
                }
                
                Debug.Log($"NavigationComponent: Set destination to {destination}, priority: {priority}, phase: {_currentPhase}");
            }
            else
            {
                Debug.Log($"NavigationComponent: Ignored command with priority {priority}, current priority is {_currentCommandPriority}");
            }
        }

        /// <summary>
        /// Đặt thông tin vị trí trong đội hình cho unit này, để sử dụng khi đến gần điểm đích
        /// </summary>
        public void SetFormationInfo(Vector3 squadCenter, Vector3 formationOffset, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            if (priority >= _currentCommandPriority)
            {
                _squadCenter = squadCenter;
                _formationOffset = formationOffset;
                _hasFormationInfo = true;
                
                Debug.Log($"NavigationComponent: Set formation info (center: {squadCenter}, offset: {formationOffset})");
            }
        }

        public void UpdatePathfinding()
        {
            if (!IsActive || _navMeshAgent == null)
                return;
            
            // Giảm ưu tiên lệnh nếu đã quá thời gian
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            if (timeSinceLastCommand > _priorityDecayTime && _currentCommandPriority > NavigationCommandPriority.Low)
            {
                _currentCommandPriority = (NavigationCommandPriority)((int)_currentCommandPriority - 1);
                _lastCommandTime = Time.time; // Reset timer
            }
            
            // Update movement based on current phase
            if (_useTwoPhaseMovement && _hasFormationInfo)
            {
                UpdateTwoPhaseMovement();
            }
            else
            {
                // Standard pathfinding
                if (HasReachedDestination)
                {
                    if (_navMeshAgent.enabled && _isPathfindingActive)
                    {
                        _navMeshAgent.isStopped = true;
                    }
                }
                else
                {
                    if (_isPathfindingActive && _navMeshAgent.enabled)
                    {
                        _navMeshAgent.isStopped = false;
                    }
                }
            }
        }

        /// <summary>
        /// Cập nhật di chuyển hai giai đoạn (approach + form)
        /// </summary>
        private void UpdateTwoPhaseMovement()
        {
            // Current position and destination
            Vector3 currentPosition = transform.position;
            
            // Calculate distance to destination
            float distanceToDestination = Vector3.Distance(currentPosition, _destination);
            
            // Calculate formation target position
            Vector3 formationPosition = _squadCenter + _formationOffset;
            
            // Update phase based on distance
            if (_currentPhase == MovementPhase.Approaching && distanceToDestination <= _formationPhaseDistance)
            {
                // Switch to formation phase when close enough
                _currentPhase = MovementPhase.Forming;
                Debug.Log($"NavigationComponent: Switching to Forming phase at distance {distanceToDestination}");
            }
            
            // Handle movement based on current phase
            if (_currentPhase == MovementPhase.Approaching)
            {
                // Continue using NavMeshAgent to get near the target
                if (_navMeshAgent.enabled && _isPathfindingActive)
                {
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.SetDestination(_destination);
                }
            }
            else // Forming phase
            {
                // Disable NavMeshAgent and move directly to formation position
                if (_navMeshAgent.enabled)
                {
                    _navMeshAgent.isStopped = true;
                }
                
                // Move directly to formation position
                MoveDirectlyToPosition(formationPosition);
            }
        }

        /// <summary>
        /// Di chuyển trực tiếp đến vị trí chỉ định, không qua NavMeshAgent
        /// </summary>
        private void MoveDirectlyToPosition(Vector3 targetPosition)
        {
            // Get current transform position
            Vector3 currentPosition = transform.position;
            
            // Calculate direction to target
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float distance = Vector3.Distance(currentPosition, targetPosition);
            
            // Calculate smooth movement
            float step = _directMovementSpeed * Time.deltaTime;
            
            // Move towards target
            if (distance > _stoppingDistance)
            {
                Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, step);
                transform.position = newPosition;
                
                // Smoothly rotate to face movement direction
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
                }
                
                // Update NavMeshAgent position if needed
                if (_navMeshAgent.enabled)
                {
                    _navMeshAgent.nextPosition = newPosition;
                }
            }
            else
            {
                // We've reached the formation position
                Debug.Log("NavigationComponent: Reached formation position");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw destination
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_destination, 0.2f);
            
            // Draw line to destination
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _destination);
            
            // Draw formation phase radius
            if (_useTwoPhaseMovement)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_destination, _formationPhaseDistance);
            }
            
            // Draw formation information if applicable
            if (_hasFormationInfo)
            {
                Gizmos.color = Color.red;
                Vector3 formationPosition = _squadCenter + _formationOffset;
                Gizmos.DrawSphere(formationPosition, 0.15f);
                Gizmos.DrawLine(_squadCenter, formationPosition);
            }
        }

        public void EnablePathfinding()
        {
            _isPathfindingActive = true;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = false;
            }
        }

        public void DisablePathfinding()
        {
            _isPathfindingActive = false;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        public List<Vector3> GetCurrentPath()
        {
            if (_navMeshAgent == null || !_navMeshAgent.hasPath || !_navMeshAgent.enabled)
                return _currentPath;
                
            // Convert NavMeshAgent path to a list of points
            NavMeshPath path = new NavMeshPath();
            _navMeshAgent.CalculatePath(_destination, path);
            
            _currentPath.Clear();
            foreach (var corner in path.corners)
            {
                _currentPath.Add(corner);
            }
            
            return _currentPath;
        }

        public override void Cleanup()
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = true;
            }
        }
        
        // Thêm phương thức để cập nhật thông tin đội hình nếu cần
        public void UpdateFormationOffset(Vector3 newOffset)
        {
            if (_hasFormationInfo)
            {
                _formationOffset = newOffset;
            }
        }
    }
    
    // Enum để định nghĩa các mức ưu tiên lệnh
    public enum NavigationCommandPriority
    {
        Low = 0,         // Ưu tiên thấp - dùng cho movement tự động, behavior thông thường
        Normal = 1,      // Ưu tiên trung bình - dùng cho formation và di chuyển squad
        High = 2,        // Ưu tiên cao - dùng cho lệnh từ người chơi
        Critical = 3     // Ưu tiên tối đa - dùng cho state đặc biệt như stun, knockback
    }
}