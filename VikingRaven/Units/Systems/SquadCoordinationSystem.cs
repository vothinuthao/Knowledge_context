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
        
        // Formation tracking
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private FormationSystem _formationSystem;
        
        // Track squad target positions
        private Dictionary<int, Vector3> _squadTargetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadTargetRotations = new Dictionary<int, Quaternion>();
        
        // Debug settings
        [SerializeField] private bool _debugLog = false;
        
        // Reference to formation system
        private FormationSystem FormationSystem
        {
            get
            {
                if (_formationSystem == null)
                {
                    _formationSystem = FindObjectOfType<FormationSystem>();
                }
                return _formationSystem;
            }
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
            UpdateFormationTypes(entities);
        }

        /// <summary>
        /// Updates formation types for all units
        /// </summary>
        private void UpdateFormationTypes(List<IEntity> entities)
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
            
            // Update formation for each squad
            foreach (var entry in squadMembers)
            {
                int squadId = entry.Key;
                var members = entry.Value;
                
                if (members.Count == 0) continue;
                
                // If a new formation type has been set for this squad, update all members
                if (_squadFormationTypes.TryGetValue(squadId, out FormationType formationType))
                {
                    bool needsUpdate = false;
                    
                    // Check if any unit needs update
                    foreach (var entity in members)
                    {
                        var formationComponent = entity.GetComponent<FormationComponent>();
                        if (formationComponent != null && formationComponent.CurrentFormationType != formationType)
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                    
                    // If needs update, update all members
                    if (needsUpdate)
                    {
                        foreach (var entity in members)
                        {
                            var formationComponent = entity.GetComponent<FormationComponent>();
                            if (formationComponent != null)
                            {
                                formationComponent.SetFormationType(formationType);
                            }
                        }
                        
                        // Notify FormationSystem
                        if (FormationSystem != null)
                        {
                            // FormationSystem.ChangeFormation(squadId, formationType);
                        }
                        
                        if (_debugLog)
                        {
                            Debug.Log($"SquadCoordinationSystem: Updated formation type for squad {squadId} to {formationType}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set squad formation type
        /// </summary>
        public void SetSquadFormation(int squadId, FormationType formationType)
        {
            _squadFormationTypes[squadId] = formationType;
            
            // Notify FormationSystem immediately
            if (FormationSystem != null)
            {
                // FormationSystem.ChangeFormation(squadId, formationType);
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Set formation type for squad {squadId} to {formationType}");
            }
        }
        
        /// <summary>
        /// Move a squad to a new position
        /// </summary>
        public virtual void MoveSquadToPosition(int squadId, Vector3 targetPosition)
        {
            _squadTargetPositions[squadId] = targetPosition;
            Quaternion targetRotation = Quaternion.identity;
            if (_squadTargetRotations.TryGetValue(squadId, out Quaternion currentRotation))
            {
                targetRotation = currentRotation;
            }
            _squadTargetRotations[squadId] = targetRotation;
            
            // Get formation type for this squad
            FormationType formationType = FormationType.Normal;
            if (_squadFormationTypes.TryGetValue(squadId, out var storedFormationType))
            {
                formationType = storedFormationType;
            }
            else if (FormationSystem != null)
            {
                // formationType = FormationSystem.GetCurrentFormationType(squadId);
            }
            
            // Notify FormationSystem about manual movement
            if (FormationSystem != null)
            {
                // FormationSystem.SetSquadManualMovement(squadId, true);
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
            
            // Get formation template - simplified approach directly commands the units
            Vector3[] offsets = GenerateSimpleFormationTemplate(formationType, squadMembers.Count);
            
            // Move each unit with different priorities
            for (int i = 0; i < squadMembers.Count; i++)
            {
                var entity = squadMembers[i];
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (navigationComponent != null && formationComponent != null)
                {
                    // Update formation component data
                    formationComponent.SetFormationSlot(i);
                    Vector3 offset = offsets[i];
                    formationComponent.SetFormationOffset(offset);
                    
                    // Calculate target position and set as destination
                    Vector3 unitTargetPosition = targetPosition + (targetRotation * offset);
                    
                    if (i == 0) // Leader
                    {
                        navigationComponent.SetDestination(unitTargetPosition, NavigationCommandPriority.High);
                    }
                    else
                    {
                        navigationComponent.SetDestination(unitTargetPosition, NavigationCommandPriority.Normal);
                    }
                }
            }
            
            if (_debugLog)
            {
                Debug.Log($"SquadCoordinationSystem: Moving squad {squadId} to {targetPosition}");
            }
        }
        
        /// <summary>
        /// Generate a simple formation template
        /// </summary>
        private Vector3[] GenerateSimpleFormationTemplate(FormationType formationType, int count)
        {
            Vector3[] positions = new Vector3[count];
            float lineSpacing = 2.0f;
            float columnSpacing = 2.0f;
            float phalanxSpacing = 1.2f;
            float testudoSpacing = 0.8f;
            float circleRadius = Mathf.Max(1.5f, count * 0.3f);
            float gridSpacing = 2.0f;
            
            switch (formationType)
            {
                    
                case FormationType.Phalanx:
                    int phalanxWidth = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / phalanxWidth;
                        int col = i % phalanxWidth;
                        
                        float xOffset = col - (phalanxWidth - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / phalanxWidth) / 2.0f;
                        
                        positions[i] = new Vector3(
                            xOffset * phalanxSpacing, 
                            0, 
                            zOffset * phalanxSpacing);
                    }
                    break;
                    
                case FormationType.Testudo:
                    int testudoWidth = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / testudoWidth;
                        int col = i % testudoWidth;
                        
                        float xOffset = col - (testudoWidth - 1) / 2.0f;
                        float zOffset = row - ((count - 1) / testudoWidth) / 2.0f;
                        
                        positions[i] = new Vector3(
                            xOffset * testudoSpacing, 
                            0, 
                            zOffset * testudoSpacing);
                    }
                    break;
                    
                case FormationType.Normal:
                default:
                    // Normal/default - 3x3 grid centered at origin
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / 3;
                        int col = i % 3;
                        
                        float xOffset = col - 1.0f;
                        float zOffset = row - 1.0f;
                        
                        positions[i] = new Vector3(
                            xOffset * gridSpacing, 
                            0, 
                            zOffset * gridSpacing);
                    }
                    break;
            }
            
            return positions;
        }
    }
}