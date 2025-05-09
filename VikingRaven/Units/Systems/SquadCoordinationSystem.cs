using System.Collections.Generic;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class SquadCoordinationSystem : BaseSystem
    {
        private Dictionary<int, FormationType> _squadFormationTypes = new Dictionary<int, FormationType>();
        
        public override void Execute()
        {
            // Get all entities with formation components
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            
            // Update formation types for entities in each squad
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null)
                {
                    int squadId = formationComponent.SquadId;
                    
                    // If a new formation type has been set for this squad, update the entity
                    if (_squadFormationTypes.TryGetValue(squadId, out FormationType formationType) &&
                        formationComponent.CurrentFormationType != formationType)
                    {
                        formationComponent.SetFormationType(formationType);
                    }
                }
            }
        }

        public void SetSquadFormation(int squadId, FormationType formationType)
        {
            _squadFormationTypes[squadId] = formationType;
        }
    }
}