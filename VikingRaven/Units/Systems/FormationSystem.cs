using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class FormationSystem : BaseSystem
    {
        // Set priority for this system
        [SerializeField] private int _systemPriority = 300;
        
        // Dictionary to track squad formations
        private Dictionary<int, Dictionary<FormationType, Vector3[]>> _formationTemplates = 
            new Dictionary<int, Dictionary<FormationType, Vector3[]>>();
        
        private Dictionary<int, FormationType> _currentFormations = new Dictionary<int, FormationType>();
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        
        // Tracking squad manual movement
        private Dictionary<int, bool> _squadManualMovement = new Dictionary<int, bool>();
        private Dictionary<int, float> _lastManualMoveTime = new Dictionary<int, float>();
        [SerializeField] private float _manualMoveTimeout = 5.0f;
        
        // Position tracking
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        // Formation spacing parameters
        [SerializeField] private float _lineSpacing = 2.0f;
        [SerializeField] private float _columnSpacing = 2.0f;
        [SerializeField] private float _phalanxSpacing = 1.2f;
        [SerializeField] private float _testudoSpacing = 0.8f;
        [SerializeField] private float _circleMultiplier = 0.3f;
        [SerializeField] private float _gridSpacing = 2.0f;
        
        // Debug flags
        [SerializeField] private bool _logDebugInfo = false;
        
        public override void Initialize()
        {
            base.Initialize();
            // Set priority for this system
            Priority = _systemPriority;
            
            if (_logDebugInfo)
            {
                Debug.Log($"FormationSystem initialized with priority {Priority}");
            }
        }
        
        public override void Execute()
        {
            // Update unit list by squad
            UpdateSquadMembers();
            
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
            
            // Update manual movement status
            UpdateManualMovementStatus();
        }
        
        /// <summary>
        /// Update squad members list
        /// </summary>
        private void UpdateSquadMembers()
        {
            _squadMembers.Clear();
            
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent == null || !formationComponent.IsActive)
                    continue;
                
                int squadId = formationComponent.SquadId;
                
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                
                _squadMembers[squadId].Add(entity);
                
                // Update current formation type for squad
                if (!_currentFormations.ContainsKey(squadId))
                {
                    _currentFormations[squadId] = formationComponent.CurrentFormationType;
                }
            }
            
            // Sort members in each squad by ID for consistency
            foreach (var squadId in _squadMembers.Keys)
            {
                _squadMembers[squadId].Sort((a, b) => a.Id.CompareTo(b.Id));
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
        /// Calculate squad centers and rotations
        /// </summary>
        private void CalculateSquadCentersAndRotations()
        {
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
                        positions[i] = new Vector3(xOffset * _lineSpacing, 0, 0);
                    }
                    break;
                
                case FormationType.Column:
                    // Column formation: units in a vertical column
                    for (int i = 0; i < count; i++)
                    {
                        float zOffset = i - (count - 1) / 2.0f; // Ensure centered
                        positions[i] = new Vector3(0, 0, zOffset * _columnSpacing);
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
                        
                        positions[i] = new Vector3(
                            xOffset * _phalanxSpacing, 
                            0, 
                            zOffset * _phalanxSpacing);
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
                        
                        positions[i] = new Vector3(
                            xOffset * _testudoSpacing, 
                            0, 
                            zOffset * _testudoSpacing);
                    }
                    break;
                    
                case FormationType.Circle:
                    // Circle formation
                    positions[0] = Vector3.zero; // Center
                    
                    float radius = Mathf.Max(1.5f, count * _circleMultiplier);
                    
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
                        
                        // Center formation
                        float xOffset = col - 1.0f; 
                        float zOffset = row - 1.0f;
                        
                        positions[i] = new Vector3(
                            xOffset * _gridSpacing, 
                            0, 
                            zOffset * _gridSpacing);
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
            
            // Get created template
            var formationTemplate = _formationTemplates[squadId][formationType];
            
            // Ensure we don't exceed template size
            int count = Mathf.Min(members.Count, formationTemplate.Length);
            
            // Update positions for each entity
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
                        
                        // Only update if command priority is appropriate
                        if (navigationComponent.CurrentCommandPriority <= NavigationCommandPriority.Normal)
                        {
                            navigationComponent.SetFormationInfo(center, formationTemplate[i], NavigationCommandPriority.Normal);
                        }
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
        }
        
        /// <summary>
        /// Change formation type for a squad
        /// </summary>
        public void ChangeFormation(int squadId, FormationType formationType)
        {
            // Store new formation type
            _currentFormations[squadId] = formationType;
            
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