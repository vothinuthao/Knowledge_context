using UnityEngine;
using UnityEngine.AI;
using VikingRaven.Core.ECS;
using Sirenix.OdinInspector;

namespace VikingRaven.Units.Components
{
    public class NavigationComponent : BaseComponent
    {
        #region Basic Configuration

        [Title("Basic Navigation Settings")]
        [PropertyOrder(1)]
        [SerializeField] private NavMeshAgent _navMeshAgent;
        
        [PropertyOrder(2)]
        [Range(0.1f, 2f)]
        [Tooltip("Minimum distance to consider destination reached")]
        [SerializeField] private float _stoppingDistance = 0.1f;
        
        [PropertyOrder(3)]
        [Tooltip("Enable/disable pathfinding system")]
        [SerializeField] private bool _isPathfindingActive = true;

        #endregion

        #region Smart Movement Configuration

        [Title("Smart Movement System")]
        [PropertyOrder(10)]
        [InfoBox("Smart Movement allows units to use two-phase movement: first to leader area, then to exact formation position")]
        [SerializeField] private bool _enableSmartMovement = true;
        
        [PropertyOrder(11)]
        [ShowIf("_enableSmartMovement")]
        [Range(5f, 15f)]
        [Tooltip("Distance threshold to activate smart movement behavior")]
        [SerializeField] private float _squadProximityThreshold = 8f;
        
        [PropertyOrder(12)]
        [ShowIf("_enableSmartMovement")]
        [Range(3f, 10f)]
        [Tooltip("Radius around leader to switch to formation positioning")]
        [SerializeField] private float _formationActivationRadius = 6f;
        
        [PropertyOrder(13)]
        [ShowIf("_enableSmartMovement")]
        [Range(1f, 1.5f)]
        [Tooltip("Speed multiplier when following leader")]
        [SerializeField] private float _leaderMovementSpeedMultiplier = 1.2f;
        
        [PropertyOrder(14)]
        [ShowIf("_enableSmartMovement")]
        [Range(0.7f, 1.2f)]
        [Tooltip("Speed multiplier when moving to formation position")]
        [SerializeField] private float _formationMovementSpeedMultiplier = 0.9f;

        #endregion

        #region Priority Control

        [Title("Command Priority System")]
        [PropertyOrder(20)]
        [EnumToggleButtons]
        [ReadOnly]
        [SerializeField] private NavigationCommandPriority _currentCommandPriority = NavigationCommandPriority.Normal;
        
        [PropertyOrder(21)]
        [Range(0.5f, 5f)]
        [Tooltip("Time before command priority decays to lower level")]
        [SerializeField] private float _priorityDecayTime = 1.0f;
        
        [PropertyOrder(22)]
        [ReadOnly]
        [ShowInInspector]
        private float _lastCommandTime = 0f;

        #endregion

        #region Formation Information

        [Title("Formation Data")]
        [PropertyOrder(30)]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 _squadCenter;
        
        [PropertyOrder(31)]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 _formationOffset;
        
        [PropertyOrder(32)]
        [ReadOnly]
        [ShowInInspector]
        private bool _hasFormationInfo = false;
        
        [PropertyOrder(33)]
        [ReadOnly]
        [ShowInInspector]
        private bool _useFormationPosition = false;

        #endregion

        #region Movement State Tracking

        [Title("Movement State Debug")]
        [PropertyOrder(40)]
        [EnumToggleButtons]
        [ReadOnly]
        [ShowInInspector]
        private MovementPhase _currentMovementPhase = MovementPhase.DirectMovement;
        
        [PropertyOrder(41)]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 _destination;
        
        [PropertyOrder(42)]
        [ReadOnly]
        [ShowInInspector]
        private bool _hasReachedExactPosition = false;
        
        [PropertyOrder(43)]
        [ReadOnly]
        [ShowInInspector]
        private bool _isSquadLeader = false;
        
        [PropertyOrder(44)]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 _leaderPosition;
        
        [PropertyOrder(45)]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 _exactPosition;
        
        [PropertyOrder(46)]
        [ReadOnly]
        [ShowInInspector]
        private float _originalSpeed = 3.5f;

        #endregion

        #region NavMesh Agent Debug Info

        [Title("NavMesh Agent Debug")]
        [PropertyOrder(50)]
        [ShowIf("_navMeshAgent")]
        [ReadOnly]
        [ShowInInspector]
        private float CurrentSpeed => _navMeshAgent != null ? _navMeshAgent.speed : 0f;
        
