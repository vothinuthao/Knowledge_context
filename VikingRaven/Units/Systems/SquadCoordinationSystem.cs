using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Enhanced Squad Coordination System with smart movement integration
    /// ENHANCED: Integrates with NavigationComponent's smart movement phases
    /// NEW FEATURES: Leader-follower coordination, smart movement statistics
    /// BACKWARD COMPATIBLE: All existing methods still work
    /// </summary>
    [SystemPriority(SystemPriority.High)]
    public class SquadCoordinationSystem : BaseSystem
    {
        #region System Configuration

        [TitleGroup("System Settings")]
        [Tooltip("System execution priority")]
        [SerializeField, Range(100, 300)] 
        private int _systemPriority = 200;
        
        [TitleGroup("System Settings")]
        [Tooltip("Enable debug logging for squad commands")]
        [SerializeField, ToggleLeft] 
        private bool _enableDebugLogging = false;
        
        [TitleGroup("System Settings")]
        [Tooltip("Update frequency for squad state monitoring")]
        [SerializeField, Range(1, 10)] 
        private int _updateFrequency = 5;

        #endregion

        #region Smart Movement Configuration

        [TitleGroup("Smart Movement Integration")]
        [Tooltip("Enable smart movement coordination")]
        [SerializeField, ToggleLeft]
        private bool _enableSmartMovementCoordination = true;
        
        [TitleGroup("Smart Movement Integration")]
        [Tooltip("Auto-update leader positions for followers")]
        [SerializeField, ToggleLeft]
        private bool _autoUpdateLeaderPositions = true;
        
        [TitleGroup("Smart Movement Integration")]
        [Tooltip("Leader position update frequency")]
        [SerializeField, Range(0.1f, 2f)]
        private float _leaderPositionUpdateInterval = 0.5f;

        #endregion

        #region Squad Management Data

        // Track active squads and their states
        private Dictionary<int, EnhancedSquadState> _squadStates = new Dictionary<int, EnhancedSquadState>();
        
        // Command queues
        private Queue<FormationCommand> _formationCommands = new Queue<FormationCommand>();
        private Queue<MovementCommand> _movementCommands = new Queue<MovementCommand>();
        private Queue<SmartMovementCommand> _smartMovementCommands = new Queue<SmartMovementCommand>();
        
        // Update timing
        private int _frameCounter = 0;
        private float _lastLeaderPositionUpdate = 0f;
        
        // Performance tracking
        private int _totalCommandsProcessed = 0;
        private int _smartMovementCommandsProcessed = 0;

        #endregion

        #region Dependencies

        private EntityRegistry _entityRegistry;
        private FormationSystem _formationSystem;

        #endregion

        #region System Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            
            _entityRegistry = EntityRegistry.Instance;
            if (_entityRegistry == null)
            {
                Debug.LogError("SquadCoordinationSystem: EntityRegistry not found!");
                return;
            }
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogWarning("SquadCoordinationSystem: FormationSystem not found - limited functionality");
            }
            
            Priority = _systemPriority;
            
            Debug.Log($"SquadCoordinationSystem: Initialized with smart movement integration, priority {Priority}");
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
            
            // Enhanced execution pipeline
            ProcessFormationCommands();
            ProcessMovementCommands();
            ProcessSmartMovementCommands();
            UpdateSquadStates();
            
            // Auto-update leader positions
            if (_enableSmartMovementCoordination && _autoUpdateLeaderPositions)
            {
                UpdateLeaderPositions();
            }
        }

        #endregion

        #region Enhanced Command Processing

        /// <summary>
        /// Process queued formation commands with smart movement integration
        /// </summary>
        private void ProcessFormationCommands()
        {
            while (_formationCommands.Count > 0)
            {
                var command = _formationCommands.Dequeue();
                ExecuteEnhancedFormationCommand(command);
                _totalCommandsProcessed++;
            }
        }

        /// <summary>
        /// Process queued movement commands
        /// </summary>
        private void ProcessMovementCommands()
        {
            while (_movementCommands.Count > 0)
            {
                var command = _movementCommands.Dequeue();
                ExecuteEnhancedMovementCommand(command);
                _totalCommandsProcessed++;
            }
        }

        /// <summary>
        /// Process queued smart movement commands
        /// NEW FEATURE: Smart movement command processing
        /// </summary>
        private void ProcessSmartMovementCommands()
        {
            while (_smartMovementCommands.Count > 0)
            {
                var command = _smartMovementCommands.Dequeue();
                ExecuteSmartMovementCommand(command);
                _smartMovementCommandsProcessed++;
                _totalCommandsProcessed++;
            }
        }

        /// <summary>
        /// ENHANCED: Formation command execution with smart movement coordination
        /// </summary>
        private void ExecuteEnhancedFormationCommand(FormationCommand command)
        {
            var squadMembers = GetSquadMembers(command.SquadId);
            if (squadMembers.Count == 0)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning($"SquadCoordinationSystem: No members found for squad {command.SquadId}");
                return;
            }
            
            // Update formation type for all squad members
            foreach (var entity in squadMembers)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationType(command.FormationType, command.SmoothTransition);
                }

                // ENHANCED: Configure smart movement for formation changes
                if (_enableSmartMovementCoordination)
                {
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.SetSmartMovementEnabled(true);
                    }
                }
            }
            
            // Update squad state
            UpdateSquadStateFormation(command.SquadId, command.FormationType);
            
            // Notify FormationSystem if available
            if (_formationSystem != null)
            {
                _formationSystem.UpdateSquadFormation(command.SquadId, command.FormationType);
            }
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Squad {command.SquadId} formation changed to {command.FormationType} with smart movement");
            }
        }

        /// <summary>
        /// ENHANCED: Movement command execution with smart movement support
        /// </summary>
        private void ExecuteEnhancedMovementCommand(MovementCommand command)
        {
            var squadMembers = GetSquadMembers(command.SquadId);
            if (squadMembers.Count == 0)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning($"SquadCoordinationSystem: No members found for squad {command.SquadId}");
                return;
            }

            // ENHANCED: Identify leader and followers for smart movement
            IEntity leader = null;
            List<IEntity> followers = new List<IEntity>();

            foreach (var entity in squadMembers)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    if (formationComponent.FormationSlotIndex == 0 || 
                        formationComponent.FormationRole == FormationRole.Leader)
                    {
                        leader = entity;
                    }
                    else
                    {
                        followers.Add(entity);
                    }
                }
            }

            // SMART MOVEMENT: Handle leader and followers differently
            if (leader != null && _enableSmartMovementCoordination)
            {
                ExecuteSmartCoordinatedMovement(command, leader, followers);
            }
            else
            {
                // FALLBACK: Traditional formation movement
                ExecuteTraditionalMovement(command, squadMembers);
            }

            // Update squad state
            UpdateSquadStateMovement(command.SquadId, command.TargetPosition, command.TargetRotation);

            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Squad {command.SquadId} executing movement to {command.TargetPosition}");
            }
        }

        /// <summary>
        /// NEW: Execute smart coordinated movement
        /// </summary>
        private void ExecuteSmartCoordinatedMovement(MovementCommand command, IEntity leader, List<IEntity> followers)
        {
            // Leader moves directly to target position
            var leaderNavigation = leader.GetComponent<NavigationComponent>();
            if (leaderNavigation != null)
            {
                leaderNavigation.SetDestination(command.TargetPosition, NavigationCommandPriority.High);
                
                if (_enableDebugLogging)
                    Debug.Log($"SquadCoordinationSystem: Leader {leader.Id} moving directly to {command.TargetPosition}");
            }

            // Update leader position for all followers
            foreach (var follower in followers)
            {
                var followerNavigation = follower.GetComponent<NavigationComponent>();
                if (followerNavigation != null)
                {
                    // SMART MOVEMENT: Followers will use two-phase movement
                    followerNavigation.UpdateLeaderPosition(command.TargetPosition);
                    followerNavigation.SetDestination(command.TargetPosition, NavigationCommandPriority.Normal);
                    
                    if (_enableDebugLogging)
                        Debug.Log($"SquadCoordinationSystem: Follower {follower.Id} following leader to {command.TargetPosition}");
                }
            }
        }

        /// <summary>
        /// FALLBACK: Traditional formation movement for backward compatibility
        /// </summary>
        private void ExecuteTraditionalMovement(MovementCommand command, List<IEntity> squadMembers)
        {
            // Get current formation type
            FormationType formationType = GetSquadFormationType(command.SquadId);
            
            // Calculate formation positions using simple templates
            var formationPositions = CalculateFormationPositions(
                formationType, 
                squadMembers.Count, 
                command.TargetPosition, 
                command.TargetRotation
            );
            
            // Move each unit to their assigned formation position
            for (int i = 0; i < squadMembers.Count && i < formationPositions.Length; i++)
            {
                var entity = squadMembers[i];
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (navigationComponent != null && formationComponent != null)
                {
                    // Use formation index to determine priority
                    int formationIndex = formationComponent.FormationSlotIndex;
                    NavigationCommandPriority priority = formationIndex == 0 
                        ? NavigationCommandPriority.High 
                        : NavigationCommandPriority.Normal;
                    
                    // Set destination
                    navigationComponent.SetDestination(formationPositions[i], priority);
                    
                    // Update formation component with new data
                    Vector3 localOffset = formationPositions[i] - command.TargetPosition;
                    formationComponent.SetFormationOffset(localOffset, command.SmoothTransition);
                }
            }
        }

        /// <summary>
        /// NEW: Execute smart movement command
        /// </summary>
        private void ExecuteSmartMovementCommand(SmartMovementCommand command)
        {
            // Move leader directly
            var leaderNavigation = command.LeaderEntity.GetComponent<NavigationComponent>();
            if (leaderNavigation != null)
            {
                leaderNavigation.SetDestination(command.TargetPosition, NavigationCommandPriority.High);
            }
            
            // Update all squad members with leader position
            var squadMembers = GetSquadMembers(command.SquadId);
            foreach (var entity in squadMembers)
            {
                if (entity != command.LeaderEntity)
                {
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.UpdateLeaderPosition(command.TargetPosition);
                        navigationComponent.SetDestination(command.TargetPosition, NavigationCommandPriority.Normal);
                    }
                }
            }
            
            // Update squad state
            UpdateSquadStateMovement(command.SquadId, command.TargetPosition, command.TargetRotation);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Executed smart movement for squad {command.SquadId} to {command.TargetPosition}");
            }
        }

        #endregion

        #region Enhanced Squad State Management

        /// <summary>
        /// Update squad states with enhanced smart movement tracking
        /// </summary>
        private void UpdateSquadStates()
        {
            var allFormationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Group by squad and update states
            var squadGroups = new Dictionary<int, List<IEntity>>();
            foreach (var entity in allFormationEntities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId >= 0)
                {
                    int squadId = formationComponent.SquadId;
                    if (!squadGroups.ContainsKey(squadId))
                        squadGroups[squadId] = new List<IEntity>();
                    
                    squadGroups[squadId].Add(entity);
                }
            }
            
            // Update state for each squad
            foreach (var kvp in squadGroups)
            {
                UpdateEnhancedSquadState(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// ENHANCED: Update state for a single squad with smart movement tracking
        /// </summary>
        private void UpdateEnhancedSquadState(int squadId, List<IEntity> members)
        {
            if (!_squadStates.TryGetValue(squadId, out EnhancedSquadState squadState))
            {
                squadState = new EnhancedSquadState { SquadId = squadId };
                _squadStates[squadId] = squadState;
            }
            
            // Calculate squad center and basic state
            Vector3 centerSum = Vector3.zero;
            int movingCount = 0;
            
            // ENHANCED: Track smart movement phases
            var smartMovementStats = new SmartMovementStatistics();
            
            foreach (var entity in members)
            {
                var transformComponent = entity.GetComponent<TransformComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (transformComponent != null)
                {
                    centerSum += transformComponent.Position;
                    
                    if (navigationComponent != null && !navigationComponent.HasReachedDestination)
                    {
                        movingCount++;
                    }

                    // ENHANCED: Track smart movement phases
                    if (navigationComponent != null && formationComponent != null)
                    {
                        bool isLeader = formationComponent.FormationSlotIndex == 0;
                        var phase = navigationComponent.CurrentMovementPhase;
                        
                        if (isLeader)
                        {
                            smartMovementStats.LeaderPhase = phase;
                        }
                        else
                        {
                            switch (phase)
                            {
                                case MovementPhase.MoveToLeader:
                                    smartMovementStats.FollowersMovingToLeader++;
                                    break;
                                case MovementPhase.MoveToFormation:
                                    smartMovementStats.FollowersMovingToFormation++;
                                    break;
                                case MovementPhase.InFormation:
                                    smartMovementStats.FollowersInFormation++;
                                    break;
                                case MovementPhase.DirectMovement:
                                    smartMovementStats.FollowersDirectMovement++;
                                    break;
                            }
                            smartMovementStats.TotalFollowers++;
                        }
                    }
                }
            }
            
            if (members.Count > 0)
            {
                squadState.CurrentPosition = centerSum / members.Count;
                squadState.IsMoving = movingCount > members.Count * 0.2f || smartMovementStats.FollowersMovingToLeader > 0;
                squadState.MemberCount = members.Count;
                squadState.SmartMovementStatistics = smartMovementStats;
            }
        }

        /// <summary>
        /// Update squad state with formation type
        /// </summary>
        private void UpdateSquadStateFormation(int squadId, FormationType formationType)
        {
            if (!_squadStates.TryGetValue(squadId, out EnhancedSquadState squadState))
            {
                squadState = new EnhancedSquadState { SquadId = squadId };
                _squadStates[squadId] = squadState;
            }
            
            squadState.CurrentFormationType = formationType;
        }

        /// <summary>
        /// Update squad state with movement data
        /// </summary>
        private void UpdateSquadStateMovement(int squadId, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (!_squadStates.TryGetValue(squadId, out EnhancedSquadState squadState))
            {
                squadState = new EnhancedSquadState { SquadId = squadId };
                _squadStates[squadId] = squadState;
            }
            
            squadState.TargetPosition = targetPosition;
            squadState.TargetRotation = targetRotation;
            squadState.IsMoving = true;
        }

        /// <summary>
        /// NEW: Auto-update leader positions for followers
        /// </summary>
        private void UpdateLeaderPositions()
        {
            if (Time.time - _lastLeaderPositionUpdate < _leaderPositionUpdateInterval)
                return;
                
            foreach (var squadState in _squadStates.Values)
            {
                var squadMembers = GetSquadMembers(squadState.SquadId);
                if (squadMembers.Count == 0) continue;
                
                // Find leader
                IEntity leader = null;
                foreach (var entity in squadMembers)
                {
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    if (formationComponent != null && formationComponent.FormationSlotIndex == 0)
                    {
                        leader = entity;
                        break;
                    }
                }
                
                if (leader == null) continue;
                
                // Get leader position
                var leaderTransform = leader.GetComponent<TransformComponent>();
                if (leaderTransform == null) continue;
                
                Vector3 leaderPosition = leaderTransform.Position;
                
                // Update leader position for all followers
                foreach (var entity in squadMembers)
                {
                    if (entity != leader)
                    {
                        var navigationComponent = entity.GetComponent<NavigationComponent>();
                        if (navigationComponent != null)
                        {
                            navigationComponent.UpdateLeaderPosition(leaderPosition);
                        }
                    }
                }
            }
            
            _lastLeaderPositionUpdate = Time.time;
        }

        #endregion

        #region Formation Position Calculation (Backward Compatible)

        /// <summary>
        /// Calculate formation positions using simple templates
        /// BACKWARD COMPATIBLE: Same as original implementation
        /// </summary>
        private Vector3[] CalculateFormationPositions(FormationType formationType, int unitCount, 
            Vector3 squadCenter, Quaternion squadRotation)
        {
            Vector3[] localPositions = new Vector3[unitCount];
            float spacing = GetFormationSpacing(formationType);
            
            switch (formationType)
            {
                case FormationType.Normal:
                    GenerateNormalFormation(localPositions, spacing);
                    break;
                    
                case FormationType.Phalanx:
                    GeneratePhalanxFormation(localPositions, spacing);
                    break;
                    
                case FormationType.Testudo:
                    GenerateTestudoFormation(localPositions, spacing);
                    break;
                    
                default:
                    GenerateNormalFormation(localPositions, spacing);
                    break;
            }
            
            // Convert to world positions
            Vector3[] worldPositions = new Vector3[unitCount];
            for (int i = 0; i < unitCount; i++)
            {
                Vector3 rotatedPosition = squadRotation * localPositions[i];
                worldPositions[i] = squadCenter + rotatedPosition;
            }
            
            return worldPositions;
        }

        private void GenerateNormalFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            const int gridWidth = 3;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / gridWidth;
                int col = i % gridWidth;
                
                float x = (col - 1) * spacing;
                float z = (row - 1) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        private void GeneratePhalanxFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            int width = Mathf.CeilToInt(Mathf.Sqrt(count));
            
            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;
                
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (Mathf.CeilToInt((float)count / width) - 1) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        private void GenerateTestudoFormation(Vector3[] positions, float spacing)
        {
            GeneratePhalanxFormation(positions, spacing);
        }

        #endregion

        #region ENHANCED PUBLIC INTERFACE

        /// <summary>
        /// ORIGINAL: Change squad formation type
        /// </summary>
        public void SetSquadFormation(int squadId, FormationType formationType, bool smoothTransition = true)
        {
            var command = new FormationCommand
            {
                SquadId = squadId,
                FormationType = formationType,
                SmoothTransition = smoothTransition
            };
            
            _formationCommands.Enqueue(command);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Queued formation change for squad {squadId} to {formationType}");
            }
        }

        /// <summary>
        /// ORIGINAL: Move squad to target position
        /// </summary>
        public void MoveSquadToPosition(int squadId, Vector3 targetPosition, Quaternion targetRotation = default, 
            bool smoothTransition = true)
        {
            if (targetRotation == default)
                targetRotation = Quaternion.identity;
            
            var command = new MovementCommand
            {
                SquadId = squadId,
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                SmoothTransition = smoothTransition
            };
            
            _movementCommands.Enqueue(command);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Queued movement for squad {squadId} to {targetPosition}");
            }
        }

        /// <summary>
        /// NEW: Set squad formation with smart movement optimization
        /// </summary>
        public void SetSquadFormationSmart(int squadId, FormationType formationType, bool enableSmartMovement = true, bool smoothTransition = true)
        {
            // First, set the formation
            SetSquadFormation(squadId, formationType, smoothTransition);
            
            // Then, configure smart movement for all squad members
            if (enableSmartMovement && _enableSmartMovementCoordination)
            {
                var squadMembers = GetSquadMembers(squadId);
                foreach (var entity in squadMembers)
                {
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.SetSmartMovementEnabled(true);
                    }
                }
                
                if (_enableDebugLogging)
                {
                    Debug.Log($"SquadCoordinationSystem: Squad {squadId} formation set to {formationType} with smart movement enabled");
                }
            }
        }

        /// <summary>
        /// NEW: Move squad with smart movement coordination
        /// </summary>
        public void MoveSquadSmartMovement(int squadId, Vector3 targetPosition, Quaternion targetRotation = default, bool smoothTransition = true)
        {
            if (targetRotation == default)
                targetRotation = Quaternion.identity;
            
            var squadMembers = GetSquadMembers(squadId);
            if (squadMembers.Count == 0) return;
            
            // Find leader
            IEntity leader = null;
            foreach (var entity in squadMembers)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.FormationSlotIndex == 0)
                {
                    leader = entity;
                    break;
                }
            }
            
            if (leader != null)
            {
                // Use smart movement command
                var command = new SmartMovementCommand
                {
                    SquadId = squadId,
                    TargetPosition = targetPosition,
                    TargetRotation = targetRotation,
                    SmoothTransition = smoothTransition,
                    LeaderEntity = leader
                };
                
                _smartMovementCommands.Enqueue(command);
                
                if (_enableDebugLogging)
                {
                    Debug.Log($"SquadCoordinationSystem: Queued smart movement for squad {squadId} to {targetPosition}");
                }
            }
            else
            {
                // Fallback to regular movement
                MoveSquadToPosition(squadId, targetPosition, targetRotation, smoothTransition);
            }
        }

        /// <summary>
        /// ORIGINAL: Get current squad state
        /// </summary>
        public EnhancedSquadState GetSquadState(int squadId)
        {
            _squadStates.TryGetValue(squadId, out EnhancedSquadState state);
            return state;
        }

        /// <summary>
        /// ENHANCED: Get all active squad states
        /// </summary>
        public Dictionary<int, EnhancedSquadState> GetAllSquadStates()
        {
            return new Dictionary<int, EnhancedSquadState>(_squadStates);
        }

        /// <summary>
        /// NEW: Get squad smart movement statistics
        /// </summary>
        public SmartMovementStatistics GetSquadSmartMovementStats(int squadId)
        {
            if (_squadStates.TryGetValue(squadId, out EnhancedSquadState state))
            {
                return state.SmartMovementStatistics;
            }
            return new SmartMovementStatistics();
        }

        #endregion

        #region Helper Methods

        private List<IEntity> GetSquadMembers(int squadId)
        {
            var members = new List<IEntity>();
            var allFormationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in allFormationEntities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    members.Add(entity);
                }
            }
            
            // Sort by formation slot index for consistent ordering
            members.Sort((a, b) => {
                var formA = a.GetComponent<FormationComponent>();
                var formB = b.GetComponent<FormationComponent>();
                return formA.FormationSlotIndex.CompareTo(formB.FormationSlotIndex);
            });
            
            return members;
        }

        private FormationType GetSquadFormationType(int squadId)
        {
            if (_squadStates.TryGetValue(squadId, out EnhancedSquadState state))
                return state.CurrentFormationType;
            
            // Fallback: check first squad member
            var members = GetSquadMembers(squadId);
            if (members.Count > 0)
            {
                var formationComponent = members[0].GetComponent<FormationComponent>();
                if (formationComponent != null)
                    return formationComponent.CurrentFormationType;
            }
            
            return FormationType.Normal;
        }

        private float GetFormationSpacing(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => 2.5f,
                FormationType.Phalanx => 1.8f,
                FormationType.Testudo => 1.2f,
                _ => 2.0f
            };
        }

        #endregion

        #region Debug Tools

        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveSquadCount => _squadStates.Count;
        
        [ShowInInspector, ReadOnly]
        private int QueuedFormationCommands => _formationCommands.Count;
        
        [ShowInInspector, ReadOnly]
        private int QueuedMovementCommands => _movementCommands.Count;
        
        [ShowInInspector, ReadOnly]
        private int QueuedSmartMovementCommands => _smartMovementCommands.Count;
        
        [ShowInInspector, ReadOnly]
        private int TotalCommandsProcessed => _totalCommandsProcessed;
        
        [ShowInInspector, ReadOnly]
        private int SmartMovementCommandsProcessed => _smartMovementCommandsProcessed;

        [Button("Show Squad States"), TitleGroup("Debug Tools")]
        public void ShowSquadStates()
        {
            string info = "=== Enhanced Squad Coordination Debug Info ===\n";
            info += $"Active Squads: {_squadStates.Count}\n";
            info += $"Smart Movement Coordination: {_enableSmartMovementCoordination}\n";
            info += $"Auto Update Leader Positions: {_autoUpdateLeaderPositions}\n";
            info += $"Queued Formation Commands: {_formationCommands.Count}\n";
            info += $"Queued Movement Commands: {_movementCommands.Count}\n";
            info += $"Queued Smart Movement Commands: {_smartMovementCommands.Count}\n";
            info += $"Total Commands Processed: {_totalCommandsProcessed}\n";
            info += $"Smart Movement Commands Processed: {_smartMovementCommandsProcessed}\n\n";
            
            foreach (var kvp in _squadStates)
            {
                var state = kvp.Value;
                info += $"Squad {state.SquadId}:\n";
                info += $"  Formation: {state.CurrentFormationType}\n";
                info += $"  Position: {state.CurrentPosition}\n";
                info += $"  Members: {state.MemberCount}\n";
                info += $"  Moving: {state.IsMoving}\n";
                info += $"  Smart Movement: {state.SmartMovementStatistics}\n\n";
            }
            
            Debug.Log(info);
        }

        [Button("Test Smart Movement Commands"), TitleGroup("Debug Tools")]
        public void TestSmartMovementCommands()
        {
            Debug.Log("=== Testing Smart Movement Commands ===");
            
            // Test smart formation changes
            SetSquadFormationSmart(1, FormationType.Phalanx, true, true);
            SetSquadFormationSmart(2, FormationType.Testudo, true, false);
            
            // Test smart movement commands
            MoveSquadSmartMovement(1, new Vector3(10, 0, 5), Quaternion.identity, true);
            MoveSquadSmartMovement(2, new Vector3(-5, 0, 8), Quaternion.identity, true);
            
            Debug.Log($"Queued {_formationCommands.Count} formation commands, {_movementCommands.Count} movement commands, and {_smartMovementCommands.Count} smart movement commands");
        }

        [Button("Clear All Commands"), TitleGroup("Debug Tools")]
        public void ClearAllCommands()
        {
            _formationCommands.Clear();
            _movementCommands.Clear();
            _smartMovementCommands.Clear();
            Debug.Log("SquadCoordinationSystem: Cleared all queued commands");
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            base.Cleanup();
            
            _squadStates.Clear();
            _formationCommands.Clear();
            _movementCommands.Clear();
            _smartMovementCommands.Clear();
            
            Debug.Log("SquadCoordinationSystem: Cleanup completed");
        }

        #endregion
    }

    #region Enhanced Data Structures
    [System.Serializable]
    public class EnhancedSquadState
    {
        public int SquadId;
        public FormationType CurrentFormationType = FormationType.Normal;
        public Vector3 CurrentPosition;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation = Quaternion.identity;
        public bool IsMoving;
        public int MemberCount;
        
        public SmartMovementStatistics SmartMovementStatistics = new SmartMovementStatistics();
        
        public override string ToString()
        {
            return $"Squad {SquadId}: {CurrentFormationType}, {MemberCount} units, Moving: {IsMoving}, {SmartMovementStatistics}";
        }
    }

    [System.Serializable]
    public class SmartMovementStatistics
    {
        public MovementPhase LeaderPhase = MovementPhase.DirectMovement;
        public int FollowersMovingToLeader = 0;
        public int FollowersMovingToFormation = 0;
        public int FollowersInFormation = 0;
        public int FollowersDirectMovement = 0;
        public int TotalFollowers = 0;
        
        public float FormationCompletionPercentage => 
            TotalFollowers > 0 ? (float)FollowersInFormation / TotalFollowers * 100f : 0f;
        
        public override string ToString()
        {
            return $"Leader: {LeaderPhase}, Formation: {FollowersInFormation}/{TotalFollowers} ({FormationCompletionPercentage:F1}%)";
        }
    }
    public struct FormationCommand
    {
        public int SquadId;
        public FormationType FormationType;
        public bool SmoothTransition;
    }

    public struct MovementCommand
    {
        public int SquadId;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public bool SmoothTransition;
    }

    public struct SmartMovementCommand
    {
        public int SquadId;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public bool SmoothTransition;
        public IEntity LeaderEntity;
    }

    #endregion
}