using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Simplified FormationSystem focusing on 3 core formation types
    /// Eliminates complexity and focuses on proper 3x3 grid positioning
    /// Enhanced debugging and clearer formation logic flow
    /// </summary>
    [SystemPriority(SystemPriority.Medium)]
    public class FormationSystem : BaseSystem
    {
        #region System Configuration

        [Header("System Settings")]
        [Tooltip("Update frequency (lower = more frequent updates)")]
        [SerializeField, Range(1, 10)] private int _updateFrequency = 3;
        
        [Tooltip("Enable debug visualization in Scene view")]
        [SerializeField] private bool _enableDebugVisualization = true;
        
        [Tooltip("Enable detailed logging for formation updates")]
        [SerializeField] private bool _enableDetailedLogging = false;

        #endregion

        #region Formation Data

        // Squad formation tracking - simplified data structure
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        private Dictionary<int, FormationSpacingConfig> _squadSpacingConfigs = new Dictionary<int, FormationSpacingConfig>();
        
        // Formation templates cache - only for 3 types
        private Dictionary<string, Vector3[]> _formationTemplateCache = new Dictionary<string, Vector3[]>();
        
        // Update timing control
        private int _frameCounter = 0;
        
        // Performance tracking
        private int _totalFormationUpdates = 0;
        private float _lastUpdateTime = 0f;

        #endregion

        #region Dependencies

        private EntityRegistry _entityRegistry;

        #endregion

        #region System Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            
            _entityRegistry = EntityRegistry.Instance;
            if (_entityRegistry == null)
            {
                Debug.LogError("FormationSystem: EntityRegistry not found!");
                return;
            }
            
            Priority = 150; // Set medium priority
            
            Debug.Log("FormationSystem: Simplified system initialized with 3 formation types (Normal, Phalanx, Testudo)");
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
            
            _lastUpdateTime = Time.time;
            
            // Core formation update pipeline
            UpdateSquadMembership();
            CalculateSquadTransforms();  
            UpdateAllSquadFormations();
            
            if (_enableDebugVisualization)
            {
                DrawFormationDebug();
            }
        }

        #endregion

        #region Squad Membership Management

        /// <summary>
        /// Update squad membership data from FormationComponents
        /// Enhanced: Better error handling and validation
        /// </summary>
        private void UpdateSquadMembership()
        {
            _squadMembers.Clear();
            var formationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"FormationSystem: Processing {formationEntities.Count} entities with FormationComponent");
            }
            
            foreach (var entity in formationEntities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (!formationComponent || !formationComponent.IsActive) continue;
                
                int squadId = formationComponent.SquadId;
                if (squadId < 0) continue;
                
                // Add to squad members
                if (!_squadMembers.ContainsKey(squadId))
                {
                    _squadMembers[squadId] = new List<IEntity>();
                }
                _squadMembers[squadId].Add(entity);
                
                // Store formation type and spacing config
                _squadFormationTypes[squadId] = formationComponent.CurrentFormationType;
                
                // Try to get spacing config from formation component or entity
                if (!_squadSpacingConfigs.ContainsKey(squadId))
                {
                    var spacingConfig = TryGetSpacingConfigForSquad(entity);
                    if (spacingConfig != null)
                    {
                        _squadSpacingConfigs[squadId] = spacingConfig;
                    }
                }
            }
            
            // Sort members by formation slot index for consistent positioning
            foreach (var squadId in _squadMembers.Keys)
            {
                _squadMembers[squadId].Sort((a, b) => {
                    var formA = a.GetComponent<FormationComponent>();
                    var formB = b.GetComponent<FormationComponent>();
                    return formA.FormationSlotIndex.CompareTo(formB.FormationSlotIndex);
                });
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"FormationSystem: Squad {squadId} has {_squadMembers[squadId].Count} members, Formation: {_squadFormationTypes[squadId]}");
                }
            }
        }

        /// <summary>
        /// Try to get spacing config for a squad from various sources
        /// </summary>
        private FormationSpacingConfig TryGetSpacingConfigForSquad(IEntity entity)
        {
            // Method 1: Check if entity has direct reference to squad data
            // This would require additional component or reference system
            
            // Method 2: Use default config if available
            // In a production system, you might want to inject this or load from resources
            
            // For now, return null and handle in formation generation
            return null;
        }

        #endregion

        #region Squad Transform Calculation

        /// <summary>
        /// Calculate center position and rotation for each squad
        /// Enhanced: More stable center calculation
        /// </summary>
        private void CalculateSquadTransforms()
        {
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                 
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
                    // Calculate average center
                    _squadCenters[squadId] = centerSum / validCount;
                    
                    // Calculate stable forward direction
                    if (forwardSum.magnitude > 0.01f)
                    {
                        Vector3 avgForward = forwardSum.normalized;
                        _squadRotations[squadId] = Quaternion.LookRotation(avgForward, Vector3.up);
                    }
                    else
                    {
                        _squadRotations[squadId] = Quaternion.identity;
                    }
                }
            }
        }

        #endregion

        #region Formation Position Updates

        /// <summary>
        /// Update formation positions for all squads
        /// Simplified: Focus on the 3 core formation types
        /// </summary>
        private void UpdateAllSquadFormations()
        {
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                
                if (members.Count == 0) continue;
                
                // Get squad transform data
                if (!_squadCenters.TryGetValue(squadId, out Vector3 squadCenter) ||
                    !_squadRotations.TryGetValue(squadId, out Quaternion squadRotation) ||
                    !_squadFormationTypes.TryGetValue(squadId, out FormationType formationType))
                {
                    continue;
                }
                
                // Get spacing config for this squad
                FormationSpacingConfig spacingConfig = null;
                _squadSpacingConfigs.TryGetValue(squadId, out spacingConfig);
                
                // Update formation for this squad
                UpdateSquadFormation(squadId, members, squadCenter, squadRotation, formationType, spacingConfig);
                _totalFormationUpdates++;
            }
        }

        /// <summary>
        /// Update formation for a specific squad
        /// Enhanced: Better formation assignment with proper spacing
        /// </summary>
        private void UpdateSquadFormation(int squadId, List<IEntity> members, Vector3 squadCenter, 
            Quaternion squadRotation, FormationType formationType, FormationSpacingConfig spacingConfig)
        {
            // Get formation template
            Vector3[] formationTemplate = GetFormationTemplate(formationType, members.Count, spacingConfig);
            
            if (formationTemplate == null || formationTemplate.Length == 0)
            {
                Debug.LogWarning($"FormationSystem: Failed to generate template for {formationType} with {members.Count} units");
                return;
            }
            
            // Update each member's formation data
            for (int i = 0; i < members.Count && i < formationTemplate.Length; i++)
            {
                var entity = members[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (formationComponent == null || transformComponent == null) continue;
                
                // Calculate world position for this slot
                Vector3 localOffset = formationTemplate[i];
                Vector3 worldOffset = squadRotation * localOffset;
                Vector3 targetPosition = squadCenter + worldOffset;
                
                // Update FormationComponent data
                formationComponent.SetFormationPositionData(
                    localOffset, 
                    targetPosition, 
                    squadCenter, 
                    squadRotation
                );
                
                // Calculate formation state
                float distanceFromTarget = Vector3.Distance(transformComponent.Position, targetPosition);
                float tolerance = spacingConfig?.PositionTolerance ?? 0.3f;
                bool isInPosition = distanceFromTarget < tolerance;
                
                formationComponent.UpdateFormationState(
                    isInPosition, 
                    distanceFromTarget, 
                    false
                );
                
                // Set formation role
                FormationRole role = DetermineFormationRole(i, members.Count, formationType);
                formationComponent.SetFormationRole(role);
                
                // Update navigation component with formation info
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    navigationComponent.SetFormationInfo(
                        squadCenter, 
                        localOffset, 
                        NavigationCommandPriority.Normal
                    );
                }
            }
        }

        #endregion

        #region Formation Template Generation (FOCUSED ON 3 TYPES)

        /// <summary>
        /// Get formation template for specific type and unit count
        /// Simplified: Only handles Normal, Phalanx, Testudo
        /// </summary>
        private Vector3[] GetFormationTemplate(FormationType formationType, int unitCount, FormationSpacingConfig spacingConfig)
        {
            if (unitCount <= 0) return new Vector3[0];
            
            string cacheKey = $"{formationType}_{unitCount}";
            
            // Check cache first
            if (_formationTemplateCache.TryGetValue(cacheKey, out Vector3[] cachedTemplate))
            {
                return cachedTemplate;
            }
            
            // Generate new template
            Vector3[] template = GenerateFormationTemplate(formationType, unitCount, spacingConfig);
            if (template != null && template.Length > 0)
            {
                _formationTemplateCache[cacheKey] = template;
            }
            
            return template;
        }

        /// <summary>
        /// Generate formation template - ONLY FOR 3 TYPES
        /// Focused implementation for Normal (3x3), Phalanx, and Testudo
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int unitCount, FormationSpacingConfig spacingConfig)
        {
            Vector3[] positions = new Vector3[unitCount];
            
            // Get spacing - use config if available, otherwise use defaults
            float spacing = GetSpacingForFormation(formationType, spacingConfig);
            
            switch (formationType)
            {
                case FormationType.Normal:
                    GenerateNormalFormation(positions, spacing);
                    break;
                    
                case FormationType.Phalanx:
                    GeneratePhalanxFormation(positions, spacing);
                    break;
                    
                case FormationType.Testudo:
                    GenerateTestudoFormation(positions, spacing);
                    break;
                    
                default:
                    Debug.LogWarning($"FormationSystem: Unknown formation type {formationType}, using Normal formation");
                    GenerateNormalFormation(positions, spacing);
                    break;
            }
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"FormationSystem: Generated {formationType} formation for {unitCount} units with {spacing} spacing");
            }
            
            return positions;
        }

        /// <summary>
        /// Get spacing for formation type from config or defaults
        /// </summary>
        private float GetSpacingForFormation(FormationType formationType, FormationSpacingConfig spacingConfig)
        {
            if (spacingConfig != null)
            {
                return spacingConfig.GetSpacing(formationType);
            }
            
            // Default spacing values if no config available
            return formationType switch
            {
                FormationType.Normal => 2.5f,
                FormationType.Phalanx => 1.8f, 
                FormationType.Testudo => 1.2f,
                _ => 2.0f
            };
        }

        /// <summary>
        /// Generate Normal formation (3x3 grid)
        /// ENHANCED: Perfect 3x3 grid with proper centering
        /// </summary>
        private void GenerateNormalFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            const int gridWidth = 3;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / gridWidth;
                int col = i % gridWidth;
                
                // Center the grid around origin (0,0,0)
                // For 3x3: positions will be (-1,-1), (-1,0), (-1,1), (0,-1), (0,0), (0,1), (1,-1), (1,0), (1,1)
                float x = (col - 1) * spacing;  // -1, 0, 1 for columns 0, 1, 2
                float z = (row - 1) * spacing;  // -1, 0, 1 for rows 0, 1, 2
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate Phalanx formation (tight combat grid)
        /// More compact than Normal formation for combat effectiveness
        /// </summary>
        private void GeneratePhalanxFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            int width = Mathf.CeilToInt(Mathf.Sqrt(count));
            int height = Mathf.CeilToInt((float)count / width);
            
            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;
                
                // Center the grid around origin
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (height - 1) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate Testudo formation (very tight defensive grid)
        /// Extremely compact formation for maximum defense
        /// </summary>
        private void GenerateTestudoFormation(Vector3[] positions, float spacing)
        {
            // Use same grid logic as Phalanx but with the tighter spacing
            // The spacing parameter already contains the reduced value from config
            GeneratePhalanxFormation(positions, spacing);
        }

        #endregion

        #region Formation Role Assignment

        /// <summary>
        /// Determine formation role based on position and formation type
        /// Simplified: Basic role assignment for 3 formation types
        /// </summary>
        private FormationRole DetermineFormationRole(int slotIndex, int totalUnits, FormationType formationType)
        {
            // Leader is always slot 0
            if (slotIndex == 0) return FormationRole.Leader;
            
            switch (formationType)
            {
                case FormationType.Normal:
                    // For 3x3 grid: front row (slots 0,1,2), middle row (3,4,5), back row (6,7,8)
                    if (slotIndex <= 2) return FormationRole.FrontLine;
                    else if (slotIndex <= 5) return FormationRole.Follower;
                    else return FormationRole.Support;
                
                case FormationType.Phalanx:
                    // Front units are front-line, others are followers
                    int width = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
                    if (slotIndex < width) return FormationRole.FrontLine;
                    else return FormationRole.Follower;
                
                case FormationType.Testudo:
                    // Very defensive - most units are support
                    return FormationRole.Support;
                
                default:
                    return FormationRole.Follower;
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Force update formation for specific squad
        /// Simplified: Clear interface for external systems
        /// </summary>
        public void UpdateSquadFormation(int squadId, FormationType newFormationType)
        {
            if (squadId < 0) return;
            
            _squadFormationTypes[squadId] = newFormationType;
            
            // Update all components in this squad
            if (_squadMembers.TryGetValue(squadId, out var members))
            {
                foreach (var entity in members)
                {
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationType(newFormationType, true);
                    }
                }
                
                Debug.Log($"FormationSystem: Updated squad {squadId} to formation {newFormationType} with {members.Count} units");
            }
            
            // Clear cache for this formation type
            ClearFormationCache(newFormationType);
        }

        /// <summary>
        /// Clear formation cache for specific formation type
        /// </summary>
        private void ClearFormationCache(FormationType formationType)
        {
            var keysToRemove = new List<string>();
            string cachePattern = $"{formationType}_";
            
            foreach (var key in _formationTemplateCache.Keys)
            {
                if (key.StartsWith(cachePattern))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _formationTemplateCache.Remove(key);
            }
        }

        /// <summary>
        /// Get formation effectiveness for a squad
        /// </summary>
        public float GetSquadFormationEffectiveness(int squadId)
        {
            if (!_squadMembers.TryGetValue(squadId, out var members) || members.Count == 0)
                return 0f;
            
            float totalEffectiveness = 0f;
            int validCount = 0;
            
            foreach (var entity in members)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    totalEffectiveness += formationComponent.GetFormationEffectiveness();
                    validCount++;
                }
            }
            
            return validCount > 0 ? totalEffectiveness / validCount : 0f;
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw debug visualization for formations
        /// Enhanced: Better visual feedback with formation-specific colors
        /// </summary>
        private void DrawFormationDebug()
        {
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                
                if (!_squadCenters.TryGetValue(squadId, out Vector3 center)) continue;
                if (!_squadFormationTypes.TryGetValue(squadId, out FormationType formationType)) continue;
                
                // Get formation-specific color
                Color formationColor = GetFormationDebugColor(formationType);
                
                // Draw squad center
                Debug.DrawLine(center, center + Vector3.up * 2f, formationColor, 0.1f);
                
                // Draw formation type indicator
                Vector3 typeIndicator = center + Vector3.up * 2.5f;
                Debug.DrawLine(center + Vector3.up * 2f, typeIndicator, formationColor, 0.1f);
                
                // Draw formation connections
                for (int i = 0; i < members.Count; i++)
                {
                    var transformComponent = members[i].GetComponent<TransformComponent>();
                    var formationComponent = members[i].GetComponent<FormationComponent>();
                    
                    if (transformComponent != null && formationComponent != null)
                    {
                        Vector3 unitPos = transformComponent.Position + Vector3.up * 0.5f;
                        Vector3 targetPos = formationComponent.TargetFormationPosition + Vector3.up * 0.5f;
                        
                        // Color coding: Green = in position, Yellow = moving, Red = far from target
                        Color lineColor;
                        if (formationComponent.IsInFormationPosition)
                            lineColor = Color.green;
                        else if (formationComponent.DistanceFromFormationPosition < 1f)
                            lineColor = Color.yellow;
                        else
                            lineColor = Color.red;
                        
                        // Draw line from unit to formation target
                        Debug.DrawLine(unitPos, targetPos, lineColor, 0.1f);
                        
                        // Draw slot number
                        Debug.DrawLine(targetPos, targetPos + Vector3.up * 0.5f, formationColor, 0.1f);
                    }
                }
            }
        }

        /// <summary>
        /// Get debug color for formation type
        /// </summary>
        private Color GetFormationDebugColor(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => Color.green,
                FormationType.Phalanx => Color.red,
                FormationType.Testudo => Color.blue,
                _ => Color.white
            };
        }

        #endregion

        #region Cleanup

        public override void Cleanup()
        {
            base.Cleanup();
            
            _squadMembers.Clear();
            _squadFormationTypes.Clear();
            _squadCenters.Clear();
            _squadRotations.Clear();
            _squadSpacingConfigs.Clear();
            _formationTemplateCache.Clear();
            
            Debug.Log("FormationSystem: Cleanup completed");
        }

        #endregion

        #region Debug Info

        /// <summary>
        /// Get debug info about current formation system state
        /// </summary>
        public string GetDebugInfo()
        {
            string info = "=== Formation System Debug Info ===\n";
            info += $"Active Squads: {_squadMembers.Count}\n";
            info += $"Total Formation Updates: {_totalFormationUpdates}\n";
            info += $"Cache Size: {_formationTemplateCache.Count}\n";
            info += $"Last Update: {_lastUpdateTime:F2}s\n";
            info += $"Update Frequency: Every {_updateFrequency} frames\n";
            
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                var formationType = _squadFormationTypes.GetValueOrDefault(squadId, FormationType.Normal);
                
                info += $"Squad {squadId}: {members.Count} units, {formationType} formation\n";
            }
            
            return info;
        }

        #endregion
    }
}