        [PropertyOrder(51)]
        [ShowIf("_navMeshAgent")]
        [ReadOnly]
        [ShowInInspector]
        private float RemainingDistance => _navMeshAgent != null ? _navMeshAgent.remainingDistance : 0f;
        
        [PropertyOrder(52)]
        [ShowIf("_navMeshAgent")]
        [ReadOnly]
        [ShowInInspector]
        private bool IsPathPending => _navMeshAgent != null ? _navMeshAgent.pathPending : false;
        
        [PropertyOrder(53)]
        [ShowIf("_navMeshAgent")]
        [ReadOnly]
        [ShowInInspector]
        private bool IsOnNavMesh => _navMeshAgent != null ? _navMeshAgent.isOnNavMesh : false;
        
        [PropertyOrder(54)]
        [ShowIf("_navMeshAgent")]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 NavMeshVelocity => _navMeshAgent != null ? _navMeshAgent.velocity : Vector3.zero;

        #endregion

        #region Distance Debug Info

        [Title("Distance Debug")]
        [PropertyOrder(60)]
        [ReadOnly]
        [ShowInInspector]
        private float DistanceToDestination => _destination != Vector3.zero ? Vector3.Distance(transform.position, _destination) : 0f;
        
        [PropertyOrder(61)]
        [ReadOnly]
        [ShowInInspector]
        private float DistanceToSquadCenter => _hasFormationInfo ? Vector3.Distance(transform.position, _squadCenter) : 0f;
        
        [PropertyOrder(62)]
        [ReadOnly]
        [ShowInInspector]
        private float DistanceToLeader => _leaderPosition != Vector3.zero ? Vector3.Distance(transform.position, _leaderPosition) : 0f;
        
        [PropertyOrder(63)]
        [ReadOnly]
        [ShowInInspector]
        private float DistanceToFormationPosition => _hasFormationInfo ? Vector3.Distance(transform.position, _squadCenter + _formationOffset) : 0f;

        #endregion

        #region Properties - BACKWARD COMPATIBLE

        public Vector3 Destination => _destination;
        public bool IsPathfindingActive => _isPathfindingActive;
        public bool HasReachedDestination => _hasReachedExactPosition;
        public NavigationCommandPriority CurrentCommandPriority => _currentCommandPriority;
        
        // NEW PROPERTIES
        public MovementPhase CurrentMovementPhase => _currentMovementPhase;
        public bool IsSquadLeader => _isSquadLeader;
        public Vector3 LeaderPosition => _leaderPosition;

        #endregion

        #region Initialization

        public override void Initialize()
        {
            if (_navMeshAgent == null)
            {
                _navMeshAgent = GetComponent<NavMeshAgent>();
            }
            
            if (_navMeshAgent != null)
            {
                _navMeshAgent.stoppingDistance = _stoppingDistance;
                _originalSpeed = _navMeshAgent.speed;
                ConfigureNavMeshAgent();
            }
            
            DetermineLeadershipStatus();
        }

