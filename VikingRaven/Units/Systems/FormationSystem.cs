using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Systems
{
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
        
        [Tooltip("Force formation positioning (overrides unit autonomy)")]
        [SerializeField] private bool _forceFormationPositioning = true;

        #endregion

        #region Formation Data

        // FIXED: Simplified but effective formation tracking
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        private Dictionary<int, FormationSpacingConfig> _squadSpacingConfigs = new Dictionary<int, FormationSpacingConfig>();
        
        // FIXED: Formation templates cache with proper key management
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
            
            Debug.Log("FormationSystem: FIXED system initialized with proper formation synchronization");
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
            
            _lastUpdateTime = Time.time;
            
            // FIXED: Core formation update pipeline with proper synchronization
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
        /// FIXED: Update squad membership with better validation and sorting
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
                
                // FIXED: Try to get spacing config from formation component
                if (!_squadSpacingConfigs.ContainsKey(squadId))
                {
                    var spacingConfig = TryGetSpacingConfigForSquad(entity);
                    if (spacingConfig != null)
                    {
                        _squadSpacingConfigs[squadId] = spacingConfig;
                    }
                }
            }
            
            // FIXED: Sort members by formation slot index for consistent positioning
            foreach (var squadId in _squadMembers.Keys)
            {
                _squadMembers[squadId].Sort((a, b) => {
                    var formA = a.GetComponent<FormationComponent>();
                    var formB = b.GetComponent<FormationComponent>();
                    
                    // Handle null components
                    if (formA == null && formB == null) return 0;
                    if (formA == null) return 1;
                    if (formB == null) return -1;
                    
                    return formA.FormationSlotIndex.CompareTo(formB.FormationSlotIndex);
                });
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"FormationSystem: Squad {squadId} has {_squadMembers[squadId].Count} members, " +
                             $"Formation: {_squadFormationTypes[squadId]}");
                }
            }
        }

        /// <summary>
        /// Try to get spacing config for a squad from various sources
        /// </summary>
        private FormationSpacingConfig TryGetSpacingConfigForSquad(IEntity entity)
        {
            // For now, return null and handle in formation generation
            // In production, this could check squad data or load from resources
            return null;
        }

        #endregion

        #region Squad Transform Calculation

        /// <summary>
        /// FIXED: Calculate stable center position and rotation for each squad
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
                    Vector3 newCenter = centerSum / validCount;
                    
                    // FIXED: Use more stable center calculation to prevent jitter
                    if (_squadCenters.ContainsKey(squadId))
                    {
                        Vector3 oldCenter = _squadCenters[squadId];
                        float distance = Vector3.Distance(oldCenter, newCenter);
                        
                        // Only update center if significant movement to reduce jitter
                        if (distance > 0.5f)
                        {
                            _squadCenters[squadId] = Vector3.Lerp(oldCenter, newCenter, 0.3f);
                        }
                    }
                    else
                    {
                        _squadCenters[squadId] = newCenter;
                    }
                    
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
        /// FIXED: Update formation positions for all squads with proper application
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
                
                // FIXED: Update formation for this squad with immediate application
                UpdateSquadFormation(squadId, members, squadCenter, squadRotation, formationType, spacingConfig);
                _totalFormationUpdates++;
            }
        }

        /// <summary>
        /// FIXED: Update formation for a specific squad with proper NavigationComponent sync
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
            
            // FIXED: Update each member's formation data and apply immediately to NavigationComponent
            for (int i = 0; i < members.Count && i < formationTemplate.Length; i++)
            {
                var entity = members[i];
                var formationComponent = entity.GetComponent<FormationComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                
                if (formationComponent == null || navigationComponent == null || transformComponent == null) 
                    continue;
                
                // Calculate world position for this slot
                Vector3 localOffset = formationTemplate[i];
                Vector3 worldOffset = squadRotation * localOffset;
                Vector3 targetPosition = squadCenter + worldOffset;
                
                // FIXED: Update FormationComponent data
                formationComponent.SetFormationPositionData(
                    localOffset, 
                    targetPosition, 
                    squadCenter, 
                    squadRotation
                );
                
                // Set formation slot index if not already set
                if (formationComponent.FormationSlotIndex != i)
                {
                    formationComponent.SetFormationSlot(i);
                }
                
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
                
                // FIXED: CRITICAL - Apply formation position to NavigationComponent
                // This is the key fix that ensures units actually move to formation positions
                if (_forceFormationPositioning || !isInPosition)
                {
                    NavigationCommandPriority priority = (i == 0) ? 
                        NavigationCommandPriority.High : NavigationCommandPriority.Normal;
                    
                    navigationComponent.SetFormationInfo(
                        squadCenter, 
                        localOffset, 
                        priority
                    );
                    
                    if (_enableDetailedLogging)
                    {
                        Debug.Log($"FormationSystem: Applied formation position to unit {entity.Id} " +
                                 $"at slot {i} with offset {localOffset} and target {targetPosition}");
                    }
                }
            }
        }

        #endregion

        #region Formation Template Generation

        /// <summary>
        /// FIXED: Get formation template with proper caching and generation
        /// </summary>
        private Vector3[] GetFormationTemplate(FormationType formationType, int unitCount, FormationSpacingConfig spacingConfig)
        {
            if (unitCount <= 0) return new Vector3[0];
            
            // Generate cache key including spacing to avoid conflicts
            float spacing = GetSpacingForFormation(formationType, spacingConfig);
            string cacheKey = $"{formationType}_{unitCount}_{spacing:F2}";
            
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
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"FormationSystem: Cached new formation template {cacheKey}");
                }
            }
            
            return template;
        }

        /// <summary>
        /// FIXED: Generate formation template with leader at center for Normal formation
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int unitCount, FormationSpacingConfig spacingConfig)
        {
            Vector3[] positions = new Vector3[unitCount];
            
            // Get spacing - use config if available, otherwise use defaults
            float spacing = GetSpacingForFormation(formationType, spacingConfig);
            
            switch (formationType)
            {
                case FormationType.Normal:
                    GenerateNormalFormationWithLeaderAtCenter(positions, spacing);
                    break;
                    
                case FormationType.Phalanx:
                    GeneratePhalanxFormation(positions, spacing);
                    break;
                    
                case FormationType.Testudo:
                    GenerateTestudoFormation(positions, spacing);
                    break;
                    
                default:
                    Debug.LogWarning($"FormationSystem: Unknown formation type {formationType}, using Normal formation");
                    GenerateNormalFormationWithLeaderAtCenter(positions, spacing);
                    break;
            }
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"FormationSystem: Generated {formationType} formation for {unitCount} units with {spacing:F2} spacing");
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
        /// FIXED: Generate Normal formation (3x3 grid) with leader at center (index 4)
        /// Positions:  0 1 2
        ///            3 L 5  (L = Leader at center)
        ///            6 7 8
        /// </summary>
        private void GenerateNormalFormationWithLeaderAtCenter(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            const int gridWidth = 3;
            
            // FIXED: Position mapping to ensure leader (first unit) gets center position
            int[] positionMapping = new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }; // Default mapping
            
            if (count > 0)
            {
                // Leader (index 0) goes to center position (grid slot 4)
                // Other units fill remaining positions
                positionMapping = new int[9] { 4, 0, 1, 2, 3, 5, 6, 7, 8 };
            }
            
            for (int i = 0; i < count; i++)
            {
                int gridSlot = (i < 9) ? positionMapping[i] : (i % 9);
                
                int row = gridSlot / gridWidth;
                int col = gridSlot % gridWidth;
                
                // Center the grid around origin (0,0,0)
                float x = (col - 1) * spacing;  // -1, 0, 1 for columns 0, 1, 2
                float z = (row - 1) * spacing;  // -1, 0, 1 for rows 0, 1, 2
                
                positions[i] = new Vector3(x, 0, z);
                
                if (_enableDetailedLogging && i == 0)
                {
                    Debug.Log($"FormationSystem: Leader placed at center position {positions[i]} (grid slot {gridSlot})");
                }
            }
        }

        /// <summary>
        /// Generate Phalanx formation (tight combat grid)
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
        /// </summary>
        private void GenerateTestudoFormation(Vector3[] positions, float spacing)
        {
            // Use same grid logic as Phalanx but with the tighter spacing
            GeneratePhalanxFormation(positions, spacing);
        }

        #endregion

        #region Formation Role Assignment

        /// <summary>
        /// Determine formation role based on position and formation type
        /// </summary>
        private FormationRole DetermineFormationRole(int slotIndex, int totalUnits, FormationType formationType)
        {
            // Leader is always slot 0
            if (slotIndex == 0) return FormationRole.Leader;
            
            switch (formationType)
            {
                case FormationType.Normal:
                    // For 3x3 grid: front row (slots 0,1,2), middle row (3,4,5), back row (6,7,8)
                    // Since leader is at center (4), adjust mapping
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
        /// FIXED: Force update formation for specific squad with immediate application
        /// </summary>
        public void UpdateSquadFormation(int squadId, FormationType newFormationType)
        {
            if (squadId < 0) return;
            
            _squadFormationTypes[squadId] = newFormationType;
            
            // FIXED: Update all components in this squad immediately
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
                
                // FIXED: Force immediate formation template recalculation and application
                if (_squadCenters.TryGetValue(squadId, out Vector3 squadCenter) &&
                    _squadRotations.TryGetValue(squadId, out Quaternion squadRotation))
                {
                    FormationSpacingConfig spacingConfig = null;
                    _squadSpacingConfigs.TryGetValue(squadId, out spacingConfig);
                    
                    UpdateSquadFormation(squadId, members, squadCenter, squadRotation, newFormationType, spacingConfig);
                }
                
                Debug.Log($"FormationSystem: FIXED - Updated squad {squadId} to formation {newFormationType} " +
                         $"with {members.Count} units and immediate application");
            }
            
            // Clear relevant cache entries
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
            
            if (keysToRemove.Count > 0)
            {
                Debug.Log($"FormationSystem: Cleared {keysToRemove.Count} cache entries for {formationType}");
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
        /// FIXED: Enhanced debug visualization showing formation structure
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
                
                // Draw squad center with formation type indicator
                Debug.DrawLine(center, center + Vector3.up * 3f, formationColor, 0.1f);
                Debug.DrawLine(center + Vector3.up * 2.5f, center + Vector3.up * 3f, Color.yellow, 0.1f);
                
                // FIXED: Draw formation grid and connections
                for (int i = 0; i < members.Count; i++)
                {
                    var entity = members[i];
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    
                    if (transformComponent != null && formationComponent != null)
                    {
                        Vector3 unitPos = transformComponent.Position + Vector3.up * 0.5f;
                        Vector3 targetPos = formationComponent.TargetFormationPosition + Vector3.up * 0.5f;
                        
                        // Color coding based on formation state
                        Color lineColor;
                        if (formationComponent.IsInFormationPosition)
                            lineColor = Color.green;
                        else if (formationComponent.DistanceFromFormationPosition < 1f)
                            lineColor = Color.yellow;
                        else
                            lineColor = Color.red;
                        
                        // Draw line from unit to formation target
                        Debug.DrawLine(unitPos, targetPos, lineColor, 0.1f);
                        
                        // FIXED: Draw leader indicator (first unit gets special marker)
                        if (i == 0)
                        {
                            Debug.DrawLine(unitPos, unitPos + Vector3.up * 1f, Color.cyan, 0.1f);
                            Debug.DrawLine(unitPos + Vector3.up * 0.8f, unitPos + Vector3.up * 1f, Color.white, 0.1f);
                        }
                        
                        // Draw slot number indicator
                        Debug.DrawLine(targetPos, targetPos + Vector3.up * 0.3f, formationColor, 0.1f);
                    }
                }
                
                // FIXED: Draw formation outline (3x3 grid for Normal formation)
                if (formationType == FormationType.Normal && members.Count > 1)
                {
                    DrawFormationGrid(center, 2.5f, formationColor);
                }
            }
        }

        /// <summary>
        /// FIXED: Draw 3x3 formation grid outline
        /// </summary>
        private void DrawFormationGrid(Vector3 center, float spacing, Color color)
        {
            // Draw 3x3 grid outline
            Vector3[] corners = new Vector3[4]
            {
                center + new Vector3(-spacing, 0, -spacing), // Bottom-left
                center + new Vector3(spacing, 0, -spacing),  // Bottom-right
                center + new Vector3(spacing, 0, spacing),   // Top-right
                center + new Vector3(-spacing, 0, spacing)   // Top-left
            };
            
            // Draw grid outline
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                Debug.DrawLine(corners[i] + Vector3.up * 0.1f, corners[next] + Vector3.up * 0.1f, color, 0.1f);
            }
            
            // Draw grid lines
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * spacing;
                
                // Vertical lines
                Vector3 start = center + new Vector3(offset, 0.1f, -spacing);
                Vector3 end = center + new Vector3(offset, 0.1f, spacing);
                Debug.DrawLine(start, end, color * 0.7f, 0.1f);
                
                // Horizontal lines
                start = center + new Vector3(-spacing, 0.1f, offset);
                end = center + new Vector3(spacing, 0.1f, offset);
                Debug.DrawLine(start, end, color * 0.7f, 0.1f);
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
            
            Debug.Log("FormationSystem: FIXED cleanup completed");
        }

        #endregion

        #region Debug Info

        /// <summary>
        /// FIXED: Enhanced debug info showing formation state
        /// </summary>
        public string GetDebugInfo()
        {
            string info = "=== FIXED Formation System Debug Info ===\n";
            info += $"Active Squads: {_squadMembers.Count}\n";
            info += $"Total Formation Updates: {_totalFormationUpdates}\n";
            info += $"Cache Size: {_formationTemplateCache.Count}\n";
            info += $"Last Update: {_lastUpdateTime:F2}s\n";
            info += $"Update Frequency: Every {_updateFrequency} frames\n";
            info += $"Force Formation Positioning: {_forceFormationPositioning}\n";
            
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                var formationType = _squadFormationTypes.GetValueOrDefault(squadId, FormationType.Normal);
                
                info += $"Squad {squadId}: {members.Count} units, {formationType} formation";
                
                if (_squadCenters.TryGetValue(squadId, out Vector3 center))
                {
                    info += $", Center: {center}";
                }
                
                info += "\n";
            }
            
            return info;
        }

        #endregion
    }
}