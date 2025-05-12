using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// System responsible for managing formations for units
    /// </summary>
    public class FormationSystem : BaseSystem
    {
        // Set priority for this system - should execute before StateManagementSystem
        [SerializeField] private int _systemPriority = 300;
        
        // Dictionary to track squad formations by squad ID
        private Dictionary<int, Dictionary<FormationType, Vector3[]>> _formationTemplates = 
            new Dictionary<int, Dictionary<FormationType, Vector3[]>>();
        
        private Dictionary<int, FormationType> _currentFormations = new Dictionary<int, FormationType>();
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        
        // Tracking squad manual movement
        private Dictionary<int, bool> _squadManualMovement = new Dictionary<int, bool>();
        private Dictionary<int, float> _lastManualMoveTime = new Dictionary<int, float>();
        [SerializeField] private float _manualMoveTimeout = 5.0f; // How long manual move priority lasts
        
        // Mapping from squad ID to center and rotation
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        // Advanced formation movement parameters
        [Header("Advanced Formation Movement Parameters")]
        [SerializeField] private bool _useCohesiveMovement = true;        // Use cohesive movement
        [SerializeField] private bool _useLeaderFollowSystem = true;      // Use leader-follower system
        [SerializeField] private float _squadCohesionForce = 0.8f;        // Cohesion force, higher = tighter formation
        [SerializeField] private float _maxLeaderFollowDistance = 5.0f;   // Maximum distance followers will track leader

        // Squad leader tracking
        private Dictionary<int, IEntity> _squadLeaders = new Dictionary<int, IEntity>();
        
        // Formation spacing parameters
        [SerializeField] private float _lineSpacing = 2.0f;      // Spacing between units in Line formation
        [SerializeField] private float _columnSpacing = 2.0f;    // Spacing between units in Column formation
        [SerializeField] private float _phalanxSpacing = 1.2f;   // Spacing between units in Phalanx formation
        [SerializeField] private float _testudoSpacing = 0.8f;   // Spacing between units in Testudo formation
        [SerializeField] private float _circleMultiplier = 0.3f; // Radius multiplier for Circle formation
        [SerializeField] private float _gridSpacing = 2.0f;      // Grid cell spacing in Normal formation
        
        // Debug flags
        [SerializeField] private bool _logDebugInfo = true;
        
        public override void Initialize()
        {
            base.Initialize();
            // Set priority for this system
            Priority = _systemPriority;
            Debug.Log($"FormationSystem initialized with priority {Priority}");
            
            // Check EntityRegistry initialization
            if (EntityRegistry != null)
            {
                Debug.Log($"FormationSystem: EntityRegistry is available with {EntityRegistry.EntityCount} entities");
            }
            else
            {
                Debug.LogError("FormationSystem: EntityRegistry is null");
            }
        }
        
        public override void Execute()
        {
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.Execute: Starting execution with {(_squadMembers != null ? _squadMembers.Count : 0)} squads");
            }
            
            // Update unit list by squad
            UpdateSquadMembers();
            
            // Identify leaders for each squad
            if (_useLeaderFollowSystem)
            {
                UpdateSquadLeaders();
            }
            
            // Update manual movement status
            UpdateManualMovementStatus();
            
            // Calculate new centers and rotations for each squad
            CalculateSquadCentersAndRotations();
            
            // Update formation positions for each squad
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
                // Check if this squad is being manually moved
                bool isManuallyMoving = false;
                if (_squadManualMovement.TryGetValue(squadId, out isManuallyMoving) && isManuallyMoving)
                {
                    // Skip formation position update if manually moving
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Squad {squadId} is being manually moved, skipping formation update");
                    }
                    continue;
                }
                
                // Get current formation type
                FormationType formationType = FormationType.Line; // Default
                if (_currentFormations.TryGetValue(squadId, out var currentFormation))
                {
                    formationType = currentFormation;
                }
                
                // Ensure template exists for formation and unit count
                EnsureFormationTemplate(squadId, formationType, members.Count);
                
                // Update formation positions
                UpdateFormationPositions(squadId, members, formationType);
            }
            
            // Perform cohesive movement processing if enabled
            if (_useCohesiveMovement)
            {
                ApplyCohesiveMovement();
            }
            
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.Execute: Finished execution, processed {_squadMembers.Count} squads");
            }
        }
        
        /// <summary>
        /// Update squad members list
        /// </summary>
        private void UpdateSquadMembers()
        {
            _squadMembers.Clear();
            
            // Debug logging to check EntityRegistry state
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.UpdateSquadMembers: EntityRegistry has {EntityRegistry.EntityCount} entities");
            }
            
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Debug log
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem.UpdateSquadMembers: Found {entities.Count} entities with FormationComponent");
            }
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null)
                {
                    Debug.LogWarning($"FormationSystem: Entity {entity.Id} has null FormationComponent");
                    continue;
                }
                
                if (!formationComponent.IsActive)
                {
                    Debug.LogWarning($"FormationSystem: Entity {entity.Id} has inactive FormationComponent");
                    continue;
                }
                
                int squadId = formationComponent.SquadId;
                
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                
                _squadMembers[squadId].Add(entity);
                
                if (_logDebugInfo)
                {
                    Debug.Log($"FormationSystem: Added entity {entity.Id} to squad {squadId}, slot {formationComponent.FormationSlotIndex}");
                }
                
                // Update current formation type for squad
                if (!_currentFormations.ContainsKey(squadId))
                {
                    _currentFormations[squadId] = formationComponent.CurrentFormationType;
                    
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Set formation type for squad {squadId} to {formationComponent.CurrentFormationType}");
                    }
                }
            }
            
            // Sort members in each squad by ID for consistency
            foreach (var squadId in _squadMembers.Keys)
            {
                _squadMembers[squadId].Sort((a, b) => a.Id.CompareTo(b.Id));
                
                if (_logDebugInfo)
                {
                    Debug.Log($"FormationSystem: Sorted squad {squadId} members by ID for consistency");
                }
            }
            
            // Log squad member counts
            if (_logDebugInfo)
            {
                foreach (var squad in _squadMembers)
                {
                    Debug.Log($"FormationSystem: Squad {squad.Key} has {squad.Value.Count} members");
                }
            }
        }

        /// <summary>
        /// Update squad leaders
        /// </summary>
        private void UpdateSquadLeaders()
        {
            _squadLeaders.Clear();
            
            foreach (var entry in _squadMembers)
            {
                int squadId = entry.Key;
                var members = entry.Value;
                
                if (members.Count > 0)
                {
                    // Always sort by ID to ensure consistent leader
                    members.Sort((a, b) => a.Id.CompareTo(b.Id));
                    
                    // First unit will be leader
                    _squadLeaders[squadId] = members[0];
                    
                    // Update leader info for each unit in squad
                    for (int i = 0; i < members.Count; i++)
                    {
                        var navigationComponent = members[i].GetComponent<NavigationComponent>();
                        if (navigationComponent != null)
                        {
                            navigationComponent.SetSquadLeader(_squadLeaders[squadId]);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Update manual movement status
        /// </summary>
        private void UpdateManualMovementStatus()
        {
            float currentTime = Time.time;
            List<int> expiredSquads = new List<int>();
            
            // Check and update squads that have timed out of manual movement
            foreach (var squadId in _squadManualMovement.Keys)
            {
                if (_squadManualMovement[squadId])
                {
                    if (_lastManualMoveTime.TryGetValue(squadId, out float lastMoveTime))
                    {
                        if (currentTime - lastMoveTime > _manualMoveTimeout)
                        {
                            // Manual movement timeout has expired
                            expiredSquads.Add(squadId);
                            if (_logDebugInfo)
                            {
                                Debug.Log($"FormationSystem: Squad {squadId} manual movement expired");
                            }
                        }
                    }
                }
            }
            
            // Reset state for expired squads
            foreach (var squadId in expiredSquads)
            {
                _squadManualMovement[squadId] = false;
            }
        }
        
        /// <summary>
        /// Apply cohesive movement to squads
        /// </summary>
        private void ApplyCohesiveMovement()
        {
            foreach (var entry in _squadMembers)
            {
                int squadId = entry.Key;
                var members = entry.Value;
                
                if (members.Count <= 1) 
                    continue; // Skip if not enough members
                    
                // Get squad leader
                if (!_squadLeaders.TryGetValue(squadId, out IEntity leader) || leader == null)
                    continue;
                    
                var leaderTransform = leader.GetComponent<TransformComponent>();
                var leaderNavigation = leader.GetComponent<NavigationComponent>();
                
                if (leaderTransform == null || leaderNavigation == null)
                    continue;
                    
                // Check if leader is moving
                bool isLeaderMoving = !leaderNavigation.HasReachedDestination;
                
                if (!isLeaderMoving)
                    continue; // Skip if leader is not moving
                    
                // Get current leader position
                Vector3 leaderPosition = leaderTransform.Position;
                
                // Check other members in squad
                for (int i = 1; i < members.Count; i++) // Start from 1 to skip leader
                {
                    var follower = members[i];
                    var followerTransform = follower.GetComponent<TransformComponent>();
                    var followerNavigation = follower.GetComponent<NavigationComponent>();
                    
                    if (followerTransform == null || followerNavigation == null)
                        continue;
                        
                    // Calculate distance to leader
                    float distanceToLeader = Vector3.Distance(followerTransform.Position, leaderPosition);
                    
                    // If too far from leader, disable avoidance with other squad members
                    if (distanceToLeader > _maxLeaderFollowDistance)
                    {
                        followerNavigation.DisableSquadMemberAvoidance(true);
                    }
                    else if (distanceToLeader < _maxLeaderFollowDistance * 0.7f)
                    {
                        // If close enough, restore normal avoidance
                        followerNavigation.DisableSquadMemberAvoidance(false);
                    }
                }
            }
        }
        
        /// <summary>
        /// Mark a squad as being manually moved
        /// </summary>
        public void SetSquadManualMovement(int squadId, bool isManuallyMoving)
        {
            _squadManualMovement[squadId] = isManuallyMoving;
            _lastManualMoveTime[squadId] = Time.time;
            
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem: Squad {squadId} manual movement set to {isManuallyMoving}");
            }
        }
        
        /// <summary>
        /// Calculate squad centers and rotations
        /// </summary>
        private void CalculateSquadCentersAndRotations()
        {
            _squadCenters.Clear();
            _squadRotations.Clear();
            
            foreach (var squadId in _squadMembers.Keys)
            {
                var members = _squadMembers[squadId];
                if (members.Count == 0) continue;
                
                Vector3 centerSum = Vector3.zero;
                Vector3 forwardSum = Vector3.zero;
                int validCount = 0;
                
                foreach (var entity in members)
                {
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        centerSum += transformComponent.Position;
                        forwardSum += transformComponent.Forward;
                        validCount++;
                    }
                }
                
                if (validCount > 0)
                {
                    // Calculate center (average position of all units)
                    Vector3 center = centerSum / validCount;
                    _squadCenters[squadId] = center;
                    
                    // Calculate average direction
                    if (forwardSum.magnitude > 0.01f)
                    {
                        Quaternion rotation = Quaternion.LookRotation(forwardSum.normalized);
                        _squadRotations[squadId] = rotation;
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                    
                    if (_logDebugInfo)
                    {
                        Debug.Log($"FormationSystem: Calculated center for squad {squadId} at {center}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Ensure formation template exists for a given formation type and member count
        /// </summary>
        private void EnsureFormationTemplate(int squadId, FormationType formationType, int memberCount)
        {
            if (!_formationTemplates.ContainsKey(squadId))
            {
                _formationTemplates[squadId] = new Dictionary<FormationType, Vector3[]>();
            }
            
            if (!_formationTemplates[squadId].ContainsKey(formationType) || 
                _formationTemplates[squadId][formationType].Length != memberCount)
            {
                _formationTemplates[squadId][formationType] = GenerateFormationTemplate(formationType, memberCount);
                
                if (_logDebugInfo)
                {
                    Debug.Log($"FormationSystem: Generated new template for squad {squadId}, formation {formationType}, members {memberCount}");
                }
            }
        }
        
        /// <summary>
        /// Generate formation position offsets based on formation type
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int count)
        {
            Vector3[] positions = new Vector3[count];
            
            switch (formationType)
            {
                case FormationType.Line:
                    // Line formation: units in a horizontal row
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f; // Ensure centered
                        
                        // Increase spacing between units in Line formation
                        positions[i] = new Vector3(xOffset * _lineSpacing * 1.1f, 0, 0);
                    }
                    break;
                
                case FormationType.Column:
                    // Column formation: units in a vertical column
                    for (int i = 0; i < count; i++)
                    {
                        float zOffset = i - (count - 1) / 2.0f; // Ensure centered
                        
                        // Increase vertical spacing
                        positions[i] = new Vector3(0, 0, zOffset * _columnSpacing * 1.2f);
                    }
                    break;
                
                case FormationType.Phalanx:
                    // Phalanx formation: grid/rectangle
                    int rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Center formation
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        // Add small random offset to avoid exact collisions
                        float randomOffsetX = Random.Range(-0.05f, 0.05f);
                        float randomOffsetZ = Random.Range(-0.05f, 0.05f);
                        
                        positions[i] = new Vector3(
                            xOffset * _phalanxSpacing + randomOffsetX, 
                            0, 
                            zOffset * _phalanxSpacing + randomOffsetZ);
                    }
                    break;
                    
                case FormationType.Testudo:
                    // Testudo formation: tighter grid
                    rowSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / rowSize;
                        int col = i % rowSize;
                        
                        // Center formation
                        float xOffset = col - (rowSize - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / rowSize) / 2.0f;
                        
                        // Add micro offset to avoid exact overlap
                        float microOffsetX = (i % 2) * 0.05f;
                        float microOffsetZ = ((i / 2) % 2) * 0.05f;
                        
                        positions[i] = new Vector3(
                            xOffset * _testudoSpacing + microOffsetX, 
                            0, 
                            zOffset * _testudoSpacing + microOffsetZ);
                    }
                    break;
                    
                case FormationType.Circle:
                    // Circle formation: leader in center, others in circle
                    
                    // Leader at center
                    positions[0] = new Vector3(0, 0, 0);
                    
                    float radius = Mathf.Max(1.5f, count * _circleMultiplier); // Auto-adjust radius
                    
                    // Other members in circle
                    for (int i = 1; i < count; i++)
                    {
                        float angle = ((i - 1) * 2 * Mathf.PI) / (count - 1);
                        float x = Mathf.Sin(angle) * radius;
                        float z = Mathf.Cos(angle) * radius;
                        positions[i] = new Vector3(x, 0, z);
                    }
                    break;
                
                case FormationType.Normal:
                    // Normal formation: fixed 3x3 grid
                    for (int i = 0; i < count; i++)
                    {
                        // Modulo 3 to calculate row/column in 3x3 grid
                        int row = i / 3;
                        int col = i % 3;
                        
                        // Center formation so slot 4 (middle) is at (0,0)
                        float xOffset = col - 1.0f; 
                        float zOffset = row - 1.0f;
                        
                        // Add small random offset
                        float randomOffsetX = Random.Range(-0.08f, 0.08f);
                        float randomOffsetZ = Random.Range(-0.08f, 0.08f);
                        
                        positions[i] = new Vector3(
                            xOffset * _gridSpacing + randomOffsetX, 
                            0, 
                            zOffset * _gridSpacing + randomOffsetZ);
                    }
                    break;
                
                default:
                    // Default to Line formation
                    for (int i = 0; i < count; i++)
                    {
                        float xOffset = i - (count - 1) / 2.0f;
                        positions[i] = new Vector3(xOffset * _lineSpacing, 0, 0);
                    }
                    break;
            }
            
            return positions;
        }
        
        /// <summary>
        /// Update formation positions for units in a squad
        /// </summary>
        private void UpdateFormationPositions(int squadId, List<IEntity> members, FormationType formationType)
        {
            // Check if squad center and rotation data exists
            if (!_squadCenters.TryGetValue(squadId, out Vector3 center) ||
                !_squadRotations.TryGetValue(squadId, out Quaternion rotation))
            {
                return;
            }
            
            // Ensure members are sorted by ID
            members.Sort((a, b) => a.Id.CompareTo(b.Id));
            
            // Get created template
            var formationTemplate = _formationTemplates[squadId][formationType];
            
            // Ensure we don't exceed template size
            int count = Mathf.Min(members.Count, formationTemplate.Length);
            
            // Determine leader (first unit)
            IEntity leader = members.Count > 0 ? members[0] : null;
            
            // Update FormationComponent for each entity
            for (int i = 0; i < count; i++)
            {
                var entity = members[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    // Update formation slot and offset
                    formationComponent.SetFormationSlot(i);
                    formationComponent.SetFormationOffset(formationTemplate[i]);
                    
                    // Ensure formation type consistency
                    if (formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                    }
                    
                    // Update navigation target if needed
                    var navigationComponent = entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null && navigationComponent.IsActive)
                    {
                        Vector3 targetPosition = center + (rotation * formationTemplate[i]);
                        
                        // Set direction when reaching formation position
                        Vector3 formationDirection = Vector3.zero;
                        if (formationType == FormationType.Line || formationType == FormationType.Column)
                        {
                            formationDirection = rotation * Vector3.forward;
                        }
                        else if (formationType == FormationType.Circle)
                        {
                            // For circle formation, face outward
                            if (formationTemplate[i].magnitude > 0.01f)
                            {
                                formationDirection = (rotation * formationTemplate[i]).normalized;
                            }
                        }
                        
                        // Update formation info including direction
                        if (formationDirection != Vector3.zero)
                        {
                            navigationComponent.SetFormationDirectionOffset(formationDirection);
                        }
                        
                        // Method to avoid overriding higher priority movement commands
                        if (_useLeaderFollowSystem && i > 0 && leader != null)
                        {
                            // Non-leader members will follow leader
                            navigationComponent.SetFormationInfo(center, formationTemplate[i], NavigationCommandPriority.Normal);
                        }
                        else
                        {
                            // Leader or case when not using leader following
                            if (navigationComponent.CurrentCommandPriority <= NavigationCommandPriority.Normal)
                            {
                                navigationComponent.SetFormationInfo(center, formationTemplate[i], NavigationCommandPriority.Normal);
                                
                                if (_logDebugInfo)
                                {
                                    Debug.Log($"FormationSystem: Set destination for entity {entity.Id} to {targetPosition} with Normal priority");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Change formation type for a squad
        /// </summary>
        public void ChangeFormation(int squadId, FormationType formationType)
        {
            // Store new formation type
            _currentFormations[squadId] = formationType;
            
            Debug.Log($"FormationSystem: Changed squad {squadId} formation to {formationType}");
            
            // Remove current template to create new one on next update
            if (_formationTemplates.ContainsKey(squadId) && 
                _formationTemplates[squadId].ContainsKey(formationType))
            {
                _formationTemplates[squadId].Remove(formationType);
            }
            
            // Update immediately if there are members
            if (_squadMembers.TryGetValue(squadId, out var members) && members.Count > 0)
            {
                // Ensure template exists
                EnsureFormationTemplate(squadId, formationType, members.Count);
                
                // Update positions
                if (_squadCenters.ContainsKey(squadId) && _squadRotations.ContainsKey(squadId))
                {
                    UpdateFormationPositions(squadId, members, formationType);
                }
            }
        }
        
        /// <summary>
        /// Get current formation type for a squad
        /// </summary>
        public FormationType GetCurrentFormationType(int squadId)
        {
            if (_currentFormations.TryGetValue(squadId, out FormationType formationType))
            {
                return formationType;
            }
            
            return FormationType.Line; // Default
        }
    }
}