        private void ConfigureNavMeshAgent()
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
                _navMeshAgent.avoidancePriority = 50;
            }
        }

        private void DetermineLeadershipStatus()
        {
            var formationComponent = Entity?.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                _isSquadLeader = formationComponent.FormationSlotIndex == 0 || 
                               formationComponent.FormationRole == FormationRole.Leader;
            }
            else
            {
                _isSquadLeader = false;
            }
        }

        #endregion

        #region CORE METHOD: UpdatePathfinding - ENHANCED

        void Update()
        {
            UpdatePathfinding();
        }
        public void UpdatePathfinding()
        {
            if (!IsActive || _navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
                return;

            if (_hasReachedExactPosition)
                return;
            UpdatePriorityDecay();
            if (_enableSmartMovement && !_isSquadLeader)
            {
                UpdateSmartFollowerMovement();
            }
            else
            {
                // LEADERS: Use direct movement
                UpdateDirectMovement();
            }

            // Check and update destination reached status
            UpdateDestinationReachedStatus();
        }

        /// <summary>
        /// Update priority decay over time
        /// </summary>
        private void UpdatePriorityDecay()
        {
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            if (timeSinceLastCommand > _priorityDecayTime && _currentCommandPriority > NavigationCommandPriority.Low)
            {
                _currentCommandPriority = (NavigationCommandPriority)((int)_currentCommandPriority - 1);
                _lastCommandTime = Time.time;
            }
        }

        /// <summary>
        /// SMART MOVEMENT: Two-phase movement system for followers
        /// Phase 1: Move to leader area (pathfinding)
        /// Phase 2: Move to exact formation position when near squad
        /// </summary>
        private void UpdateSmartFollowerMovement()
        {
            // Update movement phase based on current situation
            UpdateMovementPhase();

            // Execute movement based on current phase
            ExecutePhaseMovement();
        }

        /// <summary>
        /// Update current movement phase based on position and squad state
        /// </summary>
        private void UpdateMovementPhase()
        {
            Vector3 currentPosition = transform.position;
            float distanceToSquadCenter = Vector3.Distance(currentPosition, _squadCenter);
            float distanceToLeader = Vector3.Distance(currentPosition, _leaderPosition);

            switch (_currentMovementPhase)
            {
                case MovementPhase.DirectMovement:
                    // Check if we should use smart movement
                    if (_hasFormationInfo && _leaderPosition != Vector3.zero && 
                        distanceToSquadCenter > _squadProximityThreshold)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToLeader);
                    }
                    else if (_hasFormationInfo && distanceToSquadCenter <= _squadProximityThreshold)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToFormation);
                    }
                    break;

                case MovementPhase.MoveToLeader:
                    // Switch to formation positioning when near squad area
                    if (_hasFormationInfo && distanceToSquadCenter <= _squadProximityThreshold)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToFormation);
                    }
                    // If very close to leader, also switch to formation
                    else if (distanceToLeader <= _formationActivationRadius)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToFormation);
                    }
                    break;

                case MovementPhase.MoveToFormation:
                    // Stay in formation mode unless very far from squad
                    if (!_hasFormationInfo || distanceToSquadCenter > _squadProximityThreshold * 1.5f)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToLeader);
                    }
                    // Check if reached formation position
                    else if (_hasFormationInfo)
                    {
                        Vector3 formationPosition = _squadCenter + _formationOffset;
                        float distanceToFormation = Vector3.Distance(currentPosition, formationPosition);
                        
                        if (distanceToFormation <= _stoppingDistance * 1.5f)
                        {
                            ChangeMovementPhase(MovementPhase.InFormation);
                        }
                    }
                    break;

                case MovementPhase.InFormation:
                    // Stay in formation unless far from squad or new high priority command
                    if (!_hasFormationInfo || distanceToSquadCenter > _squadProximityThreshold)
                    {
                        ChangeMovementPhase(MovementPhase.MoveToLeader);
                    }
                    else
                    {
                        // Maintain exact formation position
                        MaintainFormationPosition();
                    }
                    break;
            }
        }

        /// <summary>
        /// Execute movement based on current phase
        /// </summary>
        private void ExecutePhaseMovement()
        {
            switch (_currentMovementPhase)
            {
                case MovementPhase.DirectMovement:
                    ExecuteDirectMovement();
                    break;

                case MovementPhase.MoveToLeader:
                    ExecuteLeaderFollowingMovement();
                    break;

                case MovementPhase.MoveToFormation:
                    ExecuteFormationMovement();
                    break;

                case MovementPhase.InFormation:
                    // Formation position maintenance handled in UpdateMovementPhase
                    break;
            }
        }

        /// <summary>
        /// Execute movement towards leader position (Phase 1)
        /// </summary>
        private void ExecuteLeaderFollowingMovement()
        {
            if (_leaderPosition == Vector3.zero) 
            {
                ExecuteDirectMovement();
                return;
            }

            // Calculate approach position near leader
            Vector3 leaderApproachPosition = CalculateLeaderApproachPosition();

            // Set movement speed for leader following
            SetMovementSpeed(_originalSpeed * _leaderMovementSpeedMultiplier);

            // Move to leader approach position
            if (Vector3.Distance(_navMeshAgent.destination, leaderApproachPosition) > 1f)
            {
                SetNavMeshDestination(leaderApproachPosition);
            }
        }

        /// <summary>
        /// Execute movement to exact formation position (Phase 2)
        /// </summary>
        private void ExecuteFormationMovement()
        {
            if (!_hasFormationInfo) 
            {
                ExecuteDirectMovement();
                return;
            }

            // Calculate exact formation position
            Vector3 formationPosition = _squadCenter + _formationOffset;

            // Set movement speed for formation positioning
            SetMovementSpeed(_originalSpeed * _formationMovementSpeedMultiplier);

            // Move to exact formation position
            if (Vector3.Distance(_navMeshAgent.destination, formationPosition) > 0.2f)
            {
                SetNavMeshDestination(formationPosition);
                _exactPosition = formationPosition;
            }
        }

        /// <summary>
        /// Execute direct movement (for leaders and basic movement)
        /// </summary>
        private void ExecuteDirectMovement()
        {
            if (_destination == Vector3.zero) return;

            // Reset to normal speed
            SetMovementSpeed(_originalSpeed);

            // Move directly to destination
            if (Vector3.Distance(_navMeshAgent.destination, _destination) > 0.5f)
            {
                SetNavMeshDestination(_destination);
            }
        }

        /// <summary>
        /// Direct movement update for leaders and fallback
        /// </summary>
        private void UpdateDirectMovement()
        {
            // Formation position handling for direct movement
            if (_useFormationPosition && _hasFormationInfo)
            {
                _exactPosition = _squadCenter + _formationOffset;
                
                // Update destination to formation position
                if (Vector3.Distance(_destination, _exactPosition) > 0.1f)
                {
                    _destination = _exactPosition;
                    SetNavMeshDestination(_exactPosition);
                }
                
                // Check if very close to formation position
                if (Vector3.Distance(transform.position, _exactPosition) < _stoppingDistance * 1.5f)
                {
                    LockIntoExactPosition(_exactPosition);
                }
            }
            else if (_destination != Vector3.zero)
            {
                // Regular destination movement
                SetNavMeshDestination(_destination);
                
                // Check if reached destination
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.1f)
                {
                    _hasReachedExactPosition = true;
                }
            }
        }

        /// <summary>
        /// Calculate approach position near leader
        /// </summary>
        private Vector3 CalculateLeaderApproachPosition()
        {
            if (_hasFormationInfo && _formationOffset != Vector3.zero)
            {
                // Calculate position offset towards formation direction
                Vector3 formationDirection = _formationOffset.normalized;
                Vector3 approachOffset = formationDirection * (_formationActivationRadius * 0.8f);
                return _leaderPosition + approachOffset;
            }
            else
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-1.5f, 1.5f), 
                    0, 
                    Random.Range(-2f, 0f)
                );
                return _leaderPosition + randomOffset;
            }
        }
        private void MaintainFormationPosition()
        {
            if (!_hasFormationInfo) return;

            Vector3 formationPosition = _squadCenter + _formationOffset;
            float distanceFromFormation = Vector3.Distance(transform.position, formationPosition);

            if (distanceFromFormation > _stoppingDistance * 2f)
            {
                ChangeMovementPhase(MovementPhase.MoveToFormation);
            }
            else if (distanceFromFormation < _stoppingDistance)
            {
                LockIntoExactPosition(formationPosition);
            }
        }

        private void ChangeMovementPhase(MovementPhase newPhase)
        {
            if (_currentMovementPhase != newPhase)
            {
                MovementPhase oldPhase = _currentMovementPhase;
                _currentMovementPhase = newPhase;

                // Reset exact position reached when changing phase
                if (newPhase != MovementPhase.InFormation)
                {
                    _hasReachedExactPosition = false;
                }

                // Reactivate agent when starting movement phases
                if (newPhase == MovementPhase.MoveToLeader || newPhase == MovementPhase.MoveToFormation)
                {
                    ReactivateNavMeshAgent();
                }
            }
        }
        private void UpdateDestinationReachedStatus()
        {
            if (_currentMovementPhase == MovementPhase.InFormation)
            {
                _hasReachedExactPosition = true;
                return;
            }

            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.1f)
            {
                switch (_currentMovementPhase)
                {
                    case MovementPhase.MoveToLeader:
                        break;
                        
                    case MovementPhase.MoveToFormation:
                        if (_hasFormationInfo)
                        {
                            Vector3 formationPosition = _squadCenter + _formationOffset;
                            float distance = Vector3.Distance(transform.position, formationPosition);
                            if (distance <= _stoppingDistance * 1.5f)
                            {
                                ChangeMovementPhase(MovementPhase.InFormation);
                            }
                        }
                        break;
                        
                    case MovementPhase.DirectMovement:
                        _hasReachedExactPosition = true;
                        break;
                }
            }
        }

        private void LockIntoExactPosition(Vector3 position)
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.updatePosition = false;
                _navMeshAgent.velocity = Vector3.zero;
            }
            
            transform.position = position;
            _hasReachedExactPosition = true;
            _exactPosition = position;
        }

        #endregion

        #region BACKWARD COMPATIBLE PUBLIC INTERFACE

        public void SetDestination(Vector3 destination, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            float timeSinceLastCommand = Time.time - _lastCommandTime;
            bool priorityDecayed = timeSinceLastCommand > _priorityDecayTime;

            if (priority >= _currentCommandPriority || priorityDecayed)
            {
                _hasReachedExactPosition = false;

                ReactivateNavMeshAgent();

                _destination = destination;
                _currentCommandPriority = priority;
                _lastCommandTime = Time.time;
                _useFormationPosition = false;

                // Update leader position for smart movement
                if (_enableSmartMovement && !_isSquadLeader)
                {
                    _leaderPosition = destination;
                    _currentMovementPhase = MovementPhase.MoveToLeader;
                }
                else
                {
                    // Direct movement for leaders
                    _currentMovementPhase = MovementPhase.DirectMovement;
                    if (_isPathfindingActive && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
                    {
                        _navMeshAgent.stoppingDistance = _stoppingDistance;
                        _navMeshAgent.SetDestination(destination);
                    }
                }
            }
        }

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
                ReactivateNavMeshAgent();

                // For smart movement, determine appropriate phase
                if (_enableSmartMovement && !_isSquadLeader)
                {
                    Vector3 currentPosition = transform.position;
                    float distanceToSquadCenter = Vector3.Distance(currentPosition, squadCenter);
                    
                    if (distanceToSquadCenter <= _squadProximityThreshold)
                    {
                        _currentMovementPhase = MovementPhase.MoveToFormation;
                    }
                    else if (_leaderPosition != Vector3.zero)
                    {
                        _currentMovementPhase = MovementPhase.MoveToLeader;
                    }
                    else
                    {
                        _currentMovementPhase = MovementPhase.DirectMovement;
                        SetDestination(_exactPosition, priority);
                    }
                }
                else
                {
                    // Direct movement to formation position
                    _currentMovementPhase = MovementPhase.DirectMovement;
                    SetDestination(_exactPosition, priority);
                }
            }
        }

        /// <summary>
        /// Update formation offset (backward compatibility)
        /// </summary>
        public void UpdateFormationOffset(Vector3 newOffset)
        {
            if (_hasFormationInfo)
            {
                _formationOffset = newOffset;
                _exactPosition = _squadCenter + _formationOffset;

                // If already at locked position, update immediately
                if (_hasReachedExactPosition || _currentMovementPhase == MovementPhase.InFormation)
                {
                    _hasReachedExactPosition = false;
                    
                    if (_enableSmartMovement && !_isSquadLeader)
                    {
                        _currentMovementPhase = MovementPhase.MoveToFormation;
                    }
                    else
                    {
                        SetDestination(_exactPosition, _currentCommandPriority);
                    }
                }
            }
        }

        /// <summary>
        /// Enable pathfinding
        /// </summary>
        public void EnablePathfinding()
        {
            _isPathfindingActive = true;
            ReactivateNavMeshAgent();
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

        #endregion

        #region NEW PUBLIC INTERFACE

        /// <summary>
        /// Update leader position for followers to track
        /// </summary>
        public void UpdateLeaderPosition(Vector3 leaderPosition)
        {
            _leaderPosition = leaderPosition;
            
            // If we're following leader and position changed significantly, restart movement
            if (_currentMovementPhase == MovementPhase.MoveToLeader && 
                Vector3.Distance(_leaderPosition, leaderPosition) > 2f)
            {
                _hasReachedExactPosition = false;
            }
        }

        /// <summary>
        /// Force specific movement phase
        /// </summary>
        public void ForceMovementPhase(MovementPhase phase)
        {
            ChangeMovementPhase(phase);
        }

        /// <summary>
        /// Enable/disable smart movement
        /// </summary>
        public void SetSmartMovementEnabled(bool enabled)
        {
            if (_enableSmartMovement != enabled)
            {
                _enableSmartMovement = enabled;
                
                // Reset to appropriate phase
                if (!enabled)
                {
                    _currentMovementPhase = MovementPhase.DirectMovement;
                }
                
                Debug.Log($"Unit {Entity?.Id}: Smart movement {(enabled ? "enabled" : "disabled")}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Reactivate NavMeshAgent for movement
        /// </summary>
        private void ReactivateNavMeshAgent()
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.updatePosition = true;
                _navMeshAgent.updateRotation = true;
            }
        }

        /// <summary>
        /// Set NavMeshAgent destination safely
        /// </summary>
        private void SetNavMeshDestination(Vector3 destination)
        {
            if (_isPathfindingActive && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.stoppingDistance = _stoppingDistance;
                _navMeshAgent.SetDestination(destination);
            }
        }

        /// <summary>
        /// Set movement speed
        /// </summary>
        private void SetMovementSpeed(float speed)
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = speed;
            }
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        #endregion
    }
}