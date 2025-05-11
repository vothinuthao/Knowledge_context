using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Công cụ debug để kiểm tra trạng thái của EntityRegistry
    /// </summary>
    public class EntityDebugHelper : MonoBehaviour
    {
        [SerializeField] private bool _logOnStart = true;
        [SerializeField] private bool _logPeriodic = false;
        [SerializeField] private float _logInterval = 5.0f;
        
        private float _lastLogTime = 0f;
        
        private void Start()
        {
            if (_logOnStart)
            {
                DebugEntityRegistry();
            }
        }
        
        private void Update()
        {
            if (_logPeriodic && Time.time - _lastLogTime > _logInterval)
            {
                DebugEntityRegistry();
                _lastLogTime = Time.time;
            }
        }
        
        [ContextMenu("Print Entity Registry Info")]
        public void DebugEntityRegistry()
        {
            if (!EntityRegistry.HasInstance)
            {
                Debug.LogError("EntityDebugHelper: EntityRegistry instance not available");
                return;
            }
            
            var registry = EntityRegistry.Instance;
            
            Debug.Log($"EntityDebugHelper: EntityRegistry contains {registry.EntityCount} entities");
            
            var formationComponents = registry.GetEntitiesWithComponent<VikingRaven.Units.Components.FormationComponent>();
            Debug.Log($"EntityDebugHelper: Found {formationComponents.Count} entities with FormationComponent");
            
            // Group by squad
            System.Collections.Generic.Dictionary<int, int> squadCounts = new System.Collections.Generic.Dictionary<int, int>();
            
            foreach (var entity in formationComponents)
            {
                var component = entity.GetComponent<VikingRaven.Units.Components.FormationComponent>();
                if (component != null)
                {
                    int squadId = component.SquadId;
                    
                    if (!squadCounts.ContainsKey(squadId))
                    {
                        squadCounts[squadId] = 0;
                    }
                    
                    squadCounts[squadId]++;
                }
            }
            
            // Log squad info
            foreach (var squadEntry in squadCounts)
            {
                Debug.Log($"EntityDebugHelper: Squad {squadEntry.Key} has {squadEntry.Value} members");
            }
            
            // Log entity details
            foreach (var entity in formationComponents)
            {
                var formationComponent = entity.GetComponent<VikingRaven.Units.Components.FormationComponent>();
                var transformComponent = entity.GetComponent<VikingRaven.Units.Components.TransformComponent>();
                
                if (formationComponent != null && transformComponent != null)
                {
                    Debug.Log($"EntityDebugHelper: Entity {entity.Id} - Squad {formationComponent.SquadId}, " +
                             $"Slot {formationComponent.FormationSlotIndex}, " +
                             $"Offset {formationComponent.FormationOffset}, " +
                             $"Position {transformComponent.Position}");
                }
            }
        }
    }
}