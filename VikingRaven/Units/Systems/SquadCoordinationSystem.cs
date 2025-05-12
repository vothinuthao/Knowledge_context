using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class SquadCoordinationSystem : BaseSystem
    {
        // Set higher priority than FormationSystem
        [SerializeField] private int _systemPriority = 200;
        
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private FormationSystem _formationSystem;
        
        // Track squad target positions
        private Dictionary<int, Vector3> _squadTargetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadTargetRotations = new Dictionary<int, Quaternion>();
        
        // Formation spacing parameters
        [Header("Formation Parameters")]
        [SerializeField] private float _lineSpacing = 2.0f;      // Spacing between units in Line
        [SerializeField] private float _columnSpacing = 2.0f;    // Spacing between units in Column
        [SerializeField] private float _phalanxSpacing = 1.2f;   // Spacing between units in Phalanx
        [SerializeField] private float _testudoSpacing = 0.8f;   // Spacing between units in Testudo
        [SerializeField] private float _circleMultiplier = 0.3f; // Radius multiplier for Circle
        [SerializeField] private float _gridSpacing = 2.0f;      // Grid cell spacing in Normal
        
        // Smart movement settings
        [Header("Smart Movement Settings")]
        [SerializeField] private bool _useSmartMovement = true;            // Enable smart movement
        [SerializeField] private float _leaderArriveDistance = 1.5f;       // Distance for leader to start formation
        [SerializeField] private float _squadFormingDelay = 0.5f;          // Delay before starting formation
        [SerializeField] private float _movementSmoothness = 0.8f;         // Movement smoothness (0-1)

        [SerializeField] private bool _debugLog = false;
        
        // Track movement states for squads
        private Dictionary<int, SquadMovementState> _squadMovementStates = new Dictionary<int, SquadMovementState>();

        // Internal class to track movement state
        private class SquadMovementState
        {
            public Vector3 TargetPosition { get; set; } = Vector3.zero;
            public Vector3 StartPosition { get; set; } = Vector3.zero;
            public float MoveStartTime { get; set; } = 0f;
            public bool IsLeaderArrived { get; set; } = false;
            public bool IsFormingUp { get; set; } = false;
            public float FormationStartTime { get; set; } = 0f;
            public List<IEntity> MembersInPosition { get; set; } = new List<IEntity>();
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Set system priority
            Priority = _systemPriority;
            
            // Find FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogError("SquadCoordinationSystem: FormationSystem not found!");
            }
            
            Debug.Log($"SquadCoordinationSystem initialized with priority {Priority}");
        }
        
        public override void Execute()
        {
            // Get all units with FormationComponent
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Update formation types for units in each squad
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // If a new formation type has been set for this squad, update the unit
                    if (_squadFormationTypes.TryGetValue(squadId, out FormationType formationType) &&
                        formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                        
                        // Notify FormationSystem about change
                        if (_formationSystem != null)
                        {
                            _formationSystem.ChangeFormation(squadId, formationType);
                        }
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Updated formation type for squad {squadId} to {formationType}");
                        }
                    }
                }
            }
            
            // If smart movement is enabled, update movement states
            if (_useSmartMovement)
            {
                UpdateSquadMovementStates(entities);
            }
        }

        /// <summary>
        /// Update movement states for squads
        /// </summary>
        private void UpdateSquadMovementStates(List<IEntity> entities)
        {
            // Group entities by squad
            Dictionary<int, List<IEntity>> squadMembers = new Dictionary<int, List<IEntity>>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    if (!squadMembers.ContainsKey(squadId))
                    {
                        squadMembers[squadId] = new List<IEntity>();
                    }
                    
                    squadMembers[squadId].Add(entity);
                }
            }
            
            // Update movement state for each squad
            foreach (var entry in squadMembers)
            {
                int squadId = entry.Key;
                var members = entry.Value;
                
                if (members.Count > 0)
                {
                    // Sort by ID to ensure consistent leader
                    members.Sort((a, b) => a.Id.CompareTo(b.Id));
                    
                    // Get current state or create new if none exists
                    if (!_squadMovementStates.TryGetValue(squadId, out var state))
                    {
                        state = new SquadMovementState();
                        _squadMovementStates[squadId] = state;
                    }
                    
                    // Update the squad's state
                    UpdateSingleSquadMovement(squadId, members, state);
                }
            }
            
            // Remove states for squads that no longer exist
            List<int> stateIdsToRemove = new List<int>();
            foreach (var squadId in _squadMovementStates.Keys)
            {
                if (!squadMembers.ContainsKey(squadId))
                {
                    stateIdsToRemove.Add(squadId);
                }
            }
            
            foreach (var id in stateIdsToRemove)
            {
                _squadMovementStates.Remove(id);
            }
        }

        /// <summary>
        /// Update movement state of a single squad
        /// </summary>
        private void UpdateSingleSquadMovement(int squadId, List<IEntity> members, SquadMovementState state)
        {
            if (members.Count == 0)
                return;
                
            // Get leader (first unit)
            var leader = members[0];
            var leaderTransform = leader.GetComponent<TransformComponent>();
            var leaderNavigation = leader.GetComponent<NavigationComponent>();
            
            if (leaderTransform == null || leaderNavigation == null)
                return;
                
            // Check if moving to target position
            bool isMovingToTarget = 
                _squadTargetPositions.TryGetValue(squadId, out Vector3 targetPosition) &&
                Vector3.Distance(leaderTransform.Position, targetPosition) > 0.1f;
                
            // If moving, continue tracking progress
            if (isMovingToTarget)
            {
                // Check if leader has arrived
                float distanceToTarget = Vector3.Distance(leaderTransform.Position, targetPosition);
                
                if (!state.IsLeaderArrived && distanceToTarget <= _leaderArriveDistance)
                {
                    // Leader has reached near destination, start formation phase
                    state.IsLeaderArrived = true;
                    state.FormationStartTime = Time.time;
                    state.IsFormingUp = true;
                    
                    if (_debugLog)
                    {
                        Debug.Log($"SquadCoordinationSystem: Leader of squad {squadId} has reached forming position, beginning formation");
                    }
                    
                    // At this phase leader will continue to move to destination but slower
                    leaderNavigation.SetDestination(targetPosition, NavigationCommandPriority.High);
                }
                
                // If in formation phase, adjust other members
                if (state.IsFormingUp)
                {
                    // Ensure we've waited long enough before starting
                    if (Time.time - state.FormationStartTime >= _squadFormingDelay)
                    {
                        // Update status of each member
                        state.MembersInPosition.Clear();
                        
                        // Get formation info
                        FormationType formationType = FormationType.Line;
                        if (_squadFormationTypes.TryGetValue(squadId, out var squadFormationType))
                        {
                            formationType = squadFormationType;
                        }
                        
                        // Last known position to perform formation
                        for (int i = 0; i < members.Count; i++)
                        {
                            var member = members[i];
                            var memberTransform = member.GetComponent<TransformComponent>();
                            var memberNavigation = member.GetComponent<NavigationComponent>();
                            var formationComponent = member.GetComponent<FormationComponent>();
                            
                            if (memberTransform != null && memberNavigation != null && formationComponent != null)
                            {
                                // Check if this member is in position
                                if (memberNavigation.HasReachedDestination)
                                {
                                    state.MembersInPosition.Add(member);
                                }
                            }
                        }
                        
                        // If all members are in position, mark as complete
                        if (state.MembersInPosition.Count >= members.Count)
                        {
                            state.IsFormingUp = false;
                            
                            if (_debugLog)
                            {
                                Debug.Log($"SquadCoordinationSystem: Squad {squadId} has completed movement and formation at {targetPosition}");
                            }
                        }
                    }
                }
            }
            else
            {
                // Not moving, reset state
                state.IsLeaderArrived = false;
                state.IsFormingUp = false;
                state.MembersInPosition.Clear();
            }
        }

        /// <summary>
        /// Set squad formation type
        /// </summary>
        public void SetSquadFormation(int squadId, FormationType formationType)
        {
            _squadFormationTypes[squadId] = formationType;
            
            // Notify FormationSystem immediately
            if (_formationSystem != null)
            {
                _formationSystem.ChangeFormation(squadId, formationType);
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Set formation type for squad {squadId} to {formationType}");
            }
            
            // Update formation for all units in squad
            UpdateSquadFormation(squadId, formationType);
        }
        
        /// <summary>
        /// Move a squad to a new position
        /// </summary>
        public void MoveSquadToPosition(int squadId, Vector3 targetPosition)
        {
            // Store target position for this squad
            _squadTargetPositions[squadId] = targetPosition;
            
            // Default to keep current rotation or face forward if none exists
            Quaternion targetRotation = Quaternion.identity;
            if (_squadTargetRotations.TryGetValue(squadId, out Quaternion currentRotation))
            {
                targetRotation = currentRotation;
            }
            _squadTargetRotations[squadId] = targetRotation;
            
            // Get formation type for this squad
            FormationType formationType = FormationType.Line;
            if (_squadFormationTypes.TryGetValue(squadId, out var storedFormationType))
            {
                formationType = storedFormationType;
            }
            else if (_formationSystem != null)
            {
                formationType = _formationSystem.GetCurrentFormationType(squadId);
            }
            
            // Notify FormationSystem about manual movement
            if (_formationSystem != null)
            {
                _formationSystem.SetSquadManualMovement(squadId, true);
            }
            
            // Find all units in this squad
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            var squadMembers = new List<IEntity>();
            
            // Group squad members
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
                    Debug.LogWarning($"SquadCoordinationSystem: Cannot move squad {squadId}, no units found");
                }
                return;
            }
            
            // Sort squad members by ID for consistency
            squadMembers.Sort((a, b) => a.Id.CompareTo(b.Id));
            
            // Store current position of squad for smart movement
            if (_useSmartMovement)
            {
                // Get or create movement state for this squad
                if (!_squadMovementStates.TryGetValue(squadId, out var state))
                {
                    state = new SquadMovementState();
                    _squadMovementStates[squadId] = state;
                }
                
                // Calculate current center
                Vector3 currentCenter = CalculateSquadCenter(squadMembers);
                
                // Update movement state
                state.TargetPosition = targetPosition;
                state.StartPosition = currentCenter;
                state.MoveStartTime = Time.time;
                state.IsLeaderArrived = false;
                state.IsFormingUp = false;
                state.MembersInPosition.Clear();
            }
            
            // Create or get formation template
            Vector3[] formationOffsets = GenerateFormationTemplate(formationType, squadMembers.Count);
            
            // Move each unit with different priorities
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent != null && navigationComponent != null)
                {
                    // Unlock position if unit was locked
                    navigationComponent.UnlockPosition();
                    
                    // Update slot and offset in formation
                    formationComponent.SetFormationSlot(i);
                    Vector3 offset = formationOffsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Set movement destination with different priorities
                    if (i == 0) // Leader
                    {
                        // Highest priority for leader
                        navigationComponent.SetFormationInfo(targetPosition, offset, NavigationCommandPriority.High);
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Moving leader {entity.Id} of squad {squadId} to {targetPosition} with High priority");
                        }
                    }
                    else
                    {
                        // Descending priority for other members
                        // This helps avoid situation where all units arrive at once and conflict in position
                        NavigationCommandPriority followerPriority = NavigationCommandPriority.High;
                        
                        // If using smart movement, reduce priority to let leader move first
                        if (_useSmartMovement)
                        {
                            followerPriority = NavigationCommandPriority.Normal;
                        }
                        
                        navigationComponent.SetFormationInfo(targetPosition, offset, followerPriority);
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Moving member {entity.Id} of squad {squadId} to formation position with offset {offset}");
                        }
                    }
                }
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Moving squad {squadId} to {targetPosition}");
            }
        }
        
        /// <summary>
        /// Update formation for all units in squad
        /// </summary>
        private void UpdateSquadFormation(int squadId, FormationType formationType)
        {
            // Check if squad has a target position
            if (!_squadTargetPositions.TryGetValue(squadId, out Vector3 targetPosition))
                return;
            
            // Find all units in squad
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
            
            // Sort by ID for consistency
            squadMembers.Sort((a, b) => a.Id.CompareTo(b.Id));
            
            // Create new formation template
            Vector3[] formationOffsets = GenerateFormationTemplate(formationType, squadMembers.Count);
            
            // Update position for each unit
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (formationComponent != null && navigationComponent != null)
                {
                    // Update slot and offset in formation
                    formationComponent.SetFormationSlot(i);
                    Vector3 offset = formationOffsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Update formation info
                    navigationComponent.SetFormationInfo(targetPosition, offset, NavigationCommandPriority.High);
                }
            }
        }
        
        /// <summary>
        /// Calculate current center of a squad
        /// </summary>
        private Vector3 CalculateSquadCenter(List<IEntity> squadMembers)
        {
            if (squadMembers.Count == 0)
                return Vector3.zero;
                
            Vector3 sum = Vector3.zero;
            int validCount = 0;
            
            foreach (var entity in squadMembers)
            {
                var transformComponent = entity.GetComponent<TransformComponent>();
                if (transformComponent != null)
                {
                    sum += transformComponent.Position;
                    validCount++;
                }
            }
            
            return validCount > 0 ? sum / validCount : Vector3.zero;
        }
        
        /// <summary>
        /// Generate formation template offsets
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int count)
        {
            // Similar to FormationSystem, with some adjustments for movement formation
            Vector3[] positions = new Vector3[count];
            
            switch (formationType)
            {
                case FormationType.Line:
                    // Line formation: horizontal row with leader in center
                    int halfLineCount = count / 2;
                    
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset;
                        
                        // Place leader (i=0) in center
                        if (i == 0)
                        {
                            xOffset = 0;
                        }
                        // Members on leader's left (even numbers)
                        else if (i % 2 == 0)
                        {
                            xOffset = (i / 2) * _lineSpacing;
                        }
                        // Members on leader's right (odd numbers)
                        else
                        {
                            xOffset = -((i + 1) / 2) * _lineSpacing;
                        }
                        
                        positions[i] = new Vector3(xOffset, 0, 0);
                    }
                    break;
                    
                case FormationType.Column:
                    // Column formation: vertical line with leader at front
                    for (int i = 0; i < count; i++)
                    {
                        // Leader (i=0) at front, others behind
                        float zOffset = -i * _columnSpacing;
                        positions[i] = new Vector3(0, 0, zOffset);
                    }
                    break;
                    
                case FormationType.Phalanx:
                    // Improved Phalanx: rectangle with leader in center
                    int rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    
                    // Determine Leader position (center)
                    int centerRow = (rowSize - 1) / 2;
                    int centerCol = (rowSize - 1) / 2;
                    
                    for (int i = 0; i < count; i++)
                    {
                        if (i == 0)
                        {
                            // Leader at center
                            positions[i] = new Vector3(0, 0, 0);
                        }
                        else
                        {
                            // Other members arranged in spiral from center
                            // Calculate position based on distance from center
                            int layer = 1;
                            int positionInLayer = 0;
                            int layerCapacity = 8 * layer; // Positions in each spiral layer
                            
                            // Find layer and position in layer
                            int position = i;
                            while (position > layerCapacity)
                            {
                                position -= layerCapacity;
                                layer++;
                                layerCapacity = 8 * layer;
                            }
                            
                            positionInLayer = position;
                            
                            // Calculate position based on layer and position in layer
                            float angle = (positionInLayer * 2 * Mathf.PI) / layerCapacity;
                            float radius = layer * _phalanxSpacing;
                            
                            float x = Mathf.Sin(angle) * radius;
                            float z = Mathf.Cos(angle) * radius;
                            
                            positions[i] = new Vector3(x, 0, z);
                        }
                    }
                    break;
                    
                case FormationType.Circle:
                    // Circle formation: leader in center, others in circle
                    positions[0] = Vector3.zero; // Leader at center
                    
                    float circleRadius = Mathf.Max(1.5f, (count - 1) * 0.25f);
                    
                    for (int i = 1; i < count; i++)
                    {
                        float angle = ((i - 1) * 2 * Mathf.PI) / (count - 1);
                        float x = Mathf.Sin(angle) * circleRadius;
                        float z = Mathf.Cos(angle) * circleRadius;
                        
                        positions[i] = new Vector3(x, 0, z);
                    }
                    break;
                    
                // Add other formations similarly...
                    
                default:
                    // Default formation: similar to Line
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f;
                        positions[i] = new Vector3(xOffset * _lineSpacing, 0, 0);
                    }
                    break;
            }
            
            // Add small random offset to avoid exact collisions
            for (int i = 1; i < count; i++) // Skip leader (i=0)
            {
                positions[i] += new Vector3(
                    Random.Range(-0.05f, 0.05f),
                    0,
                    Random.Range(-0.05f, 0.05f));
            }
            
            return positions;
        }
    }
}