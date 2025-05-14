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
        
        // Priority control variables
        [SerializeField] private NavigationCommandPriority _currentCommandPriority = NavigationCommandPriority.Normal;
        [SerializeField] private float _priorityDecayTime = 1.0f;
        private float _lastCommandTime = 0f;
        
        // Formation information
        private Vector3 _squadCenter;
        private Vector3 _formationOffset;
        private bool _hasFormationInfo = false;
        private bool _useFormationPosition = false;
        
        // Position control
        private bool _hasReachedExactPosition = false;
        private Vector3 _exactPosition;
        
        // Properties for external access
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
            }
            
            if (_navMeshAgent != null)
            {
                _navMeshAgent.stoppingDistance = _stoppingDistance;
                ConfigureNavMeshAgent();
            }
            else
            {
                Debug.LogError("NavigationComponent: NavMeshAgent not found on entity: " + Entity?.Id);
            }
        }
        
        /// <summary>
        /// Configure NavMeshAgent with optimized settings
        /// </summary>
        private void ConfigureNavMeshAgent()
        {
            if (_navMeshAgent == null) return;
            
            // Basic settings
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            _navMeshAgent.radius = 0.4f;
            _navMeshAgent.autoRepath = true;
            
            // Squad-specific avoidance
            var formationComponent = Entity?.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                int squadId = formationComponent.SquadId;
                _navMeshAgent.avoidancePriority = 50 + (squadId % 10);
            }
            else
            {
                _navMeshAgent.avoidancePriority = 50;
            }
        }

        /// <summary>
        /// Set destination for the unit with priority handling
        /// </summary>
        public void SetDestination(Vector3 destination, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            bool priorityDecayed = timeSinceLastCommand > _priorityDecayTime;
            
            // Only accept command if higher priority or previous priority has decayed
            if (priority >= _currentCommandPriority || priorityDecayed)
            {
                // Reset position lock state
                _hasReachedExactPosition = false;
                
                // Reactivate NavMeshAgent
                if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
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
            }
        }

        /// <summary>
        /// Set formation information for positioning in squad
        /// </summary>
        public void SetFormationInfo(Vector3 squadCenter, Vector3 formationOffset, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            // Store formation information
            _squadCenter = squadCenter;
            _formationOffset = formationOffset;
            _hasFormationInfo = true;
            _useFormationPosition = true;
            
            // Calculate exact formation position in world space
            _exactPosition = _squadCenter + formationOffset;
            
            // Only update destination if priority allows
            if (priority >= _currentCommandPriority)
            {
                // Reset position lock state
                _hasReachedExactPosition = false;
                _currentCommandPriority = priority;
                _lastCommandTime = Time.time;
                
                // Reactivate NavMeshAgent
                if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.updatePosition = true;
                    _navMeshAgent.updateRotation = true;
                }
                
                // Set destination to formation position
                SetDestination(_exactPosition, priority);
            }
        }

        void Update()
        {
            UpdatePathfinding();
        }

        /// <summary>
        /// Update pathfinding - original method retained for compatibility
        /// </summary>
        public void UpdatePathfinding()
        {
            if (!IsActive || _navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
                return;
    
            // If already at exact position, do nothing
            if (_hasReachedExactPosition)
                return;
    
            // Reduce priority over time
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            if (timeSinceLastCommand > _priorityDecayTime && _currentCommandPriority > NavigationCommandPriority.Low)
            {
                _currentCommandPriority = (NavigationCommandPriority)((int)_currentCommandPriority - 1);
                _lastCommandTime = Time.time;
            }
    
            // Check if we're near destination
            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
            {
                // Formation position handling
                if (_useFormationPosition && _hasFormationInfo)
                {
                    // Update exact position for current formation info
                    _exactPosition = _squadCenter + _formationOffset;
            
                    // When very close to destination, lock into exact position
                    if (Vector3.Distance(transform.position, _exactPosition) < 0.2f)
                    {
                        // Stop the agent and snap to exact position
                        _navMeshAgent.isStopped = true;
                        _navMeshAgent.updatePosition = false;
                        _navMeshAgent.velocity = Vector3.zero;
                        transform.position = _exactPosition;
                        _hasReachedExactPosition = true;
                    }
                }
                else
                {
                    _navMeshAgent.isStopped = true;
                    _hasReachedExactPosition = true;
                }
            }
        }

        /// <summary>
        /// Enable pathfinding
        /// </summary>
        public void EnablePathfinding()
        {
            _isPathfindingActive = true;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = false;
            }
        }

        /// <summary>
        /// Disable pathfinding
        /// </summary>
        public void DisablePathfinding()
        {
            _isPathfindingActive = false;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        /// <summary>
        /// Update formation offset
        /// </summary>
        public void UpdateFormationOffset(Vector3 newOffset)
        {
            if (_hasFormationInfo)
            {
                _formationOffset = newOffset;
                _exactPosition = _squadCenter + _formationOffset;
                
                // If already at locked position, update immediately
                if (_hasReachedExactPosition)
                {
                    // Unlock, move to new position
                    _hasReachedExactPosition = false;
                    SetDestination(_exactPosition, _currentCommandPriority);
                }
            }
        }
        
        /// <summary>
        /// Unlock position to allow movement
        /// </summary>
        public void UnlockPosition()
        {
            _hasReachedExactPosition = false;
            
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.updatePosition = true;
                _navMeshAgent.updateRotation = true;
            }
        }
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }
    }
    
    /// <summary>
    /// Enum defining priority levels for movement commands
    /// </summary>
    public enum NavigationCommandPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}