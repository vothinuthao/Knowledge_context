using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// SIMPLIFIED: Squad Coordination System with direct command interface
    /// No complex formation recalculation - uses assigned formation indices
    /// Provides simple commands for squad movement and formation changes
    /// Works with enhanced FormationSystem for coordinated unit management
    /// </summary>
    [SystemPriority(SystemPriority.High)]
    public class SquadCoordinationSystem : BaseSystem
    {
        #region System Configuration - SIMPLIFIED

        [TitleGroup("System Settings")]
        [Tooltip("System execution priority")]
        [SerializeField, Range(100, 300)] private int _systemPriority = 200;
        
        [Tooltip("Enable debug logging for squad commands")]
        [SerializeField, ToggleLeft] private bool _enableDebugLogging = false;
        
        [Tooltip("Update frequency for squad state monitoring")]
        [SerializeField, Range(1, 10)] private int _updateFrequency = 5;

        #endregion

        #region Squad Management Data - SIMPLIFIED

        // Track active squads and their states
        private Dictionary<int, SquadState> _squadStates = new Dictionary<int, SquadState>();
        
        // Formation command queue
        private Queue<FormationCommand> _formationCommands = new Queue<FormationCommand>();
        
        // Movement command queue  
        private Queue<MovementCommand> _movementCommands = new Queue<MovementCommand>();
        
        // Update timing
        private int _frameCounter = 0;
        
        // Performance tracking
        private int _totalCommandsProcessed = 0;

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
            
            Debug.Log($"SquadCoordinationSystem: Initialized with priority {Priority}");
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
            
            // SIMPLIFIED: Core execution pipeline
            ProcessFormationCommands();
            ProcessMovementCommands();
            UpdateSquadStates();
        }

        #endregion

        #region Command Processing - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Process queued formation commands
        /// Direct formation type changes without complex recalculation
        /// </summary>
        private void ProcessFormationCommands()
        {
            while (_formationCommands.Count > 0)
            {
                var command = _formationCommands.Dequeue();
                ExecuteFormationCommand(command);
                _totalCommandsProcessed++;
            }
        }

        /// <summary>
        /// SIMPLIFIED: Process queued movement commands
        /// Direct position updates using existing formation indices
        /// </summary>
        private void ProcessMovementCommands()
        {
            while (_movementCommands.Count > 0)
            {
                var command = _movementCommands.Dequeue();
                ExecuteMovementCommand(command);
                _totalCommandsProcessed++;
            }
        }

        /// <summary>
        /// SIMPLIFIED: Execute formation command for a squad
        /// </summary>
        private void ExecuteFormationCommand(FormationCommand command)
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
            }
            
            // Update squad state
            UpdateSquadState(command.SquadId, command.FormationType);
            
            // Notify FormationSystem if available
            if (_formationSystem != null)
            {
                _formationSystem.UpdateSquadFormation(command.SquadId, command.FormationType);
            }
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Squad {command.SquadId} formation changed to {command.FormationType}");
            }
        }

        /// <summary>
        /// SIMPLIFIED: Execute movement command for a squad
        /// Uses existing formation indices - no recalculation needed
        /// </summary>
        private void ExecuteMovementCommand(MovementCommand command)
        {
            var squadMembers = GetSquadMembers(command.SquadId);
            if (squadMembers.Count == 0)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning($"SquadCoordinationSystem: No members found for squad {command.SquadId}");
                return;
            }
            
            // Get current formation type for the squad
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
            
            // Update squad state
            if (_squadStates.TryGetValue(command.SquadId, out SquadState squadState))
            {
                squadState.TargetPosition = command.TargetPosition;
                squadState.TargetRotation = command.TargetRotation;
                squadState.IsMoving = true;
            }
            
            if (_enableDebugLogging)
            {
                Debug.Log($"SquadCoordinationSystem: Squad {command.SquadId} moving to {command.TargetPosition}");
            }
        }

        #endregion

        #region Squad State Management - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Update squad states based on member status
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
                UpdateSingleSquadState(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// SIMPLIFIED: Update state for a single squad
        /// </summary>
        private void UpdateSingleSquadState(int squadId, List<IEntity> members)
        {
            if (!_squadStates.TryGetValue(squadId, out SquadState squadState))
            {
                squadState = new SquadState { SquadId = squadId };
                _squadStates[squadId] = squadState;
            }
            
            // Calculate squad center
            Vector3 centerSum = Vector3.zero;
            int movingCount = 0;
            
            foreach (var entity in members)
            {
                var transformComponent = entity.GetComponent<TransformComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (transformComponent != null)
                {
                    centerSum += transformComponent.Position;
                    
                    if (navigationComponent != null && !navigationComponent.HasReachedDestination)
                    {
                        movingCount++;
                    }
                }
            }
            
            if (members.Count > 0)
            {
                squadState.CurrentPosition = centerSum / members.Count;
                squadState.IsMoving = movingCount > members.Count * 0.3f; // 30% threshold
                squadState.MemberCount = members.Count;
            }
        }

        /// <summary>
        /// SIMPLIFIED: Update squad state with formation type
        /// </summary>
        private void UpdateSquadState(int squadId, FormationType formationType)
        {
            if (!_squadStates.TryGetValue(squadId, out SquadState squadState))
            {
                squadState = new SquadState { SquadId = squadId };
                _squadStates[squadId] = squadState;
            }
            
            squadState.CurrentFormationType = formationType;
        }

        #endregion

        #region Formation Position Calculation - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Calculate formation positions using simple templates
        /// Based on formation type and unit count
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

        #region Public Command Interface - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Change squad formation type
        /// Queues command for processing in next update
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
        /// SIMPLIFIED: Move squad to target position
        /// Queues command for processing in next update
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
        /// Get current squad state
        /// </summary>
        public SquadState GetSquadState(int squadId)
        {
            _squadStates.TryGetValue(squadId, out SquadState state);
            return state;
        }

        /// <summary>
        /// Get all active squad states
        /// </summary>
        public Dictionary<int, SquadState> GetAllSquadStates()
        {
            return new Dictionary<int, SquadState>(_squadStates);
        }

        #endregion

        #region Helper Methods - SIMPLIFIED

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
            if (_squadStates.TryGetValue(squadId, out SquadState state))
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

        #region Debug Tools - ENHANCED ODIN

        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveSquadCount => _squadStates.Count;
        
        [ShowInInspector, ReadOnly]
        private int QueuedFormationCommands => _formationCommands.Count;
        
        [ShowInInspector, ReadOnly]
        private int QueuedMovementCommands => _movementCommands.Count;
        
        [ShowInInspector, ReadOnly]
        private int TotalCommandsProcessed => _totalCommandsProcessed;

        [Button("Show Squad States"), TitleGroup("Debug Tools")]
        public void ShowSquadStates()
        {
            string info = "=== Squad Coordination Debug Info ===\n";
            info += $"Active Squads: {_squadStates.Count}\n";
            info += $"Queued Formation Commands: {_formationCommands.Count}\n";
            info += $"Queued Movement Commands: {_movementCommands.Count}\n";
            info += $"Total Commands Processed: {_totalCommandsProcessed}\n\n";
            
            foreach (var kvp in _squadStates)
            {
                var state = kvp.Value;
                info += $"Squad {state.SquadId}:\n";
                info += $"  Formation: {state.CurrentFormationType}\n";
                info += $"  Position: {state.CurrentPosition}\n";
                info += $"  Members: {state.MemberCount}\n";
                info += $"  Moving: {state.IsMoving}\n\n";
            }
            
            Debug.Log(info);
        }

        [Button("Test Squad Commands"), TitleGroup("Debug Tools")]
        public void TestSquadCommands()
        {
            Debug.Log("=== Testing Squad Commands ===");
            
            // Test formation changes
            SetSquadFormation(1, FormationType.Phalanx, true);
            SetSquadFormation(2, FormationType.Testudo, false);
            
            // Test movement commands
            MoveSquadToPosition(1, new Vector3(10, 0, 5), Quaternion.identity, true);
            MoveSquadToPosition(2, new Vector3(-5, 0, 8), Quaternion.identity, true);
            
            Debug.Log($"Queued {_formationCommands.Count} formation commands and {_movementCommands.Count} movement commands");
        }

        [Button("Clear All Commands"), TitleGroup("Debug Tools")]
        public void ClearAllCommands()
        {
            _formationCommands.Clear();
            _movementCommands.Clear();
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
            
            Debug.Log("SquadCoordinationSystem: Cleanup completed");
        }

        #endregion
    }

    #region Supporting Data Structures - SIMPLIFIED

    /// <summary>
    /// SIMPLIFIED: Squad state information
    /// Contains essential data for squad coordination
    /// </summary>
    [System.Serializable]
    public class SquadState
    {
        public int SquadId;
        public FormationType CurrentFormationType = FormationType.Normal;
        public Vector3 CurrentPosition;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation = Quaternion.identity;
        public bool IsMoving;
        public int MemberCount;
        
        public override string ToString()
        {
            return $"Squad {SquadId}: {CurrentFormationType}, {MemberCount} units, Moving: {IsMoving}";
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

    #endregion
}