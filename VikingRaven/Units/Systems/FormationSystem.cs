using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Enhanced FormationSystem theo chuẩn ECS pattern
    /// Xử lý tất cả logic formation, FormationComponent chỉ chứa data
    /// Được inject dependency thay vì sử dụng singleton
    /// </summary>
    public class FormationSystem : BaseSystem
    {
        #region System Configuration

        [Header("System Settings")]
        [Tooltip("Priority of this system in execution order")]
        [SerializeField] private int _systemPriority = 100;
        
        [Tooltip("Update frequency (frames) - lower is more frequent")]
        [SerializeField, Range(1, 10)] private int _updateFrequency = 2;
        
        [Tooltip("Enable debug visualization")]
        [SerializeField] private bool _enableDebugVisualization = false;

        #endregion

        #region Formation Configuration

        [Header("Formation Templates")]
        [Tooltip("Spacing between units in different formations")]
        [SerializeField] private FormationSpacingConfig _spacingConfig;

        #endregion

        #region Runtime Data

        // Squad formation tracking
        private Dictionary<int, List<IEntity>> _squadMembers = new Dictionary<int, List<IEntity>>();
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        private Dictionary<int, Vector3> _squadCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _squadRotations = new Dictionary<int, Quaternion>();
        
        // Formation templates cache
        private Dictionary<string, Vector3[]> _formationTemplateCache = new Dictionary<string, Vector3[]>();
        
        // Update timing
        private int _frameCounter = 0;

        #endregion

        #region Dependencies (Injected by GameManager)

        private EntityRegistry _entityRegistry;
        
        /// <summary>
        /// Initialize system with dependencies (called by GameManager)
        /// </summary>
        public void Initialize(EntityRegistry entityRegistry)
        {
            _entityRegistry = entityRegistry;
            Priority = _systemPriority;
            
            // Initialize default spacing config if not assigned
            if (_spacingConfig == null)
            {
                _spacingConfig = CreateDefaultSpacingConfig();
            }
            
        }

        #endregion

        #region System Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            if (!_entityRegistry)
            {
                Debug.LogWarning("FormationSystem: EntityRegistry not injected, using fallback");
                _entityRegistry = EntityRegistry.Instance;
            }
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
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
        /// </summary>
        private void UpdateSquadMembership()
        {
            _squadMembers.Clear();
            var formationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
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
                _squadFormationTypes[squadId] = formationComponent.CurrentFormationType;
            }
            foreach (var squadId in _squadMembers.Keys)
            {
                _squadMembers[squadId].Sort((a, b) => {
                    var formA = a.GetComponent<FormationComponent>();
                    var formB = b.GetComponent<FormationComponent>();
                    return formA.FormationSlotIndex.CompareTo(formB.FormationSlotIndex);
                });
            }
        }

        #endregion

        #region Squad Transform Calculation

        /// <summary>
        /// Calculate center position and rotation for each squad
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
                    // Store calculated transforms
                    _squadCenters[squadId] = centerSum / validCount;
                    
                    // Calculate average forward direction
                    if (forwardSum.magnitude > 0.01f)
                    {
                        _squadRotations[squadId] = Quaternion.LookRotation(forwardSum.normalized);
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
                
                // Update formation for this squad
                UpdateSquadFormation(squadId, members, squadCenter, squadRotation, formationType);
            }
        }

        /// <summary>
        /// Update formation for a specific squad
        /// </summary>
        private void UpdateSquadFormation(int squadId, List<IEntity> members, Vector3 squadCenter, 
            Quaternion squadRotation, FormationType formationType)
        {
            // Get or generate formation template
            Vector3[] formationTemplate = GetFormationTemplate(formationType, members.Count);
            
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
                
                // Update formation state
                float distanceFromTarget = Vector3.Distance(transformComponent.Position, targetPosition);
                bool isInPosition = distanceFromTarget < 0.5f; // Tolerance threshold
                
                formationComponent.UpdateFormationState(
                    isInPosition, 
                    distanceFromTarget, 
                    false // Not transitioning in this simple implementation
                );
                
                // Set formation role based on position
                FormationRole role = DetermineFormationRole(i, members.Count, formationType);
                formationComponent.SetFormationRole(role);
            }
        }

        #endregion

        #region Formation Template Generation

        /// <summary>
        /// Get formation template for specific type and unit count
        /// </summary>
        private Vector3[] GetFormationTemplate(FormationType formationType, int unitCount)
        {
            string cacheKey = $"{formationType}_{unitCount}";
            
            // Check cache first
            if (_formationTemplateCache.TryGetValue(cacheKey, out Vector3[] cachedTemplate))
            {
                return cachedTemplate;
            }
            
            // Generate new template
            Vector3[] template = GenerateFormationTemplate(formationType, unitCount);
            _formationTemplateCache[cacheKey] = template;
            
            return template;
        }

        /// <summary>
        /// Generate formation template based on type and unit count
        /// </summary>
        private Vector3[] GenerateFormationTemplate(FormationType formationType, int unitCount)
        {
            Vector3[] positions = new Vector3[unitCount];
            float spacing = _spacingConfig.GetSpacing(formationType);
            
            switch (formationType)
            {
                case FormationType.Line:
                    GenerateLineFormation(positions, spacing);
                    break;
                    
                case FormationType.Column:
                    GenerateColumnFormation(positions, spacing);
                    break;
                    
                case FormationType.Phalanx:
                    GeneratePhalanxFormation(positions, spacing);
                    break;
                    
                case FormationType.Testudo:
                    GenerateTestudoFormation(positions, spacing);
                    break;
                    
                case FormationType.Circle:
                    GenerateCircleFormation(positions, spacing);
                    break;
                    
                case FormationType.Normal:
                    GenerateNormalFormation(positions, spacing);
                    break;
                    
                default:
                    GenerateLineFormation(positions, spacing); // Fallback
                    break;
            }
            
            return positions;
        }

        /// <summary>
        /// Generate line formation (horizontal line)
        /// </summary>
        private void GenerateLineFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * spacing;
                positions[i] = new Vector3(x, 0, 0);
            }
        }

        /// <summary>
        /// Generate column formation (vertical line)
        /// </summary>
        private void GenerateColumnFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            
            for (int i = 0; i < count; i++)
            {
                float z = (i - (count - 1) * 0.5f) * spacing;
                positions[i] = new Vector3(0, 0, z);
            }
        }

        /// <summary>
        /// Generate phalanx formation (tight grid)
        /// </summary>
        private void GeneratePhalanxFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            int width = Mathf.CeilToInt(Mathf.Sqrt(count));
            
            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;
                
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - ((count - 1) / width) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate testudo formation (very tight defensive grid)
        /// </summary>
        private void GenerateTestudoFormation(Vector3[] positions, float spacing)
        {
            // Similar to phalanx but with tighter spacing
            GeneratePhalanxFormation(positions, spacing * 0.7f);
        }

        /// <summary>
        /// Generate circle formation
        /// </summary>
        private void GenerateCircleFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            
            if (count == 1)
            {
                positions[0] = Vector3.zero;
                return;
            }
            
            float radius = count * spacing / (2 * Mathf.PI);
            radius = Mathf.Max(radius, spacing * 1.5f); // Minimum radius
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 2 * Mathf.PI) / count;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate normal formation (3x3 grid)
        /// </summary>
        private void GenerateNormalFormation(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / 3;
                int col = i % 3;
                
                float x = (col - 1) * spacing;
                float z = (row - 1) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        #endregion

        #region Formation Role Assignment

        /// <summary>
        /// Determine formation role based on position and formation type
        /// </summary>
        private FormationRole DetermineFormationRole(int slotIndex, int totalUnits, FormationType formationType)
        {
            // Leader is always slot 0
            if (slotIndex == 0)
                return FormationRole.Leader;
            
            // Role assignment based on formation type
            switch (formationType)
            {
                case FormationType.Phalanx:
                case FormationType.Testudo:
                    // Front row units are front-line
                    int width = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
                    if (slotIndex < width)
                        return FormationRole.FrontLine;
                    else
                        return FormationRole.Support;
                
                case FormationType.Circle:
                    return FormationRole.Flanker; // All circle units are flankers
                
                case FormationType.Line:
                    return FormationRole.FrontLine; // All line units are front-line
                
                case FormationType.Column:
                    if (slotIndex == 1) // Second unit in column
                        return FormationRole.FrontLine;
                    else
                        return FormationRole.Support;
                
                default:
                    return FormationRole.Follower;
            }
        }

        #endregion

        #region Public Interface (Called by GameManager/Other Systems)

        /// <summary>
        /// Force update formation for specific squad
        /// </summary>
        public void UpdateSquadFormation(int squadId, FormationType newFormationType)
        {
            if (_squadFormationTypes.ContainsKey(squadId))
            {
                _squadFormationTypes[squadId] = newFormationType;
                
                // Update all components in this squad
                if (_squadMembers.TryGetValue(squadId, out var members))
                {
                    foreach (var entity in members)
                    {
                        var formationComponent = entity.GetComponent<FormationComponent>();
                        if (formationComponent != null)
                        {
                            formationComponent.SetFormationType(newFormationType);
                        }
                    }
                }
                
                // Clear template cache for this formation
                string cachePattern = $"{newFormationType}_";
                var keysToRemove = new List<string>();
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
        }

        /// <summary>
        /// Get formation effectiveness for a squad
        /// </summary>
        public float GetSquadFormationEffectiveness(int squadId)
        {
            if (!_squadMembers.TryGetValue(squadId, out var members))
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

        #region Configuration

        /// <summary>
        /// Create default spacing configuration
        /// </summary>
        private FormationSpacingConfig CreateDefaultSpacingConfig()
        {
            var config = ScriptableObject.CreateInstance<FormationSpacingConfig>();
            config.LineSpacing = 2.0f;
            config.ColumnSpacing = 2.0f;
            config.PhalanxSpacing = 1.5f;
            config.TestudoSpacing = 1.0f;
            config.CircleSpacing = 1.8f;
            config.NormalSpacing = 2.0f;
            return config;
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draw debug visualization for formations
        /// </summary>
        private void DrawFormationDebug()
        {
            foreach (var squadData in _squadMembers)
            {
                int squadId = squadData.Key;
                var members = squadData.Value;
                
                if (!_squadCenters.TryGetValue(squadId, out Vector3 center)) continue;
                
                // Draw squad center
                Debug.DrawLine(center, center + Vector3.up * 2f, Color.yellow, 0.1f);
                
                // Draw formation connections
                for (int i = 0; i < members.Count; i++)
                {
                    var transformComponent = members[i].GetComponent<TransformComponent>();
                    var formationComponent = members[i].GetComponent<FormationComponent>();
                    
                    if (transformComponent != null && formationComponent != null)
                    {
                        Vector3 unitPos = transformComponent.Position;
                        Vector3 targetPos = formationComponent.TargetFormationPosition;
                        
                        // Draw line from unit to formation target
                        Color lineColor = formationComponent.IsInFormationPosition ? Color.green : Color.red;
                        Debug.DrawLine(unitPos, targetPos, lineColor, 0.1f);
                        
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Configuration class for formation spacing
    /// </summary>
    [System.Serializable]
    public class FormationSpacingConfig : ScriptableObject
    {
        [Header("Formation Spacing Settings")]
        public float LineSpacing = 2.0f;
        public float ColumnSpacing = 2.0f;
        public float PhalanxSpacing = 1.5f;
        public float TestudoSpacing = 1.0f;
        public float CircleSpacing = 1.8f;
        public float NormalSpacing = 2.0f;
        
        public float GetSpacing(FormationType formationType)
        {
            switch (formationType)
            {
                case FormationType.Line: return LineSpacing;
                case FormationType.Column: return ColumnSpacing;
                case FormationType.Phalanx: return PhalanxSpacing;
                case FormationType.Testudo: return TestudoSpacing;
                case FormationType.Circle: return CircleSpacing;
                case FormationType.Normal: return NormalSpacing;
                default: return LineSpacing;
            }
        }
    }
}