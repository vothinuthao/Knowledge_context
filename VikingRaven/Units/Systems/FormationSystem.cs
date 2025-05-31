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
        #region System Configuration - Simplified

        [Header("Update Settings")]
        [Tooltip("Update frequency (frames between updates)")]
        [SerializeField, Range(1, 10)] private int _updateFrequency = 3;
        
        [Tooltip("Enable debug visualization")]
        [SerializeField] private bool _enableDebugVisualization = true;
        
        [Tooltip("Enable detailed logging")]
        [SerializeField] private bool _enableDetailedLogging = false;

        #endregion

        #region Formation Data - Simplified Structure

        private Dictionary<int, SquadFormationData> _squadFormations = new Dictionary<int, SquadFormationData>();
        
        private Dictionary<string, FormationTemplate> _formationTemplateCache = new Dictionary<string, FormationTemplate>();
        private int _frameCounter = 0;
        private int _totalUpdates = 0;
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
            
            Priority = 150;
        }

        public override void Execute()
        {
            _frameCounter++;
            if (_frameCounter % _updateFrequency != 0) return;

            if (_entityRegistry == null) return;
            
            _lastUpdateTime = Time.time;
            UpdateSquadFormations();
            
            if (_enableDebugVisualization)
            {
                DrawFormationDebug();
            }
            
            _totalUpdates++;
        }

        #endregion

        #region Core Formation Update - SIMPLIFIED

        private void UpdateSquadFormations()
        {
            // Get all formation entities
            var formationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            _squadFormations.Clear();
            
            // First pass: collect squad data
            foreach (var entity in formationEntities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (!formationComponent || !formationComponent.IsActive) continue;
                
                int squadId = formationComponent.SquadId;
                if (squadId < 0) continue;
                
                // Get or create squad formation data
                if (!_squadFormations.TryGetValue(squadId, out SquadFormationData squadData))
                {
                    squadData = new SquadFormationData
                    {
                        SquadId = squadId,
                        FormationType = formationComponent.CurrentFormationType,
                        Members = new List<IEntity>()
                    };
                    _squadFormations[squadId] = squadData;
                }
                
                squadData.Members.Add(entity);
            }
            
            // Second pass: update each squad formation
            foreach (var squadData in _squadFormations.Values)
            {
                UpdateSingleSquadFormation(squadData);
            }
        }

        /// <summary>
        /// SIMPLIFIED: Update formation for a single squad
        /// Uses existing formation indices - no recalculation
        /// </summary>
        private void UpdateSingleSquadFormation(SquadFormationData squadData)
        {
            if (squadData.Members.Count == 0) return;
            
            // Calculate current squad center and rotation
            CalculateSquadTransform(squadData);
            
            // Get formation template for this squad
            FormationTemplate template = GetFormationTemplate(
                squadData.FormationType, 
                squadData.Members.Count
            );
            
            if (template == null) return;
            
            // Update each member's target position based on their assigned index
            foreach (var entity in squadData.Members)
            {
                UpdateUnitFormationPosition(entity, squadData, template);
            }
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"FormationSystem: Updated squad {squadData.SquadId} " +
                         $"({squadData.FormationType}) with {squadData.Members.Count} units");
            }
        }

        /// <summary>
        /// SIMPLIFIED: Calculate squad center and rotation from member positions
        /// </summary>
        private void CalculateSquadTransform(SquadFormationData squadData)
        {
            Vector3 centerSum = Vector3.zero;
            Vector3 forwardSum = Vector3.zero;
            int validCount = 0;
            
            foreach (var entity in squadData.Members)
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
                squadData.Center = centerSum / validCount;
                
                if (forwardSum.magnitude > 0.01f)
                {
                    Vector3 avgForward = forwardSum.normalized;
                    squadData.Rotation = Quaternion.LookRotation(avgForward, Vector3.up);
                }
                else
                {
                    squadData.Rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// SIMPLIFIED: Update single unit formation position
        /// Uses the unit's assigned formation index - no recalculation
        /// </summary>
        private void UpdateUnitFormationPosition(IEntity entity, SquadFormationData squadData, FormationTemplate template)
        {
            var formationComponent = entity.GetComponent<FormationComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            
            if (formationComponent == null || transformComponent == null) return;
            
            // Get unit's assigned formation index
            int formationIndex = formationComponent.FormationSlotIndex;
            
            // Validate index
            if (formationIndex < 0 || formationIndex >= template.Positions.Length)
            {
                Debug.LogWarning($"FormationSystem: Invalid formation index {formationIndex} for entity {entity.Id}");
                return;
            }
            
            // Calculate target position using assigned index
            Vector3 localOffset = template.Positions[formationIndex];
            Vector3 worldOffset = squadData.Rotation * localOffset;
            Vector3 targetPosition = squadData.Center + worldOffset;
            
            // Update formation component with new position data
            formationComponent.SetFormationPositionData(
                localOffset,
                targetPosition,
                squadData.Center,
                squadData.Rotation
            );
            
            // Calculate formation state
            float distance = Vector3.Distance(transformComponent.Position, targetPosition);
            bool isInPosition = distance < GetPositionTolerance();
            
            formationComponent.UpdateFormationState(isInPosition, distance, false);
            
            // Update navigation component if present
            var navigationComponent = entity.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.SetFormationInfo(
                    squadData.Center,
                    localOffset,
                    NavigationCommandPriority.Normal
                );
            }
        }

        #endregion

        #region Formation Template Management - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Get formation template with caching
        /// Templates are cached for performance
        /// </summary>
        private FormationTemplate GetFormationTemplate(FormationType formationType, int unitCount)
        {
            string cacheKey = $"{formationType}_{unitCount}";
            
            if (_formationTemplateCache.TryGetValue(cacheKey, out FormationTemplate cachedTemplate))
            {
                return cachedTemplate;
            }
            
            // Generate new template
            FormationTemplate newTemplate = GenerateFormationTemplate(formationType, unitCount);
            if (newTemplate != null)
            {
                _formationTemplateCache[cacheKey] = newTemplate;
            }
            
            return newTemplate;
        }

        /// <summary>
        /// SIMPLIFIED: Generate formation template for specific type and count
        /// Only handles 3 formation types
        /// </summary>
        private FormationTemplate GenerateFormationTemplate(FormationType formationType, int unitCount)
        {
            if (unitCount <= 0) return null;
            
            Vector3[] positions = new Vector3[unitCount];
            float spacing = GetDefaultSpacing(formationType);
            
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
                    GenerateNormalFormation(positions, spacing);
                    break;
            }
            
            return new FormationTemplate
            {
                FormationType = formationType,
                UnitCount = unitCount,
                Positions = positions,
                Spacing = spacing
            };
        }

        /// <summary>
        /// Generate Normal formation (3x3 grid)
        /// </summary>
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

        /// <summary>
        /// Generate Phalanx formation (combat grid)
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
                float z = (row - (Mathf.CeilToInt((float)count / width) - 1) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate Testudo formation (tight defensive)
        /// </summary>
        private void GenerateTestudoFormation(Vector3[] positions, float spacing)
        {
            GeneratePhalanxFormation(positions, spacing);
        }

        #endregion

        #region Public Interface - SIMPLIFIED

        /// <summary>
        /// SIMPLIFIED: Force update formation for specific squad
        /// Clear interface for external systems
        /// </summary>
        public void UpdateSquadFormation(int squadId, FormationType newFormationType)
        {
            if (squadId < 0) return;
            
            var formationEntities = _entityRegistry.GetEntitiesWithComponent<FormationComponent>();
            bool foundSquad = false;
            
            foreach (var entity in formationEntities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    formationComponent.SetFormationType(newFormationType, true);
                    foundSquad = true;
                }
            }
            
            if (foundSquad)
            {
                // Clear cache for this formation type
                ClearFormationCache(newFormationType);
                
                Debug.Log($"FormationSystem: Updated squad {squadId} to formation {newFormationType}");
            }
        }

        /// <summary>
        /// Get formation effectiveness for a squad
        /// </summary>
        public float GetSquadFormationEffectiveness(int squadId)
        {
            if (!_squadFormations.TryGetValue(squadId, out SquadFormationData squadData))
                return 0f;
            
            float totalEffectiveness = 0f;
            int validCount = 0;
            
            foreach (var entity in squadData.Members)
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

        #region Helper Methods

        private float GetDefaultSpacing(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => 2.5f,
                FormationType.Phalanx => 1.8f,
                FormationType.Testudo => 1.2f,
                _ => 2.0f
            };
        }

        private float GetPositionTolerance()
        {
            return 0.3f;
        }

        private void ClearFormationCache(FormationType formationType)
        {
            var keysToRemove = new List<string>();
            string pattern = $"{formationType}_";
            
            foreach (var key in _formationTemplateCache.Keys)
            {
                if (key.StartsWith(pattern))
                    keysToRemove.Add(key);
            }
            
            foreach (var key in keysToRemove)
            {
                _formationTemplateCache.Remove(key);
            }
        }

        #endregion

        #region Debug Visualization - SIMPLIFIED

        private void DrawFormationDebug()
        {
            foreach (var squadData in _squadFormations.Values)
            {
                if (squadData.Members.Count == 0) continue;
                
                Color formationColor = GetFormationColor(squadData.FormationType);
                Debug.DrawLine(squadData.Center, squadData.Center + Vector3.up * 2f, formationColor, 0.1f);
                foreach (var entity in squadData.Members)
                {
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    var formationComponent = entity.GetComponent<FormationComponent>();
                    
                    if (transformComponent && formationComponent != null)
                    {
                        Vector3 unitPos = transformComponent.Position + Vector3.up * 0.5f;
                        Vector3 targetPos = formationComponent.TargetFormationPosition + Vector3.up * 0.5f;
                        
                        Color lineColor = formationComponent.IsInFormationPosition ? Color.green : Color.yellow;
                        Debug.DrawLine(unitPos, targetPos, lineColor, 0.1f);
                    }
                }
            }
        }

        private Color GetFormationColor(FormationType formationType)
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
            
            _squadFormations.Clear();
            _formationTemplateCache.Clear();
            
            Debug.Log("FormationSystem: Cleanup completed");
        }

        #endregion

        #region Debug Info

        public string GetDebugInfo()
        {
            string info = "=== Formation System Debug Info ===\n";
            info += $"Active Squads: {_squadFormations.Count}\n";
            info += $"Total Updates: {_totalUpdates}\n";
            info += $"Cache Size: {_formationTemplateCache.Count}\n";
            info += $"Update Frequency: Every {_updateFrequency} frames\n";
            
            foreach (var squadData in _squadFormations.Values)
            {
                info += $"Squad {squadData.SquadId}: {squadData.Members.Count} units, {squadData.FormationType}\n";
            }
            
            return info;
        }

        #endregion
    }

    #region Supporting Data Structures - SIMPLIFIED

    /// <summary>
    /// SIMPLIFIED: Squad formation data container
    /// Only stores essential data for formation updates
    /// </summary>
    public class SquadFormationData
    {
        public int SquadId;
        public FormationType FormationType;
        public List<IEntity> Members = new List<IEntity>();
        public Vector3 Center;
        public Quaternion Rotation;
    }

    /// <summary>
    /// SIMPLIFIED: Formation template for caching
    /// Contains precomputed formation positions
    /// </summary>
    public class FormationTemplate
    {
        public FormationType FormationType;
        public int UnitCount;
        public Vector3[] Positions;
        public float Spacing;
    }

    #endregion
}