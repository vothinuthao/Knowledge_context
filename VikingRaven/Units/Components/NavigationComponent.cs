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
        
        // Precise position control variables
        private bool _hasReachedExactPosition = false;
        private Vector3 _exactPosition;
        
        // New fields for enhanced formation movement
        [SerializeField] private bool _ignoreSquadMemberCollision = true;  // Ignore collision between squad members
        [SerializeField] private int _avoidanceLayer = -1;                // Avoidance layer (default: all)
        [SerializeField] private float _squadCohesionFactor = 0.5f;      // Cohesion factor with the squad

        private bool _isFollowingLeader = false;                         // Flag if following leader
        private IEntity _squadLeader = null;                             // Reference to squad leader
        private Vector3 _formationDirectionOffset = Vector3.zero;        // Direction offset in formation
        
        // Properties for access
        public Vector3 Destination => _destination;
        public bool IsPathfindingActive => _isPathfindingActive;
        public bool HasReachedDestination => _hasReachedExactPosition;
        public NavigationCommandPriority CurrentCommandPriority => _currentCommandPriority;
        
        private Vector3 _destination;

        public override void Initialize()
        {
            _navMeshAgent.stoppingDistance = _stoppingDistance;
            ConfigureObstacleAvoidance();
            ConfigureFormationMovement();
        }
        
        /// <summary>
        /// Configure obstacle avoidance for NavMeshAgent
        /// </summary>
        private void ConfigureObstacleAvoidance()
        {
            if (_navMeshAgent == null) return;
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            _navMeshAgent.radius = 0.4f;
            _navMeshAgent.autoRepath = true;
            var formationComponent = Entity?.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                int squadId = formationComponent.SquadId;
                _navMeshAgent.avoidancePriority = 50 + (squadId % 10);
            }
            else
            {
                _navMeshAgent.avoidancePriority = 50 + Random.Range(0, 10);
            }
        }

        /// <summary>
        /// Configure formation movement parameters
        /// </summary>
        private void ConfigureFormationMovement()
        {
            if (_navMeshAgent == null) return;
            
            // Get information about the squad
            var formationComponent = Entity?.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                int squadId = formationComponent.SquadId;
                
                // Set special avoidance parameters for units in the same squad
                if (_ignoreSquadMemberCollision)
                {
                    // Higher number = lower avoidance priority
                    _navMeshAgent.avoidancePriority = 50 + (squadId % 10);
                }
                
                // Check if this is the squad leader (slot 0)
                _isFollowingLeader = formationComponent.FormationSlotIndex > 0;
            }
            
            // Adjust NavMeshAgent parameters for formation movement
            _navMeshAgent.acceleration = 10f; // Increase acceleration and deceleration
            _navMeshAgent.angularSpeed = 360f; // Increase rotation speed
        }

        /// <summary>
        /// Set destination for the unit
        /// </summary>
        public void SetDestination(Vector3 destination, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            bool priorityDecayed = timeSinceLastCommand > _priorityDecayTime;
            
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
                    
                    // Apply speed adjustments if moving in formation
                    if (_isFollowingLeader)
                    {
                        float distanceToDestination = Vector3.Distance(transform.position, destination);
                        
                        // If too far from leader, increase speed to catch up
                        if (distanceToDestination > 3.0f)
                        {
                            _navMeshAgent.speed *= 1.2f;
                        }
                        else
                        {
                            _navMeshAgent.speed = 3.0f; // Reset to default speed
                        }
                    }
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
                
                Debug.Log($"NavigationComponent: Set destination {destination}, priority: {priority}");
            }
        }

        /// <summary>
        /// Set information about position in formation
        /// </summary>
        public void SetFormationInfo(Vector3 squadCenter, Vector3 formationOffset, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            // Store formation information regardless of setting destination
            _squadCenter = squadCenter;
            _formationOffset = formationOffset;
            _hasFormationInfo = true;
            _useFormationPosition = true;
            
            // Calculate exact formation position
            _exactPosition = _squadCenter + _formationOffset;
            
            // Calculate direction offset for use when reaching destination
            if (formationOffset.magnitude > 0.01f)
            {
                _formationDirectionOffset = formationOffset.normalized;
            }
            
            if (priority >= _currentCommandPriority)
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
                
                // Set destination to formation position
                SetDestination(_exactPosition, priority);
                
                Debug.Log($"NavigationComponent: Set formation info (center: {squadCenter}, offset: {formationOffset})");
            }
        }

        void Update()
        {
            UpdatePathfinding();
        }

        /// <summary>
        /// Update movement and handle precise positioning
        /// </summary>
        // In NavigationComponent.cs - UpdatePathfinding method

        public void UpdatePathfinding()
        {
            if (!IsActive || _navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
                return;
    
            // If already at exact position, don't do anything further
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
                    else
                    {
                        // Move directly to exact position
                        transform.position = Vector3.MoveTowards(
                            transform.position, 
                            _exactPosition, 
                            Time.deltaTime * _navMeshAgent.speed * 0.5f);
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
        /// Handle following leader behavior
        /// </summary>
        private void HandleFollowLeader()
        {
            if (_squadLeader == null)
                return;
                
            var leaderTransform = _squadLeader.GetComponent<TransformComponent>();
            if (leaderTransform == null)
                return;
                
            // Get leader position
            Vector3 leaderPosition = leaderTransform.Position;
            
            // Calculate distance to leader
            float distanceToLeader = Vector3.Distance(transform.position, leaderPosition);
            
            // Calculate target position based on leader and offset
            Vector3 targetPosition = leaderPosition + _formationOffset;
            
            // If too far from leader, increase speed to catch up
            if (distanceToLeader > 4.0f)
            {
                // Update destination directly to new position
                _navMeshAgent.SetDestination(targetPosition);
                
                // Increase speed to catch up
                _navMeshAgent.speed = 5.0f;
            }
            else if (distanceToLeader > 2.0f)
            {
                // At moderate distance, move normally
                _navMeshAgent.SetDestination(targetPosition);
                _navMeshAgent.speed = 3.5f;
            }
            else
            {
                // Already close to leader, maintain normal speed
                _navMeshAgent.SetDestination(targetPosition);
                _navMeshAgent.speed = 3.0f;
            }
            
            // Update exactPosition
            _exactPosition = targetPosition;
        }

        /// <summary>
        /// Set the squad leader for this unit
        /// </summary>
        public void SetSquadLeader(IEntity leader)
        {
            _squadLeader = leader;
            
            // Update state based on whether there is a leader
            _isFollowingLeader = (_squadLeader != null && _squadLeader != Entity);
        }

        /// <summary>
        /// Set formation direction offset
        /// </summary>
        public void SetFormationDirectionOffset(Vector3 directionOffset)
        {
            _formationDirectionOffset = directionOffset;
        }

        /// <summary>
        /// Temporarily disable avoidance between squad members
        /// </summary>
        public void DisableSquadMemberAvoidance(bool disable)
        {
            if (_navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
                return;
                
            if (disable)
            {
                // Save current avoidance layer
                _avoidanceLayer = _navMeshAgent.avoidancePriority;
                
                // Set low avoidance priority (high number = low priority)
                _navMeshAgent.avoidancePriority = 99;
            }
            else
            {
                // Restore original avoidance layer
                if (_avoidanceLayer >= 0)
                {
                    _navMeshAgent.avoidancePriority = _avoidanceLayer;
                }
                else
                {
                    _navMeshAgent.avoidancePriority = 50;
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
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
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
            
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.updatePosition = true;
                _navMeshAgent.updateRotation = true;
            }
        }
    }
    
    /// <summary>
    /// Enum defining priority levels for movement commands
    /// </summary>
    public enum NavigationCommandPriority
    {
        Low = 0,         // Low priority - used for automatic movement, normal behaviors
        Normal = 1,      // Medium priority - used for formation and squad movement
        High = 2,        // High priority - used for player commands
        Critical = 3     // Maximum priority - used for special states like stun, knockback
    }
}